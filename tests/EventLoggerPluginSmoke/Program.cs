var culture = System.Globalization.CultureInfo.GetCultureInfo("zh-CN");
System.Globalization.CultureInfo.CurrentCulture = culture;
System.Globalization.CultureInfo.CurrentUICulture = culture;
RepositoryChecks.Run();
SourceDtoContract.Run();
using var ui = new WorkspaceSmokeSession();
if (args.Length != 0 && args is not ["--package-load"]) throw new ArgumentException("Usage: EventLoggerPluginSmoke [--package-load]");
if (args.Length == 0)
{
    WorkspaceLifecycleSmoke.Run(ui, () => new EventLoggerPlugin.EventLoggerPlugin(), "EventLoggerPlugin");
    await AnalyzerRegistrationSmoke.Run(ui);
    AnalyzerOrderSmoke.Run(ui.Application);
    EventLoggerRuntimeSmoke.Run(ui);
}
PackageLoadSmoke.Run(ui);
Console.WriteLine("PASS EventLogger source, dependency order and fresh ZIP behavior");
