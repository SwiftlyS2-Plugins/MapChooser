using System.Threading;
using MapChooser.Api;
using MapChooser.Commands;
using MapChooser.Contract;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using MapChooser.Menu;
using MapChooser.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace MapChooser;

[PluginMetadata(Id = "MapChooser", Version = "1.5.0", Name = "Map Chooser", Author = "aga", Description = "Map chooser plugin for SwiftlyS2")]
public sealed class MapChooser : BasePlugin
{
    private MapChooserConfig _config = new();
    private MapsConfig _mapsConfig = new();
    private PluginState _state = new();
    private MapLister _mapLister = new();
    private MapCooldown _mapCooldown = null!;
    private ChangeMapManager _changeMapManager = null!;
    private VoteManager _rtvVoteManager = null!;
    private VoteManager _extVoteManager = null!;
    private EndOfMapVoteManager _eofManager = null!;
    private ExtendManager _extendManager = null!;
    private MapChooserMenus _menus = null!;

    private MapCycleManager _cycleManager = null!;

    private MenuCommand _menuCmd = null!;
    private RtvCommand _rtvCmd = null!;
    private UnRtvCommand _unRtvCmd = null!;
    private NominateCommand _nominateCmd = null!;
    private TimeleftCommand _timeleftCmd = null!;
    private NextmapCommand _nextmapCmd = null!;
    private VotemapCommand _votemapCmd = null!;
    private RevoteCommand _revoteCmd = null!;
    private SetNextMapCommand _setNextMapCmd = null!;
    private ExtendCommand _extendCmd = null!;
    private AdminMapsVoteCommand _adminMapsVoteCmd = null!;
    private AdminChangeMapCommand _adminChangeMapCmd = null!;
    private MapListCommand _mapListCmd = null!;
    private AddMapCommand _addMapCmd = null!;
    private RemoveMapCommand _removeMapCmd = null!;

    private CancellationTokenSource? _checkVoteTimer;

    // Each command alias registered so far, and the Commands setting it came from.
    private readonly Dictionary<string, string> _registeredAliases = new(StringComparer.OrdinalIgnoreCase);

    // Built with the plugin rather than in Load, so it exists whichever of the two SwiftlyS2 calls first.
    private readonly MapChooserApi _api = new();

    public MapChooser(ISwiftlyCore core) : base(core)
    {
    }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        // Re-runs on every plugin load and unload, before consumers subscribe again in their handshake.
        _api.ClearSubscribers();
        interfaceManager.AddSharedInterface<IMapChooserApi, MapChooserApi>(IMapChooserApi.InterfaceKey, _api);
    }

    public override void Load(bool hotReload)
    {
        Core.Configuration
            .InitializeJsonWithModel<MapChooserConfig>("config.jsonc", "MapChooser")
            .Configure(builder =>
            {
                builder.AddJsonFile("config.jsonc", optional: false, reloadOnChange: true);
            });

        Core.Configuration
            .InitializeJsonWithModel<MapsConfig>("maps.jsonc", "MapChooserMaps")
            .Configure(builder =>
            {
                builder.AddJsonFile("maps.jsonc", optional: false, reloadOnChange: true);
            });

        _config = Core.Configuration.Manager.GetSection("MapChooser").Get<MapChooserConfig>() ?? new MapChooserConfig();
        _mapsConfig = Core.Configuration.Manager.GetSection("MapChooserMaps").Get<MapsConfig>() ?? new MapsConfig();
        _mapLister.UpdateMaps(_mapsConfig.Maps);

        _mapCooldown = new MapCooldown(Core, _config);
        _changeMapManager = new ChangeMapManager(Core, _state, _mapLister, _config);
        _rtvVoteManager = new VoteManager();
        _extVoteManager = new VoteManager();
        _extendManager = new ExtendManager(Core, _state, _config);
        _eofManager = new EndOfMapVoteManager(Core, _state, _rtvVoteManager, _mapLister, _mapCooldown, _changeMapManager, _extendManager, _config);
        _menus = new MapChooserMenus(Core, _config, _eofManager);
        _eofManager.Menus = _menus;
        _api.Attach(_eofManager, _mapLister, _state, _mapCooldown, _config, Core, Core.Logger);

        var mapsFilePath = Core.Configuration.GetConfigPath("maps.jsonc");
        _cycleManager = new MapCycleManager(Core, _state, _mapLister, _changeMapManager, _config, mapsFilePath);

        _state.ExtendsLeft = _config.EndOfMap.ExtendLimit;
        _state.NextEofVotePossibleRound = 0;
        _state.NextEofVotePossibleTime = 0;
        _state.RoundsPlayed = 0;
        var warmupConVar = Core.ConVar.Find<int>("mp_warmup_period");
        _state.WarmupRunning = warmupConVar?.Value == 1;

        _menuCmd = new MenuCommand(Core, _menus);
        _rtvCmd = new RtvCommand(Core, _state, _rtvVoteManager, _eofManager, _config);
        _unRtvCmd = new UnRtvCommand(Core, _state, _rtvVoteManager, _eofManager, _config);
        _nominateCmd = new NominateCommand(Core, _state, _mapLister, _mapCooldown, _config, _menus);
        _timeleftCmd = new TimeleftCommand(Core, _state, _config);
        _nextmapCmd = new NextmapCommand(Core, _state, _cycleManager, _config);
        _votemapCmd = new VotemapCommand(Core, _state, _mapLister, _mapCooldown, _changeMapManager, _config);
        _revoteCmd = new RevoteCommand(Core, _state, _eofManager, _config);
        _setNextMapCmd = new SetNextMapCommand(Core, _state, _mapLister, _changeMapManager, _menus);
        _extendCmd = new ExtendCommand(Core, _state, _extVoteManager, _extendManager, _config);
        _adminMapsVoteCmd = new AdminMapsVoteCommand(Core, _state, _mapLister, _eofManager, _config, _menus);
        _adminChangeMapCmd = new AdminChangeMapCommand(Core, _state, _mapLister, _changeMapManager, _menus);
        _mapListCmd = new MapListCommand(Core, _mapLister, _mapCooldown);
        _addMapCmd = new AddMapCommand(Core, _cycleManager);
        _removeMapCmd = new RemoveMapCommand(Core, _cycleManager);

        _menus.SetPage(MapChooserMenuIds.Vote, _revoteCmd.BuildMenu);
        _menus.SetPage(MapChooserMenuIds.Nominate, _nominateCmd.BuildMenu);
        _menus.SetPage(MapChooserMenuIds.ChangeMap, _adminChangeMapCmd.BuildMenu);
        _menus.SetPage(MapChooserMenuIds.SetNextMap, _setNextMapCmd.BuildMenu);
        _menus.SetPage(MapChooserMenuIds.StartVote, _adminMapsVoteCmd.BuildMenu);
        _menus.SetPage(MapChooserMenuIds.Cycle, BuildCycleMenu);

        // The hub first: an alias it shares with a later setting stays the hub's.
        RegisterCommands(nameof(CommandsConfig.Menu), _config.Commands.Menu, _menuCmd.Execute);
        RegisterCommands(nameof(CommandsConfig.Rtv), _config.Commands.Rtv, _rtvCmd.Execute);
        RegisterCommands(nameof(CommandsConfig.UnRtv), _config.Commands.UnRtv, _unRtvCmd.Execute);
        RegisterCommands(nameof(CommandsConfig.Nominate), _config.Commands.Nominate, _nominateCmd.Execute);
        RegisterCommands(nameof(CommandsConfig.Timeleft), _config.Commands.Timeleft, _timeleftCmd.Execute);
        RegisterCommands(nameof(CommandsConfig.Nextmap), _config.Commands.Nextmap, _nextmapCmd.Execute);
        RegisterCommands(nameof(CommandsConfig.Votemap), _config.Commands.Votemap, _votemapCmd.Execute);
        RegisterCommands(nameof(CommandsConfig.Revote), _config.Commands.Revote, _revoteCmd.Execute);
        RegisterCommands(nameof(CommandsConfig.SetNextMap), _config.Commands.SetNextMap, _setNextMapCmd.Execute, permission: _config.SetNextMapPermission);
        RegisterCommands(nameof(CommandsConfig.Extend), _config.Commands.Extend, _extendCmd.Execute);
        RegisterCommands(nameof(CommandsConfig.MapsVote), _config.Commands.MapsVote, _adminMapsVoteCmd.Execute, permission: _config.MapsVotePermission);
        RegisterCommands(nameof(CommandsConfig.ChangeMap), _config.Commands.ChangeMap, _adminChangeMapCmd.Execute, permission: _config.ChangeMapPermission);
        RegisterCommands(nameof(CommandsConfig.MapList), _config.Commands.MapList, _mapListCmd.Execute);
        RegisterCommands(nameof(CommandsConfig.AddMap), _config.Commands.AddMap, _addMapCmd.Execute, permission: _config.Cycle.AddMapPermission);
        RegisterCommands(nameof(CommandsConfig.RemoveMap), _config.Commands.RemoveMap, _removeMapCmd.Execute, permission: _config.Cycle.RemoveMapPermission);
        RegisterCommands(nameof(CommandsConfig.CycleMenu), _config.Commands.CycleMenu, ExecuteCycleMenu, permission: _config.Cycle.CycleMenuPermission);

        Core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        Core.GameEvent.HookPost<EventRoundStart>(OnRoundStart);
        Core.GameEvent.HookPost<EventRoundAnnounceWarmup>(OnAnnounceWarmup);
        Core.GameEvent.HookPost<EventWarmupEnd>(OnWarmupEnd);
        Core.GameEvent.HookPost<EventCsWinPanelMatch>(OnWinPanelMatch);
        Core.GameEvent.HookPost<EventGamePhaseChanged>(OnGamePhaseChanged);
        Core.GameEvent.HookPost<EventRoundAnnounceMatchStart>(OnMatchStart);
        Core.GameEvent.HookPost<EventRoundAnnounceMatchPoint>(OnMatchPoint);
        Core.Event.OnMapLoad += OnMapLoad;

        _checkVoteTimer = Core.Scheduler.DelayAndRepeat(1000, 1000, () =>
        {
            CheckAutomatedVote();
        });
        Core.Scheduler.StopOnMapChange(_checkVoteTimer);
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        if (@event.MapName == null) return;
        if (string.IsNullOrEmpty(@event.MapName)) return;

        _eofManager?.ResetVote();
        _state.MapChangeScheduled = false;
        _state.EofVoteHappening = false;
        _state.NextMap = null;
        _state.RoundsPlayed = 0;

        // Release the guard latched by the previous map's ChangeMap. Without this the flag
        // stays true for the rest of the process and every later ChangeMap/EOF vote no-ops.
        _state.MapSwitchInFlight = false;

        // MapStartTime is set to 0 here; it will be updated properly in OnWarmupEnd or OnMatchStart
        // when the engine is fully initialized (avoiding crash from accessing GlobalVars during map load)
        _state.MapStartTime = 0;

        _state.RtvCooldownEndTime = null;
        _state.IsRtv = false;
        _state.ChangeMapImmediately = false;

        _rtvVoteManager?.Clear();
        _extVoteManager?.Clear();
        _nominateCmd?.Clear();
        _state.ExtendsLeft = _config.EndOfMap.ExtendLimit;
        _state.NextEofVotePossibleRound = 0;
        _state.NextEofVotePossibleTime = 0;
        _state.MatchEnded = false;
        _state.EofVoteCompleted = false;

        // Workshop ID and cooldown/cycle updates are deferred until engine is ready
        // This prevents crashes from accessing gamerules/GlobalVars during early map load
        string workshopId = "";
        _mapCooldown.OnMapStart(@event.MapName, workshopId);
        _cycleManager.OnMapStart(@event.MapName, workshopId);

        _checkVoteTimer = Core.Scheduler.DelayAndRepeat(1000, 1000, () =>
        {
            CheckAutomatedVote();
        });
        Core.Scheduler.StopOnMapChange(_checkVoteTimer);
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        CheckAutomatedVote();
        return HookResult.Continue;
    }

    private HookResult OnAnnounceWarmup(EventRoundAnnounceWarmup @event)
    {
        _state.WarmupRunning = true;
        return HookResult.Continue;
    }

    private HookResult OnWarmupEnd(EventWarmupEnd @event)
    {
        _state.WarmupRunning = false;
        try
        {
            _state.MapStartTime = Core.Engine is { } e ? e.GlobalVars.CurrentTime : 0;
        }
        catch
        {
            _state.MapStartTime = 0;
        }
        return HookResult.Continue;
    }

    private HookResult OnMatchStart(EventRoundAnnounceMatchStart @event)
    {
        _eofManager?.ResetVote();
        _state.RoundsPlayed = 0;
        try
        {
            _state.MapStartTime = Core.Engine is { } e ? e.GlobalVars.CurrentTime : 0;
        }
        catch
        {
            _state.MapStartTime = 0;
        }
        _state.WarmupRunning = false;
        _state.NextEofVotePossibleRound = 0;
        _state.NextEofVotePossibleTime = 0;
        _state.MapChangeScheduled = false;
        _state.EofVoteHappening = false;
        _state.EofVoteCompleted = false;
        _state.IsRtv = false;
        _state.ChangeMapImmediately = false;
        _state.QueuedRoundEndChange = false;
        _state.NextMap = null;
        _state.ExtendsLeft = _config.EndOfMap.ExtendLimit;

        _rtvVoteManager?.Clear();
        _extVoteManager?.Clear();
        return HookResult.Continue;
    }

    private HookResult OnMatchPoint(EventRoundAnnounceMatchPoint @event)
    {
        CheckAutomatedVote(true);
        return HookResult.Continue;
    }

    private HookResult OnWinPanelMatch(EventCsWinPanelMatch @event)
    {
        try
        {
            if (Core.Game.MatchData.Phase == GamePhase.GAMEPHASE_HALFTIME) return HookResult.Continue;
        }
        catch (InvalidOperationException ex)
        {
            Core.Logger.LogWarning(ex, "GameRules not available in OnWinPanelMatch - proceeding without halftime check");
        }

        _state.MatchEnded = true;
        if (_state.EofVoteHappening)
            _eofManager.ForceEnd();
        else if (_state.MapChangeScheduled)
            _changeMapManager.ChangeMap();
        else if (_config.Cycle.Enabled)
            _cycleManager.TriggerCycleChange();
        return HookResult.Continue;
    }

    private HookResult OnGamePhaseChanged(EventGamePhaseChanged @event)
    {
        if (@event.NewPhase != (short)GamePhase.GAMEPHASE_MATCH_ENDED) return HookResult.Continue;
        if (_state.MatchEnded) return HookResult.Continue;

        _state.MatchEnded = true;
        if (_state.EofVoteHappening)
            _eofManager.ForceEnd();
        else if (_state.MapChangeScheduled)
            _changeMapManager.ChangeMap();
        else if (_config.Cycle.Enabled)
            _cycleManager.TriggerCycleChange();
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event)
    {
        _state.RoundsPlayed++;

        if (_config.DetailedLogging)
            Core.Logger.LogInformation(
                "MapChooser: OnRoundEnd scheduled={Scheduled} queuedRoundEnd={Queued} eofVote={Eof} immediate={Immediate} isRtv={IsRtv} nextMap={NextMap}",
                _state.MapChangeScheduled, _state.QueuedRoundEndChange, _state.EofVoteHappening,
                _state.ChangeMapImmediately, _state.IsRtv, _state.NextMap ?? "<null>");

        if (_state.QueuedRoundEndChange && _state.MapChangeScheduled && !_state.EofVoteHappening)
        {
            if (_config.DetailedLogging)
                Core.Logger.LogInformation("MapChooser: OnRoundEnd firing queued ChangeMap for '{Map}'.", _state.NextMap ?? "<null>");
            _changeMapManager.ChangeMap();
        }
        else if (!_state.MapChangeScheduled)
        {
            CheckAutomatedVote();
        }

        return HookResult.Continue;
    }

    private void CheckAutomatedVote(bool force = false)
    {
        try
        {
            CheckAutomatedVoteCore(force);
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "MapChooser: CheckAutomatedVote threw");
        }
    }

    private void CheckAutomatedVoteCore(bool force)
    {
        if (!_config.EndOfMap.Enabled || _state.EofVoteHappening || _state.MapChangeScheduled || _state.WarmupRunning) return;
        if (_state.MapSwitchInFlight) return;

        // Silent early exit if game is not fully initialized yet (MapStartTime is set in OnWarmupEnd/OnMatchStart)
        // This prevents spamming logs with GameRules exceptions during early map load
        if (_state.MapStartTime <= 0) return;

        int totalRoundsPlayed;
        try
        {
            if (Core.Game.MatchData.Phase == GamePhase.GAMEPHASE_HALFTIME) return;
            totalRoundsPlayed = Core.Game.MatchData.TerroristScoreTotal + Core.Game.MatchData.CTScoreTotal;
        }
        catch (InvalidOperationException)
        {
            // GameRules not available yet - silently skip this tick
            return;
        }

        bool pastDueNoMap = _state.EofVoteCompleted && string.IsNullOrEmpty(_state.NextMap);

        if (!force && !pastDueNoMap)
        {
            if (_state.EofVoteCompleted) return;
            if (totalRoundsPlayed < _state.NextEofVotePossibleRound) return;
            if (Core.Engine != null && Core.Engine.GlobalVars.CurrentTime < _state.NextEofVotePossibleTime) return;
        }

        var timelimitConVar = Core.ConVar.Find<float>("mp_timelimit");
        var maxroundsConVar = Core.ConVar.Find<int>("mp_maxrounds");
        var winlimitConVar = Core.ConVar.Find<int>("mp_winlimit");

        float timelimit = timelimitConVar?.Value ?? 0;
        int maxrounds = maxroundsConVar?.Value ?? 0;
        int winlimit = winlimitConVar?.Value ?? 0;

        bool trigger = false;

        if (timelimit > 0 && Core.Engine != null)
        {
            if (_state.MapStartTime <= 0)
            {
                _state.MapStartTime = Core.Engine.GlobalVars.CurrentTime;
            }
            float timePlayed = Core.Engine.GlobalVars.CurrentTime - _state.MapStartTime;
            float timeRemaining = (timelimit * 60) - timePlayed;
            if (timeRemaining <= _config.EndOfMap.TriggerSecondsBeforeEnd)
            {
                trigger = true;
            }
        }

        if (!trigger && maxrounds > 0)
        {
            int roundsRemaining = maxrounds - totalRoundsPlayed;
            if (roundsRemaining <= _config.EndOfMap.TriggerRoundsBeforeEnd)
            {
                trigger = true;
            }
        }

        if (!trigger && winlimit > 0)
        {
            int maxTeamScore = TryGetMaxTeamScore();
            if (winlimit - maxTeamScore <= _config.EndOfMap.TriggerRoundsBeforeEnd)
            {
                trigger = true;
            }
        }

        // When mp_winlimit is not set, CS2 still ends the match when a team wins (maxrounds/2)+1 rounds
        if (!trigger && winlimit == 0 && maxrounds > 0)
        {
            int effectiveWinlimit = maxrounds / 2 + 1;
            int maxTeamScore = TryGetMaxTeamScore();
            if (effectiveWinlimit - maxTeamScore <= _config.EndOfMap.TriggerRoundsBeforeEnd)
            {
                trigger = true;
            }
        }

        if (trigger)
        {
            _state.EofVoteCompleted = false;
            _state.NextEofVotePossibleRound = totalRoundsPlayed + 1;
            if (Core.Engine != null)
                _state.NextEofVotePossibleTime = Core.Engine.GlobalVars.CurrentTime + _config.EndOfMap.VoteDuration + 1;
            _eofManager.StartVote(_config.EndOfMap.VoteDuration, _config.EndOfMap.MapsToShow);
        }
    }

    /// <summary>
    /// Safely read the highest team score. The entity system can throw if it
    /// isn't fully initialised yet (e.g. during early map load), so swallow
    /// and return 0 in that case.
    /// </summary>
    private int TryGetMaxTeamScore()
    {
        try
        {
            var teams = Core.EntitySystem.GetAllEntitiesByClass<CCSTeam>();
            int maxTeamScore = 0;
            foreach (var team in teams)
            {
                int score = team.ScoreFirstHalf + team.ScoreSecondHalf + team.ScoreOvertime;
                if (score > maxTeamScore) maxTeamScore = score;
            }
            return maxTeamScore;
        }
        catch (Exception ex)
        {
            Core.Logger.LogWarning(ex, "MapChooser: failed to read team scores");
            return 0;
        }
    }

    private void ExecuteCycleMenu(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("This command can only be used by players.");
            return;
        }
        var player = context.Sender!;
        _menus.Open(player, BuildCycleMenu(player));
    }

    private IMenu BuildCycleMenu(IPlayer player) => new CycleMenu(Core, _mapLister, _cycleManager, _config).Build(player);

    private void RegisterCommands(string setting, string commandNames, ICommandService.CommandListener handler, string? permission = null)
    {
        var names = commandNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var name in names)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                // SwiftlyS2 runs every listener registered under a name, so a second one would run both commands.
                if (_registeredAliases.TryGetValue(name, out var owner))
                {
                    Core.Logger.LogInformation("MapChooser: Commands.{Setting} lists '{Alias}', which Commands.{Owner} already registers, so it was skipped; it can be removed from config.jsonc.",
                        setting, name, owner);
                    continue;
                }
                _registeredAliases[name] = setting;

                if (permission != null)
                    Core.Command.RegisterCommand(name, handler, permission: permission);
                else
                    Core.Command.RegisterCommand(name, handler);
            }
        }
    }

    public override void Unload()
    {
        try
        {
            _api.Detach();

            _checkVoteTimer?.Cancel();
            _checkVoteTimer = null;

            _changeMapManager?.CancelPending();

            Core.Event.OnMapLoad -= OnMapLoad;

            Core.GameEvent.UnhookPost<EventRoundEnd>();
            Core.GameEvent.UnhookPost<EventRoundStart>();
            Core.GameEvent.UnhookPost<EventRoundAnnounceWarmup>();
            Core.GameEvent.UnhookPost<EventWarmupEnd>();
            Core.GameEvent.UnhookPost<EventCsWinPanelMatch>();
            Core.GameEvent.UnhookPost<EventGamePhaseChanged>();
            Core.GameEvent.UnhookPost<EventRoundAnnounceMatchStart>();
            Core.GameEvent.UnhookPost<EventRoundAnnounceMatchPoint>();
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "MapChooser: Unload cleanup failed");
        }
    }
}
