using MapChooser.Models;
using MapChooser.Helpers;
using MapChooser.Dependencies;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Players;

namespace MapChooser.Commands;

public class RevoteCommand
{
    private readonly ISwiftlyCore _core;
    private readonly PluginState _state;
    private readonly EndOfMapVoteManager _eofManager;
    private readonly MapChooserConfig _config;

    public RevoteCommand(ISwiftlyCore core, PluginState state, EndOfMapVoteManager eofManager, MapChooserConfig config)
    {
        _core = core;
        _state = state;
        _eofManager = eofManager;
        _config = config;
    }

    public void Execute(ICommandContext context)
    {
        if (!context.IsSentByPlayer) return;

        var player = context.Sender!;
        if (!CanRevote(player)) return;

        _eofManager.OpenVoteMenu(player);
    }

    /// <summary>The ballot <c>!revote</c> reopens, behind its checks; null, with the reason in chat, when one fails.</summary>
    public IMenu? BuildMenu(IPlayer player) => CanRevote(player) ? _eofManager.BuildBallot(player) : null;

    private bool CanRevote(IPlayer player)
    {
        var localizer = _core.Translation.GetPlayerLocalizer(player);

        if (!_config.AllowSpectatorsToVote && player.Controller?.TeamNum == 1)
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.spectator"]);
            return false;
        }

        if (!_state.EofVoteHappening)
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.revote.no_vote_active"]);
            return false;
        }

        return true;
    }
}
