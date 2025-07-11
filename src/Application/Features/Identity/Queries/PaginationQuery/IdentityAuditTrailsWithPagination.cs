using Cfo.Cats.Application.Common.Security;
using Cfo.Cats.Application.Common.Validators;
using Cfo.Cats.Application.Features.Identity.DTOs;
using Cfo.Cats.Application.Features.Identity.Specifications;
using Cfo.Cats.Application.SecurityConstants;

namespace Cfo.Cats.Application.Features.Identity.Queries.PaginationQuery;

public static class IdentityAuditTrailsWithPagination
{
    [RequestAuthorize(Policy = SecurityPolicies.SystemFunctionsRead)]
    public class Query : IdentityAuditTrailAdvancedFilter,
        IRequest<Result<PaginatedData<IdentityAuditTrailDto>>>
    {
        public IdentityAuditTrailAdvancedSpecification Specification => new (this);
    }

    public class Handler : IRequestHandler<Query, Result<PaginatedData<IdentityAuditTrailDto>>>
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;

        public Handler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }

        public async Task<Result<PaginatedData<IdentityAuditTrailDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var data = await unitOfWork.DbContext
                        .IdentityAuditTrails.OrderBy($"{request.OrderBy} {request.SortDirection}")
                        .ProjectToPaginatedDataAsync<IdentityAuditTrail, IdentityAuditTrailDto>(
                            request.Specification,
                            request.PageNumber,
                            request.PageSize,
                            mapper.ConfigurationProvider,
                            cancellationToken
                        );

            return data;
        }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.PageNumber)
            .GreaterThan(0)
            .WithMessage(string.Format(ValidationConstants.PositiveNumberMessage, "Page Number"));

            RuleFor(r => r.PageSize)
                .GreaterThan(0)
                .LessThanOrEqualTo(1000)
                .WithMessage(string.Format(ValidationConstants.MaximumPageSizeMessage, "Page Size"));

            RuleFor(r => r.SortDirection)
                .Matches(ValidationConstants.SortDirection)
                .WithMessage(ValidationConstants.SortDirectionMessage);

            When(r => r.UserName is not null, () => {
                RuleFor(r => r.UserName)
                    .EmailAddress();
            });
         
        }
    }
}