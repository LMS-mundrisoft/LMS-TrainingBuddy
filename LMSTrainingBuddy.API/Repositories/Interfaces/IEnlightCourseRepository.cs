using LMSTrainingBuddy.API.Domain.Entities;

namespace LMSTrainingBuddy.API.Repositories.Interfaces;

public interface IEnlightCourseRepository
{
    Task<IReadOnlyCollection<Course>> GetCoursesAsync(CancellationToken cancellationToken);
}
