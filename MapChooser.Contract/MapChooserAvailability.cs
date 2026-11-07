namespace MapChooser.Contract;

/// <summary>Whether a pool map can be nominated or voted for now, as <see cref="IMapChooserApi.GetAvailability"/> reports it.</summary>
public enum MapChooserAvailability
{
    Available,

    /// <summary>Not a map in the pool; <see cref="IMapChooserApi.ExtendOption"/> included.</summary>
    NotInPool,

    /// <summary>The map being played.</summary>
    CurrentMap,

    /// <summary>Played recently: one of the last <c>MapsInCooldown</c> maps.</summary>
    Cooldown,

    /// <summary>Fewer players are connected than its <see cref="MapChooserMap.MinPlayers"/>.</summary>
    TooFewPlayers,

    /// <summary>More players are connected than its <see cref="MapChooserMap.MaxPlayers"/>.</summary>
    TooManyPlayers,
}
