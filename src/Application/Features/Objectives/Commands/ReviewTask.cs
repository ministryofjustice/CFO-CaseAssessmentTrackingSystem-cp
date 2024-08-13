﻿using Cfo.Cats.Application.Common.Security;
using Cfo.Cats.Application.Common.Validators;
using Cfo.Cats.Application.SecurityConstants;

namespace Cfo.Cats.Application.Features.Objectives.Commands;

public class ReviewTask
{
    [RequestAuthorize(Policy = SecurityPolicies.Enrol)]
    public class Command : IRequest<Result>
    {
        [Description("Task Id")]
        public required Guid TaskId { get; set; }

        [Description("Objective Id")]
        public required Guid ObjectiveId { get; set; }

        [Description("Cancel")]
        public required bool Close { get; set; }

        [Description("Justification")]
        public string Justification { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork unitOfWork) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var objective = await unitOfWork.DbContext.Objectives
                .FindAsync(request.ObjectiveId);

            if (objective is null)
            {
                throw new NotFoundException("Cannot find objective", request.ObjectiveId);
            }

            var task = objective.Tasks
                .FirstOrDefault(task => task.Id == request.TaskId);

            if (task is null)
            {
                throw new NotFoundException("Cannot find task", request.TaskId);
            }

            if (request.Close)
            {
                task.Close(request.Justification);
            }
            else
            {
                task.Complete(request.Justification);
            }

            return Result.Success();
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TaskId)
                .NotNull();

            RuleFor(x => x.ObjectiveId)
                .NotNull();

            When(x => x.Close, () =>
            {
                RuleFor(x => x.Justification)
                    .NotEmpty()
                    .WithMessage("Justification is required when closing a task");
            });

            RuleFor(x => x.Justification)
                .Matches(ValidationConstants.Notes)
                .WithMessage(string.Format(ValidationConstants.NotesMessage, "Justification"));
        }

    }

}
