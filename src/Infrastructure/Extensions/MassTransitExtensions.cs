using Cfo.Cats.Application.Features.ManagementInformation.IntegrationEventHandlers;
using Cfo.Cats.Application.Features.Participants.MessageBus;
using Cfo.Cats.Application.Features.PRIs.IntegrationEventHandlers;
using MassTransit;

namespace Cfo.Cats.Infrastructure.Extensions;

public static class MassTransitExtensions
{
    static void Configure<T>(IBusFactoryConfigurator<T> cfg, IBusRegistrationContext context) where T : IReceiveEndpointConfigurator
        => cfg.ConfigureEndpoints(context);

    public static void ConfigureInMemory(this IInMemoryBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        cfg.UseConcurrencyLimit(1); // all consumers should be limited to 1 unless otherwise specified

        // Override for specific consumer with a custom concurrency limit
        cfg.ReceiveEndpoint("overnight-service", e =>
        {
            e.Consumer<SyncParticipantCommandHandler>(context, c =>
            {
                c.UseConcurrencyLimit(5); // Custom concurrency limit for this consumer
            });
        });

        Configure(cfg, context);
    }

    public static void ConfigureRabbitMq(this IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context, string connectionString)
    {
        cfg.Host(connectionString);

        cfg.ReceiveEndpoint("overnight-service", e =>
        {
            e.PrefetchCount = 64;
            e.ConcurrentMessageLimit = 5;
            e.ConfigureConsumer<SyncParticipantCommandHandler>(context);
        });

        cfg.ReceiveEndpoint("tasks-service", e =>
        {
            e.PrefetchCount = 64;
            e.ConcurrentMessageLimit = 5;

            e.ConfigureConsumer<PriTaskCompletedWatcherConsumer>(context);
            e.ConfigureConsumer<RaisePaymentsAfterApprovalConsumer>(context);
        });

        cfg.ReceiveEndpoint("payment-service", e =>
        {
            e.ConcurrentMessageLimit = 1;

            e.ConfigureConsumer<RecordActivityPaymentConsumer>(context);
            e.ConfigureConsumer<RecordEducationPayment>(context);
            e.ConfigureConsumer<RecordEmploymentPayment>(context);
            e.ConfigureConsumer<RecordEnrolmentPaymentConsumer>(context);
            e.ConfigureConsumer<RecordHubInductionPaymentConsumer>(context);
            e.ConfigureConsumer<RecordPreReleaseSupportPayment>(context);
            e.ConfigureConsumer<RecordThroughTheGatePaymentConsumer>(context);
            e.ConfigureConsumer<RecordWingInductionPaymentConsumer>(context);
        });

        Configure(cfg, context);
    }
}
