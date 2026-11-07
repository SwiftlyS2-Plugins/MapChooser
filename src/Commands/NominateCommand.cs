using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Players;
using System.Threading.Tasks;

using MapChooser.Menu;
namespace MapChooser.Commands;

public class NominateCommand
{
    private readonly ISwiftlyCore _core;
    private readonly PluginState _state;
    private readonly MapLister _mapLister;
    private readonly MapCooldown _mapCooldown;
    private readonly MapChooserConfig _config;
    private readonly MapChooserMenus _menus;


    public NominateCommand(ISwiftlyCore core, PluginState state, MapLister mapLister, MapCooldown mapCooldown, MapChooserConfig config, MapChooserMenus menus)
    {
        _core = core;
        _state = state;
        _mapLister = mapLister;
        _mapCooldown = mapCooldown;
        _config = config;
        _menus = menus;
    }

    public void Execute(ICommandContext context)
    {
        if (!context.IsSentByPlayer) return;
        
        var player = context.Sender!;
        if (!CanNominate(player)) return;

        string? mapName = context.Args.Length > 0 ? context.Args[0] : null;

        if (string.IsNullOrEmpty(mapName))
        {
            _menus.Open(player, BuildPage(player));
        }
        else
        {
            HandleNomination(player, mapName);
        }
    }

    /// <summary>The nominate page, behind the checks <c>!nominate</c> runs first; null, with the reason in chat, when one fails.</summary>
    public IMenu? BuildMenu(IPlayer player) => CanNominate(player) ? BuildPage(player) : null;

    private IMenu BuildPage(IPlayer player) => new NominateMenu(_core, _mapLister, _mapCooldown).Build(player, HandleNomination);

    private bool CanNominate(IPlayer player)
    {
        if (!_config.Rtv.NominationEnabled) return false;

        if (_state.WarmupRunning)
        {
            var localizer = _core.Translation.GetPlayerLocalizer(player);
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.warmup"]);
            return false;
        }

        if (!_config.AllowSpectatorsToVote && player.Controller?.TeamNum == 1)
        {
            var localizer = _core.Translation.GetPlayerLocalizer(player);
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.spectator"]);
            return false;
        }

        return true;
    }

    private void HandleNomination(IPlayer player, string mapName)
    {
        if (!_config.AllowSpectatorsToVote && player.Controller?.TeamNum == 1)
        {
            var local = _core.Translation.GetPlayerLocalizer(player);
            player.SendChat(local["map_chooser.prefix"] + " " + local["map_chooser.general.validation.spectator"]);
            return;
        }
        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var currentMapName = _core.ConVar.FindAsString("mapname")?.ValueAsString;
        var map = _mapLister.Maps.FirstOrDefault(m => m.Name.Contains(mapName, StringComparison.OrdinalIgnoreCase) || (m.Id != null && m.Id.Equals(mapName, StringComparison.OrdinalIgnoreCase)));
        if (map == null)
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.nominate.not_found", mapName]);
            return;
        }

        if (!string.IsNullOrEmpty(currentMapName) && map.Id != null && map.Id.Equals(currentMapName, StringComparison.OrdinalIgnoreCase))
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.nominate.current_map"]);
            return;
        }

        if (_mapCooldown.IsMapInCooldown(map))
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.votemap.cooldown", map.Name]);
            return;
        }

        var playerCount = _core.PlayerManager.GetAllPlayers()
            .Count(p => p.IsValid && !p.IsFakeClient);
        if (!map.IsValidForPlayerCount(playerCount))
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.player_count", map.Name]);
            return;
        }

        _state.Nominate(player.Slot, map.Name);
        _core.PlayerManager.SendChat(localizer["map_chooser.prefix"] + " " + _core.Localizer["map_chooser.nominate.success", player.Controller?.PlayerName ?? "Unknown", map.Name]);
    }

    public List<string> GetNominations()
    {
        return _state.Nominations.Values.Distinct().ToList();
    }

    public void Clear()
    {
        _state.ClearNominations();
    }
}
