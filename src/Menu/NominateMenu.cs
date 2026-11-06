using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
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
        var builder = _core.Menu.CreateMenu("mapchooser.nominate");
        builder.AddHeader(new TitleComponent(localizer["map_chooser.nominate.title"] ?? "Nominate a map:") { Overflow = MenuTextOverflow.None });
        var playerCount = _core.PlayerManager.GetAllPlayers()
            .Count(p => p.IsValid && !p.IsFakeClient);
        foreach (var map in _mapLister.Maps)
        {
            if (IsCurrentMap(map, currentMapId, currentWorkshopId)) continue;
            if (_mapCooldown.IsMapInCooldown(map)) continue;
            if (!map.IsValidForPlayerCount(playerCount)) continue;

            var option = new ButtonComponent(map.Name, ctx =>
            {
                _core.Scheduler.NextTick(() => {
                    onNominate(ctx.Player, map.Name);
                    ctx.Session.Close();
                });
                return ValueTask.CompletedTask;
            });
            option.Style = option.Style.WithColor("#90EE90");
            // A row's Tag is the map's name, which IMapChooserApi.GetMap resolves.
            option.Tag = map.Name;

            builder.Add(option);
        }

        var menu = builder.Build().WithDefaultOption();
        menu.Open(player);
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
