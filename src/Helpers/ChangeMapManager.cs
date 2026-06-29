using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace MapChooser.Helpers;

public class ChangeMapManager
{
    private readonly ISwiftlyCore _core;
    private readonly PluginState _state;
    private readonly MapLister _mapLister;
    private readonly MapChooserConfig _config;
    private CancellationTokenSource? _pendingChange;

    public ChangeMapManager(ISwiftlyCore core, PluginState state, MapLister mapLister, MapChooserConfig config)
    {
        _core = core;
        _state = state;
        _mapLister = mapLister;
        _config = config;
    }

    public void ScheduleMapChange(string mapName, bool changeImmediately = false, bool isRtv = false)
    {
        _state.NextMap = mapName;
        _state.MapChangeScheduled = true;
        _state.ChangeMapImmediately = changeImmediately;
        _state.IsRtv = isRtv;

        bool fireNow = changeImmediately || _state.MatchEnded;
        // Round-end queueing only applies to RTV. Non-RTV votes (end-of-map,
        // admin maps vote, votemap) that aren't immediate wait for the match
        // to end naturally and are then triggered by OnWinPanelMatch /
        // OnGamePhaseChanged.
        _state.QueuedRoundEndChange = !fireNow && isRtv;

        if (_config.DetailedLogging)
            _core.Logger.LogInformation(
                "MapChooser: ScheduleMapChange map={Map} immediate={Immediate} isRtv={IsRtv} matchEnded={MatchEnded} -> {Action}",
                mapName, changeImmediately, isRtv, _state.MatchEnded, fireNow ? "ChangeMap" : "QueuedRoundEnd");

        if (fireNow)
        {
            ChangeMap();
        }
        else
        {
            _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.next_map_announced", mapName]);
        }
    }

    public void ChangeMap()
    {
        // Debounce concurrent triggers (WinPanelMatch + GamePhaseChanged + cycle, etc.)
        if (_state.MapSwitchInFlight) return;
        if (string.IsNullOrEmpty(_state.NextMap))
        {
            if (_config.DetailedLogging)
                _core.Logger.LogInformation("MapChooser: ChangeMap aborted — NextMap is empty.");
            return;
        }

        var mapName = _state.NextMap;
        _state.NextMap = null;
        _state.MapChangeScheduled = false;
        _state.ChangeMapImmediately = true;
        _state.QueuedRoundEndChange = false;

        var map = _mapLister.Maps.FirstOrDefault(m => m.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase));
        if (map == null)
        {
            _core.Logger.LogWarning("MapChooser: ChangeMap aborted — map '{Map}' not found in map list.", mapName);
            return;
        }

        int delay = _state.IsRtv ? _config.Rtv.ChangeMapDelay : _config.EndOfMap.ChangeMapDelay;
        _state.IsRtv = false;
        _state.MapSwitchInFlight = true;
        _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.changing_map", map.Name, delay]);

        // Cancel any previously scheduled (but not yet fired) change to prevent stacking.
        _pendingChange?.Cancel();

        _pendingChange = _core.Scheduler.DelayBySeconds(delay, () =>
        {
            try
            {
                if (_core.Engine is not { } engine) return;

                if (!string.IsNullOrEmpty(map.Id) && (map.Id.StartsWith("ws:") || long.TryParse(map.Id, out _)))
                {
                    string workshopId = map.Id.StartsWith("ws:") ? map.Id.Substring(3) : map.Id;
                    engine.ExecuteCommandWithBuffer($"nextlevel {map.Name}", _ => { });
                    engine.ExecuteCommandWithBuffer($"host_workshop_map {workshopId}", _ => { });
                }
                else
                {
                    engine.ExecuteCommandWithBuffer($"nextlevel {map.Name}", _ => { });
                    engine.ExecuteCommandWithBuffer($"changelevel {map.Id ?? map.Name}", _ => { });
                }
            }
            catch (Exception ex)
            {
                _core.Logger.LogError(ex, "MapChooser: failed to issue map change to {Map}", map.Name);
                // Clear in-flight so a retry/fallback path can run.
                _state.MapSwitchInFlight = false;
            }
            finally
            {
                _pendingChange = null;
            }
        });

        // Belt & braces: if the map actually changes for any other reason before
        // the delay fires, the scheduler will auto-cancel this token.
        _core.Scheduler.StopOnMapChange(_pendingChange);
    }

    /// <summary>Cancel any pending map-change callback (used during plugin unload).</summary>
    public void CancelPending()
    {
        _pendingChange?.Cancel();
        _pendingChange = null;
    }
}
