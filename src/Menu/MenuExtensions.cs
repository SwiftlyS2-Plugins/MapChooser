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

    private sealed class NoOptionsComponent : TextComponent
    {
        public override MenuNode Render(IMenuComponentRenderContext context)
            => new MenuTextNode(context.Translate("menu.default_option"), Style);
    }
}
