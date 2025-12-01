namespace LMSTrainingBuddy.API.Domain.Entities;

public sealed class Course
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? LastModified { get; set; }
    public bool AvailableToAllOrganizations { get; set; }
    public bool AvailableInstructorLed { get; set; }
    public bool AvailableSelfPaced { get; set; }
    public bool Archived { get; set; }
    public bool IsFileUpdated { get; set; }
    public string? OutlineObjective { get; set; }
    public string? OutlineOverview { get; set; }
    public string? OutlineTargetAudience { get; set; }
    public string? OutlineLessons { get; set; }
    public string? OrganizationIds { get; set; }
}
