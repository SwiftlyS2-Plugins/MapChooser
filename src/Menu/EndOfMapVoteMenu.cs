using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Players;
using System.Threading.Tasks;

namespace MapChooser.Menu;

public class EndOfMapVoteMenu
{
    private const string CustomHudRendererId = "customhud.panorama";

    private readonly ISwiftlyCore _core;
    private readonly MapCooldown _mapCooldown;

    public EndOfMapVoteMenu(ISwiftlyCore core, MapCooldown mapCooldown)
    {
        _core = core;
        _mapCooldown = mapCooldown;
    }

    public IMenu Show(IPlayer player, List<string> mapsInVote, IReadOnlyDictionary<string, int> votes,
        IReadOnlyDictionary<int, string> playerVotes, Action<IPlayer, string> onVote, bool disableExit = false)
    {
        var menu = Build(player, mapsInVote, votes, playerVotes, onVote, disableExit);
        menu.Open(player);
        return menu;
    }

    // The tally a voter keeps, with nothing selectable: E, Shift and F also act in game, and must not
    // cast a new vote. Null when the menu would not draw through CustomHUD, which alone shows counts.
    public IMenu? ShowTally(IPlayer player, List<string> mapsInVote, IReadOnlyDictionary<string, int> votes,
        IReadOnlyDictionary<int, string> playerVotes)
    {
        var menu = Build(player, mapsInVote, votes, playerVotes, onVote: null, disableExit: false);
        if (!string.Equals(menu.Renderer.Id, CustomHudRendererId, StringComparison.OrdinalIgnoreCase))
        {
            menu.Dispose();
            return null;
        }

        menu.Open(player);
        return menu;
    }

    private IMenu Build(IPlayer player, List<string> mapsInVote, IReadOnlyDictionary<string, int> votes,
        IReadOnlyDictionary<int, string> playerVotes, Action<IPlayer, string>? onVote, bool disableExit)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        // WithAction registers the unbound Close for the whole menu id, so the closable variant needs an id of its own.
        var builder = _core.Menu.CreateMenu(disableExit ? "mapchooser.eof-vote.locked" : "mapchooser.eof-vote");
        builder.AddHeader(new TitleComponent(localizer["map_chooser.vote.title"] ?? "Vote for the next map:") { Overflow = MenuTextOverflow.None });
        if (disableExit)
        {
            builder.WithAction(MenuActions.Close.Name, MenuKey.None);
        }
        foreach (var map in mapsInVote)
        {
            string displayName = map;
            bool isExtend = map == "map_chooser.extend_option";
            if (isExtend)
            {
                displayName = localizer["map_chooser.extend_option"];
            }

            Func<MenuActionContext, ValueTask>? select = null;
            if (onVote is not null)
            {
                select = ctx =>
                {
                    _core.Scheduler.NextTick(() => {
                        onVote(ctx.Player, map);
                    });
                    return ValueTask.CompletedTask;
                };
            }

            var option = new VoteOptionComponent(displayName, map, votes, playerVotes, select) { ReadOnly = onVote is null };
            option.Style = option.Style.WithColor(isExtend ? "#FFA500" : "#90EE90");
            option.Enabled = isExtend || !_mapCooldown.IsMapInCooldown(map);

            builder.Add(option);
        }

        var menu = builder.Build().WithDefaultOption();
        menu.Tag = "EofVoteMenu";
        return menu;
    }

    // CustomHUD draws the count, a share bar and the player's own pick from the raw node; other renderers ignore it.
    private sealed class VoteOptionComponent(string text, string map, IReadOnlyDictionary<string, int> votes,
        IReadOnlyDictionary<int, string> playerVotes, Func<MenuActionContext, ValueTask>? onSelect) : ButtonComponent(text, onSelect)
    {
        public bool ReadOnly { get; init; }

        public override bool IsFocusable => !ReadOnly;

        public override bool NeedsRedraw(IMenuSession session, DateTime now)
            => base.NeedsRedraw(session, now) || session.GetState<TallyState>(this).Shown != Tally(session);

        public override MenuNode Render(IMenuComponentRenderContext context)
        {
            var (count, total, chosen) = context.Session.GetState<TallyState>(this).Shown = Tally(context.Session);
            var share = total > 0 ? (int)Math.Round(100.0 * count / total, MidpointRounding.AwayFromZero) : 0;
            var raw = new MenuRawNode(CustomHudRendererId, $"text=\t{count};bar={share}" + (chosen ? ";chosen" : ""));
            var node = base.Render(context);
            return node is MenuLineNode line ? line with { Children = [.. line.Children, raw] } : MenuLineNode.Of(node, raw);
        }

        private (int Count, int Total, bool Chosen) Tally(IMenuSession session)
            => (votes.GetValueOrDefault(map), votes.Values.Sum(),
                playerVotes.TryGetValue(session.Player.Slot, out var vote) && vote == map);

        private sealed class TallyState
        {
            public (int Count, int Total, bool Chosen) Shown = (-1, -1, false);
        }
    }
}
