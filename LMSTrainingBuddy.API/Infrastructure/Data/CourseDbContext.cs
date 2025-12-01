using LMSTrainingBuddy.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMSTrainingBuddy.API.Infrastructure.Data;

public sealed class CourseDbContext : DbContext
{
    public CourseDbContext(DbContextOptions<CourseDbContext> options) : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Courses");

            entity.HasKey(course => course.CourseId);

            entity.Property(course => course.CourseId)
                  .HasMaxLength(50);

            entity.Property(course => course.CourseName)
                  .HasMaxLength(255);

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

            entity.Property(course => course.IsFileUpdated)
                  .HasColumnName("IsFileUpdated");

            entity.Property(course => course.OutlineObjective)
                  .HasColumnName("Outline_Objective");

            entity.Property(course => course.OutlineOverview)
                  .HasColumnName("Outline_Overview");

            entity.Property(course => course.OutlineTargetAudience)
                  .HasColumnName("Outline_TargetAudience");

            entity.Property(course => course.OutlineLessons)
                  .HasColumnName("Outline_Lessons");

            entity.Property(course => course.OrganizationIds)
                  .HasColumnName("OrganizationIds");
        });
    }
}

