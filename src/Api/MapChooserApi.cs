using MapChooser.Contract;
using MapChooser.Helpers;
using Microsoft.Extensions.Logging;

namespace MapChooser.Api;

/// <summary>What MapChooser publishes as <see cref="IMapChooserApi.InterfaceKey"/>.</summary>
/// <remarks>
/// One instance lives as long as the plugin, so its subscriptions do not depend on when <c>Load</c>
/// attaches the vote manager. Detached — before <c>Load</c>, after <c>Unload</c> — it reports no vote.
/// </remarks>
internal sealed class MapChooserApi : IMapChooserApi
{
    private EndOfMapVoteManager? _manager;
    private ILogger? _logger;

    public bool IsVoteActive => _manager?.IsVoteActive ?? false;

    public IReadOnlyList<string> VoteOptions => _manager?.VoteOptions ?? Array.Empty<string>();

    public int GetVoteCount(string option) => _manager?.GetVoteCount(option) ?? 0;

    public int TotalVotes => _manager?.TotalVotes ?? 0;

    public string? GetPlayerVote(int playerSlot) => _manager?.GetPlayerVote(playerSlot);

    public event Action? VoteChanged;

    public event Action<int, string>? PlayerVoted;

    internal void Attach(EndOfMapVoteManager manager, ILogger logger)
    {
        Unwire();
        _logger = logger;
        _manager = manager;
        manager.VoteChanged += ForwardVoteChanged;
        manager.PlayerVoted += ForwardPlayerVoted;
    }

    // Unload: report no vote from here on, and let go of other plugins' handlers.
    internal void Detach()
    {
        Unwire();
        ClearSubscribers();
    }

    // SwiftlyS2's shared-API guidance for APIs with events: consumers subscribe again in every
    // handshake, so the provider drops the old subscriptions on each ConfigureSharedInterface.
    internal void ClearSubscribers()
    {
        VoteChanged = null;
        PlayerVoted = null;
    }

    private void Unwire()
    {
        if (_manager is null) return;
        _manager.VoteChanged -= ForwardVoteChanged;
        _manager.PlayerVoted -= ForwardPlayerVoted;
        _manager = null;
    }

    // One subscriber at a time, so a throwing one costs neither the others nor the vote.
    private void ForwardVoteChanged()
    {
        if (VoteChanged is not { } handlers) return;
        foreach (var handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action)handler)();
            }
            catch (Exception ex)
            {
                LogSubscriberFault(ex, nameof(VoteChanged), handler);
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
