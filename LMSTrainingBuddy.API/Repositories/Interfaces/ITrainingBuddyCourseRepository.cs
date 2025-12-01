using LMSTrainingBuddy.API.Domain.Entities;

namespace LMSTrainingBuddy.API.Repositories.Interfaces;

public interface ITrainingBuddyCourseRepository
{
    Task<IReadOnlyCollection<Course>> GetCoursesAsync(IReadOnlyCollection<int> courseIds, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Course>> GetTopCoursesAsync(int limit, CancellationToken cancellationToken);
    Task UpsertAsync(IEnumerable<Course> courses, CancellationToken cancellationToken);
    Task UpdateIsFileUpdatedAsync(int courseId, CancellationToken cancellationToken);
}
