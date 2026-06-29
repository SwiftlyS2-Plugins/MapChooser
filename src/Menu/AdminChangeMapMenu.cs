using MapChooser.Models;
using MapChooser.Helpers;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
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

    public void Show(IPlayer player, Action<IPlayer, string> onChangeMap)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var builder = _core.MenusAPI.CreateBuilder();
        builder.Design.SetMenuTitle(localizer["map_chooser.change_map.title"] ?? "Change map to:");
        foreach (var map in _mapLister.Maps)
        {
            var option = new ButtonMenuOption($"<font color='lightgreen'>{map.Name}</font>");
            option.Click += (sender, args) =>
            {
                _core.Scheduler.NextTick(() => {
                    onChangeMap(args.Player, map.Name);
                    var currentMenu = _core.MenusAPI.GetCurrentMenu(args.Player);
                    if (currentMenu != null)
                    {
                        _core.MenusAPI.CloseMenuForPlayer(args.Player, currentMenu);
                    }
                });
                return ValueTask.CompletedTask;
            };

            builder.AddOption(option);
        }

        var menu = builder.Build();
        _core.MenusAPI.OpenMenuForPlayer(player, menu);
    }
}
