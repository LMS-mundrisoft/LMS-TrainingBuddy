namespace LMSTrainingBuddy.API.Domain.Entities;

public sealed class CourseLesson
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int LessonId { get; set; }
    public string? LessonTitle { get; set; }
    public string? LessonDescription { get; set; }
}
