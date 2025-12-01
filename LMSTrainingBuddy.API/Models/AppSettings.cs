namespace LMSTrainingBuddy.API.Models;

public sealed class AppSettings
{
    public ConnectionStringSettings ConnectionStrings { get; set; } = new();

    public BackgroundJobSettings BackgroundJobs { get; set; } = new();

    public OpenAiVectorStoreSettings OpenAiVectorStore { get; set; } = new();

    public AiAnswerSettings AiAnswer { get; set; } = new();
}

public sealed class ConnectionStringSettings
{
    public string EnlightDatabase { get; set; } = string.Empty;

    public string CourseDatabase { get; set; } = string.Empty;
}

public sealed class BackgroundJobSettings
{
    public int RunIntervalMinutes { get; set; } = 10;
}

public sealed class OpenAiVectorStoreSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";

    public string VectorStoreId { get; set; } = string.Empty;
}

public sealed class AiAnswerSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";

    public string AssistantId { get; set; } = string.Empty;

    public string BetaHeaderValue { get; set; } = "assistants=v2";
}
