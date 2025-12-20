using MediatR;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using RogueLearn.User.Application.Exceptions;
using OpenAI;
using OpenAI.Audio;
using System.IO;
using System.ClientModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace RogueLearn.User.Application.Features.Meetings.Commands.ProcessArtifactsAndSummarize;

public class ProcessArtifactsAndSummarizeCommandHandler : IRequestHandler<ProcessArtifactsAndSummarizeCommand, Unit>
{
    private readonly IMeetingSummaryRepository _summaryRepo;
    private readonly ILogger<ProcessArtifactsAndSummarizeCommandHandler> _logger;
    private readonly AudioClient _audioClient;
    private readonly Kernel _kernel;

    public ProcessArtifactsAndSummarizeCommandHandler(IMeetingSummaryRepository summaryRepo, ILogger<ProcessArtifactsAndSummarizeCommandHandler> logger, AudioClient audioClient, Kernel kernel)
    {
        _summaryRepo = summaryRepo;
        _logger = logger;
        _audioClient = audioClient;
        _kernel = kernel;
    }

    public async Task<Unit> Handle(ProcessArtifactsAndSummarizeCommand request, CancellationToken cancellationToken)
    {
        var transcripts = new List<string>();
        string? firstRecordingLink = null;

        foreach (var art in request.Artifacts)
        {
            if (string.IsNullOrWhiteSpace(art.FileId))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(request.AccessToken))
            {
                continue;
            }

            var tempRoot = Path.Combine(Path.GetTempPath(), "RogueLearn.User", "meetings");
            Directory.CreateDirectory(tempRoot);

            var videoPath = Path.Combine(tempRoot, Guid.NewGuid().ToString("N") + ".mp4");

            try
            {
                await DownloadFromGoogleDriveAsync(request.AccessToken, art.FileId, videoPath, cancellationToken);

                var fileInfo = new FileInfo(videoPath);
                if (fileInfo.Length == 0)
                {
                    throw new BadRequestException("Downloaded file is empty; couldn't transcribe");
                }

                var maxBytes = 100 * 1024 * 1024;
                if (fileInfo.Length > maxBytes)
                {
                    throw new BadRequestException("File is too large; couldn't transcribe (max 100MB)");
                }

                var transcript = await TranscribeAudioAsync(videoPath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(transcript))
                {
                    transcripts.Add(transcript);
                    if (firstRecordingLink is null)
                    {
                        firstRecordingLink = !string.IsNullOrWhiteSpace(art.Url)
                            ? art.Url
                            : $"https://drive.google.com/file/d/{art.FileId}/view";
                    }
                }
            }
            finally
            {
                TryDeleteFile(videoPath);
            }
        }

        if (transcripts.Count > 0)
        {
            var combinedTranscript = string.Join("\n\n", transcripts);
            var summaryText = await SummarizeTranscriptAsync(combinedTranscript, cancellationToken);
            var output = string.IsNullOrWhiteSpace(firstRecordingLink)
                ? summaryText
                : summaryText + "\n\nHere is the recorded meeting link: " + firstRecordingLink;
            var existing = await _summaryRepo.GetByMeetingAsync(request.MeetingId, cancellationToken);
            if (existing != null)
            {
                existing.SummaryText = output;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                await _summaryRepo.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                var entity = new MeetingSummary
                {
                    MeetingId = request.MeetingId,
                    SummaryText = output,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                await _summaryRepo.AddAsync(entity, cancellationToken);
            }
        }

        return Unit.Value;
    }

    private async Task DownloadFromGoogleDriveAsync(string accessToken, string fileId, string destinationPath, CancellationToken cancellationToken)
    {
        var credential = GoogleCredential.FromAccessToken(accessToken);
        using var service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "RogueLearn"
        });
        _logger.LogInformation("Drive service initialized: {Name}", service.Name);

        var metaRequest = service.Files.Get(fileId);
        metaRequest.Fields = "id, name, mimeType, size, webContentLink";
        metaRequest.SupportsAllDrives = true;
        Google.Apis.Drive.v3.Data.File? meta;
        try
        {
            meta = await metaRequest.ExecuteAsync(cancellationToken);
        }
        catch (Google.GoogleApiException ex)
        {
            _logger.LogWarning("Drive metadata access failed: {Message}", ex.Message);
            throw new BadRequestException($"Failed to access Google Drive file: {ex.Message}");
        }

        if (meta is null)
        {
            throw new BadRequestException("Google Drive returned no metadata for the file.");
        }

        _logger.LogInformation("Drive file metadata: Id={Id}, Name={Name}, MimeType={MimeType}, Size={Size}", meta.Id, meta.Name, meta.MimeType, meta.Size);

        if (string.IsNullOrWhiteSpace(meta.MimeType) || meta.MimeType.StartsWith("application/vnd.google-apps", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("File is a Google Docs/Drive native type and not directly downloadable; please provide a binary file (e.g., mp4).");
        }

        using var stream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        var downloadRequest = service.Files.Get(fileId);
        downloadRequest.SupportsAllDrives = true;
        downloadRequest.AcknowledgeAbuse = true;
        _logger.LogInformation("Downloading file content: Id={Id} to {Path}", fileId, destinationPath);
        await downloadRequest.DownloadAsync(stream, cancellationToken);
    }

    private async Task<string> TranscribeAudioAsync(string audioPath, CancellationToken cancellationToken)
    {
        if (_audioClient is null)
        {
            var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
                ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Audio client is not configured and no API key found; skipping transcription.");
                return string.Empty;
            }

            var endpoint = Environment.GetEnvironmentVariable("GROQ_BASE_URL") ?? "https://api.groq.com/openai";
            var client = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri(endpoint)
                });
            _logger.LogInformation("Created OpenAI client for audio with endpoint {Endpoint}", endpoint);
            var localAudioClient = client.GetAudioClient("whisper-large-v3");
            await using var fsLocal = new FileStream(audioPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var optionsLocal = new AudioTranscriptionOptions { ResponseFormat = AudioTranscriptionFormat.Simple };
            var resultLocal = await localAudioClient.TranscribeAudioAsync(fsLocal, Path.GetFileName(audioPath), optionsLocal, cancellationToken);
            var transcriptionLocal = resultLocal.Value;
            return transcriptionLocal.Text ?? string.Empty;
        }

        _logger.LogInformation("Using DI-configured audio client for transcription.");

        await using var fs = new FileStream(audioPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var options = new AudioTranscriptionOptions { ResponseFormat = AudioTranscriptionFormat.Simple };
        var result = await _audioClient.TranscribeAudioAsync(fs, Path.GetFileName(audioPath), options, cancellationToken);
        var transcription = result.Value;
        return transcription.Text ?? string.Empty;
    }

    private async Task<string> SummarizeTranscriptAsync(string transcript, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transcript)) return string.Empty;

        if (_kernel is null)
        {
            _logger.LogWarning("Semantic Kernel is not configured; returning raw transcript.");
            return transcript;
        }

        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddSystemMessage("You are an assistant that summarizes meeting recordings clearly and concisely. Provide a structured summary (use Markdown format) of key points, decisions, action items, and next steps. Keep it under 300 words.");
        history.AddUserMessage(transcript);

        try
        {
            var response = await chat.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);
            var content = response?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Semantic Kernel returned empty summary; falling back to transcript.");
                return transcript;
            }
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic Kernel summarization failed; falling back to transcript.");
            return transcript;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}