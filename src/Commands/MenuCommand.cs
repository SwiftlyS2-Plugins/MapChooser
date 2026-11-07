using MapChooser.Menu;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;

namespace MapChooser.Commands;

public class MenuCommand
{
    private readonly ISwiftlyCore _core;
    private readonly MapChooserMenus _menus;

    public MenuCommand(ISwiftlyCore core, MapChooserMenus menus)
    {
        _core = core;
        _menus = menus;
    }

    public void Execute(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("This command can only be used by players.");
            return;
        }

        var player = context.Sender!;
        if (_menus.BuildHub(player) is { } hub)
        {
            hub.Open(player);
            return;
        }

        var localizer = _core.Translation.GetPlayerLocalizer(player);
        player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.hub.empty"]);
    }
}
