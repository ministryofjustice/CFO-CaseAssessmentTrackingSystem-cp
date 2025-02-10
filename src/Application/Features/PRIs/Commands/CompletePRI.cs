﻿using Cfo.Cats.Application.Common.Security;
using Cfo.Cats.Application.Common.Validators;
using Cfo.Cats.Application.SecurityConstants;

namespace Cfo.Cats.Application.Features.PRIs.Commands;

public static class CompletePRI
{
    [RequestAuthorize(Policy = SecurityPolicies.Enrol)]

    public class Command : IRequest<Result>
    {
        [Description("Participant Id")]
        public required string ParticipantId { get; set; }

        [Description("Completed By")]
        public required string? CompletedBy { get; set; }
    }

    class Handler(IUnitOfWork unitOfWork) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var pri = await unitOfWork.DbContext.PRIs
                .SingleOrDefaultAsync(p => p.ParticipantId == request.ParticipantId
                && p.Status == PriStatus.Accepted, cancellationToken);

            pri!.Complete(request.CompletedBy);

            return Result.Success();
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ParticipantId)
                .Length(9)
                .Matches(ValidationConstants.AlphaNumeric)
                .WithMessage(string.Format(ValidationConstants.AlphaNumericMessage, "Participant Id"));

            RuleFor(c => c.CompletedBy)
                    .NotNull()
                    .MinimumLength(36);
        }
    }

    public class A_PRIExists : AbstractValidator<Command>
    {
        private readonly IUnitOfWork _unitOfWork;

        public A_PRIExists(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(p => p.ParticipantId)
                .NotNull()
                .Length(9)
                .WithMessage("Invalid Participant Id")
                .Must(Exist)
                .WithMessage("PRI does not exist")
                .Matches(ValidationConstants.AlphaNumeric).WithMessage(string.Format(ValidationConstants.AlphaNumericMessage, "Participant Id"));
        }

        // Check if the PRI exists in the database
        private bool Exist(string participantId)
            => _unitOfWork.DbContext.PRIs.Any(p => p.ParticipantId == participantId);
    }

    public class B_EntryMustHaveActualReleaseDate : AbstractValidator<Command>
    {
        private readonly IUnitOfWork _unitOfWork;

        public B_EntryMustHaveActualReleaseDate(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(c => c.ParticipantId)
                .Must(ActualReleaseDateMustExist)
                .WithMessage("Actual Release Date Required");
        }
        private bool ActualReleaseDateMustExist(string identifier)
            => _unitOfWork.DbContext.PRIs.Any(p => p.ParticipantId == identifier && p.ActualReleaseDate != null);
    }

    public class C_PRIMustNotAlreadyBeCompletedOrRejected : AbstractValidator<Command>
    {
        private readonly IUnitOfWork _unitOfWork;

        public C_PRIMustNotAlreadyBeCompletedOrRejected(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(p => p.ParticipantId)
                .Must(NotBeCompletedRejected)
                .WithMessage("PRI has been already completed or abandoned")
                .Matches(ValidationConstants.AlphaNumeric).WithMessage(string.Format(ValidationConstants.AlphaNumericMessage, "Participant Id"));
        }

        private bool NotBeCompletedRejected(string participantId)
            => _unitOfWork.DbContext.PRIs.Any(p => p.ParticipantId == participantId
                    && PriStatus.ActiveList.Contains(p.Status));                        
    }

    public class D_PRIMustHaveClosedTasks : AbstractValidator<Command>
    {
        private readonly IUnitOfWork _unitOfWork;

        public D_PRIMustHaveClosedTasks(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(p => p.ParticipantId)
                .Must(PRIMustHaveClosedTasks)
                .WithMessage("PRI must not have open Tasks")
                .Matches(ValidationConstants.AlphaNumeric).WithMessage(string.Format(ValidationConstants.AlphaNumericMessage, "Participant Id"));

        }

        private bool PRIMustHaveClosedTasks(string participantId)
        {
            var pri = _unitOfWork.DbContext.PRIs.First(p => p.ParticipantId == participantId
                        && (PriStatus.ActiveList.Contains(p.Status)));


            var tasks = _unitOfWork.DbContext.PathwayPlans
              .AsNoTracking()
              .SelectMany(p => p.Objectives)
                .SelectMany(o => o.Tasks)
              .Where(t => t.ObjectiveId == pri.ObjectiveId)
              .ToArray();

            // All mandatory tasks must be complete
            if (tasks.Where(t => t.IsMandatory).Count(t => t.IsCompleted && t.CompletedStatus == CompletionStatus.Done) != 2)
            {
                return false;
            }

            return true;
        }
    }
}    