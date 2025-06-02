using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Repositories;
using FlowOrchestrator.EntitiesManagers.Core.Interfaces.Services;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MongoDB;
using FlowOrchestrator.EntitiesManagers.Infrastructure.Repositories;
using FlowOrchestrator.EntitiesManagers.Infrastructure.Services;
using MongoDB.Driver;

namespace FlowOrchestrator.EntitiesManagers.Api.Configuration;

public static class MongoDbConfiguration
{
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure BSON serialization
        BsonConfiguration.Configure();

        // Register MongoDB client and database
        services.AddSingleton<IMongoClient>(provider =>
        {
            var connectionString = configuration.GetConnectionString("MongoDB");
            var settings = MongoClientSettings.FromConnectionString(connectionString);

            return new MongoClient(settings);
        });

        services.AddScoped<IMongoDatabase>(provider =>
        {
            var client = provider.GetRequiredService<IMongoClient>();
            var databaseName = configuration.GetValue<string>("MongoDB:DatabaseName") ?? "EntitiesManagerDb";
            return client.GetDatabase(databaseName);
        });

        // Register event publisher
        services.AddScoped<IEventPublisher, EventPublisher>();

        // Register referential integrity service
        services.AddScoped<IReferentialIntegrityService, ReferentialIntegrityService>();

        // Register repositories
        services.AddScoped<IAddressEntityRepository, AddressEntityRepository>();
        services.AddScoped<IStepEntityRepository, StepEntityRepository>();
        services.AddScoped<IDeliveryEntityRepository, DeliveryEntityRepository>();
        services.AddScoped<ISchemaEntityRepository, SchemaEntityRepository>();
        services.AddScoped<IProcessorEntityRepository, ProcessorEntityRepository>();
        services.AddScoped<IFlowEntityRepository, FlowEntityRepository>();
        services.AddScoped<IOrchestratedFlowEntityRepository>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            var logger = provider.GetRequiredService<ILogger<OrchestratedFlowEntityRepository>>();
            var eventPublisher = provider.GetRequiredService<IEventPublisher>();
            var integrityService = provider.GetRequiredService<IReferentialIntegrityService>();
            return new OrchestratedFlowEntityRepository(database, logger, eventPublisher, integrityService);
        });
        services.AddScoped<IAssignmentEntityRepository, AssignmentEntityRepository>();
        return services;
    }
}
