using LMSTrainingBuddy.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMSTrainingBuddy.API.Infrastructure.Data;

public sealed class EnlightDbContext : DbContext
{
    public EnlightDbContext(DbContextOptions<EnlightDbContext> options) : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseLesson> CourseLessons => Set<CourseLesson>();
    public DbSet<CourseOrganization> CourseOrganizations => Set<CourseOrganization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Courses");

            entity.HasKey(course => course.CourseId);

            entity.Property(course => course.CourseId)
                  .HasColumnName("CourseId");

            entity.Property(course => course.CourseName)
                  .HasColumnName("Name");

            entity.Property(course => course.Code)
                  .HasColumnName("Code");

            entity.Property(course => course.Description)
                  .HasColumnName("Description");

            entity.Property(course => course.Created)
                  .HasColumnName("Created");

            entity.Property(course => course.LastModified)
                  .HasColumnName("LastModified");

            entity.Property(course => course.AvailableToAllOrganizations)
                  .HasColumnName("AvailableToAllOrganizations");

            entity.Property(course => course.AvailableInstructorLed)
                  .HasColumnName("AvailableInstructorLed");

            entity.Property(course => course.AvailableSelfPaced)
                  .HasColumnName("AvailableSelfPaced");

            entity.Property(course => course.Archived)
                  .HasColumnName("Archived");

            entity.Ignore(course => course.IsFileUpdated);

            entity.Property(course => course.OutlineObjective)
                  .HasColumnName("Objective");

            entity.Property(course => course.OutlineOverview)
                  .HasColumnName("Overview");

            entity.Property(course => course.OutlineTargetAudience)
                  .HasColumnName("TargetAudience");

            entity.Ignore(course => course.OutlineLessons);
            entity.Ignore(course => course.OrganizationIds);
        });

        modelBuilder.Entity<CourseLesson>(entity =>
        {
            entity.ToTable("CourseLessons");

            entity.HasKey(lesson => lesson.Id);

            entity.Property(lesson => lesson.Id)
                  .HasColumnName("Id");

            entity.Property(lesson => lesson.CourseId)
                  .HasColumnName("CourseId");

            entity.Property(lesson => lesson.LessonId)
                  .HasColumnName("LessonId");

            entity.Property(lesson => lesson.LessonTitle)
                  .HasColumnName("LessonTitle");

            entity.Property(lesson => lesson.LessonDescription)
                  .HasColumnName("LessonDescription");
        });

        modelBuilder.Entity<CourseOrganization>(entity =>
        {
            entity.ToTable("Courses_Organizations");

            entity.HasKey(courseOrganization => new { courseOrganization.CourseId, courseOrganization.OrganizationId });

            entity.Property(courseOrganization => courseOrganization.CourseId)
                  .HasColumnName("CourseId");

            entity.Property(courseOrganization => courseOrganization.OrganizationId)
                  .HasColumnName("OrganizationId");
        });
    }
}

