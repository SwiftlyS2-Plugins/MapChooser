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

public class SetNextMapMenu
{
    private readonly ISwiftlyCore _core;
    private readonly MapLister _mapLister;

    public SetNextMapMenu(ISwiftlyCore core, MapLister mapLister)
    {
        _core = core;
        _mapLister = mapLister;
    }

    public IMenu Build(IPlayer player, Action<IPlayer, string> onSelect)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var currentMapName = _core.ConVar.FindAsString("mapname")?.ValueAsString;
        var builder = _core.Menu.CreateMenu(MapChooserMenuIds.SetNextMap);
        string title = "Set Next Map:";
        try {
            title = localizer["map_chooser.setnextmap.title"] ?? "Set Next Map:";
        } catch { /* Ignore missing key */ }
        
        builder.AddHeader(new TitleComponent(title) { Overflow = MenuTextOverflow.None });
        
        foreach (var map in _mapLister.Maps)
        {
            if (!string.IsNullOrEmpty(currentMapName) && map.Name.Equals(currentMapName, StringComparison.OrdinalIgnoreCase)) continue;

            var option = new ButtonComponent(map.Name, ctx =>
            {
                _core.Scheduler.NextTick(() => {
                    onSelect(ctx.Player, map.Name);
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
