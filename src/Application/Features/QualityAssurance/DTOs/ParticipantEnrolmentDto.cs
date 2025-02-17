namespace Cfo.Cats.Application.Features.QualityAssurance.DTOs;

public record ParticipantEnrolmentDto
{
    public required EnrolmentQueueEntryDto[] Pqa { get; init; }
    public required EnrolmentQueueEntryDto[] Qa1 { get; init; }
    public required EnrolmentQueueEntryDto[] Qa2 { get; init; }
    public required EnrolmentQueueEntryDto[] Escalation { get; init; }
}