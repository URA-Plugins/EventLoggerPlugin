using Gallop.Endpoints;
using Terminal.Gui.App;
using UmamusumeResponseAnalyzer.Plugin;
static class AnalyzerOrderSmoke
{
internal static void Run(IApplication application)
{
    var sharedEndpoints = new[]
    {
        typeof(GameApi.SingleModeLegend.CheckEvent),
        typeof(GameApi.SingleModeLegend.ExecCommand),
        typeof(GameApi.SingleModeLegend.Load),
    };
    var eventPlugin = new EventResponseAnalyzer.EventResponseAnalyzer();
    var legendPlugin = new LegendScenarioAnalyzer.LegendScenarioAnalyzer();
    using var eventContext = new RuntimePluginContext(application);
    using var legendContext = new RuntimePluginContext(application);
    eventPlugin.Initialize(eventContext);
    legendPlugin.Initialize(legendContext);
    var eventPriorities = ResponseAnalyzerPriorities(eventContext.AnalyzerRegistry);
    var legendPriorities = ResponseAnalyzerPriorities(legendContext.AnalyzerRegistry);

    foreach (var endpoint in sharedEndpoints)
    {
        if (!eventPriorities.TryGetValue(endpoint, out var eventPriority))
            throw new InvalidOperationException($"EventResponseAnalyzer missing priority assertion endpoint: {endpoint.FullName}.");
        if (!legendPriorities.TryGetValue(endpoint, out var legendPriority))
            throw new InvalidOperationException($"LegendScenarioAnalyzer missing priority assertion endpoint: {endpoint.FullName}.");
        if (eventPriority <= legendPriority)
            throw new InvalidOperationException(
                $"EventResponseAnalyzer must run after LegendScenarioAnalyzer for {endpoint.FullName}: eventPriority={eventPriority}, legendPriority={legendPriority}.");
    }
    eventPlugin.DisposeAsync().AsTask().GetAwaiter().GetResult();
    legendPlugin.DisposeAsync().AsTask().GetAwaiter().GetResult();
}

static Dictionary<Type, int> ResponseAnalyzerPriorities(RecordingAnalyzerRegistry registry)
    => registry.Registrations
        .Where(registration => registration.Kind == AnalyzerKind.Response)
        .SelectMany(registration => PluginManager.ExpandEndpointPatterns(registration.Patterns)
            .Select(endpoint => (endpoint.EndpointType, registration.Priority)))
        .ToDictionary(pair => pair.EndpointType, pair => pair.Priority);

}
