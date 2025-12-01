using LMSTrainingBuddy.API.Domain.Entities;

namespace LMSTrainingBuddy.API.Infrastructure.VectorStore;

public interface IVectorStore
{
    Task UpsertChunksAsync(IEnumerable<CourseContentChunk> chunks, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CourseContentChunk>> SearchAsync(
        string query,
        IReadOnlyCollection<string> allowedCourseIds,
        int top,
        CancellationToken cancellationToken);
}
