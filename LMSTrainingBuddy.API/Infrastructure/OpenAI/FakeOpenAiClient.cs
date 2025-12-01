using System.Text;

namespace LMSTrainingBuddy.API.Infrastructure.OpenAI;

public sealed class FakeOpenAiClient : IOpenAiClient
{
    public Task<string> GetChatCompletionAsync(IReadOnlyCollection<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[Mocked AI Response]");
        foreach (var message in messages)
        {
            builder.AppendLine($"{message.Role}: {message.Content}");
        }

        return Task.FromResult(builder.ToString());
    }

    public Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        var random = new Random(text.GetHashCode());
        var vector = Enumerable.Range(0, 10).Select(_ => (float)random.NextDouble()).ToArray();
        return Task.FromResult(vector);
    }
}
