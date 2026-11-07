using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Players;
using MapChooser.Menu;

namespace MapChooser.Commands;

public class AdminMapsVoteCommand
{
    private readonly ISwiftlyCore _core;
    private readonly PluginState _state;
    private readonly MapLister _mapLister;
    private readonly EndOfMapVoteManager _eofManager;
    private readonly MapChooserConfig _config;
    private readonly MapChooserMenus _menus;

    public AdminMapsVoteCommand(ISwiftlyCore core, PluginState state, MapLister mapLister, EndOfMapVoteManager eofManager, MapChooserConfig config, MapChooserMenus menus)
    {
        _core = core;
        _state = state;
        _mapLister = mapLister;
        _eofManager = eofManager;
        _config = config;
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
        _menus.Open(player, BuildMenu(player));
    }

    /// <summary>The page that starts a vote on the maps an admin ticks; the command's permission is the only check before it.</summary>
    public IMenu BuildMenu(IPlayer player) => new AdminMapsVoteMenu(_core, _mapLister, _eofManager).Build(player,
        maps => _eofManager.ScheduleCustomVote(maps, CountdownSeconds, _config.EndOfMap.VoteDuration));

    private const int CountdownSeconds = 5;
}
