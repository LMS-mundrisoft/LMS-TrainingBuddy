using LMSTrainingBuddy.API.Domain.Entities;
using LMSTrainingBuddy.API.Infrastructure.Data;
using LMSTrainingBuddy.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LMSTrainingBuddy.API.Repositories;

public sealed class EnlightCourseRepository : IEnlightCourseRepository
{
    private readonly IDbContextFactory<EnlightDbContext> _contextFactory;

    public EnlightCourseRepository(IDbContextFactory<EnlightDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyCollection<Course>> GetCoursesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var courseLessons = await dbContext.CourseLessons
                                              .AsNoTracking()
                                              .ToListAsync(cancellationToken);

            var lessonsLookup = courseLessons
                .GroupBy(lesson => lesson.CourseId)
                .ToDictionary(
                    grouping => grouping.Key,
                    grouping => string.Join(
                        "\n",
                        grouping
                            .OrderBy(lesson => lesson.LessonId)
                            .Select(FormatLesson)
                            .Where(formattedLesson => !string.IsNullOrWhiteSpace(formattedLesson))));

            var organizationsLookup = await dbContext.CourseOrganizations
                                                     .AsNoTracking()
                                                     .GroupBy(courseOrganization => courseOrganization.CourseId)
                                                     .ToDictionaryAsync(
                                                         grouping => grouping.Key,
                                                         grouping => string.Join(
                                                             ",",
                                                             grouping
                                                                 .Select(courseOrganization => courseOrganization.OrganizationId)
                                                                 .Distinct()
                                                                 .OrderBy(organizationId => organizationId)),
                                                         cancellationToken);

            var courses = await dbContext.Courses
                                         .AsNoTracking()
                                         .Where(course => course.Archived == false)
                                         .Select(course => new Course
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
                                             OutlineObjective = course.OutlineObjective,
                                             OutlineOverview = course.OutlineOverview,
                                             OutlineTargetAudience = course.OutlineTargetAudience,
                                             OutlineLessons = null,
                                             OrganizationIds = null
                                         }).ToListAsync(cancellationToken);

            foreach (var course in courses)
            {
                if (lessonsLookup.TryGetValue(course.CourseId, out string? outlineLessons))
                {
                    course.OutlineLessons = outlineLessons;
                }

                if (course.AvailableToAllOrganizations == false && organizationsLookup.TryGetValue(course.CourseId, out string? organizationIds))
                {
                    course.OrganizationIds = organizationIds;
                }
            }

            return courses;
        }
        catch (Exception ex)
        {
            // Log the exception (logging mechanism not shown here)
            throw new ApplicationException("An error occurred while retrieving courses.", ex);
        }
    }

    private static string FormatLesson(CourseLesson lesson)
    {
        var lessonTitle = lesson.LessonTitle?.Trim();
        var lessonDescription = lesson.LessonDescription?.Trim();

        if (string.IsNullOrWhiteSpace(lessonTitle) && string.IsNullOrWhiteSpace(lessonDescription))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(lessonDescription))
        {
            return lessonTitle ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(lessonTitle))
        {
            return lessonDescription ?? string.Empty;
        }

        return $"{lessonTitle}: {lessonDescription}";
    }
}
