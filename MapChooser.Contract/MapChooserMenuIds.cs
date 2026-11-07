namespace MapChooser.Contract;

/// <summary>
/// The ids of MapChooser's menus. Every page in the hub opens as the hub's child, and each hub entry
/// carries its page's id, so a renderer can mirror the hub as navigation.
/// </summary>
public static class MapChooserMenuIds
{
    /// <summary>The per-player menu <c>!maps</c> opens: Vote, Nominate, then the admin pages.</summary>
    public const string Hub = "mapchooser.hub";

    /// <summary>The end-of-map, RTV or <c>!mapsvote</c> ballot.</summary>
    public const string Vote = "mapchooser.eof-vote";

    /// <summary>The ballot when <c>DisableVoteMenuExit</c> is on: no close key, and no hub to leave through.</summary>
    public const string LockedVote = "mapchooser.eof-vote.locked";

    public const string Nominate = "mapchooser.nominate";

    /// <summary><c>!votemap</c>; not in the hub.</summary>
    public const string Votemap = "mapchooser.votemap";

    public const string ChangeMap = "mapchooser.admin-change-map";

    public const string SetNextMap = "mapchooser.set-next-map";

    public const string StartVote = "mapchooser.admin-maps-vote";

    public const string Cycle = "mapchooser.cycle";
}
