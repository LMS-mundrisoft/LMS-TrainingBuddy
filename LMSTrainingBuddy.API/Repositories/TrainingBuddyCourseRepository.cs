using LMSTrainingBuddy.API.Domain.Entities;
using LMSTrainingBuddy.API.Infrastructure.Data;
using LMSTrainingBuddy.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LMSTrainingBuddy.API.Repositories;

public sealed class TrainingBuddyCourseRepository : ITrainingBuddyCourseRepository
{
    private readonly IDbContextFactory<CourseDbContext> _contextFactory;

    public TrainingBuddyCourseRepository(IDbContextFactory<CourseDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyCollection<Course>> GetTopCoursesAsync(int limit, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var courses = await context.Courses
                                   .AsNoTracking()
                                   .Where(course => course.IsFileUpdated == false)
                                   .OrderBy(course => course.CourseId)
                                   .Take(limit)
                                   .ToListAsync(cancellationToken);

        return courses;
    }

    public async Task<IReadOnlyCollection<Course>> GetCoursesAsync(IReadOnlyCollection<int> courseIds, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Courses.AsNoTracking();

        if (courseIds != null && courseIds.Count > 0)
        {
            query = query.Where(course => courseIds.Contains(course.CourseId));
        }

        var courses = await query.ToListAsync(cancellationToken);
        return courses;
    }
    public async Task UpdateIsFileUpdatedAsync(int courseId, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var existingCourse = await context.Courses
                                                 .FirstOrDefaultAsync(entity => entity.CourseId == courseId, cancellationToken);

            if (existingCourse is not null)
            {
                existingCourse.IsFileUpdated = true;
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task UpsertAsync(IEnumerable<Course> courses, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            foreach (var course in courses)
            {
                var existingCourse = await context.Courses
                                                  .FirstOrDefaultAsync(entity => entity.CourseId == course.CourseId, cancellationToken);

                if (existingCourse is null)
                {
                    await context.Courses.AddAsync(new Course
                    {
                        CourseId = course.CourseId,
                        CourseName = course.CourseName,
                        Code = course.Code,
                        Description = course.Description,
                        Created = course.Created,
                        LastModified = course.LastModified,
                        AvailableToAllOrganizations = course.AvailableToAllOrganizations,
                        AvailableInstructorLed = course.AvailableInstructorLed,
                        AvailableSelfPaced = course.AvailableSelfPaced,
                        Archived = course.Archived,
                        IsFileUpdated = false,
                        OutlineObjective = course.OutlineObjective,
                        OutlineOverview = course.OutlineOverview,
                        OutlineTargetAudience = course.OutlineTargetAudience,
                        OutlineLessons = course.OutlineLessons,
                        OrganizationIds = course.OrganizationIds
                    }, cancellationToken);
                }
                else
                {
                    existingCourse.CourseName = course.CourseName;
                    existingCourse.Code = course.Code;
                    existingCourse.Description = course.Description;
                    existingCourse.Created = course.Created;
                    existingCourse.LastModified = course.LastModified;
                    existingCourse.AvailableToAllOrganizations = course.AvailableToAllOrganizations;
                    existingCourse.AvailableInstructorLed = course.AvailableInstructorLed;
                    existingCourse.AvailableSelfPaced = course.AvailableSelfPaced;
                    existingCourse.Archived = course.Archived;
                    existingCourse.IsFileUpdated = false;
                    existingCourse.OutlineObjective = course.OutlineObjective;
                    existingCourse.OutlineOverview = course.OutlineOverview;
                    existingCourse.OutlineTargetAudience = course.OutlineTargetAudience;
                    existingCourse.OutlineLessons = course.OutlineLessons;
                    existingCourse.OrganizationIds = course.OrganizationIds;
                }
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
}
