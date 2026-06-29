using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using System.Threading.Tasks;

namespace MapChooser.Menu;

public class NominateMenu
{
    private readonly ISwiftlyCore _core;
    private readonly MapLister _mapLister;
    private readonly MapCooldown _mapCooldown;

    public NominateMenu(ISwiftlyCore core, MapLister mapLister, MapCooldown mapCooldown)
    {
        _core = core;
        _mapLister = mapLister;
        _mapCooldown = mapCooldown;
    }

    public void Show(IPlayer player, Action<IPlayer, string> onNominate)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        _core.TryGetEngineSnapshot(out var currentMapId, out var currentWorkshopId, out _);
        var builder = _core.MenusAPI.CreateBuilder();
        builder.Design.SetMenuTitle(localizer["map_chooser.nominate.title"] ?? "Nominate a map:");
        var playerCount = _core.PlayerManager.GetAllPlayers()
            .Count(p => p.IsValid && !p.IsFakeClient);
        foreach (var map in _mapLister.Maps)
        {
            if (IsCurrentMap(map, currentMapId, currentWorkshopId)) continue;
            if (_mapCooldown.IsMapInCooldown(map)) continue;
            if (!map.IsValidForPlayerCount(playerCount)) continue;

            var option = new ButtonMenuOption($"<font color='lightgreen'>{map.Name}</font>");
            option.Click += (sender, args) =>
            {
                _core.Scheduler.NextTick(() => {
                    onNominate(args.Player, map.Name);
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

    private static bool IsCurrentMap(Map map, string? currentMapId, string? currentWorkshopId)
    {
        if (string.IsNullOrEmpty(currentMapId) && string.IsNullOrEmpty(currentWorkshopId)) return false;
        if (map.Id != null)
        {
            if (!string.IsNullOrEmpty(currentMapId) && map.Id.Equals(currentMapId, StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.IsNullOrEmpty(currentWorkshopId) && map.Id.Equals(currentWorkshopId, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
