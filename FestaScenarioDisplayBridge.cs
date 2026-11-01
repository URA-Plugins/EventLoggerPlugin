using FestaScenarioAnalyzer;

namespace EventLoggerPlugin;

internal static class FestaScenarioDisplayBridge
{
    internal static IDisposable Register() => FestaTrainingDisplay.RegisterPartProducer("EventLogger");

    internal static void Update(IDisposable registration, EventLoggerScenarioDisplayPart part)
    {
        var rows = new List<FestaDisplayLine>();
        var snapshot = part.Snapshot;
        foreach (var line in part.CardEventLines)
            if (line.Length != 0)
                rows.Add(new([new(line, FestaDisplayColor.Yellow)]));
        if (snapshot.CurrentTurn == part.TargetTurn && snapshot.TrainingFailures is { } failures)
            rows.Add(new([
                new("训练赌博: "), new(failures.GambleTimes.ToString(), FestaDisplayColor.Yellow),
                new("次, 失败"), new(failures.FailureTimes.ToString(), FestaDisplayColor.Yellow),
                new("次, 总失败率"), new(failures.TotalFailureRate.ToString(), FestaDisplayColor.Yellow), new("%")
            ]));
        if (snapshot.EventCount > 0)
            rows.Add(new([new("事件数: "), new(snapshot.EventCount.ToString(), FestaDisplayColor.Yellow)]));
        if (snapshot.SuccessEvents.Appeared > 0)
            rows.Add(new([
                new("赌狗事件: "), new(snapshot.SuccessEvents.Appeared.ToString(), FestaDisplayColor.Yellow),
                new("次, 选择"), new(snapshot.SuccessEvents.Selected.ToString(), FestaDisplayColor.Yellow),
                new("次, 成功"), new(snapshot.SuccessEvents.Succeeded.ToString(), FestaDisplayColor.Yellow), new("次")
            ]));
        if (part.InheritGains.Count > 0)
            rows.Add(new([
                new("继承属性: "), new(string.Join('+', part.InheritGains.Select(gain => gain.Stats)), FestaDisplayColor.Cyan),
                new(", PT: "), new(string.Join('+', part.InheritGains.Select(gain => gain.SkillPoints)), FestaDisplayColor.Cyan)
            ]));
        if (snapshot.RaceWinCount > 0)
            rows.Add(new([new("胜场: "), new(snapshot.RaceWinCount.ToString(), FestaDisplayColor.Yellow)]));
        ((FestaTrainingDisplayPartProducer)registration).Update(new(part.SingleModeCharaId, part.TargetTurn), rows);
    }
}
