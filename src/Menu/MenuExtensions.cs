using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;

namespace MapChooser.Menu;

internal static class MenuExtensions
{
    // The classic builder filled an empty menu with SwiftlyS2's own "No options" row; Core.Menu draws only the title.
    public static IMenu WithDefaultOption(this IMenu menu)
    {
        if (menu.GetComponents(MenuRegion.Body).Count == 0)
        {
            menu.Add(MenuRegion.Body, new NoOptionsComponent());
        }

        return menu;
    }

    /// <summary>Closes a page whose job is done. <see cref="IMenuSession.Close"/> would return to its parent, the hub, which only the close key should do.</summary>
    public static void Dismiss(this IMenuSession session)
    {
        session.Menu.Parent = null;
        session.Close();
    }

    private sealed class NoOptionsComponent : TextComponent
    {
        public override MenuNode Render(IMenuComponentRenderContext context)
            => new MenuTextNode(context.Translate("menu.default_option"), Style);
    }
}
