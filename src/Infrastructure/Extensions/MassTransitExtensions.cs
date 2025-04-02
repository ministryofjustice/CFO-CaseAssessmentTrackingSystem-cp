using Amazon;
using Cfo.Cats.Application.Features.ManagementInformation.IntegrationEventHandlers;
using Cfo.Cats.Application.Features.Participants.MessageBus;
using Cfo.Cats.Application.Features.PRIs.IntegrationEventHandlers;
using MassTransit;
using MassTransit.AmazonSqsTransport.Configuration;

namespace Cfo.Cats.Infrastructure.Extensions;

public static class MassTransitExtensions
{
    static void Configure<T>(IBusFactoryConfigurator<T> configurator, IBusRegistrationContext context) where T : IReceiveEndpointConfigurator
        => configurator.ConfigureEndpoints(context);

    public static void ConfigureInMemory(this IInMemoryBusFactoryConfigurator configurator, IBusRegistrationContext context)
    {
        configurator.UseConcurrencyLimit(1); // all consumers should be limited to 1 unless otherwise specified

        // Override for specific consumer with a custom concurrency limit
        configurator.ReceiveEndpoint("overnight-service", e =>
        {
            e.Consumer<SyncParticipantCommandHandler>(context, c =>
            {
                c.UseConcurrencyLimit(5); // Custom concurrency limit for this consumer
            });
        });

        Configure(configurator, context);
    }

    public static void ConfigureRabbitMq(this IRabbitMqBusFactoryConfigurator configurator, IBusRegistrationContext context, string connectionString)
    {
        configurator.Host(connectionString);

        configurator.ReceiveEndpoint("overnight-service", e =>
        {
            e.PrefetchCount = 64;
            e.ConcurrentMessageLimit = 5;
            e.ConfigureConsumer<SyncParticipantCommandHandler>(context);
        });

        configurator.ReceiveEndpoint("tasks-service", e =>
        {
            e.PrefetchCount = 64;
            e.ConcurrentMessageLimit = 5;

            e.ConfigureConsumer<PriTaskCompletedWatcherConsumer>(context);
            e.ConfigureConsumer<RaisePaymentsAfterApprovalConsumer>(context);
        });

        configurator.ReceiveEndpoint("payment-service", e =>
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

        Configure(configurator, context);
    }

    public static void ConfigureAmazonSqs(this IAmazonSqsBusFactoryConfigurator configurator, IBusRegistrationContext context, string host, Action<IAmazonSqsHostConfigurator> settings)
    {
        configurator.Host(host, settings);

        //configurator.ReceiveEndpoint("overnight-service", e =>
        //{
        //    e.PrefetchCount = 64;
        //    e.ConcurrentMessageLimit = 5;
        //    e.ConfigureConsumer<SyncParticipantCommandHandler>(context);
        //});

        //configurator.ReceiveEndpoint("tasks-service", e =>
        //{
        //    e.PrefetchCount = 64;
        //    e.ConcurrentMessageLimit = 5;

        //    e.ConfigureConsumer<PriTaskCompletedWatcherConsumer>(context);
        //    e.ConfigureConsumer<RaisePaymentsAfterApprovalConsumer>(context);
        //});

        configurator.ReceiveEndpoint("hmpps-co-financing-organisation-development-cfocats-dev_events_queue", e =>
        {
            e.ConcurrentMessageLimit = 1;
            e.ConfigureConsumeTopology = false;

            e.ConfigureConsumer<RecordActivityPaymentConsumer>(context);
            e.ConfigureConsumer<RecordEducationPayment>(context);
            e.ConfigureConsumer<RecordEmploymentPayment>(context);
            e.ConfigureConsumer<RecordEnrolmentPaymentConsumer>(context);
            e.ConfigureConsumer<RecordHubInductionPaymentConsumer>(context);
            e.ConfigureConsumer<RecordPreReleaseSupportPayment>(context);
            e.ConfigureConsumer<RecordThroughTheGatePaymentConsumer>(context);
            e.ConfigureConsumer<RecordWingInductionPaymentConsumer>(context);

            e.ConfigureConsumers(context); // temp fix
        });

        //configurator.ReceiveEndpoint("general-consumer-queue", e =>
        //{
        //    e.ConfigureConsumeTopology = false;
        //    e.ConfigureConsumers(context);
        //});

        Configure(configurator, context);
    }
}
