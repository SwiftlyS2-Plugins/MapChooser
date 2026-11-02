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

    public void Show(IPlayer player, Action<IPlayer, string> onSelect)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var currentMapName = _core.ConVar.FindAsString("mapname")?.ValueAsString;
        var builder = _core.Menu.CreateMenu("mapchooser.set-next-map");
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
                    ctx.Session.Close();
                });
                return ValueTask.CompletedTask;
            });
            option.Style = option.Style.WithColor("#90EE90");

            builder.Add(option);
        }

        var menu = builder.Build().WithDefaultOption();
        menu.Open(player);
    }
}
