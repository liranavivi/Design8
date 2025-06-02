using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Address;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Step;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Delivery;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Schema;



using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Processor;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Flow;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.OrchestratedFlow;
using FlowOrchestrator.EntitiesManagers.Infrastructure.MassTransit.Consumers.Assignment;
using MassTransit;

namespace FlowOrchestrator.EntitiesManagers.Api.Configuration;

public static class MassTransitConfiguration
{
    public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            // Add address consumers
            x.AddConsumer<CreateAddressCommandConsumer>();
            x.AddConsumer<UpdateAddressCommandConsumer>();
            x.AddConsumer<DeleteAddressCommandConsumer>();
            x.AddConsumer<GetAddressQueryConsumer>();

            // Add step consumers
            x.AddConsumer<CreateStepCommandConsumer>();
            x.AddConsumer<UpdateStepCommandConsumer>();
            x.AddConsumer<DeleteStepCommandConsumer>();
            x.AddConsumer<GetStepQueryConsumer>();

            // Add delivery consumers
            x.AddConsumer<CreateDeliveryCommandConsumer>();
            x.AddConsumer<UpdateDeliveryCommandConsumer>();
            x.AddConsumer<DeleteDeliveryCommandConsumer>();
            x.AddConsumer<GetDeliveryQueryConsumer>();

            // Add schema consumers
            x.AddConsumer<CreateSchemaCommandConsumer>();
            x.AddConsumer<UpdateSchemaCommandConsumer>();
            x.AddConsumer<DeleteSchemaCommandConsumer>();
            x.AddConsumer<GetSchemaQueryConsumer>();
            x.AddConsumer<GetSchemaDefinitionQueryConsumer>();







            // Add processor consumers
            x.AddConsumer<CreateProcessorCommandConsumer>();
            x.AddConsumer<UpdateProcessorCommandConsumer>();
            x.AddConsumer<DeleteProcessorCommandConsumer>();
            x.AddConsumer<GetProcessorQueryConsumer>();

            // Add flow consumers
            x.AddConsumer<CreateFlowCommandConsumer>();
            x.AddConsumer<UpdateFlowCommandConsumer>();
            x.AddConsumer<DeleteFlowCommandConsumer>();
            x.AddConsumer<GetFlowQueryConsumer>();

            // Add orchestrated flow consumers
            x.AddConsumer<CreateOrchestratedFlowCommandConsumer>();
            x.AddConsumer<UpdateOrchestratedFlowCommandConsumer>();
            x.AddConsumer<DeleteOrchestratedFlowCommandConsumer>();
            x.AddConsumer<GetOrchestratedFlowQueryConsumer>();

            // Add assignment consumers
            x.AddConsumer<CreateAssignmentCommandConsumer>();
            x.AddConsumer<UpdateAssignmentCommandConsumer>();
            x.AddConsumer<DeleteAssignmentCommandConsumer>();
            x.AddConsumer<GetAssignmentQueryConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqSettings = configuration.GetSection("RabbitMQ");

                cfg.Host(rabbitMqSettings["Host"] ?? "localhost", rabbitMqSettings["VirtualHost"] ?? "/", h =>
                {
                    h.Username(rabbitMqSettings["Username"] ?? "guest");
                    h.Password(rabbitMqSettings["Password"] ?? "guest");
                });

                // Configure retry policy
                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15),
                    TimeSpan.FromSeconds(30)
                ));

                // Configure error handling
                // cfg.UseInMemoryOutbox(); // Commented out due to obsolete warning

                // Configure endpoints to use message type routing
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
