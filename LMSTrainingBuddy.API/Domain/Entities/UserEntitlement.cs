namespace LMSTrainingBuddy.API.Domain.Entities;

public sealed class UserEntitlement
{
    public required string UserId { get; init; }
    public required string CourseId { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
