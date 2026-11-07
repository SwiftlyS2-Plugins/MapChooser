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

    /// <summary>The configured map pool, in config order. Read on the game thread.</summary>
    IReadOnlyList<MapChooserMap> Maps { get; }

    /// <summary>A pool map by the name its menus and ballot show, case-insensitively; null for any other name, <see cref="ExtendOption"/> included.</summary>
    MapChooserMap? GetMap(string name);

    /// <summary>Seconds until the running vote closes, rounded up; 0 when no vote is running.</summary>
    int VoteSecondsLeft { get; }

    /// <summary>Players who may vote in the running vote: connected humans, spectators only where MapChooser lets them vote. 0 when no vote is running.</summary>
    int EligibleVoters { get; }

    /// <summary>How many more times the current map can be extended.</summary>
    int ExtendsLeft { get; }

    /// <summary>Minutes an extend adds to <c>mp_timelimit</c>; 0 when the map has no time limit.</summary>
    int ExtendMinutes { get; }

    /// <summary>Rounds an extend adds to <c>mp_maxrounds</c>; 0 when the map has no round limit.</summary>
    int ExtendRounds { get; }

    /// <summary>Seconds left on the current map under <c>mp_timelimit</c>; null without a time limit or during warmup. Read on the game thread.</summary>
    int? MapSecondsLeft { get; }

    /// <summary>Rounds left on the current map under <c>mp_maxrounds</c>; null without a round limit or during warmup. Read on the game thread.</summary>
    int? MapRoundsLeft { get; }

    /// <summary>The maps nominated for the next vote, each once, by the name <see cref="GetMap"/> takes.</summary>
    IReadOnlyList<string> Nominations { get; }

    /// <summary>The map this player nominated, or null.</summary>
    string? GetPlayerNomination(int playerSlot);

    /// <summary>Raised on the game thread when a nomination is made, replaced or cleared.</summary>
    event Action? NominationsChanged;

    /// <summary>Whether a pool map can be nominated or voted for now, and if not, why. Read on the game thread.</summary>
    MapChooserAvailability GetAvailability(string name);
}
