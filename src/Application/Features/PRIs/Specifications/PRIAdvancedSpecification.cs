﻿namespace Cfo.Cats.Application.Features.PRIs.Specifications;

public class PRIAdvancedSpecification : Specification<Domain.Entities.PRIs.PRI>
{
    public PRIAdvancedSpecification(PRIAdvancedFilter filter)
    {
        Query.Where(p => p.CreatedBy == filter.CurrentUser!.UserId, filter.IncludeOutgoing);

        Query.Where(p => p.AssignedTo == filter.CurrentUser!.UserId, filter.IncludeIncoming);

        Query.Where(
                   // if we have passed a filter through, search the surname and current location
                   p => p.ParticipantId!.Contains(filter.Keyword!)
                        || p.CreatedBy!.Contains(filter.Keyword!)
                        || p.AssignedTo!.Contains(filter.Keyword!)
                        || p.ExpectedReleaseRegion.Name.Contains(filter.Keyword!),
                   string.IsNullOrEmpty(filter.Keyword) == false);
    }
}