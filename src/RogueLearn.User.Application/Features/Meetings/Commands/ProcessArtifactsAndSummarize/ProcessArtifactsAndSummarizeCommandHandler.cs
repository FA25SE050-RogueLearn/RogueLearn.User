using MediatR;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Meetings.Commands.ProcessArtifactsAndSummarize;

public class ProcessArtifactsAndSummarizeCommandHandler : IRequestHandler<ProcessArtifactsAndSummarizeCommand, Unit>
{
    private readonly IFileSummarizationPlugin _fileSummarizationPlugin;
    private readonly IMeetingSummaryRepository _summaryRepo;

    public ProcessArtifactsAndSummarizeCommandHandler(IFileSummarizationPlugin fileSummarizationPlugin, IMeetingSummaryRepository summaryRepo)
    {
        _fileSummarizationPlugin = fileSummarizationPlugin;
        _summaryRepo = summaryRepo;
    }

    public async Task<Unit> Handle(ProcessArtifactsAndSummarizeCommand request, CancellationToken cancellationToken)
    {
        var summaries = new List<string>();
        foreach (var art in request.Artifacts)
        {
            // We cannot reliably download arbitrary URLs here; instead, summarize the URL list as text
            // If later we need to fetch content, add a downloader service with auth support
        }

        var text = string.Join("\n", request.Artifacts.Select(a => $"[{a.ArtifactType}] {a.Url}"));
        if (!string.IsNullOrWhiteSpace(text))
        {
            var attachment = new AiFileAttachment
            {
                Bytes = System.Text.Encoding.UTF8.GetBytes(text),
                ContentType = "text/plain",
                FileName = "artifacts.txt"
            };
            var summaryJson = await _fileSummarizationPlugin.SummarizeAsync(attachment, cancellationToken);
            if (!string.IsNullOrWhiteSpace(summaryJson))
            {
                var existing = await _summaryRepo.GetByMeetingAsync(request.MeetingId, cancellationToken);
                if (existing != null)
                {
                    existing.SummaryText = summaryJson;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    await _summaryRepo.UpdateAsync(existing, cancellationToken);
                }
                else
                {
                    var entity = new MeetingSummary
                    {
                        MeetingId = request.MeetingId,
                        SummaryText = summaryJson,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    await _summaryRepo.AddAsync(entity, cancellationToken);
                }
            }
        }

        return Unit.Value;
    }
}