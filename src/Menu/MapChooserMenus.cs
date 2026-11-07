using MapChooser.Contract;
using MapChooser.Helpers;
using MapChooser.Models;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Players;

namespace MapChooser.Menu;

/// <summary>The map menu (<see cref="MapChooserMenuIds.Hub"/>), built per player, and the way its pages open: as its child.</summary>
public sealed class MapChooserMenus
{
    private readonly ISwiftlyCore _core;
    private readonly MapChooserConfig _config;
    private readonly EndOfMapVoteManager _eofManager;
    private readonly Dictionary<string, Func<IPlayer, IMenu?>> _pages = new(StringComparer.Ordinal);

    public MapChooserMenus(ISwiftlyCore core, MapChooserConfig config, EndOfMapVoteManager eofManager)
    {
        _core = core;
        _config = config;
        _eofManager = eofManager;
    }

    /// <summary>What the hub entry with this page id opens: a builder that runs its command's checks, replying in chat, and returns null when one fails.</summary>
    public void SetPage(string pageId, Func<IPlayer, IMenu?> build) => _pages[pageId] = build;

    /// <summary>The hub as this player may use it now, or null when it would have no entries.</summary>
    /// <remarks>Entries are plain submenus with fixed text and their page's id, left out rather than hidden, so a renderer can mirror them as navigation.</remarks>
    public IMenu? BuildHub(IPlayer player)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var entries = new List<IMenuComponent>();

        if (_eofManager.IsVoteActive && !_config.DisableVoteMenuExit && _eofManager.CanVote(player))
            entries.Add(Entry(localizer["map_chooser.hub.vote"], MapChooserMenuIds.Vote));
        if (_config.Rtv.NominationEnabled)
            entries.Add(Entry(localizer["map_chooser.hub.nominate"], MapChooserMenuIds.Nominate));

        var admin = new List<IMenuComponent>();
        AddAdminEntry(admin, player, localizer["map_chooser.hub.change_map"], MapChooserMenuIds.ChangeMap, _config.ChangeMapPermission);
        AddAdminEntry(admin, player, localizer["map_chooser.hub.set_next_map"], MapChooserMenuIds.SetNextMap, _config.SetNextMapPermission);
        AddAdminEntry(admin, player, localizer["map_chooser.hub.start_vote"], MapChooserMenuIds.StartVote, _config.MapsVotePermission);
        AddAdminEntry(admin, player, localizer["map_chooser.hub.cycle"], MapChooserMenuIds.Cycle, _config.Cycle.CycleMenuPermission);

        if (entries.Count > 0 && admin.Count > 0)
            entries.Add(new DividerComponent());
        entries.AddRange(admin);

        if (entries.Count == 0) return null;

        var builder = _core.Menu.CreateMenu(MapChooserMenuIds.Hub);
        builder.AddHeader(new TitleComponent(localizer["map_chooser.hub.title"]) { Overflow = MenuTextOverflow.None });
        foreach (var entry in entries)
            builder.Add(entry);

        return builder.Build();
    }

    /// <summary>Opens a page as the hub's child; on its own when the player's hub would be empty.</summary>
    public IMenuSession Open(IPlayer player, IMenu page)
    {
        page.Parent = BuildHub(player);
        return page.Open(player);
    }

    private void AddAdminEntry(List<IMenuComponent> entries, IPlayer player, string text, string pageId, string permission)
    {
        if (HasPermission(player, permission))
            entries.Add(Entry(text, pageId, permission));
    }

    // SubmenuComponent makes the hub the parent of the page it opens; a null page cancels the click.
    private SubmenuComponent Entry(string text, string pageId, string? permission = null)
        => new(text, session => BuildPage(session.Player, pageId, permission), pageId);

    private IMenu? BuildPage(IPlayer player, string pageId, string? permission)
    {
        // Again on the click: a permission can be taken away while the hub is open.
        if (permission is not null && !HasPermission(player, permission)) return null;
        return _pages.TryGetValue(pageId, out var build) ? build(player) : null;
    }

    // SwiftlyS2's rule for a command's permission: a blank one lets everyone in.
    private bool HasPermission(IPlayer player, string permission)
        => string.IsNullOrWhiteSpace(permission) || _core.Permission.PlayerHasPermission(player.SteamID, permission);
}
