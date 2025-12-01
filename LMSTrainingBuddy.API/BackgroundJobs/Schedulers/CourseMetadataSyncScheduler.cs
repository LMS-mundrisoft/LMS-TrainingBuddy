using LMSTrainingBuddy.API.Domain.Entities;
using LMSTrainingBuddy.API.Models;
using LMSTrainingBuddy.API.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using System.Threading;

namespace LMSTrainingBuddy.API.BackgroundJobs.Schedulers;

/// <summary>
///     Hosted service that kicks off <see cref="CourseMetadataSyncJob"/> every five minutes so
///     course information is continuously synced into the configured repository.
/// </summary>
public sealed class CourseMetadataSyncScheduler : BackgroundService
{
    private readonly ILogger<CourseMetadataSyncScheduler> _logger;
    private readonly IEnlightCourseRepository _enlightCourseRepository;
    private readonly ITrainingBuddyCourseRepository _trainingBuddyRepository;
    private readonly TimeSpan _runInterval;

    public CourseMetadataSyncScheduler(
                                       ILogger<CourseMetadataSyncScheduler> logger,
                                       IEnlightCourseRepository enlightCourseRepository,
                                       IOptions<AppSettings> appSettings,
                                       ITrainingBuddyCourseRepository trainingBuddyRepository)
    {
        _logger = logger;
        _enlightCourseRepository = enlightCourseRepository;
        _runInterval = TimeSpan.FromMinutes(appSettings.Value.BackgroundJobs.RunIntervalMinutes);
        _trainingBuddyRepository = trainingBuddyRepository;
    }

    public async Task<List<Course>> GetCoursesFromDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            var courses = await _enlightCourseRepository.GetCoursesAsync(cancellationToken);
            return courses.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching courses from Enlight database.");
        }

        return new List<Course>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Course metadata sync scheduler started; interval: {Interval}.", _runInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var courses = await GetCoursesFromDatabaseAsync(stoppingToken);

                if (courses != null && courses.Any())
                {
                    await _trainingBuddyRepository.UpsertAsync(courses, stoppingToken);
                    _logger.LogInformation("Course metadata sync completed at {Timestamp}.", DateTimeOffset.UtcNow);
                }
                else
                {
                    _logger.LogWarning("No course IDs available for syncing.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Course metadata sync scheduler cancellation requested.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Course metadata sync failed.");
            }

            try
            {
                await Task.Delay(_runInterval, stoppingToken);
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Course metadata sync scheduler cancellation requested during delay.");
                break;
            }
        }

        _logger.LogInformation("Course metadata sync scheduler stopped.");
    }
}
