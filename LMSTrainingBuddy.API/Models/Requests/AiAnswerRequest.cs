namespace LMSTrainingBuddy.API.Models.Requests;

public sealed class AiAnswerRequest
{
    public required string UserId { get; init; }
    public required string Question { get; init; }
    public string? ThreadId { get; init; }
    public string? OrganizationIds { get; init; }
}
