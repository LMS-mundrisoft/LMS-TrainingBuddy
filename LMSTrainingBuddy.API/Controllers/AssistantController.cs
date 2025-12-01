using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LMSTrainingBuddy.API.Models;
using LMSTrainingBuddy.API.Models.Requests;
using LMSTrainingBuddy.API.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LMSTrainingBuddy.API.Controllers;

[ApiController]
[Route("api/assistant/answer")]
public sealed class AssistantController : ControllerBase
{
    private const string JsonMediaType = "application/json";
    private const string OpenAiBetaHeader = "OpenAI-Beta";
    private const string ThreadMessageCompleted = "event: thread.message.completed";
    private const string DataPrefix = "data:";

    private readonly AiAnswerSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AssistantController> _logger;

    public AssistantController(HttpClient httpClient, IOptions<AppSettings> appSettings, ILogger<AssistantController> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = appSettings.Value.AiAnswer;

        if (!string.IsNullOrWhiteSpace(_settings.BaseUrl) && _httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(AiAnswerResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Post([FromBody] AiAnswerRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Question is required.");
        }

        string threadId = request.ThreadId ?? string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(threadId))
            {
                threadId = await CreateThreadAsync(cancellationToken);
            }

            string answer = await SendMessageAsync(threadId, request.Question, request.OrganizationIds, cancellationToken);

            var response = new AiAnswerResponse
            {
                Answer = answer,
                ThreadId = threadId
            };

            return Ok(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve AI answer.");
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    private async Task<string> CreateThreadAsync(CancellationToken cancellationToken)
    {
        using var threadRequestMessage = CreateAuthorizedRequest(HttpMethod.Post, "threads");
        threadRequestMessage.Content = new StringContent("{}", Encoding.UTF8, JsonMediaType);

        using HttpResponseMessage response = await _httpClient.SendAsync(threadRequestMessage, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Error creating thread: {errorContent}");
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(content);
        return document.RootElement.GetProperty("id").GetString() ?? throw new InvalidOperationException("Thread id not found.");
    }

    private async Task<string> SendMessageAsync(string threadId, string message, string organizationIds, CancellationToken cancellationToken)
    {
        var payload = new
        {
            role = "user",
            content = new[]
            {
                new { type = "text", text = message },
                new { type = "text", text = $"[orgIds:{organizationIds}]" } // hidden context for assistant
            }
        };

        using var messageRequest = CreateAuthorizedRequest(HttpMethod.Post, $"threads/{threadId}/messages");
        messageRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, JsonMediaType);

        using HttpResponseMessage messageResponse = await _httpClient.SendAsync(messageRequest, cancellationToken);

        if (!messageResponse.IsSuccessStatusCode)
        {
            string errorContent = await messageResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Error sending message: {errorContent}");
        }

        var runPayload = new
        {
            assistant_id = _settings.AssistantId,
            stream = true,
            tool_choice = (object?)null
        };

        using var runRequest = CreateAuthorizedRequest(HttpMethod.Post, $"threads/{threadId}/runs");
        runRequest.Content = new StringContent(JsonSerializer.Serialize(runPayload), Encoding.UTF8, JsonMediaType);

        using HttpResponseMessage runResponse = await _httpClient.SendAsync(runRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!runResponse.IsSuccessStatusCode)
        {
            string errorContent = await runResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Error starting run: {errorContent}");
        }

        return await ReadAssistantMessageAsync(runResponse, cancellationToken);
    }

    private async Task<string> ReadAssistantMessageAsync(HttpResponseMessage runResponse, CancellationToken cancellationToken)
    {
        string jsonLine = string.Empty;

        await using (var stream = await runResponse.Content.ReadAsStreamAsync(cancellationToken))
        using (var reader = new StreamReader(stream))
        {
            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync();
                if (line is null)
                {
                    continue;
                }

                line = line.Trim();

                if (line == ThreadMessageCompleted)
                {
                    jsonLine = (await reader.ReadLineAsync())?.Trim() ?? string.Empty;
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(jsonLine))
        {
            return "No response received from assistant.";
        }

        jsonLine = jsonLine.StartsWith(DataPrefix, StringComparison.OrdinalIgnoreCase)
            ? jsonLine[DataPrefix.Length..].Trim()
            : jsonLine;

        using var document = JsonDocument.Parse(jsonLine);
        var root = document.RootElement;

        if (root.TryGetProperty("content", out JsonElement contentElement) &&
            contentElement.ValueKind == JsonValueKind.Array &&
            contentElement.GetArrayLength() > 0)
        {
            var firstItem = contentElement[0];

            if (firstItem.TryGetProperty("text", out JsonElement textElement) &&
                textElement.TryGetProperty("value", out JsonElement valueElement))
            {
                return valueElement.GetString() ?? "";
            }
        }

        return "No response received from assistant.";
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        if (!string.IsNullOrWhiteSpace(_settings.BetaHeaderValue))
        {
            request.Headers.Add(OpenAiBetaHeader, _settings.BetaHeaderValue);
        }

        return request;
    }
}
