using MapChooser.Models;
using SwiftlyS2.Shared;

namespace MapChooser.Helpers;

public class MapCooldown
{
    private List<string> _mapsOnCooldown = new();
    private readonly MapChooserConfig _config;
    private readonly ISwiftlyCore _core;

    public MapCooldown(ISwiftlyCore core, MapChooserConfig config)
    {
        _core = core;
        _config = config;
    }

    public void OnMapStart(string mapName, string? workshopId = null)
    {
        if (_config.MapsInCooldown <= 0)
        {
            _mapsOnCooldown.Clear();
            return;
        }

        string identity = (!string.IsNullOrEmpty(workshopId) ? workshopId : mapName).Trim().ToLower();
        
        if (_mapsOnCooldown.Contains(identity))
            _mapsOnCooldown.Remove(identity);

        _mapsOnCooldown.Add(identity);

        int limit = _config.MapsInCooldown + 1;
        while (_mapsOnCooldown.Count > limit)
        {
            _mapsOnCooldown.RemoveAt(0);
        }
    }

    public bool IsMapInCooldown(string mapIdentity)
    {
        string identity = mapIdentity.Trim().ToLower();
        
        // Find if this identity exists in history
        if (!_mapsOnCooldown.Contains(identity)) return false;

        // Verify it's not the current map. During map transitions the engine
        // pointer / GlobalVars / WorkshopId may all be null — bail safely.
        if (!_core.TryGetEngineSnapshot(out var currentMapName, out var currentWorkshopId, out _))
            return false;

        currentMapName = currentMapName.ToLower();
        currentWorkshopId = currentWorkshopId.ToLower();

        if (identity == currentMapName || (!string.IsNullOrEmpty(currentWorkshopId) && identity == currentWorkshopId))
            return false;

        return true;
    }

    public bool IsMapInCooldown(Map map)
    {
        if (map.Id != null && IsMapInCooldown(map.Id)) return true;
        if (IsMapInCooldown(map.Name)) return true;
        return false;
    }

    public void AddMapToCooldown(string mapIdentity)
    {
        string identity = mapIdentity.Trim().ToLower();
        if (!_mapsOnCooldown.Contains(identity))
        {
            _mapsOnCooldown.Add(identity);
            int limit = _config.MapsInCooldown + 1;
            while (_mapsOnCooldown.Count > limit && limit > 0)
            {
                _mapsOnCooldown.RemoveAt(0);
            }
        }
    }
}

