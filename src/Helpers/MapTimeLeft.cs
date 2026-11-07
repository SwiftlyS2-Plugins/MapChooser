using MapChooser.Dependencies;
using SwiftlyS2.Shared;

namespace MapChooser.Helpers;

/// <summary>What is left of the current map, as <c>!timeleft</c> and the API both count it.</summary>
internal static class MapTimeLeft
{
    /// <summary>Seconds left under <c>mp_timelimit</c>, unrounded and below zero once time is up; null without a time limit or during warmup.</summary>
    public static float? Seconds(ISwiftlyCore core, PluginState state)
    {
        if (state.WarmupRunning) return null;

        float timelimit = core.ConVar.Find<float>("mp_timelimit")?.Value ?? 0;
        if (timelimit <= 0) return null;

        float currentTime = core.TryGetCurrentTime();
        float timePlayed = currentTime - state.MapStartTime;
        return (timelimit * 60) - timePlayed;
    }

    /// <summary>Rounds left under <c>mp_maxrounds</c>, the one being played included; null without a round limit or during warmup.</summary>
    /// <param name="onGameRulesMissing">Told when the scores cannot be read yet; no rounds then count as played.</param>
    public static int? Rounds(ISwiftlyCore core, PluginState state, Action<InvalidOperationException>? onGameRulesMissing = null)
    {
        if (state.WarmupRunning) return null;

        int maxrounds = core.ConVar.Find<int>("mp_maxrounds")?.Value ?? 0;
        if (maxrounds <= 0) return null;

        int winlimit = core.ConVar.Find<int>("mp_winlimit")?.Value ?? 0;

        int totalRoundsPlayed;
        int maxTeamScore;
        try
        {
            totalRoundsPlayed = core.Game.MatchData.TerroristScoreTotal + core.Game.MatchData.CTScoreTotal;
            maxTeamScore = Math.Max(core.Game.MatchData.TerroristScoreTotal, core.Game.MatchData.CTScoreTotal);
        }
        catch (InvalidOperationException ex)
        {
            onGameRulesMissing?.Invoke(ex);
            totalRoundsPlayed = 0;
            maxTeamScore = 0;
        }
        int roundsRemaining = maxrounds - totalRoundsPlayed;

        if (winlimit == 0)
        {
            // CS2 ends when a team wins (maxrounds/2)+1 rounds even without explicit mp_winlimit
            int effectiveWinlimit = maxrounds / 2 + 1;
            roundsRemaining = Math.Min(roundsRemaining, effectiveWinlimit - maxTeamScore);
        }

        return roundsRemaining;
    }
}
