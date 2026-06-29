using MapChooser.Models;

namespace MapChooser.Dependencies;

public class PluginState
{
    public bool MapChangeScheduled { get; set; }
    public bool EofVoteHappening { get; set; }
    public bool CommandsDisabled { get; set; }
    public string? NextMap { get; set; }
    public bool ChangeMapImmediately { get; set; }
    public bool IsRtv { get; set; }
    /// <summary>
    /// True when a map change has been scheduled and is waiting for the next
    /// EventRoundEnd to fire the actual change. Independent of <see cref="IsRtv"/>
    /// so it survives state resets that clear IsRtv (e.g. ChangeMap, OnMatchStart).
    /// Cleared by ChangeMap, OnMapLoad and OnMatchStart.
    /// </summary>
    public bool QueuedRoundEndChange { get; set; }
    public float MapStartTime { get; set; }
    public int RoundsPlayed { get; set; }
    public bool WarmupRunning { get; set; }
    public int ExtendsLeft { get; set; }
    public int NextEofVotePossibleRound { get; set; }
    public float NextEofVotePossibleTime { get; set; }
    public DateTime? RtvCooldownEndTime { get; set; }
    public bool MatchEnded { get; set; }
    public bool EofVoteCompleted { get; set; }
    /// <summary>
    /// True once a `changelevel`/`host_workshop_map` command has actually been queued.
    /// Used to debounce reentrant `ChangeMap()` calls from concurrent end‑of‑match
    /// event sources (WinPanelMatch / GamePhaseChanged / RoundEnd / cycle trigger).
    /// Cleared in OnMapLoad.
    /// </summary>
    public bool MapSwitchInFlight { get; set; }
    public Dictionary<int, string> Nominations { get; set; } = new();
}
