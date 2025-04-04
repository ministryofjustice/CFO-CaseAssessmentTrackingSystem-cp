using Amazon;
using Cfo.Cats.Application.Features.Activities.IntegrationEvents;
using Cfo.Cats.Application.Features.Inductions.IntegrationEvents;
using Cfo.Cats.Application.Features.ManagementInformation.IntegrationEventHandlers;
using Cfo.Cats.Application.Features.Participants.IntegrationEvents;
using Cfo.Cats.Application.Features.Participants.MessageBus;
using Cfo.Cats.Application.Features.PathwayPlans.IntegrationEvents;
using Cfo.Cats.Application.Features.PRIs.IntegrationEventHandlers;
using Cfo.Cats.Application.Features.PRIs.IntegrationEvents;
using MassTransit;
using MassTransit.AmazonSqsTransport.Configuration;

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

    public static void ConfigureAmazonSqs(this IAmazonSqsBusFactoryConfigurator cfg, IBusRegistrationContext context, string host)
    {
        cfg.Host(host, settings => 
        {
            // Use "default" SDK configuration
        });


        // Configure topics
        //cfg.Message<ActivityApprovedIntegrationEvent>(cfg => cfg.SetEntityName("cloud-platform-hmpps-co-financing-organisation-9d0a123dedcdc30d3a6dc3dd142a63f3.fifo"));
        //cfg.Message<HubInductionCreatedIntegrationEvent>(cfg => cfg.SetEntityName("cloud-platform-hmpps-co-financing-organisation-3c88c879923a967e8fa7323fdf97ef6d.fifo"));
        //cfg.Message<ObjectiveTaskCompletedIntegrationEvent>(cfg => cfg.SetEntityName("cloud-platform-hmpps-co-financing-organisation-3abc996c60465a3612055d7eea3fa4e0.fifo"));
        //cfg.Message<ParticipantTransitionedIntegrationEvent>(cfg => cfg.SetEntityName("cloud-platform-hmpps-co-financing-organisation-35ac474a94cf42adb6ed59d1b28079df.fifo"));
        //cfg.Message<PRIAssignedIntegrationEvent>(cfg => cfg.SetEntityName("cloud-platform-hmpps-co-financing-organisation-9c81b968c997a4d6d4fcf8001571e570.fifo"));
        //cfg.Message<PRIThroughTheGateCompletedIntegrationEvent>(cfg => cfg.SetEntityName("cloud-platform-hmpps-co-financing-organisation-1c9a14cee6aaacfe83381c8809b444e2.fifo"));
        //cfg.Message<SyncParticipantCommand>(cfg => cfg.SetEntityName("cloud-platform-hmpps-co-financing-organisation-c06851029602fd08a735064924f8ae1b.fifo"));
        //cfg.Message<WingInductionCreatedIntegrationEvent>(cfg => cfg.SetEntityName("cloud-platform-hmpps-co-financing-organisation-f7d5d3cc77fc4d96b80d029c90c88d34.fifo"));

        cfg.ReceiveEndpoint("hmpps-co-financing-organisation-development-cfocats-dev_overnight_queue.fifo", e =>
        {
            e.PrefetchCount = 64;
            e.ConcurrentMessageLimit = 5;

            // Prevent MassTransit from creating topics/subscriptions.
            e.ConfigureConsumeTopology = false;

            // Use SQS redrive DLQ
            e.ThrowOnSkippedMessages();
            e.RethrowFaultedMessages();

            e.ConfigureConsumer<SyncParticipantCommandHandler>(context);
        });

        cfg.ReceiveEndpoint("hmpps-co-financing-organisation-development-cfocats-dev_tasks_queue.fifo", e =>
        {
            e.PrefetchCount = 64;
            e.ConcurrentMessageLimit = 5;

            // Prevent MassTransit from creating topics/subscriptions.
            e.ConfigureConsumeTopology = false;

            // Use SQS redrive DLQ
            e.ThrowOnSkippedMessages();
            e.RethrowFaultedMessages();

            e.ConfigureConsumer<PriTaskCompletedWatcherConsumer>(context);
            e.ConfigureConsumer<RaisePaymentsAfterApprovalConsumer>(context);
        });

        cfg.ReceiveEndpoint("hmpps-co-financing-organisation-development-cfocats-dev_payment_queue.fifo", e =>
        {
            e.ConcurrentMessageLimit = 1;

            // Prevent MassTransit from creating topics/subscriptions.
            e.ConfigureConsumeTopology = false;

            // Use SQS redrive DLQ
            e.ThrowOnSkippedMessages();
            e.RethrowFaultedMessages();

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
