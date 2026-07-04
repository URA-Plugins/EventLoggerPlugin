using Spectre.Console.Rendering;
using UmamusumeResponseAnalyzer.LiveDisplay;
using UmamusumeResponseAnalyzer.Plugin;

namespace EventLoggerPlugin;

static class EventLoggerDisplay
{
    static ILiveDisplayOutput? liveDisplay;
    static LiveDisplayWorkspace? workspace;

    public static void Initialize(IPluginContext context)
    {
        liveDisplay = context.LiveDisplay;
        workspace = liveDisplay.CreateWorkspace("事件记录");
    }

    public static void MarkupLog(string markup, LiveDisplaySeverity severity = LiveDisplaySeverity.Info)
    {
        LiveDisplay.MarkupLog(Workspace, markup, severity);
    }

    public static void Notify(string text, LiveDisplaySeverity severity = LiveDisplaySeverity.Info, TimeSpan? ttl = null)
    {
        LiveDisplay.Notify(Workspace, text, severity, ttl);
    }

    public static void SetPanel(string key, string title, IRenderable content, bool fullBleed = false)
    {
        LiveDisplay.SetPanel(Workspace, key, title, content, fullBleed);
    }

    static ILiveDisplayOutput LiveDisplay => liveDisplay
        ?? throw new InvalidOperationException("EventLoggerPlugin 尚未初始化 LiveDisplay。");

    static LiveDisplayWorkspace Workspace => workspace
        ?? throw new InvalidOperationException("EventLoggerPlugin 尚未创建 LiveDisplay workspace。");
}
