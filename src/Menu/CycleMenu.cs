using MapChooser.Contract;
using MapChooser.Helpers;
using MapChooser.Models;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Players;

namespace MapChooser.Menu;

public class CycleMenu
{
    private readonly ISwiftlyCore _core;
    private readonly MapLister _mapLister;
    private readonly MapCycleManager _cycleManager;
    private readonly MapChooserConfig _config;

    public CycleMenu(ISwiftlyCore core, MapLister mapLister, MapCycleManager cycleManager, MapChooserConfig config)
    {
        _core = core;
        _mapLister = mapLister;
        _cycleManager = cycleManager;
        _config = config;
    }

    public IMenu Build(IPlayer player)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var maps = _mapLister.Maps;

        var currentMapName = _core.Engine?.GlobalVars.MapName.ToString() ?? "";
        var currentWorkshopId = _core.Engine?.WorkshopId ?? "";

        string modeKey = _config.Cycle.RandomOrder
            ? "map_chooser.cycle.mode_random"
            : "map_chooser.cycle.mode_sequential";

        var builder = _core.Menu.CreateMenu(MapChooserMenuIds.Cycle);
        builder.AddHeader(new TitleComponent(localizer["map_chooser.cycle.menu_title"] + " — " + localizer[modeKey]) { Overflow = MenuTextOverflow.None });

        if (maps.Count == 0)
        {
            var emptyOption = new ButtonComponent(localizer["map_chooser.cycle.no_maps"]);
            builder.Add(emptyOption);
        }
        else
        {
            for (int i = 0; i < maps.Count; i++)
            {
                var map = maps[i];
                bool isCurrent = map.Name.Equals(currentMapName, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(map.Id) && map.Id.Equals(currentMapName, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(map.Id) && !string.IsNullOrEmpty(currentWorkshopId) &&
                     map.Id.Equals(currentWorkshopId, StringComparison.OrdinalIgnoreCase));

                bool isNext = !_config.Cycle.RandomOrder && i == (_cycleManager.CycleIndex + 1) % maps.Count;

                string label = $"{i + 1}. {map.Name}";
                if (isCurrent) label += " ◄";
                else if (isNext) label += " →";

                var mapCapture = map;
                var option = new ButtonComponent(label, ctx =>
                {
                    // The map's pages have no parent of their own; they bring the list back under the one it had.
                    var listParent = ctx.Menu.Parent;
                    _core.Scheduler.NextTick(() => ShowMapActions(ctx.Player, mapCapture, listParent));
                    return System.Threading.Tasks.ValueTask.CompletedTask;
                });
                option.Style = option.Style.WithColor(isCurrent ? "#FFFF00" : isNext ? "#90EE90" : "#FFFFFF");
                // A row's Tag is the map's name, which IMapChooserApi.GetMap resolves.
                option.Tag = map.Name;
                builder.Add(option);
            }
        }

        return builder.Build();
    }

    private void ShowList(IPlayer player, IMenu? parent)
    {
        var menu = Build(player);
        menu.Parent = parent;
        menu.Open(player);
    }

    private void ShowMapActions(IPlayer player, Map map, IMenu? listParent)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var maps = _mapLister.Maps;
        int idx = maps.ToList().FindIndex(m => m.Name.Equals(map.Name, StringComparison.OrdinalIgnoreCase));

        var builder = _core.Menu.CreateMenu("mapchooser.cycle-map-actions");
        builder.AddHeader(new TitleComponent(localizer["map_chooser.cycle.actions_title", map.Name]) { Overflow = MenuTextOverflow.None });

        if (idx > 0)
        {
            var upOption = new ButtonComponent(localizer["map_chooser.cycle.action_move_up"], ctx =>
            {
                _core.Scheduler.NextTick(() =>
                {
                    _cycleManager.MoveMapUp(map.Name);
                    ShowList(ctx.Player, listParent);
                });
                return System.Threading.Tasks.ValueTask.CompletedTask;
            });
            upOption.Style = upOption.Style.WithColor("#ADD8E6");
            builder.Add(upOption);
        }

        if (idx >= 0 && idx < maps.Count - 1)
        {
            var downOption = new ButtonComponent(localizer["map_chooser.cycle.action_move_down"], ctx =>
            {
                _core.Scheduler.NextTick(() =>
                {
                    _cycleManager.MoveMapDown(map.Name);
                    ShowList(ctx.Player, listParent);
                });
                return System.Threading.Tasks.ValueTask.CompletedTask;
            });
            downOption.Style = downOption.Style.WithColor("#ADD8E6");
            builder.Add(downOption);
        }

        var removeOption = new ButtonComponent(localizer["map_chooser.cycle.action_remove"], ctx =>
        {
            _core.Scheduler.NextTick(() => ShowRemoveConfirm(ctx.Player, map, listParent));
            return System.Threading.Tasks.ValueTask.CompletedTask;
        });
        removeOption.Style = removeOption.Style.WithColor("#FF0000");
        builder.Add(removeOption);

        var backOption = new ButtonComponent(localizer["map_chooser.cycle.action_back"], ctx =>
        {
            _core.Scheduler.NextTick(() => ShowList(ctx.Player, listParent));
            return System.Threading.Tasks.ValueTask.CompletedTask;
        });
        backOption.Style = backOption.Style.WithColor("#808080");
        builder.Add(backOption);

        var menu = builder.Build();
        menu.Open(player);
    }

    private void ShowRemoveConfirm(IPlayer player, Map map, IMenu? listParent)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);

        var builder = _core.Menu.CreateMenu("mapchooser.cycle-remove-confirm");
        builder.AddHeader(new TitleComponent(localizer["map_chooser.cycle.remove_confirm_title", map.Name]) { Overflow = MenuTextOverflow.None });

        var yesOption = new ButtonComponent(localizer["map_chooser.cycle.remove_confirm_yes"], ctx =>
        {
            _core.Scheduler.NextTick(() =>
            {
                ctx.Session.Close();

                bool removed = _cycleManager.RemoveMap(map.Name);
                if (removed)
                    _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.cycle.map_removed", map.Name]);
            });
            return System.Threading.Tasks.ValueTask.CompletedTask;
        });
        yesOption.Style = yesOption.Style.WithColor("#FF0000");

        var cancelOption = new ButtonComponent(localizer["map_chooser.cycle.remove_cancel"], ctx =>
        {
            _core.Scheduler.NextTick(() => ShowList(ctx.Player, listParent));
            return System.Threading.Tasks.ValueTask.CompletedTask;
        });
        cancelOption.Style = cancelOption.Style.WithColor("#90EE90");

        builder.Add(yesOption);
        builder.Add(cancelOption);

        var menu = builder.Build();
        menu.Open(player);
    }
}
