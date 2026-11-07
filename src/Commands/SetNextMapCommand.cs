using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Players;
using MapChooser.Menu;

namespace MapChooser.Commands;

public class SetNextMapCommand
{
    private readonly ISwiftlyCore _core;
    private readonly PluginState _state;
    private readonly MapLister _mapLister;
    private readonly ChangeMapManager _changeMapManager;
    private readonly MapChooserMenus _menus;

    public SetNextMapCommand(ISwiftlyCore core, PluginState state, MapLister mapLister, ChangeMapManager changeMapManager, MapChooserMenus menus)
    {
        _core = core;
        _state = state;
        _mapLister = mapLister;
        _changeMapManager = changeMapManager;
        _menus = menus;
    }

    public void Execute(ICommandContext context)
    {
        // Permission check is handled by registration
        
        string? mapNameArg = context.Args.Length > 0 ? context.Args[0] : null;

        if (context.IsSentByPlayer)
        {
            var player = context.Sender!;
            if (string.IsNullOrEmpty(mapNameArg))
            {
                _menus.Open(player, BuildMenu(player));
                return;
            }
        }
        else if (string.IsNullOrEmpty(mapNameArg))
        {
            context.Reply("Usage: setnextmap <mapname>");
            return;
        }

        // Handle map argument (fuzzy match)
        var map = _mapLister.Maps.FirstOrDefault(m => m.Name.Equals(mapNameArg, StringComparison.OrdinalIgnoreCase));
        if (map == null)
        {
             map = _mapLister.Maps.FirstOrDefault(m => m.Name.Contains(mapNameArg!, StringComparison.OrdinalIgnoreCase));
        }

        if (map == null)
        {
            string notFoundMsg = _core.Localizer["map_chooser.nominate.not_found", mapNameArg!] ?? $"Map '{mapNameArg}' not found.";
            if (context.IsSentByPlayer)
            {
                 context.Sender!.SendChat(_core.Localizer["map_chooser.prefix"] + " " + notFoundMsg);
            }
            else
            {
                context.Reply(notFoundMsg);
            }
            return;
        }

        _changeMapManager.ScheduleMapChange(map.Name);
        
        // Optional: Notify that admin set the next map?
        // ChangeMapManager already notifies "Next map will be X"
    }

    /// <summary>The set-next-map page; the command's permission is the only check before it.</summary>
    public IMenu BuildMenu(IPlayer player) => new SetNextMapMenu(_core, _mapLister).Build(player, (p, selectedMap) =>
    {
        _changeMapManager.ScheduleMapChange(selectedMap);
        // Log to console or admins?
    });
}
