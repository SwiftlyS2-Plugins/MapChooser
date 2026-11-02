using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using System.Threading.Tasks;

using MapChooser.Menu;
namespace MapChooser.Commands;

public class VotemapCommand
{
    private readonly ISwiftlyCore _core;
    private readonly PluginState _state;
    private readonly MapLister _mapLister;
    private readonly MapCooldown _mapCooldown;
    private readonly ChangeMapManager _changeMapManager;
    private readonly MapChooserConfig _config;
    private readonly Dictionary<string, VoteManager> _mapVotes = new();

    public VotemapCommand(ISwiftlyCore core, PluginState state, MapLister mapLister, MapCooldown mapCooldown, ChangeMapManager changeMapManager, MapChooserConfig config)
    {
        _core = core;
        _state = state;
        _mapLister = mapLister;
        _mapCooldown = mapCooldown;
        _changeMapManager = changeMapManager;
        _config = config;
    }

    public void Execute(ICommandContext context)
    {
        if (!_config.Votemap.Enabled) return;
        if (!context.IsSentByPlayer) return;

        var player = context.Sender!;
        
        if (!_config.AllowSpectatorsToVote && player.Controller?.TeamNum == 1)
        {
            var localizer = _core.Translation.GetPlayerLocalizer(player);
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.spectator"]);
            return;
        }

        if (_config.Votemap.MinPlayers > 0)
        {
            int playerCount = _core.PlayerManager.GetAllPlayers().Count(p => p.IsValid && !p.IsFakeClient);
            if (playerCount < _config.Votemap.MinPlayers)
            {
                var localizer = _core.Translation.GetPlayerLocalizer(player);
                player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.general.validation.min_players", _config.Votemap.MinPlayers]);
                return;
            }
        }

        string? mapName = context.Args.Length > 0 ? context.Args[0] : null;

        if (string.IsNullOrEmpty(mapName))
        {
            OpenVotemapMenu(player);
        }
        else
        {
            HandleVotemap(player, mapName);
        }
    }

    private void OpenVotemapMenu(IPlayer player)
    {
        var menu = new VotemapMenu(_core, _mapLister, _mapCooldown);
        menu.Show(player, HandleVotemap);
    }

    private void HandleVotemap(IPlayer player, string mapName)
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

        if (!_mapVotes.ContainsKey(map.Name))
        {
            _mapVotes[map.Name] = new VoteManager(_config.Votemap.VotePercentage);
        }

        var voteManager = _mapVotes[map.Name];
        if (voteManager.AddVote(player.Slot))
        {
            var allPlayers = _core.PlayerManager.GetAllPlayers()
                .Where(p => p.IsValid && !p.IsFakeClient && (_config.AllowSpectatorsToVote || p.Controller?.TeamNum > 1))
                .ToList();
            int totalPlayers = allPlayers.Count;
            int needed = voteManager.GetRequiredVotes(totalPlayers);

            if (_config.AnnounceVotes)
                _core.PlayerManager.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.votemap.voted", player.Controller?.PlayerName ?? "Unknown", map.Name, voteManager.VoteCount, needed]);

            if (voteManager.HasReached(totalPlayers))
            {
                _mapVotes.Clear();
                _changeMapManager.ScheduleMapChange(map.Name, _config.Votemap.ChangeMapImmediately);
            }
        }
        else
        {
            player.SendChat(localizer["map_chooser.prefix"] + " " + localizer["map_chooser.votemap.already_voted", map.Name]);
        }
    }

    public void Clear()
    {
        _mapVotes.Clear();
    }
}
