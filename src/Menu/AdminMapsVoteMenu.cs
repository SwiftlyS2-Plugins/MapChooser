using MapChooser.Contract;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;
using System.Threading.Tasks;

namespace MapChooser.Menu;

/// <summary>The Start a vote page: every map as a toggle, and a button that starts a vote on the ones ticked.</summary>
public class AdminMapsVoteMenu
{
    private readonly ISwiftlyCore _core;
    private readonly MapLister _mapLister;
    private readonly EndOfMapVoteManager _eofManager;

    public AdminMapsVoteMenu(ISwiftlyCore core, MapLister mapLister, EndOfMapVoteManager eofManager)
    {
        _core = core;
        _mapLister = mapLister;
        _eofManager = eofManager;
    }

    /// <param name="startVote">Starts the vote on the ticked maps; false when a vote is already running or counting down.</param>
    public IMenu Build(IPlayer player, Func<List<string>, bool> startVote)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var builder = _core.Menu.CreateMenu(MapChooserMenuIds.StartVote);
        builder.AddHeader(new TitleComponent(localizer["map_chooser.admin_vote.title"]) { Overflow = MenuTextOverflow.None });

        // A toggle flips in place, so ticking a map leaves the page where the player had it.
        // A row's Tag is the map's name, which IMapChooserApi.GetMap resolves.
        var toggles = _mapLister.Maps.Select(map => new ToggleComponent(map.Name) { Tag = map.Name }).ToList();
        List<string> Ticked(IMenuSession session) => toggles.Where(t => t.GetValue(session)).Select(t => (string)t.Tag!).ToList();

        var startText = localizer["map_chooser.admin_vote.start"];
        var start = new StartButton(_ => Blocked(localizer) ?? localizer["map_chooser.admin_vote.description"], ctx =>
        {
            var maps = Ticked(ctx.Session);
            _core.Scheduler.NextTick(() =>
            {
                if (maps.Count == 0) return;
                if (!startVote(maps))
                {
                    ctx.Player.SendChat(localizer["map_chooser.prefix"] + " " + (Blocked(localizer) ?? localizer["map_chooser.admin_vote.running"]));
                    return;
                }
                ctx.Session.Dismiss();
            });
            return ValueTask.CompletedTask;
        })
        {
            TextProvider = session => Ticked(session).Count is var count and > 0 ? $"{startText} ({count})" : startText,
            EnabledWhen = session => Blocked(localizer) is null && toggles.Any(t => t.GetValue(session)),
        };
        start.Style = start.Style.WithColor("#FFA500");
        builder.Add(start);

        foreach (var toggle in toggles)
            builder.Add(toggle);

        return builder.Build();
    }

    // Why no vote can be started now, or null when one can.
    private string? Blocked(ILocalizer localizer)
        => _eofManager.IsVoteActive ? localizer["map_chooser.admin_vote.running"]
            : _eofManager.IsVoteScheduled ? localizer["map_chooser.admin_vote.scheduled"]
            : null;

    // Its hint says what it does, or why it can't; a vote opening or closing elsewhere redraws it.
    private sealed class StartButton(Func<IMenuSession, string> hint, Func<MenuActionContext, ValueTask> onSelect)
        : ButtonComponent(onSelect: onSelect)
    {
        private string? _drawn;

        public override string? GetHint(IMenuSession session, Func<string, string> translate) => hint(session);

        public override bool NeedsRedraw(IMenuSession session, DateTime now)
            => base.NeedsRedraw(session, now) || State(session) != _drawn;

        public override MenuNode Render(IMenuComponentRenderContext context)
        {
            _drawn = State(context.Session);
            return base.Render(context);
        }

        private string State(IMenuSession session) => $"{IsEnabled(session)}|{hint(session)}";
    }
}