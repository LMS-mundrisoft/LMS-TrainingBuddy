using LMSTrainingBuddy.API.Domain.Entities;
using System.Linq;

namespace LMSTrainingBuddy.API.Infrastructure.VectorStore;

public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly List<CourseContentChunk> _chunks = new();

    public Task UpsertChunksAsync(IEnumerable<CourseContentChunk> chunks, CancellationToken cancellationToken)
    {
        foreach (var chunk in chunks)
        {
            var existing = _chunks.FirstOrDefault(c => c.ChunkId == chunk.ChunkId);
            if (existing != null)
            {
                _chunks.Remove(existing);
            }
            _chunks.Add(chunk);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<CourseContentChunk>> SearchAsync(string query, IReadOnlyCollection<string> allowedCourseIds, int top, CancellationToken cancellationToken)
    {
        var normalized = query.ToLowerInvariant();
        var results = _chunks
            .Where(c => allowedCourseIds.Contains(c.CourseId))
            .Select(chunk => new CourseContentChunk
            {
                ChunkId = chunk.ChunkId,
                CourseId = chunk.CourseId,
                ModuleId = chunk.ModuleId,
                LessonId = chunk.LessonId,
                ClientId = chunk.ClientId,
                Language = chunk.Language,
                Text = chunk.Text,
                Score = ComputeScore(normalized, chunk.Text)
            })
            .OrderByDescending(c => c.Score)
            .Take(top)
            .ToArray();

        return Task.FromResult((IReadOnlyCollection<CourseContentChunk>)results);
    }

    private static double ComputeScore(string normalizedQuery, string text)
    {
        var normalizedText = text.ToLowerInvariant();
        return normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Count(token => normalizedText.Contains(token));
    }
}
