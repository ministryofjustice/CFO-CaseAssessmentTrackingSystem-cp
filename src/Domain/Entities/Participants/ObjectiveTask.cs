using Cfo.Cats.Domain.Common.Entities;
using Cfo.Cats.Domain.Common.Enums;
using Cfo.Cats.Domain.Events;
using Cfo.Cats.Domain.Identity;

namespace Cfo.Cats.Domain.Entities.Participants;

public class ObjectiveTask : BaseAuditableEntity<Guid>
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private ObjectiveTask()
    {
        Id = Guid.CreateVersion7();
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public ObjectiveTask Complete(CompletionStatus status, string completedBy, string? justification = null)
    {
        Completed = DateTime.UtcNow;
        Justification = justification;
        CompletedStatus = status;
        CompletedBy = completedBy;
        AddDomainEvent(new ObjectiveTaskCompletedDomainEvent(this));
        return this;
    }

    public static ObjectiveTask Create(string description, DateTime due, bool isMandatory = false)
    {
        ObjectiveTask task = new()
        {
            Description = description,
            Due = due,
            IsMandatory = isMandatory
        };

        task.AddDomainEvent(new ObjectiveTaskCreatedDomainEvent(task));
        return task;
    }

    public void Extend(DateTime due)
    {
        Due = due;
    }

    public void Rename(string description)
    {
        Description = description;
    }

    public ObjectiveTask AtIndex(int index)
    {
        Index = index;
        return this;
    }

    public DateTime Due { get; private set; }
    public DateTime? Completed { get; private set; }
    public string? CompletedBy { get; private set; }
    public CompletionStatus? CompletedStatus { get; private set; }
    public int Index { get; private set; }
    public string? Justification { get; private set; }
    public Guid ObjectiveId { get; private set; }
    public string Description { get; private set; }

    public bool IsMandatory { get; private set; }
    public bool IsCompleted => Completed is not null;

    public virtual ApplicationUser? CompletedByUser { get; set; }
}
