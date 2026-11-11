namespace EventLoggerPlugin;

/// <summary>继承属性单次数值的三档显示色。</summary>
internal enum InheritStatsTone
{
    Red,
    Cyan,
    Green,
}

/// <summary>
/// 按单次继承属性值判定显示色档；阈值在 <see cref="EventConstants.InheritStatsRedBelow"/> /
/// <see cref="EventConstants.InheritStatsGreenFrom"/> 配置。
/// </summary>
internal static class InheritStatsColorRules
{
    internal static InheritStatsTone Of(int stats) =>
        stats < EventConstants.InheritStatsRedBelow ? InheritStatsTone.Red
        : stats < EventConstants.InheritStatsGreenFrom ? InheritStatsTone.Cyan
        : InheritStatsTone.Green;
}