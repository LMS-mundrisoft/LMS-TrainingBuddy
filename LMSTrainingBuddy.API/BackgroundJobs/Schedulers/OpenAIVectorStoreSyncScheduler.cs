using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LMSTrainingBuddy.API.Domain.Entities;
using LMSTrainingBuddy.API.Models;
using LMSTrainingBuddy.API.Repositories.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LMSTrainingBuddy.API.BackgroundJobs.Schedulers;

/// <summary>
///     Hosted service that kicks off <see cref="CourseMetadataSyncJob"/> every five minutes so
///     course information is continuously synced into the configured repository.
/// </summary>
public sealed class OpenAIVectorStoreSyncScheduler : BackgroundService
{
    private readonly HttpClient _http;
    private readonly ITrainingBuddyCourseRepository _courseRepository;
    private readonly TimeSpan _runInterval;

    private readonly ILogger<CourseMetadataSyncScheduler> _logger;
    private readonly OpenAiVectorStoreSettings _options;

    public OpenAIVectorStoreSyncScheduler(ILogger<CourseMetadataSyncScheduler> logger,
                                          IOptions<AppSettings> appSettings,
                                          ITrainingBuddyCourseRepository courseRepository)
    {
        _logger = logger;
        _options = appSettings.Value.OpenAiVectorStore;
        _courseRepository = courseRepository;
        _runInterval = TimeSpan.FromMinutes(appSettings.Value.BackgroundJobs.RunIntervalMinutes);
        _http = new HttpClient
        {
            BaseAddress = new Uri(_options.BaseUrl)
        };
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Course metadata to Open AI vector store sync scheduler started; interval: {Interval}.", _runInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //await RemoveAllFilesFromVectorStoresAsync(_options.VectorStoreId);
                //await RemoveAllFilesFromFilesAsync();

                var courses = await _courseRepository.GetTopCoursesAsync(5000, stoppingToken);
                foreach (var course in courses)
                {
                    (string fileName, MemoryStream memoryStream) = CreateCourseMetadataStream(course);

                    await using (memoryStream)
                    {
                        await UploadAndAttachAsync(_options.VectorStoreId, fileName, memoryStream);
                        await _courseRepository.UpdateIsFileUpdatedAsync(course.CourseId, stoppingToken);
                    }
                }

                _logger.LogInformation("Course metadata to Open AI vector store sync completed at {Timestamp}.", DateTimeOffset.UtcNow);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Course metadata to Open AI vector store sync failed.");
            }

            try
            {
                await Task.Delay(_runInterval, stoppingToken);
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Course metadata to Open AI vector store sync scheduler stopped.");
    }

    public async Task UploadAndAttachAsync(string vectorStoreId, string fileName, Stream fileStream)
    {
        try
        {
            //await RemoveExistingFileFromVectorStoresAsync(vectorStoreId, fileName);
            //await RemoveExistingFileFromFilesAsync(fileName);
            string fileId = await UploadFileAsync(fileStream, fileName);
            await AttachFileToVectorStoreAsync(vectorStoreId, fileId);
        }catch(Exception ex)
        {
            throw ex;
        }
    }
    private static string getCourseString(Course course)
    {
        var parts = new List<string>
         {
           $"CourseId = \"{course.CourseId}\""
         };

        if (!string.IsNullOrWhiteSpace(course.CourseName))
            parts.Add($"CourseName = \"{course.CourseName}\"");

        if (!string.IsNullOrWhiteSpace(course.Description))
            parts.Add($"Description = \"{course.Description}\"");

        //if (!string.IsNullOrWhiteSpace(course.Code))
        //    parts.Add($"Code = \"{course.Code}\"");

        //if (course.Created.HasValue)
        //    parts.Add($"Created = \"{course.Created:yyyy-MM-dd HH:mm:ss}\"");

        //if (course.LastModified.HasValue)
        //    parts.Add($"LastModified = \"{course.LastModified:yyyy-MM-dd HH:mm:ss}\"");

        if (course.AvailableToAllOrganizations)
            parts.Add($"AvailableToAllOrganizations = \"{course.AvailableToAllOrganizations}\"");

        //if (course.AvailableInstructorLed)
        //    parts.Add($"AvailableInstructorLed = \"{course.AvailableInstructorLed}\"");

        //if (course.AvailableSelfPaced)
        //    parts.Add($"AvailableSelfPaced = \"{course.AvailableSelfPaced}\"");

        //if (course.Archived)
        //    parts.Add($"Archived = \"{course.Archived}\"");

        if (!string.IsNullOrWhiteSpace(course.OutlineObjective))
            parts.Add($"OutlineObjective = \"{course.OutlineObjective}\"");

        if (!string.IsNullOrWhiteSpace(course.OutlineOverview))
            parts.Add($"OutlineOverview = \"{course.OutlineOverview}\"");

        if (!string.IsNullOrWhiteSpace(course.OutlineTargetAudience))
            parts.Add($"OutlineTargetAudience = \"{course.OutlineTargetAudience}\"");

        if (!string.IsNullOrWhiteSpace(course.OutlineLessons))
            parts.Add($"OutlineLessons = \"{course.OutlineLessons}\"");

        if (!string.IsNullOrWhiteSpace(course.OrganizationIds))
            parts.Add($"OrganizationIds = \"{course.OrganizationIds}\"");

        string content = string.Join(",\n", parts);
        return content;
    }
    private static (string FileName, MemoryStream Stream) CreateCourseMetadataStream(Course course)
    {
        string safeCourseName = SanitizeForFileName(course.CourseName);
        string fileName = $"{course.CourseId}-{safeCourseName}.txt";

        string content = getCourseString(course);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        return (fileName, stream);
    }

    private static string SanitizeForFileName(string fileName)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return fileName;
    }

    private async Task RemoveExistingFileFromVectorStoresAsync(string vectorStoreId, string fileName)
    {
        string? existingFileId = await FindExistingFileIdAsync(vectorStoreId, fileName);

        if (existingFileId is null)
        {
            return;
        }

        using var detachRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"vector_stores/{vectorStoreId}/files/{existingFileId}");

        using HttpResponseMessage detachResponse = await _http.SendAsync(detachRequest);
        detachResponse.EnsureSuccessStatusCode();

        using var deleteFileRequest = new HttpRequestMessage(HttpMethod.Delete, $"files/{existingFileId}");
        using HttpResponseMessage deleteFileResponse = await _http.SendAsync(deleteFileRequest);
        deleteFileResponse.EnsureSuccessStatusCode();

        _logger.LogInformation(
            "Replaced existing file {FileName} (File ID: {FileId}) in vector store {VectorStoreId}.",
            fileName,
            existingFileId,
            vectorStoreId);
    }

    private async Task RemoveAllFilesFromVectorStoresAsync(string vectorStoreId)
    {
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, $"vector_stores/{vectorStoreId}/files");
        using HttpResponseMessage listResponse = await _http.SendAsync(listRequest);
        listResponse.EnsureSuccessStatusCode();

        await using var listStream = await listResponse.Content.ReadAsStreamAsync();
        using var listJson = await JsonDocument.ParseAsync(listStream);

        if (!listJson.RootElement.TryGetProperty("data", out JsonElement dataElement) ||
            dataElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement item in dataElement.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out JsonElement idElement))
            {
                continue;
            }

            string? fileId = idElement.GetString();

            if (fileId is null)
            {
                continue;
            }

            using var detachRequest = new HttpRequestMessage(HttpMethod.Delete, $"vector_stores/{vectorStoreId}/files/{fileId}");
            using HttpResponseMessage detachResponse = await _http.SendAsync(detachRequest);
            detachResponse.EnsureSuccessStatusCode();

            using var deleteFileRequest = new HttpRequestMessage(HttpMethod.Delete, $"files/{fileId}");
            using HttpResponseMessage deleteFileResponse = await _http.SendAsync(deleteFileRequest);
            deleteFileResponse.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Removed file (File ID: {FileId}) from vector store {VectorStoreId}.",
                fileId,
                vectorStoreId);
        }
    }

    private async Task<string?> FindExistingFileIdAsync(string vectorStoreId, string fileName)
    {
        try
        {
            using var listRequest = new HttpRequestMessage(HttpMethod.Get, $"vector_stores/{vectorStoreId}/files");
            using HttpResponseMessage listResponse = await _http.SendAsync(listRequest);
            listResponse.EnsureSuccessStatusCode();

            await using var listStream = await listResponse.Content.ReadAsStreamAsync();
            using var listJson = await JsonDocument.ParseAsync(listStream);

            if (!listJson.RootElement.TryGetProperty("data", out JsonElement dataElement) ||
                dataElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (JsonElement item in dataElement.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out JsonElement idElement))
                {
                    continue;
                }

                string? fileId = idElement.GetString();

                if (fileId is null)
                {
                    continue;
                }

                string? existingFileName = await GetFileNameAsync(fileId);

                if (existingFileName != null &&
                    string.Equals(existingFileName, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return fileId;
                }
            }

            return null;
        }catch(Exception ex)
        {
            throw ex;
        }
    }

    private async Task<string?> GetFileNameAsync(string fileId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"files/{fileId}");
        using HttpResponseMessage response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(responseStream);

        return json.RootElement.TryGetProperty("filename", out JsonElement filenameElement)
            ? filenameElement.GetString()
            : null;
    }

    private async Task<string> UploadFileAsync(Stream fileStream, string fileName)
    {

        using var form = new MultipartFormDataContent();

        form.Add(new StringContent("assistants"), "purpose");

        fileStream.Position = 0;
        var fileContent = new StreamContent(fileStream);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        form.Add(fileContent, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "files")
        {
            Content = form
        };

        using HttpResponseMessage response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync();
        var json = await JsonDocument.ParseAsync(responseStream);

        string fileId = json.RootElement.GetProperty("id").GetString()
                        ?? throw new Exception("File id not found in response.");

        _logger.LogInformation("Uploaded file to OpenAI. File ID: {FileId}", fileId);
        return fileId;
    }

    private async Task RemoveExistingFileFromFilesAsync(string fileName)
    {
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "files");
        using HttpResponseMessage listResponse = await _http.SendAsync(listRequest);
        listResponse.EnsureSuccessStatusCode();

        await using var listStream = await listResponse.Content.ReadAsStreamAsync();
        using var listJson = await JsonDocument.ParseAsync(listStream);

        if (!listJson.RootElement.TryGetProperty("data", out JsonElement dataElement) ||
            dataElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement item in dataElement.EnumerateArray())
        {
            if (!item.TryGetProperty("filename", out JsonElement filenameElement) ||
                !item.TryGetProperty("id", out JsonElement idElement))
            {
                continue;
            }

            string? existingFileName = filenameElement.GetString();
            string? existingFileId = idElement.GetString();

            if (existingFileId is null ||
                existingFileName is null ||
                !string.Equals(existingFileName, fileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var deleteFileRequest = new HttpRequestMessage(HttpMethod.Delete, $"files/{existingFileId}");
            using HttpResponseMessage deleteFileResponse = await _http.SendAsync(deleteFileRequest);
            deleteFileResponse.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Removed existing file {FileName} (File ID: {FileId}) from platform storage before upload.",
                fileName,
                existingFileId);
        }
    }

    private async Task RemoveAllFilesFromFilesAsync()
    {
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "files");
        using HttpResponseMessage listResponse = await _http.SendAsync(listRequest);
        listResponse.EnsureSuccessStatusCode();

        await using var listStream = await listResponse.Content.ReadAsStreamAsync();
        using var listJson = await JsonDocument.ParseAsync(listStream);

        if (!listJson.RootElement.TryGetProperty("data", out JsonElement dataElement) ||
            dataElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement item in dataElement.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out JsonElement idElement))
            {
                continue;
            }

            string? existingFileId = idElement.GetString();

            if (existingFileId is null)
            {
                continue;
            }

            string? existingFileName = item.TryGetProperty("filename", out JsonElement filenameElement)
                ? filenameElement.GetString()
                : null;

            using var deleteFileRequest = new HttpRequestMessage(HttpMethod.Delete, $"files/{existingFileId}");
            using HttpResponseMessage deleteFileResponse = await _http.SendAsync(deleteFileRequest);
            deleteFileResponse.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Removed file {FileName} (File ID: {FileId}) from platform storage before upload.",
                existingFileName,
                existingFileId);
        }
    }

    private async Task AttachFileToVectorStoreAsync(string vectorStoreId, string fileId)
    {
        var payload = new
        {
            file_id = fileId
        };

        string jsonBody = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"vector_stores/{vectorStoreId}/files")
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Attached file {FileId} to vector store {VectorStoreId}.", fileId, vectorStoreId);
    }
}
