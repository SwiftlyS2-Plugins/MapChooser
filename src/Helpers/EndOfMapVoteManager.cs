using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using MapChooser.Menu;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
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

    private Dictionary<string, int> _votes = new();
    private Dictionary<int, string> _playerVotes = new();
    private List<string> _mapsInVote = new();
    private bool _voteActive = false;
    private bool _changeImmediately = false;
    private DateTime _voteEndTime;
    private readonly HashSet<int> _playersReceivedMenu = new();
    private int _voteSessionId = 0;
    private readonly Dictionary<int, IMenuAPI> _activeVoteMenus = new();

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
        if (mapName == "map_chooser.extend_option") return false;

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
        _voteSessionId++;
        _voteActive = false;
        _state.EofVoteHappening = false;
        _state.EofVoteCompleted = false;
        _isRtvVote = false;

        foreach (var player in _core.PlayerManager.GetAllPlayers().Where(p => p.IsValid))
        {
            var menu = _core.MenusAPI.GetCurrentMenu(player);
            if (menu?.Tag?.ToString() == "EofVoteMenu")
            {
                _core.MenusAPI.CloseMenuForPlayer(player, menu);
            }
        }

        _votes.Clear();
        _playerVotes.Clear();
        _playersReceivedMenu.Clear();
        _mapsInVote.Clear();
        _activeVoteMenus.Clear();
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
            _mapsInVote.Add("map_chooser.extend_option");
        }

        _mapsInVote = _mapsInVote.Where(m => !IsMapInCooldownForVote(m)).ToList();


        foreach (var map in _mapsInVote)
            _votes[map] = 0;

        _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.vote.started"]);

        _voteEndTime = DateTime.Now.AddSeconds(voteDuration);

        RefreshVoteMenu(true);
        _core.Scheduler.DelayBySeconds(1, () => RunVoteTimer(_voteSessionId));
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

        foreach (var map in _mapsInVote)
            _votes[map] = 0;

        _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.vote.started"]);

        _voteEndTime = DateTime.Now.AddSeconds(voteDuration);

        RefreshVoteMenu(true);
        _core.Scheduler.DelayBySeconds(1, () => RunVoteTimer(_voteSessionId));
    }

    private void RunVoteTimer(int sessionId)
    {
        if (!_voteActive) return;
        if (sessionId != _voteSessionId) return;

        int timeRemaining = (int)Math.Max(0, Math.Ceiling((_voteEndTime - DateTime.Now).TotalSeconds));
        
        if (timeRemaining <= 0)
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

        int timeRemaining = (int)Math.Max(0, Math.Ceiling((_voteEndTime - DateTime.Now).TotalSeconds));
        foreach (var player in _core.PlayerManager.GetAllPlayers().Where(p => p.IsValid))
        {
            var currentMenu = _core.MenusAPI.GetCurrentMenu(player);
            bool hasEofMenuOpen = currentMenu?.Tag?.ToString() == "EofVoteMenu";

            if (!_config.AllowSpectatorsToVote && player.Controller?.TeamNum == 1)
            {
                if (hasEofMenuOpen)
                {
                    _core.MenusAPI.CloseMenuForPlayer(player, currentMenu!);
                    _activeVoteMenus.Remove(player.Slot);
                }
                continue;
            }

            if (_playerVotes.ContainsKey(player.Slot))
            {
                if (hasEofMenuOpen)
                    OpenVoteMenu(player, timeRemaining);
                continue;
            }

            if (forceOpen || !_playersReceivedMenu.Contains(player.Slot))
            {
                OpenVoteMenu(player, timeRemaining);
                _playersReceivedMenu.Add(player.Slot);
            }
        }
    }

    private void RefreshVoteMenuTitle()
    {
        if (!_voteActive) return;

        int timeRemaining = (int)Math.Max(0, Math.Ceiling((_voteEndTime - DateTime.Now).TotalSeconds));
        foreach (var player in _core.PlayerManager.GetAllPlayers().Where(p => p.IsValid))
        {
            var currentMenu = _core.MenusAPI.GetCurrentMenu(player);
            bool hasEofMenuOpen = currentMenu?.Tag?.ToString() == "EofVoteMenu";

            if (!_config.AllowSpectatorsToVote && player.Controller?.TeamNum == 1)
            {
                if (hasEofMenuOpen)
                {
                    _core.MenusAPI.CloseMenuForPlayer(player, currentMenu!);
                    _activeVoteMenus.Remove(player.Slot);
                }
                continue;
            }

            if (!_playersReceivedMenu.Contains(player.Slot))
            {
                OpenVoteMenu(player, timeRemaining);
                _playersReceivedMenu.Add(player.Slot);
            }
        }
    }
    
    public void OpenVoteMenu(IPlayer player)
    {
        if (!_voteActive) return;
        if (!_config.AllowSpectatorsToVote && player.Controller?.TeamNum == 1) return;
        _playersReceivedMenu.Add(player.Slot);
        OpenVoteMenu(player, 0);
    }

    public void OpenVoteMenu(IPlayer player, int timeRemaining)
    {
        if (!_voteActive) return;
        var menu = new EndOfMapVoteMenu(_core, _mapCooldown);
        var builtMenu = menu.Show(player, _mapsInVote, RegisterVote, _config.DisableVoteMenuExit);
        _activeVoteMenus[player.Slot] = builtMenu;
    }

    private void RegisterVote(IPlayer player, string map)
    {
        if (!_voteActive) return;
        if (!_config.AllowSpectatorsToVote && player.Controller?.TeamNum == 1) return;

        int slot = player.Slot;
        if (_playerVotes.ContainsKey(slot))
        {
            _votes[_playerVotes[slot]]--;
        }

        _playerVotes[slot] = map;
        _votes[map]++;

        var localizer = _core.Translation.GetPlayerLocalizer(player);
        string displayName = map == "map_chooser.extend_option" ? localizer["map_chooser.extend_option"] : map;

        if (_config.AnnounceVotes)
        {
            int mapVoteCount = _votes.GetValueOrDefault(map, 0);
            _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.vote.player_voted", player.Controller?.PlayerName ?? "Unknown", displayName, mapVoteCount]);
        }
        else
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.vote.you_voted", displayName]);
        }
        
        var currentMenu = _core.MenusAPI.GetCurrentMenu(player);
        if (currentMenu?.Tag?.ToString() == "EofVoteMenu")
        {
            _core.MenusAPI.CloseMenuForPlayer(player, currentMenu);
            _activeVoteMenus.Remove(slot);
        }
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
            var menu = _core.MenusAPI.GetCurrentMenu(player);
            if (menu?.Tag?.ToString() == "EofVoteMenu")
                _core.MenusAPI.CloseMenuForPlayer(player, menu);
        }

        _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.rtv.vote_cancelled"]);
        _votes.Clear();
        _playerVotes.Clear();
        _playersReceivedMenu.Clear();
        _activeVoteMenus.Clear();
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

            if (winner == "map_chooser.extend_option")
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
                var menu = _core.MenusAPI.GetCurrentMenu(player);
                if (menu?.Tag?.ToString() == "EofVoteMenu")
                    _core.MenusAPI.CloseMenuForPlayer(player, menu);
            }
            
            _votes.Clear();
            _playerVotes.Clear();
            _playersReceivedMenu.Clear();
            _activeVoteMenus.Clear();
        }
    }
}
