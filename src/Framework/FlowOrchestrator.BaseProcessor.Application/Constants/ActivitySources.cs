namespace FlowOrchestrator.BaseProcessor.Application.Constants;

/// <summary>
/// Activity source names used throughout the application
/// Following EntitiesManager.Api naming conventions
/// </summary>
public static class ActivitySources
{
    public const string Core = "BaseProcessorApplication.Core";
    public const string Services = "BaseProcessorApplication.Services";
    public const string Cache = "BaseProcessorApplication.Cache";
    public const string Validation = "BaseProcessorApplication.Validation";
    public const string HealthCheck = "BaseProcessorApplication.HealthCheck";
    public const string MassTransit = "MassTransit";
}
