namespace LMSTrainingBuddy.API.Infrastructure.OpenAI;

public sealed record ChatMessage(string Role, string Content);

public interface IOpenAiClient
{
    Task<string> GetChatCompletionAsync(
        IReadOnlyCollection<ChatMessage> messages,
        CancellationToken cancellationToken);

    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken);
}
