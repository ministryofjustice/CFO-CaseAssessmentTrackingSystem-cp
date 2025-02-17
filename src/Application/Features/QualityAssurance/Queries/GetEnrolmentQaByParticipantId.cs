using Cfo.Cats.Application.Common.Security;
using Cfo.Cats.Application.Features.QualityAssurance.DTOs;
using Cfo.Cats.Application.SecurityConstants;
using Cfo.Cats.Domain.Entities.Participants;

namespace Cfo.Cats.Application.Features.QualityAssurance.Queries;

public static class GetEnrolmentQaByParticipantId
{
    [RequestAuthorize(Policy = SecurityPolicies.SeniorInternal)]
    public class Query : IRequest<ParticipantEnrolmentDto>
    {
        public required string ParticipantId { get; set; }
    }

    private class Handler(IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<Query, ParticipantEnrolmentDto>
    {
        public async Task<ParticipantEnrolmentDto> Handle(Query request, CancellationToken cancellationToken)
        {
            var model = new ParticipantEnrolmentDto
            {
                Pqa = await Pqa(request, cancellationToken),
                Qa1 = await Qa1(request, cancellationToken),
                Qa2 = await Qa2(request, cancellationToken),
                Escalation = await Escalation(request, cancellationToken),
            };

            return model;
        }
        
        private Task<EnrolmentQueueEntryDto[]> Pqa(Query request, CancellationToken cancellationToken)
        {
            return unitOfWork.DbContext
                .EnrolmentPqaQueue
                .AsNoTracking()
                .Where(q => q.ParticipantId == request.ParticipantId)
                .ProjectTo<EnrolmentQueueEntryDto>(mapper.ConfigurationProvider)
                .ToArrayAsync(cancellationToken);
        }
        
        private Task<EnrolmentQueueEntryDto[]> Qa1(Query request, CancellationToken cancellationToken)
        {
            return unitOfWork.DbContext
                .EnrolmentQa1Queue
                .AsNoTracking()
                .Where(q => q.ParticipantId == request.ParticipantId)
                .ProjectTo<EnrolmentQueueEntryDto>(mapper.ConfigurationProvider)
                .ToArrayAsync(cancellationToken);
        }
        
        private Task<EnrolmentQueueEntryDto[]> Qa2(Query request, CancellationToken cancellationToken)
        {
            return unitOfWork.DbContext
                .EnrolmentQa2Queue
                .AsNoTracking()
                .Where(q => q.ParticipantId == request.ParticipantId)
                .ProjectTo<EnrolmentQueueEntryDto>(mapper.ConfigurationProvider)
                .ToArrayAsync(cancellationToken);
        }
        
        private Task<EnrolmentQueueEntryDto[]> Escalation(Query request, CancellationToken cancellationToken)
        {
            return unitOfWork.DbContext
                .EnrolmentEscalationQueue
                .AsNoTracking()
                .Where(q => q.ParticipantId == request.ParticipantId)
                .ProjectTo<EnrolmentQueueEntryDto>(mapper.ConfigurationProvider)
                .ToArrayAsync(cancellationToken);
        }

    }
    
    
}