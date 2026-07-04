namespace EventLoggerPlugin;

public sealed record EventLoggerDisplaySnapshot(
    bool IsStarted,
    int CurrentTurn,
    int CurrentScenario,
    int EventCount,
    EventLoggerCardEventSummary CardEvents,
    EventLoggerSuccessEventSummary SuccessEvents,
    EventLoggerTrainingFailureSummary? TrainingFailures,
    EventLoggerScenarioFriendSummary? ScenarioFriend,
    IReadOnlyList<int> InheritStats,
    int RaceWinCount)
{
    public static EventLoggerDisplaySnapshot Empty { get; } = new(
        false,
        0,
        0,
        0,
        EventLoggerCardEventSummary.Empty,
        EventLoggerSuccessEventSummary.Empty,
        null,
        null,
        [],
        0);

    public bool HasData =>
        IsStarted ||
        CurrentTurn > 0 ||
        EventCount > 0 ||
        CardEvents.HasData ||
        SuccessEvents.HasData ||
        TrainingFailures is not null ||
        ScenarioFriend is not null ||
        InheritStats.Count > 0 ||
        RaceWinCount > 0;
}

public sealed record EventLoggerCardEventSummary(
    int Appeared,
    int Finished,
    int Remaining,
    int FinishedTurn)
{
    public static EventLoggerCardEventSummary Empty { get; } = new(0, 0, 0, 0);

    public bool HasData => Appeared > 0 || Finished > 0 || Remaining > 0 || FinishedTurn > 0;
}

public sealed record EventLoggerSuccessEventSummary(
    int Appeared,
    int Selected,
    int Succeeded)
{
    public static EventLoggerSuccessEventSummary Empty { get; } = new(0, 0, 0);

    public bool HasData => Appeared > 0 || Selected > 0 || Succeeded > 0;
}

public sealed record EventLoggerTrainingFailureSummary(
    int GambleTimes,
    int FailureTimes,
    int TotalFailureRate);

public sealed record EventLoggerScenarioFriendSummary(
    string Label,
    int ClickedTimes,
    int ActivatedTimes);

public static class EventLoggerDisplaySource
{
    public static EventLoggerDisplaySnapshot Current => Capture();

    public static EventLoggerDisplaySnapshot Capture()
    {
        if (!EventLogger.IsStart && GameStats.currentTurn <= 0 && EventLogger.AllEvents.Count == 0)
            return EventLoggerDisplaySnapshot.Empty;

        return new(
            EventLogger.IsStart,
            GameStats.currentTurn,
            EventLogger.CurrentScenario,
            EventLogger.AllEvents.Count,
            new(
                EventLogger.CardEventCount,
                EventLogger.CardEventFinishCount,
                EventLogger.CardEventRemaining,
                EventLogger.CardEventFinishTurn),
            new(
                EventLogger.SuccessEventCount,
                EventLogger.SuccessEventSelectCount,
                EventLogger.SuccessEventSuccessCount),
            CaptureTrainingFailures(),
            CaptureScenarioFriend(),
            EventLogger.InheritStats?.ToArray() ?? [],
            EventLogger.raceHistory?.Count ?? 0);
    }

    static EventLoggerTrainingFailureSummary? CaptureTrainingFailures()
    {
        var gambleTimes = 0;
        var failureTimes = 0;
        var totalFailureRate = 0;

        for (var turn = LastPastTurnIndex(); turn >= 1; turn--)
        {
            var stats = GameStats.stats[turn];
            if (stats is null)
                break;
            if (!TryGetTrainStat(stats, out var trainStat))
                continue;

            if (stats.isTrainingFailed)
                failureTimes++;

            if (trainStat.FailureRate <= 0)
                continue;

            gambleTimes++;
            totalFailureRate += trainStat.FailureRate;
        }

        return gambleTimes == 0 && failureTimes == 0
            ? null
            : new(gambleTimes, failureTimes, totalFailureRate);
    }

    static EventLoggerScenarioFriendSummary? CaptureScenarioFriend()
        => GameStats.whichScenario switch
        {
            (int)ScenarioType.LArc => CaptureLArcFriend(),
            (int)ScenarioType.UAF => CaptureUafFriend(),
            (int)ScenarioType.Legend => CaptureLegendFriend(),
            _ => null
        };

    static EventLoggerScenarioFriendSummary CaptureLArcFriend()
    {
        var clickedTimes = 0;
        var activatedTimes = 0;

        foreach (var stats in CompletedTrainingTurns())
        {
            if (!TryGetTrainIndex(stats, out var trainIndex))
                continue;
            if (!IsAtTrain(stats.larc_zuoyueAtTrain, trainIndex) || stats.larc_zuoyueEvent == 5)
                continue;

            clickedTimes++;
            if (stats.larc_zuoyueEvent is 1 or 2 or 4)
                activatedTimes++;
        }

        return new("佐岳", clickedTimes, activatedTimes);
    }

    static EventLoggerScenarioFriendSummary CaptureUafFriend()
    {
        var clickedTimes = 0;
        var activatedTimes = 0;

        foreach (var stats in CompletedTrainingTurns())
        {
            if (!TryGetTrainIndex(stats, out var trainIndex))
                continue;
            if (!IsAtTrain(stats.uaf_friendAtTrain, trainIndex) || stats.uaf_friendEvent == 5)
                continue;

            clickedTimes++;
            if (stats.uaf_friendEvent is 1 or 2)
                activatedTimes++;
        }

        return new("凉花", clickedTimes, activatedTimes);
    }

    static EventLoggerScenarioFriendSummary CaptureLegendFriend()
    {
        var clickedTimes = 0;
        var activatedTimes = 0;

        foreach (var stats in CompletedTrainingTurns())
        {
            if (!TryGetTrainIndex(stats, out var trainIndex))
                continue;
            if (!IsAtTrain(stats.legend_friendAtTrain, trainIndex) || !stats.legend_friendClickEventCountConcerned)
                continue;

            clickedTimes++;
            if (stats.legend_friendClickEvent)
                activatedTimes++;
        }

        return new("团卡", clickedTimes, activatedTimes);
    }

    static IEnumerable<TurnStats> CompletedTrainingTurns()
    {
        for (var turn = LastCurrentTurnIndex(); turn >= 1; turn--)
        {
            var stats = GameStats.stats[turn];
            if (stats is null)
                yield break;
            if (stats.isTrainingFailed)
                continue;
            if (!TryGetTrainIndex(stats, out _))
                continue;

            yield return stats;
        }
    }

    static bool TryGetTrainIndex(TurnStats stats, out int trainIndex)
    {
        if (!GameGlobal.TrainIds.Contains(stats.playerChoice))
        {
            trainIndex = -1;
            return false;
        }

        return GameGlobal.ToTrainIndex.TryGetValue(stats.playerChoice, out trainIndex);
    }

    static bool TryGetTrainStat(TurnStats stats, out TrainStats trainStat)
    {
        trainStat = null!;
        if (!TryGetTrainIndex(stats, out var trainIndex))
            return false;
        if (stats.fiveTrainStats is null || trainIndex < 0 || trainIndex >= stats.fiveTrainStats.Length)
            return false;

        trainStat = stats.fiveTrainStats[trainIndex];
        return trainStat is not null;
    }

    static bool IsAtTrain(bool[]? values, int trainIndex)
        => values is not null && trainIndex >= 0 && trainIndex < values.Length && values[trainIndex];

    static int LastPastTurnIndex()
        => Math.Min(Math.Max(GameStats.currentTurn - 1, 0), GameStats.stats.Length - 1);

    static int LastCurrentTurnIndex()
        => Math.Min(Math.Max(GameStats.currentTurn, 0), GameStats.stats.Length - 1);
}
