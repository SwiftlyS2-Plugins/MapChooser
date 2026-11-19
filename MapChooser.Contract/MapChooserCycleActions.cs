namespace MapChooser.Contract;

/// <summary>
/// What a map row of the <see cref="MapChooserMenuIds.Cycle"/> page does besides Select, which opens the map's own page.
/// </summary>
/// <remarks>
/// The page declares these in its own scope with no key. A server binds one under <c>Menus</c> in SwiftlyS2's
/// <c>menus.jsonc</c>; a renderer sends one to a row through <c>IMenuComponent.HandleActionAsync</c>. A row refuses a
/// move its map can't make, so it returns <see langword="false"/> for MoveUp on the first map.
/// </remarks>
public static class MapChooserCycleActions
{
    /// <summary>Moves the row's map one place up the cycle.</summary>
    public const string MoveUp = "MoveUp";

    /// <summary>Moves the row's map one place down the cycle.</summary>
    public const string MoveDown = "MoveDown";

    /// <summary>Opens the page that asks before removing the row's map from the cycle.</summary>
    public const string Remove = "Remove";
}
