namespace MapChooser.Contract;

/// <summary>A map in MapChooser's pool, as <c>maps.jsonc</c> configures it.</summary>
/// <param name="Name">The name its menus and ballot show, and the key <see cref="IMapChooserApi"/> takes.</param>
/// <param name="MapName">The map the server loads (<c>de_dust2</c>), or null for a workshop map.</param>
/// <param name="WorkshopId">The Steam Workshop ID, or null for a map installed on the server.</param>
/// <param name="MinPlayers">Players needed before it is offered; 0 for no minimum.</param>
/// <param name="MaxPlayers">Players above which it is not offered; 0 for no maximum.</param>
public sealed record MapChooserMap(string Name, string? MapName, string? WorkshopId, int MinPlayers, int MaxPlayers);
