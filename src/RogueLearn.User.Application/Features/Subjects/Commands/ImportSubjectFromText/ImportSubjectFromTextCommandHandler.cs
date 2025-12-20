// src/RogueLearn.User.Application/Features/Subjects/Commands/ImportSubjectFromText/ImportSubjectFromTextCommandHandler.cs
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

/// <summary>
/// Handles the import command by scheduling a background job.
/// This decouples the long-running AI/Scraping logic from the HTTP request cycle.
/// </summary>
public class ImportSubjectFromTextCommandHandler : IRequestHandler<ImportSubjectFromTextCommand, ImportSubjectFromTextResponse>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<ImportSubjectFromTextCommandHandler> _logger;

    public ImportSubjectFromTextCommandHandler(
        IBackgroundJobClient backgroundJobClient,
        ILogger<ImportSubjectFromTextCommandHandler> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public Task<ImportSubjectFromTextResponse> Handle(
        ImportSubjectFromTextCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduling background job for subject import.");

        // Enqueue the job using the new Interface-based service
        var jobId = _backgroundJobClient.Enqueue<ISubjectImportService>(
            service => service.ImportSubjectAsync(request, null!)); // Context is injected by Hangfire

        _logger.LogInformation("Subject import job scheduled with ID: {JobId}", jobId);

        return Task.FromResult(new ImportSubjectFromTextResponse
        {
            JobId = jobId,
            Status = "Queued",
            Message = "Subject import has been scheduled. Check status endpoint for progress."
        });
    }
}