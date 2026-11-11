using PioneerScenarioAnalyzer;

namespace EventLoggerPlugin;

internal static class PioneerScenarioDisplayBridge
{
    internal static IDisposable Register()
        => PioneerTrainingDisplay.RegisterPartProducer("EventLogger");

    internal static void Update(
        IDisposable registration,
        EventLoggerScenarioDisplayPart part,
        IReadOnlyList<string> warnings)
    {
        var producer = (PioneerTrainingDisplayPartProducer)registration;
        var warningSnapshot = warnings.ToArray();
        producer.Update(new(part.SingleModeCharaId, part.TargetTurn),
            (_, display) => Apply(part, warningSnapshot, display));
    }

    static void Apply(EventLoggerScenarioDisplayPart part, IReadOnlyList<string> warnings, PioneerTrainingDisplayEditor display)
    {
        var snapshot = part.Snapshot;
        foreach (var warning in warnings)
            display.Important.AddStyled(new PioneerDisplaySegment(warning, PioneerDisplayColor.Red));

        foreach (var line in part.CardEventLines)
            if (line.Length != 0)
                display.Extra.AddStyled(new PioneerDisplaySegment(line, PioneerDisplayColor.Yellow));

        if (snapshot.CurrentTurn == part.TargetTurn && snapshot.TrainingFailures is { } failures)
            display.Extra.AddStyled(
                new PioneerDisplaySegment("训练赌博: "),
                new PioneerDisplaySegment(failures.GambleTimes.ToString(), PioneerDisplayColor.Yellow),
                new PioneerDisplaySegment("次, 失败"),
                new PioneerDisplaySegment(failures.FailureTimes.ToString(), PioneerDisplayColor.Yellow),
                new PioneerDisplaySegment("次, 总失败率"),
                new PioneerDisplaySegment(failures.TotalFailureRate.ToString(), PioneerDisplayColor.Yellow),
                new PioneerDisplaySegment("%"));

        if (snapshot.EventCount > 0)
            display.Extra.AddStyled(
                new PioneerDisplaySegment("事件数: "),
                new PioneerDisplaySegment(snapshot.EventCount.ToString(), PioneerDisplayColor.Yellow));
        if (snapshot.SuccessEvents.Appeared > 0)
            display.Extra.AddStyled(
                new PioneerDisplaySegment("赌狗事件: "),
                new PioneerDisplaySegment(snapshot.SuccessEvents.Appeared.ToString(), PioneerDisplayColor.Yellow),
                new PioneerDisplaySegment("次, 选择"),
                new PioneerDisplaySegment(snapshot.SuccessEvents.Selected.ToString(), PioneerDisplayColor.Yellow),
                new PioneerDisplaySegment("次, 成功"),
                new PioneerDisplaySegment(snapshot.SuccessEvents.Succeeded.ToString(), PioneerDisplayColor.Yellow),
                new PioneerDisplaySegment("次"));
        if (part.InheritGains.Count > 0)
        {
            var inherit = new List<PioneerDisplaySegment> { new("继承属性: ") };
            for (var i = 0; i < part.InheritGains.Count; i++)
            {
                if (i > 0)
                    inherit.Add(new("+"));
                var stats = part.InheritGains[i].Stats;
                inherit.Add(new(stats.ToString(), InheritColor(stats)));
            }
            inherit.Add(new(", PT: "));
            inherit.Add(new(string.Join('+', part.InheritGains.Select(gain => gain.SkillPoints)), PioneerDisplayColor.Cyan));
            display.Extra.AddStyled(inherit.ToArray());
        }
        if (snapshot.RaceWinCount > 0)
            display.Extra.AddStyled(
                new PioneerDisplaySegment("胜场: "),
                new PioneerDisplaySegment(snapshot.RaceWinCount.ToString(), PioneerDisplayColor.Yellow));
    }

    static PioneerDisplayColor InheritColor(int stats) => InheritStatsColorRules.Of(stats) switch
    {
        InheritStatsTone.Red => PioneerDisplayColor.Red,
        InheritStatsTone.Cyan => PioneerDisplayColor.Cyan,
        _ => PioneerDisplayColor.Lime,
    };
}
