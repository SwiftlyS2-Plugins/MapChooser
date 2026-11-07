using MapChooser.Contract;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using MapChooser.Models;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace MapChooser.Api;

/// <summary>What MapChooser publishes as <see cref="IMapChooserApi.InterfaceKey"/>.</summary>
/// <remarks>
/// One instance lives as long as the plugin, so its subscriptions do not depend on when <c>Load</c>
/// attaches the vote manager. Detached — before <c>Load</c>, after <c>Unload</c> — it reports no vote,
/// no maps and no nominations.
/// </remarks>
internal sealed class MapChooserApi : IMapChooserApi
{
    private EndOfMapVoteManager? _manager;
    private MapLister? _maps;
    private PluginState? _state;
    private MapCooldown? _cooldown;
    private MapChooserConfig? _config;
    private ISwiftlyCore? _core;
    private ILogger? _logger;

    public bool IsVoteActive => _manager?.IsVoteActive ?? false;

    public IReadOnlyList<string> VoteOptions => _manager?.VoteOptions ?? Array.Empty<string>();

    public int GetVoteCount(string option) => _manager?.GetVoteCount(option) ?? 0;

    public int TotalVotes => _manager?.TotalVotes ?? 0;

    public string? GetPlayerVote(int playerSlot) => _manager?.GetPlayerVote(playerSlot);

    public IReadOnlyList<MapChooserMap> Maps => _maps?.Maps.Select(ToContract).ToArray() ?? Array.Empty<MapChooserMap>();

    public MapChooserMap? GetMap(string name) => Find(name) is { } map ? ToContract(map) : null;

    public int VoteSecondsLeft => _manager?.SecondsLeft ?? 0;

    public int EligibleVoters => _manager?.EligibleVoters ?? 0;

    public int ExtendsLeft => _state?.ExtendsLeft ?? 0;

    // ExtendManager.ExtendMap's rule: each step applies only to a limit the map has.
    public int ExtendMinutes => _config is { } config && ConVar<float>("mp_timelimit") > 0 ? Math.Max(0, config.EndOfMap.ExtendTimeStep) : 0;

    public int ExtendRounds => _config is { } config && ConVar<int>("mp_maxrounds") > 0 ? Math.Max(0, config.EndOfMap.ExtendRoundStep) : 0;

    public int? MapSecondsLeft
        => _core is { } core && _state is { } state && MapTimeLeft.Seconds(core, state) is { } seconds ? (int)Math.Max(0f, seconds) : null;

    public int? MapRoundsLeft
        => _core is { } core && _state is { } state && MapTimeLeft.Rounds(core, state) is { } rounds ? Math.Max(0, rounds) : null;

    public IReadOnlyList<string> Nominations => _state?.Nominations.Values.Distinct().ToArray() ?? Array.Empty<string>();

    public string? GetPlayerNomination(int playerSlot)
        => _state is not null && _state.Nominations.TryGetValue(playerSlot, out var map) ? map : null;

    public MapChooserAvailability GetAvailability(string name)
    {
        if (_core is null || _cooldown is null || Find(name) is not { } map) return MapChooserAvailability.NotInPool;
        return MapAvailability.Of(_core, _cooldown, map);
    }

    public event Action? VoteChanged;

    public event Action<int, string>? PlayerVoted;

    public event Action? NominationsChanged;

    internal void Attach(EndOfMapVoteManager manager, MapLister maps, PluginState state, MapCooldown cooldown, MapChooserConfig config, ISwiftlyCore core, ILogger logger)
    {
        Unwire();
        _logger = logger;
        _manager = manager;
        _maps = maps;
        _state = state;
        _cooldown = cooldown;
        _config = config;
        _core = core;
        manager.VoteChanged += ForwardVoteChanged;
        manager.PlayerVoted += ForwardPlayerVoted;
        state.NominationsChanged += ForwardNominationsChanged;
    }

    // Unload: report no vote from here on, and let go of other plugins' handlers.
    internal void Detach()
    {
        Unwire();
        _maps = null;
        _cooldown = null;
        _config = null;
        _core = null;
        ClearSubscribers();
    }

    private Map? Find(string name)
        => _maps?.Maps.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private T? ConVar<T>(string name) where T : struct
        => _core?.ConVar.Find<T>(name)?.Value;

    // ChangeMapManager's rule: an Id that is "ws:"-prefixed or numeric is a workshop ID; otherwise
    // the server changelevels to the Id, or to the name when there is none.
    private static MapChooserMap ToContract(Map map)
    {
        var id = map.Id?.Trim();
        string? workshopId = string.IsNullOrEmpty(id) ? null
            : id.StartsWith("ws:", StringComparison.OrdinalIgnoreCase) ? id[3..]
            : long.TryParse(id, out _) ? id
            : null;
        var mapName = workshopId is null ? (string.IsNullOrEmpty(id) ? map.Name : id) : null;
        return new MapChooserMap(map.Name, mapName, workshopId, map.MinPlayers, map.MaxPlayers);
    }

    // SwiftlyS2's shared-API guidance for APIs with events: consumers subscribe again in every
    // handshake, so the provider drops the old subscriptions on each ConfigureSharedInterface.
    internal void ClearSubscribers()
    {
        VoteChanged = null;
        PlayerVoted = null;
        NominationsChanged = null;
    }

    private void Unwire()
    {
        if (_manager is not null)
        {
            _manager.VoteChanged -= ForwardVoteChanged;
            _manager.PlayerVoted -= ForwardPlayerVoted;
            _manager = null;
        }

        if (_state is not null)
        {
            _state.NominationsChanged -= ForwardNominationsChanged;
            _state = null;
        }
    }

    private void ForwardVoteChanged() => Raise(VoteChanged, nameof(VoteChanged));

    private void ForwardNominationsChanged() => Raise(NominationsChanged, nameof(NominationsChanged));

    // One subscriber at a time, so a throwing one costs neither the others nor the vote.
    private void Raise(Action? handlers, string eventName)
    {
        if (handlers is null) return;
        foreach (var handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action)handler)();
            }
            catch (Exception ex)
            {
                LogSubscriberFault(ex, eventName, handler);
            }
        }
    }

    private void ForwardPlayerVoted(int playerSlot, string option)
    {
        if (PlayerVoted is not { } handlers) return;
        foreach (var handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action<int, string>)handler)(playerSlot, option);
            }
            catch (Exception ex)
            {
                LogSubscriberFault(ex, nameof(PlayerVoted), handler);
            }
        }
    }

    private void LogSubscriberFault(Exception ex, string eventName, Delegate handler)
    {
        _logger?.LogWarning(ex, "MapChooser: a {Event} subscriber ({Subscriber}) threw; the vote is unaffected.",
            eventName, handler.Method.DeclaringType?.FullName ?? handler.Method.Name);
    }
}
