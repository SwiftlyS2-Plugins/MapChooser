using MapChooser.Contract;
using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using MapChooser.Menu;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Players;
using System.Threading.Tasks;

namespace MapChooser.Helpers;

public class EndOfMapVoteManager
{
    private readonly ISwiftlyCore _core;
    private readonly PluginState _state;
    private readonly VoteManager _voteManager;
    private readonly MapLister _mapLister;
    private readonly MapCooldown _mapCooldown;
    private readonly ChangeMapManager _changeMapManager;
    private readonly ExtendManager _extendManager;
    private readonly MapChooserConfig _config;

    private readonly Dictionary<string, int> _votes = new();
    private readonly Dictionary<int, string> _playerVotes = new();
    private List<string> _mapsInVote = new();
    private bool _voteActive = false;
    private bool _changeImmediately = false;
    private DateTime _voteEndTime;
    private readonly HashSet<int> _playersReceivedMenu = new();
    private int _voteSessionId = 0;
    private readonly Dictionary<int, IMenu> _activeVoteMenus = new();
    private IReadOnlyList<string> _ballot = Array.Empty<string>();
    private bool _voteScheduled;
    private int _scheduleId;

    // MapChooserApi forwards both to other plugins. Raised on the game thread once MapChooser's own
    // work for the change is done, so a subscriber always reads the new state.
    public event Action? VoteChanged;
    public event Action<int, string>? PlayerVoted;

    /// <summary>The hub a closable ballot opens under; assigned after construction, since the hub's Vote entry needs this manager.</summary>
    public MapChooserMenus? Menus { get; set; }

    public bool IsVoteActive => _voteActive;

    /// <summary>A vote an admin started that is still counting down to open.</summary>
    public bool IsVoteScheduled => _voteScheduled;

    /// <summary>Seconds until the running vote closes, rounded up; 0 when none is running.</summary>
    public int SecondsLeft => _voteActive ? (int)Math.Max(0, Math.Ceiling((_voteEndTime - DateTime.Now).TotalSeconds)) : 0;

    /// <summary>Who gets a ballot: valid humans, spectators only when <c>AllowSpectatorsToVote</c> is on.</summary>
    public bool CanVote(IPlayer player)
        => player.IsValid && !player.IsFakeClient && (_config.AllowSpectatorsToVote || player.Controller?.TeamNum != 1);

    public int EligibleVoters => _voteActive ? _core.PlayerManager.GetAllPlayers().Count(CanVote) : 0;

    // _mapsInVote outlives its vote (only ResetVote clears it), hence the _voteActive gate.
    public IReadOnlyList<string> VoteOptions => _voteActive ? _ballot : Array.Empty<string>();

    public int GetVoteCount(string option)
        => _voteActive && option is not null ? _votes.GetValueOrDefault(option) : 0;

    public int TotalVotes
    {
        get
        {
            if (!_voteActive) return 0;
            int total = 0;
            foreach (var count in _votes.Values)
                total += count;
            return total;
        }
    }

    public string? GetPlayerVote(int playerSlot)
        => _voteActive && _playerVotes.TryGetValue(playerSlot, out var option) ? option : null;

    public EndOfMapVoteManager(ISwiftlyCore core, PluginState state, VoteManager voteManager, MapLister mapLister, MapCooldown mapCooldown, ChangeMapManager changeMapManager, ExtendManager extendManager, MapChooserConfig config)
    {
        _core = core;
        _state = state;
        _voteManager = voteManager;
        _mapLister = mapLister;
        _mapCooldown = mapCooldown;
        _changeMapManager = changeMapManager;
        _extendManager = extendManager;
        _config = config;
    }

    private bool IsMapInCooldownForVote(string mapName)
    {
        if (string.IsNullOrWhiteSpace(mapName)) return false;
        if (mapName == IMapChooserApi.ExtendOption) return false;

        var map = _mapLister.Maps.FirstOrDefault(m => m.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase));
        if (map != null) return _mapCooldown.IsMapInCooldown(map);

        return _mapCooldown.IsMapInCooldown(mapName);
    }

    private bool _isRtvVote = false;

    public bool HasPlayerVoted(int playerSlot)
    {
        return _playerVotes.ContainsKey(playerSlot);
    }

    public void ResetVote()
    {
        bool wasActive = _voteActive;
        _voteSessionId++;
        _voteActive = false;
        _voteScheduled = false;
        _scheduleId++;
        _state.EofVoteHappening = false;
        _state.EofVoteCompleted = false;
        _isRtvVote = false;

        foreach (var player in _core.PlayerManager.GetAllPlayers().Where(p => p.IsValid))
        {
            var session = _core.Menu.GetSession(player);
            if (session?.Menu.Tag?.ToString() == "EofVoteMenu")
            {
                session.Dismiss();
            }
        }

        _votes.Clear();
        _playerVotes.Clear();
        _playersReceivedMenu.Clear();
        _mapsInVote.Clear();
        _activeVoteMenus.Clear();

        if (wasActive)
            VoteChanged?.Invoke();
    }

    public void StartVote(int voteDuration, int mapsToShow, bool changeImmediately = false, bool isRtv = false)
    {
        if (_voteActive) return;

        _voteSessionId++;

        _isRtvVote = isRtv;
        _voteActive = true;
        _changeImmediately = changeImmediately;
        _state.EofVoteHappening = true;
        _votes.Clear();
        _playerVotes.Clear();
        _playersReceivedMenu.Clear();

        var currentMapId = _core.Engine.GlobalVars.MapName.ToString();
        var currentWorkshopId = _core.Engine.WorkshopId;

        var playerCount = _core.PlayerManager.GetAllPlayers()
            .Count(p => p.IsValid && !p.IsFakeClient);

        var allMaps = _mapLister.Maps
            .Where(m => m.IsValidForPlayerCount(playerCount))
            .ToList();
        
        string? currentMapDisplayName = null;
        if (!string.IsNullOrEmpty(currentMapId) || !string.IsNullOrEmpty(currentWorkshopId))
        {
            currentMapDisplayName = allMaps.FirstOrDefault(m => 
                (!string.IsNullOrEmpty(currentMapId) && string.Equals(m.Id, currentMapId, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(currentWorkshopId) && string.Equals(m.Id, currentWorkshopId, StringComparison.OrdinalIgnoreCase))
            )?.Name;
        }
        
        if (!string.IsNullOrEmpty(currentMapId) || !string.IsNullOrEmpty(currentWorkshopId))
        {
            allMaps = allMaps.Where(m => {
                var matchesMapId = !string.IsNullOrEmpty(currentMapId) && string.Equals(m.Id, currentMapId, StringComparison.OrdinalIgnoreCase);
                var matchesWorkshopId = !string.IsNullOrEmpty(currentWorkshopId) && string.Equals(m.Id, currentWorkshopId, StringComparison.OrdinalIgnoreCase);
                
                return !(matchesMapId || matchesWorkshopId);
            }).ToList();
        }
        
        var nominations = _state.Nominations.Values
            .Distinct()
            .Where(n => !IsMapInCooldownForVote(n))
            .Where(n => {
                var map = _mapLister.Maps.FirstOrDefault(m => m.Name.Equals(n, StringComparison.OrdinalIgnoreCase));
                return map == null || map.IsValidForPlayerCount(playerCount);
            })
            .ToList();
        
        if (!string.IsNullOrEmpty(currentMapDisplayName))
        {
            nominations = nominations.Where(n => !n.Equals(currentMapDisplayName, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        var random = new Random();

        _mapsInVote = new List<string>();

        if (nominations.Count >= mapsToShow)
        {
            _mapsInVote.AddRange(nominations.OrderBy(x => random.Next()).Take(mapsToShow));
        }
        else
        {
            _mapsInVote.AddRange(nominations);
            var remainingSlots = mapsToShow - _mapsInVote.Count;
            var candidateMaps = allMaps.Where(m => !_mapsInVote.Contains(m.Name) && !_mapCooldown.IsMapInCooldown(m)).ToList();
            _mapsInVote.AddRange(candidateMaps.Select(m => m.Name).OrderBy(x => random.Next()).Take(remainingSlots));
        }

        if (_mapsInVote.Count < mapsToShow)
        {
            var remainingSlots = mapsToShow - _mapsInVote.Count;
            var candidateMaps = allMaps
                .Where(m => !_mapsInVote.Contains(m.Name) && !_mapCooldown.IsMapInCooldown(m))
                .Select(m => m.Name)
                .ToList();

            _mapsInVote.AddRange(candidateMaps.OrderBy(x => random.Next()).Take(remainingSlots));
        }

        if (_config.EndOfMap.AllowExtend && _state.ExtendsLeft > 0 && !_isRtvVote)
        {
            _mapsInVote.Add(IMapChooserApi.ExtendOption);
        }

        _mapsInVote = _mapsInVote.Where(m => !IsMapInCooldownForVote(m)).ToList();
        _ballot = _mapsInVote.AsReadOnly();


        foreach (var map in _mapsInVote)
            _votes[map] = 0;

        _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.vote.started"]);

        _voteEndTime = DateTime.Now.AddSeconds(voteDuration);

        RefreshVoteMenu(true);
        _core.Scheduler.DelayBySeconds(1, () => RunVoteTimer(_voteSessionId));
        VoteChanged?.Invoke();
    }

    /// <summary>Opens a vote on these maps after a countdown in chat; false, scheduling nothing, while a vote is running or counting down.</summary>
    public bool ScheduleCustomVote(List<string> maps, int countdownSeconds, int voteDuration)
    {
        if (_voteActive || _voteScheduled) return false;

        _voteScheduled = true;
        CountDown(++_scheduleId, countdownSeconds, maps, voteDuration);
        return true;
    }

    private void CountDown(int scheduleId, int seconds, List<string> maps, int voteDuration)
    {
        if (scheduleId != _scheduleId || !_voteScheduled) return;

        // The end of map's vote or an rtv opened first: that one stands.
        if (_voteActive || seconds <= 0)
        {
            _voteScheduled = false;
            if (!_voteActive) StartCustomVote(maps, voteDuration);
            return;
        }

        _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.admin_vote.starting", seconds]);
        _core.Scheduler.DelayBySeconds(1, () => CountDown(scheduleId, seconds - 1, maps, voteDuration));
    }

    public void StartCustomVote(List<string> maps, int voteDuration, bool changeImmediately = false)
    {
        if (_voteActive) return;

        _voteSessionId++;

        _isRtvVote = false;
        _voteActive = true;
        _changeImmediately = changeImmediately;
        _state.EofVoteHappening = true;
        _votes.Clear();
        _playerVotes.Clear();
        _playersReceivedMenu.Clear();

        _mapsInVote = maps.Where(m => !IsMapInCooldownForVote(m)).ToList();
        _ballot = _mapsInVote.AsReadOnly();

        foreach (var map in _mapsInVote)
            _votes[map] = 0;

        _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.vote.started"]);

        _voteEndTime = DateTime.Now.AddSeconds(voteDuration);

        RefreshVoteMenu(true);
        _core.Scheduler.DelayBySeconds(1, () => RunVoteTimer(_voteSessionId));
        VoteChanged?.Invoke();
    }

    private void RunVoteTimer(int sessionId)
    {
        if (!_voteActive) return;
        if (sessionId != _voteSessionId) return;

        if (SecondsLeft <= 0)
        {
            EndVote(sessionId);
            return;
        }

        RefreshVoteMenu();
        _core.Scheduler.DelayBySeconds(1, () => RunVoteTimer(sessionId));
    }

    private void RefreshVoteMenu(bool forceOpen = false)
    {
        if (!_voteActive) return;

        foreach (var player in _core.PlayerManager.GetAllPlayers().Where(p => p.IsValid))
        {
            var currentSession = _core.Menu.GetSession(player);
            bool hasEofMenuOpen = currentSession?.Menu.Tag?.ToString() == "EofVoteMenu";

            if (!CanVote(player))
            {
                if (hasEofMenuOpen)
                {
                    currentSession!.Dismiss();
                    _activeVoteMenus.Remove(player.Slot);
                }
                continue;
            }

            // Voters are not rebuilt each second: that reset the cursor of a menu reopened with !revote.
            if (_playerVotes.ContainsKey(player.Slot))
                continue;

            if (forceOpen || !_playersReceivedMenu.Contains(player.Slot))
            {
                OpenVoteMenu(player);
            }
        }
    }

    /// <summary>The ballot for one player, not yet open; null when no vote is running or they may not vote.</summary>
    public IMenu? BuildBallot(IPlayer player)
    {
        if (!_voteActive || !CanVote(player)) return null;

        _playersReceivedMenu.Add(player.Slot);
        var menu = new EndOfMapVoteMenu(_core, _mapCooldown).Build(player, _mapsInVote, RegisterVote, _config.DisableVoteMenuExit);
        _activeVoteMenus[player.Slot] = menu;
        return menu;
    }

    public void OpenVoteMenu(IPlayer player)
    {
        if (BuildBallot(player) is not { } menu) return;

        // The locked ballot gets no parent: the hub would be a way out of having to vote.
        if (_config.DisableVoteMenuExit || Menus is null)
            menu.Open(player);
        else
            Menus.Open(player, menu);
    }

    private void RegisterVote(IPlayer player, string map)
    {
        if (!_voteActive) return;
        if (!CanVote(player)) return;
        // The click is applied a tick late, so it can name an option the current ballot lacks.
        if (!_votes.ContainsKey(map)) return;

        int slot = player.Slot;
        if (_playerVotes.ContainsKey(slot))
        {
            _votes[_playerVotes[slot]]--;
        }

        _playerVotes[slot] = map;
        _votes[map]++;

        var localizer = _core.Translation.GetPlayerLocalizer(player);
        string displayName = map == IMapChooserApi.ExtendOption ? localizer["map_chooser.extend_option"] : map;

        if (_config.AnnounceVotes)
        {
            int mapVoteCount = _votes.GetValueOrDefault(map, 0);
            _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.vote.player_voted", player.Controller?.PlayerName ?? "Unknown", displayName, mapVoteCount]);
        }
        else
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.vote.you_voted", displayName]);
        }
        
        var currentSession = _core.Menu.GetSession(player);
        if (currentSession?.Menu.Tag?.ToString() == "EofVoteMenu")
        {
            currentSession.Dismiss();
            _activeVoteMenus.Remove(slot);
        }

        // After the close, so anything a subscriber opens for the voter is not closed with it.
        VoteChanged?.Invoke();
        PlayerVoted?.Invoke(slot, map);
    }
    
    public void RemoveMapVote(IPlayer player)
    {
        if (!_voteActive) return;
        
        int slot = player.Slot;
        if (_playerVotes.ContainsKey(slot))
        {
            string map = _playerVotes[slot];
            _votes[map]--;
            _playerVotes.Remove(slot);
            VoteChanged?.Invoke();
        }
    }

    public void CancelVote()
    {
        if (!_voteActive) return;
        _voteActive = false;
        _state.EofVoteHappening = false;
        _isRtvVote = false;

        foreach (var player in _core.PlayerManager.GetAllPlayers().Where(p => p.IsValid))
        {
            var session = _core.Menu.GetSession(player);
            if (session?.Menu.Tag?.ToString() == "EofVoteMenu")
                session.Dismiss();
        }

        _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.rtv.vote_cancelled"]);
        _votes.Clear();
        _playerVotes.Clear();
        _playersReceivedMenu.Clear();
        _activeVoteMenus.Clear();
        VoteChanged?.Invoke();
    }
    
    public void ForceEnd()
    {
        if (_voteActive)
            EndVote(_voteSessionId);
    }

    private void EndVote(int sessionId)
    {
        if (!_voteActive) return;
        if (sessionId != _voteSessionId) return;

        try
        {
            _state.EofVoteCompleted = true;

            if (_isRtvVote)
            {
                _voteManager.Clear();
            }

            if (_isRtvVote && _playerVotes.Count == 0)
            {
                _state.EofVoteCompleted = false;
                _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.rtv.vote_failed_no_votes"]);
                _state.RtvCooldownEndTime = DateTime.Now.AddSeconds(_config.Rtv.VoteCooldownTime);
                return;
            }

            if (_votes.Count == 0) return;

            string winner = _votes.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            if (string.IsNullOrEmpty(winner))
            {
                winner = _mapsInVote.OrderBy(_ => Guid.NewGuid()).FirstOrDefault() ?? "";
            }

            if (string.IsNullOrEmpty(winner)) return;

            if (winner == IMapChooserApi.ExtendOption)
            {
                _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.extend.vote_passed", _votes.GetValueOrDefault(winner, 0)]);
                _extendManager.ExtendMap(_config.EndOfMap.ExtendTimeStep, _config.EndOfMap.ExtendRoundStep);
                _state.EofVoteCompleted = false;
            }
            else
            {
                _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.vote.ended", winner, _votes.GetValueOrDefault(winner, 0)]);
                
                bool changeImmediately = _changeImmediately || _state.MatchEnded;
                _changeMapManager.ScheduleMapChange(winner, changeImmediately, _isRtvVote);
            }
        }
        finally
        {
            _voteActive = false;
            _state.EofVoteHappening = false;
            _isRtvVote = false;

            foreach (var player in _core.PlayerManager.GetAllPlayers().Where(p => p.IsValid))
            {
                var session = _core.Menu.GetSession(player);
                if (session?.Menu.Tag?.ToString() == "EofVoteMenu")
                    session.Dismiss();
            }
            
            _votes.Clear();
            _playerVotes.Clear();
            _playersReceivedMenu.Clear();
            _activeVoteMenus.Clear();
            VoteChanged?.Invoke();
        }
    }
}
