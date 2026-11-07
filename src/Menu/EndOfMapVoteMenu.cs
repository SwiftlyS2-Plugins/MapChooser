using MapChooser.Contract;
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
    private readonly ISwiftlyCore _core;
    private readonly MapCooldown _mapCooldown;

    public EndOfMapVoteMenu(ISwiftlyCore core, MapCooldown mapCooldown)
    {
        _core = core;
        _mapCooldown = mapCooldown;
    }

    public IMenu Build(IPlayer player, List<string> mapsInVote, Action<IPlayer, string> onVote, bool disableExit = false)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        // WithAction registers the unbound Close for the whole menu id, so the closable variant needs an id of its own.
        var builder = _core.Menu.CreateMenu(disableExit ? MapChooserMenuIds.LockedVote : MapChooserMenuIds.Vote);
        builder.AddHeader(new TitleComponent(localizer["map_chooser.vote.title"] ?? "Vote for the next map:") { Overflow = MenuTextOverflow.None });
        if (disableExit)
        {
            builder.WithAction(MenuActions.Close.Name, MenuKey.None);
        }
        foreach (var map in mapsInVote)
        {
            string displayName = map;
            bool isExtend = map == IMapChooserApi.ExtendOption;
            if (isExtend)
            {
                displayName = localizer["map_chooser.extend_option"];
            }

            Func<MenuActionContext, ValueTask> select = ctx =>
            {
                _core.Scheduler.NextTick(() => {
                    onVote(ctx.Player, map);
                });
                return ValueTask.CompletedTask;
            };

            var option = new ButtonComponent(displayName, select)
            {
                // A row's Tag is the option it stands for, so any renderer can identify it.
                Tag = map,
            };
            option.Style = option.Style.WithColor(isExtend ? "#FFA500" : "#90EE90");
            option.Enabled = isExtend || !_mapCooldown.IsMapInCooldown(map);

            builder.Add(option);
        }

        var menu = builder.Build().WithDefaultOption();
        menu.Tag = "EofVoteMenu";
        return menu;
    }
}
