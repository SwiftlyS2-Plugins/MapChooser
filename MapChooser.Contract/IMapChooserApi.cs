namespace MapChooser.Contract;

/// <summary>The <c>MapChooser.Api.v1</c> shared interface: the running map vote, read-only.</summary>
/// <remarks>Additive only: there is no version negotiation between plugins, so a renamed or re-signed member is a MissingMethodException at call time.</remarks>
public interface IMapChooserApi
{
    const string InterfaceKey = "MapChooser.Api.v1";

    /// <summary>The ballot entry that extends the current map instead of changing it.</summary>
    const string ExtendOption = "map_chooser.extend_option";

    /// <summary>True while an end-of-map or rock-the-vote ballot is open.</summary>
    bool IsVoteActive { get; }

    /// <summary>The ballot in the order it is shown: map names, plus <see cref="ExtendOption"/> when extending is offered. Empty when no vote is running.</summary>
    IReadOnlyList<string> VoteOptions { get; }

    int GetVoteCount(string option);

    int TotalVotes { get; }

    /// <summary>The option this player voted for, or null.</summary>
    string? GetPlayerVote(int playerSlot);

    /// <summary>Raised on the game thread whenever the tally changes: a vote starts, a ballot is cast, changed or withdrawn, or the vote ends or is cancelled.</summary>
    event Action? VoteChanged;

    /// <summary>Raised on the game thread after a player casts or changes a ballot: player slot, option.</summary>
    event Action<int, string>? PlayerVoted;
}
