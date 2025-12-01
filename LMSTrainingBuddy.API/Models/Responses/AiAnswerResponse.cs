namespace LMSTrainingBuddy.API.Models.Responses;

public sealed class AiAnswerResponse
{
    public required string Answer { get; init; }
    public IReadOnlyCollection<string> Sources { get; init; } = Array.Empty<string>();
    public string? ThreadId { get; init; }
    public string? Classification { get; init; }
}
