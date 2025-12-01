namespace LMSTrainingBuddy.API.Domain.Entities;

public sealed class CourseContentChunk
{
    public required string ChunkId { get; init; }
    public required string CourseId { get; init; }
    public string? ModuleId { get; init; }
    public string? LessonId { get; init; }
    public string? ClientId { get; init; }
    public string? Language { get; init; }
    public required string Text { get; init; }
    public float[]? Embedding { get; init; }
    public double Score { get; init; }
}
