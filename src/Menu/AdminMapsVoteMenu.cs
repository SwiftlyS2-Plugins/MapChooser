using MapChooser.Models;
using MapChooser.Dependencies;
using MapChooser.Helpers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menu;
using SwiftlyS2.Shared.Menu.Components;
using SwiftlyS2.Shared.Players;
using System.Threading.Tasks;

namespace MapChooser.Menu;

public class AdminMapsVoteMenu
{
    private readonly ISwiftlyCore _core;
    private readonly MapLister _mapLister;
    private readonly HashSet<string> _selectedMaps = new();

    public AdminMapsVoteMenu(ISwiftlyCore core, MapLister mapLister)
    {
        _core = core;
        _mapLister = mapLister;
    }

    public void Show(IPlayer player, Action<IPlayer, List<string>> onStartVote)
    {
        var oldSession = _core.Menu.GetSession(player);
        int? indexToRestore = null;
        if (oldSession?.Menu.Tag?.ToString() == "AdminMapsVoteMenu")
        {
            indexToRestore = oldSession.FocusedIndex;
        }

        var localizer = _core.Translation.GetPlayerLocalizer(player);
        var builder = _core.Menu.CreateMenu("mapchooser.admin-maps-vote");
        
        string title = "Select Maps for Vote:";
        try { title = localizer["map_chooser.admin_vote.title"]; } catch { }
        builder.AddHeader(new TitleComponent($"{title} ({_selectedMaps.Count})") { Overflow = MenuTextOverflow.None });

        // Start Vote Option
        string startText = "START VOTE";
        try { startText = localizer["map_chooser.admin_vote.start"]; } catch { }
        var startOption = new ButtonComponent(startText, ctx =>
        {
            _core.Scheduler.NextTick(() =>
            {
                ctx.Session.Close();

                StartCountdown(ctx.Player, 5, onStartVote);
            });
            return ValueTask.CompletedTask;
        });
        startOption.Style = startOption.Style.WithColor("#FFA500");
        startOption.Enabled = _selectedMaps.Count > 0;
        builder.Add(startOption);

        // Map Options
        foreach (var map in _mapLister.Maps)
        {
            bool isSelected = _selectedMaps.Contains(map.Name);
            var option = new CheckboxButtonComponent(map.Name, isSelected, ctx =>
            {
                if (isSelected)
                    _selectedMaps.Remove(map.Name);
                else
                    _selectedMaps.Add(map.Name);

                _core.Scheduler.NextTick(() => Show(player, onStartVote));
                return ValueTask.CompletedTask;
            });
            builder.Add(option);
        }

        var menu = builder.Build();
        menu.Tag = "AdminMapsVoteMenu";
        var session = menu.Open(player);

        if (indexToRestore.HasValue)
        {
            session.SetFocus(indexToRestore.Value);
        }
    }

    private void StartCountdown(IPlayer admin, int seconds, Action<IPlayer, List<string>> onStartVote)
    {
        if (seconds <= 0)
        {
            onStartVote(admin, _selectedMaps.ToList());
            return;
        }

        var localizer = _core.Translation.GetPlayerLocalizer(admin);
        _core.PlayerManager.SendChat($"{localizer["map_chooser.prefix"]} Vote starting in [red]{seconds}[default] seconds...");
        
        _core.Scheduler.DelayBySeconds(1, () => StartCountdown(admin, seconds - 1, onStartVote));
    }

    // Inline <font> markup would print literally under the chat renderer, so the two colours are separate nodes.
    private sealed class CheckboxButtonComponent(string text, bool isChecked, Func<MenuActionContext, ValueTask> onSelect)
        : ButtonComponent(text, onSelect)
    {
        public override MenuNode Render(IMenuComponentRenderContext context) => MenuLineNode.Of(
            new MenuTextNode(isChecked ? "[X] " : "[ ] ", Style.WithColor(isChecked ? "#008000" : "#FF0000")),
            base.Render(context));
    }
}
