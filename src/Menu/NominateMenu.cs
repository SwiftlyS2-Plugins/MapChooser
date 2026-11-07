using MapChooser.Contract;
using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;
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

    public IMenu Build(IPlayer player, Action<IPlayer, string> onNominate)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        _core.TryGetEngineSnapshot(out var currentMapId, out var currentWorkshopId, out _);
        var builder = _core.Menu.CreateMenu(MapChooserMenuIds.Nominate);
        builder.AddHeader(new TitleComponent(localizer["map_chooser.nominate.title"] ?? "Nominate a map:") { Overflow = MenuTextOverflow.None });
        var playerCount = MapAvailability.CountHumans(_core);
        foreach (var map in _mapLister.Maps)
        {
            var availability = MapAvailability.Of(map, currentMapId, currentWorkshopId, _mapCooldown, playerCount);
            if (availability == MapChooserAvailability.CurrentMap) continue;

            var option = new ButtonComponent(map.Name, ctx =>
            {
                _core.Scheduler.NextTick(() => {
                    onNominate(ctx.Player, map.Name);
                    ctx.Session.Dismiss();
                });
                return ValueTask.CompletedTask;
            });
            option.Style = option.Style.WithColor("#90EE90");
            // A row's Tag is the map's name, which IMapChooserApi.GetMap resolves.
            option.Tag = map.Name;
            if (Reason(localizer, map, availability) is { } reason)
            {
                option.Enabled = false;
                option.Comment = reason;
            }

            builder.Add(option);
        }

        return builder.Build().WithDefaultOption();
    }

    private static string? Reason(ILocalizer localizer, Map map, MapChooserAvailability availability) => availability switch
    {
        MapChooserAvailability.Cooldown => localizer["map_chooser.nominate.reason.cooldown"],
        MapChooserAvailability.TooFewPlayers => localizer["map_chooser.nominate.reason.too_few", map.MinPlayers],
        MapChooserAvailability.TooManyPlayers => localizer["map_chooser.nominate.reason.too_many", map.MaxPlayers],
        _ => null,
    };
}
