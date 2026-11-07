using MapChooser.Contract;
using MapChooser.Models;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Players;

namespace MapChooser.Menu;

public class AdminChangeMapMenu
{
    private readonly ISwiftlyCore _core;
    private readonly MapLister _mapLister;

    public AdminChangeMapMenu(ISwiftlyCore core, MapLister mapLister)
    {
        _core = core;
        _mapLister = mapLister;
    }

    public IMenu Build(IPlayer player, Action<IPlayer, string> onChangeMap)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var builder = _core.Menu.CreateMenu(MapChooserMenuIds.ChangeMap);
        builder.AddHeader(new TitleComponent(localizer["map_chooser.change_map.title"] ?? "Change map to:") { Overflow = MenuTextOverflow.None });
        foreach (var map in _mapLister.Maps)
        {
            var option = new ButtonComponent(map.Name, ctx =>
            {
                _core.Scheduler.NextTick(() => {
                    onChangeMap(ctx.Player, map.Name);
                    ctx.Session.Dismiss();
                });
                return ValueTask.CompletedTask;
            });
            option.Style = option.Style.WithColor("#90EE90");
            // A row's Tag is the map's name, which IMapChooserApi.GetMap resolves.
            option.Tag = map.Name;

            builder.Add(option);
        }

        return builder.Build().WithDefaultOption();
    }
}
