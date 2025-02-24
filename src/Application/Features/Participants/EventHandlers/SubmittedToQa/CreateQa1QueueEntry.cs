﻿using Cfo.Cats.Domain.Entities.Participants;
using Cfo.Cats.Domain.Events;

namespace Cfo.Cats.Application.Features.Participants.EventHandlers.SubmittedToQa;

public class CreateQa1QueueEntry(IUnitOfWork unitOfWork) : INotificationHandler<ParticipantTransitionedDomainEvent>
{

    public async Task Handle(ParticipantTransitionedDomainEvent notification, CancellationToken cancellationToken)
    {
        if (notification.To == EnrolmentStatus.SubmittedToAuthorityStatus)
        {
            var qa1 = EnrolmentQa1QueueEntry.Create(notification.Item.Id);

            // get the most recent PQA entry
            var pqa = await unitOfWork
                .DbContext.EnrolmentPqaQueue
                .AsNoTracking()
                .Where(q => q.ParticipantId == notification.Item.Id)
                .OrderByDescending(q => q.Created)
                .Take(1)
                .Select(q => new
                {
                    q.TenantId,
                    q.SupportWorkerId,
                    q.ConsentDate
                })
                .FirstAsync(cancellationToken);
            
            qa1.TenantId = pqa.TenantId;
            qa1.SupportWorkerId = pqa.SupportWorkerId;
            qa1.ConsentDate = pqa.ConsentDate;
            
            await unitOfWork.DbContext.EnrolmentQa1Queue.AddAsync(qa1, cancellationToken);    
        }
    }
}