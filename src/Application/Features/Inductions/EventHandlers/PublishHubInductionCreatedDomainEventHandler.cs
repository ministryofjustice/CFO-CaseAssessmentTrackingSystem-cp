using Cfo.Cats.Application.Features.Inductions.IntegrationEvents;
using Cfo.Cats.Application.Outbox;
using Cfo.Cats.Domain.Events;

namespace Cfo.Cats.Application.Features.Inductions.EventHandlers;

public class PublishHubInductionCreatedDomainEventHandler(IUnitOfWork unitOfWork) : INotificationHandler<HubInductionCreatedDomainEvent>
{
    public async Task Handle(HubInductionCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var e = new HubInductionCreatedIntegrationEvent(notification.Item.Id, DateTime.UtcNow);
        await unitOfWork.DbContext.InsertOutboxMessage(e);
    }
}
