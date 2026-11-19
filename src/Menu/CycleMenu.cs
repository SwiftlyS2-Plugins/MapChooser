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
        // Unbound: a server binds them in menus.jsonc, and a renderer may offer them on each row.
        DeclareAction(builder, MapChooserCycleActions.MoveUp, localizer["map_chooser.cycle.action_move_up"], 10);
        DeclareAction(builder, MapChooserCycleActions.MoveDown, localizer["map_chooser.cycle.action_move_down"], 11);
        DeclareAction(builder, MapChooserCycleActions.Remove, localizer["map_chooser.cycle.action_remove"], 12);

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
                var option = new MapRow(label, ctx =>
                {
                    var list = ctx.Menu;
                    _core.Scheduler.NextTick(() => ShowMapActions(ctx.Player, mapCapture, list));
                    return System.Threading.Tasks.ValueTask.CompletedTask;
                }, ctx => ActionFor(ctx, mapCapture));
                option.Style = option.Style.WithColor(isCurrent ? "#FFFF00" : isNext ? "#90EE90" : "#FFFFFF");
                // A row's Tag is the map's name, which IMapChooserApi.GetMap resolves.
                option.Tag = map.Name;
                builder.Add(option);
            }
        }

        return builder.Build();
    }

    // Core's traversal actions are ordered 0-5, so a bound one's footer hint follows them.
    private static void DeclareAction(IMenuBuilder builder, string name, string label, int order)
        => builder.WithAction(new MenuActionDescriptor
        {
            Id = new MenuActionId(MapChooserMenuIds.Cycle, name),
            DefaultKey = MenuKey.None,
            Label = label,
            Order = order,
        });

    private Action? ActionFor(MenuActionContext ctx, Map map)
    {
        var name = ctx.Action.Name;
        var index = IndexOf(map.Name);
        var player = ctx.Player;
        var list = ctx.Menu;
        if (Is(name, MapChooserCycleActions.MoveUp))
            return index > 0 ? () => Move(player, map, list.Parent, up: true) : null;
        if (Is(name, MapChooserCycleActions.MoveDown))
            return index >= 0 && index < _mapLister.Maps.Count - 1 ? () => Move(player, map, list.Parent, up: false) : null;
        if (Is(name, MapChooserCycleActions.Remove))
            return index >= 0 ? () => _core.Scheduler.NextTick(() => ShowRemoveConfirm(player, map, list)) : null;
        return null;
    }

    private static bool Is(string action, string name) => string.Equals(action, name, StringComparison.OrdinalIgnoreCase);

    private int IndexOf(string name)
        => _mapLister.Maps.ToList().FindIndex(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private void Move(IPlayer player, Map map, IMenu? listParent, bool up)
        => _core.Scheduler.NextTick(() =>
        {
            if (up) _cycleManager.MoveMapUp(map.Name);
            else _cycleManager.MoveMapDown(map.Name);
            ShowList(player, listParent, map.Name);
        });

    // Focus lands back on a map just moved, so a bound key moves it on.
    private void ShowList(IPlayer player, IMenu? parent, string? focus = null)
    {
        var menu = Build(player);
        menu.Parent = parent;
        var session = menu.Open(player);
        var index = focus is null ? -1 : IndexOf(focus);
        if (index > 0) session.SetFocus(index);
    }

    // The map's pages open under the list, so their close key returns to it; a change opens a fresh list.
    private void ShowMapActions(IPlayer player, Map map, IMenu list)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var maps = _mapLister.Maps;
        var listParent = list.Parent;
        int idx = maps.ToList().FindIndex(m => m.Name.Equals(map.Name, StringComparison.OrdinalIgnoreCase));

        var builder = _core.Menu.CreateMenu(MapChooserMenuIds.CycleMap);
        builder.AddHeader(new TitleComponent(localizer["map_chooser.cycle.actions_title", map.Name]) { Overflow = MenuTextOverflow.None });

        if (idx > 0)
        {
            var upOption = new ButtonComponent(localizer["map_chooser.cycle.action_move_up"], ctx =>
            {
                Move(ctx.Player, map, listParent, up: true);
                return System.Threading.Tasks.ValueTask.CompletedTask;
            });
            upOption.Style = upOption.Style.WithColor("#ADD8E6");
            builder.Add(upOption);
        }

        if (idx >= 0 && idx < maps.Count - 1)
        {
            var downOption = new ButtonComponent(localizer["map_chooser.cycle.action_move_down"], ctx =>
            {
                Move(ctx.Player, map, listParent, up: false);
                return System.Threading.Tasks.ValueTask.CompletedTask;
            });
            downOption.Style = downOption.Style.WithColor("#ADD8E6");
            builder.Add(downOption);
        }

        var removeOption = new ButtonComponent(localizer["map_chooser.cycle.action_remove"], ctx =>
        {
            _core.Scheduler.NextTick(() => ShowRemoveConfirm(ctx.Player, map, list));
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
        menu.Parent = list;
        menu.Open(player);
    }

    private void ShowRemoveConfirm(IPlayer player, Map map, IMenu list)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var listParent = list.Parent;

        var builder = _core.Menu.CreateMenu(MapChooserMenuIds.CycleRemove);
        builder.AddHeader(new TitleComponent(localizer["map_chooser.cycle.remove_confirm_title", map.Name]) { Overflow = MenuTextOverflow.None });

        var yesOption = new ButtonComponent(localizer["map_chooser.cycle.remove_confirm_yes"], ctx =>
        {
            _core.Scheduler.NextTick(() =>
            {
                bool removed = _cycleManager.RemoveMap(map.Name);
                if (removed)
                    _core.PlayerManager.SendChat(_core.Localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.cycle.map_removed", map.Name]);
                ShowList(ctx.Player, listParent);
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
        menu.Parent = list;
        menu.Open(player);
    }

    private sealed class MapRow(string text, Func<MenuActionContext, ValueTask> select, Func<MenuActionContext, Action?> actionFor)
        : ButtonComponent(text, select)
    {
        public override async ValueTask<bool> HandleActionAsync(MenuActionContext context)
        {
            if (await base.HandleActionAsync(context)) return true;
            if (actionFor(context) is not { } run) return false;
            return await ActivateAsync(context, _ =>
            {
                run();
                return ValueTask.CompletedTask;
            });
        }
    }
}
