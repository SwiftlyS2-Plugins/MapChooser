using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Players;
using System.Threading.Tasks;

namespace MapChooser.Menu;

public class VotemapMenu
{
    private readonly ISwiftlyCore _core;
    private readonly MapLister _mapLister;
    private readonly MapCooldown _mapCooldown;

    public VotemapMenu(ISwiftlyCore core, MapLister mapLister, MapCooldown mapCooldown)
    {
        _core = core;
        _mapLister = mapLister;
        _mapCooldown = mapCooldown;
    }

    public void Show(IPlayer player, Action<IPlayer, string> onVote)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var currentMapName = _core.ConVar.FindAsString("mapname")?.ValueAsString;
        var builder = _core.Menu.CreateMenu("mapchooser.votemap");
        builder.AddHeader(new TitleComponent(localizer["map_chooser.votemap.title"] ?? "Vote for the next map:") { Overflow = MenuTextOverflow.None });
        var playerCount = _core.PlayerManager.GetAllPlayers()
            .Count(p => p.IsValid && !p.IsFakeClient);
        foreach (var map in _mapLister.Maps)
        {
            if (!string.IsNullOrEmpty(currentMapName) && map.Id != null && map.Id.Equals(currentMapName, StringComparison.OrdinalIgnoreCase)) continue;
            if (_mapCooldown.IsMapInCooldown(map)) continue;
            if (!map.IsValidForPlayerCount(playerCount)) continue;

            var option = new ButtonComponent(map.Name, ctx =>
            {
                _core.Scheduler.NextTick(() => {
                    onVote(ctx.Player, map.Name);
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
}
