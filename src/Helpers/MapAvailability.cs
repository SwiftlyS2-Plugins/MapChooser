using MapChooser.Contract;
using MapChooser.Models;
using SwiftlyS2.Shared;

namespace MapChooser.Helpers;

/// <summary>Whether a pool map can be nominated or voted for now, as the API and the nominate page both judge it.</summary>
internal static class MapAvailability
{
    /// <summary>The players a map's limits are checked against: connected humans, bots excluded.</summary>
    public static int CountHumans(ISwiftlyCore core)
        => core.PlayerManager.GetAllPlayers().Count(p => p.IsValid && !p.IsFakeClient);

    public static MapChooserAvailability Of(ISwiftlyCore core, MapCooldown cooldown, Map map)
    {
        core.TryGetEngineSnapshot(out var currentMapId, out var currentWorkshopId, out _);
        return Of(map, currentMapId, currentWorkshopId, cooldown, CountHumans(core));
    }

    /// <summary>For a map in the pool; whether a name is in it is the caller's NotInPool.</summary>
    public static MapChooserAvailability Of(Map map, string? currentMapId, string? currentWorkshopId, MapCooldown cooldown, int humans)
    {
        if (IsCurrentMap(map, currentMapId, currentWorkshopId)) return MapChooserAvailability.CurrentMap;
        if (cooldown.IsMapInCooldown(map)) return MapChooserAvailability.Cooldown;
        if (map.MinPlayers > 0 && humans < map.MinPlayers) return MapChooserAvailability.TooFewPlayers;
        if (map.MaxPlayers > 0 && humans > map.MaxPlayers) return MapChooserAvailability.TooManyPlayers;
        return MapChooserAvailability.Available;
    }

    public static bool IsCurrentMap(Map map, string? currentMapId, string? currentWorkshopId)
    {
        if (string.IsNullOrEmpty(currentMapId) && string.IsNullOrEmpty(currentWorkshopId)) return false;
        if (map.Id != null)
        {
            if (!string.IsNullOrEmpty(currentMapId) && map.Id.Equals(currentMapId, StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.IsNullOrEmpty(currentWorkshopId) && map.Id.Equals(currentWorkshopId, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
