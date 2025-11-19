// RogueLearn.User/src/RogueLearn.User.Api/Controllers/QuestsController.cs
// CORRECTED HANGFIRE API - Using proper JobStorage.Current.GetMonitoringApi()

using BuildingBlocks.Shared.Authentication;
using Hangfire;
using Hangfire.States;
using Hangfire.Storage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;
using RogueLearn.User.Domain.Entities;
// MODIFIED: This using is updated to point to the refactored command location.
using RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;
using System.Text.Json.Serialization;

[ApiController]
[Route("api/quests")]
[Authorize]
public class QuestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ILogger<QuestsController> _logger;

    public QuestsController(
        IMediator mediator,
        IBackgroundJobClient backgroundJobClient,
        IQuestRepository questRepository,
        IQuestStepRepository questStepRepository,
        ILogger<QuestsController> logger)
    {
        _mediator = mediator;
        _backgroundJobClient = backgroundJobClient;
        _questRepository = questRepository;
        _questStepRepository = questStepRepository;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(QuestDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestById(Guid id)
    {
        var result = await _mediator.Send(new GetQuestByIdQuery { Id = id });
        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Schedules quest step generation as a background job.
    /// Returns immediately (202 Accepted) with a JobId for status tracking.
    /// </summary>
    [HttpPost("{questId:guid}/generate-steps")]
    [ProducesResponseType(typeof(GeneratedQuestStepsResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateQuestSteps(Guid questId)
    {
        var authUserId = User.GetAuthUserId();

        _logger.LogInformation(
            "GenerateQuestSteps endpoint called for Quest {QuestId} by user {AuthUserId}. " +
            "Scheduling background job instead of blocking request.",
            questId, authUserId);

        try
        {
            // ========== FAST VALIDATION CHECKS ==========

            // Validate quest exists (fail fast)
            var quest = await _questRepository.GetByIdAsync(questId, CancellationToken.None);
            if (quest == null)
            {
                _logger.LogWarning("Quest {QuestId} not found", questId);
                return NotFound($"Quest {questId} not found");
            }

            // Check if steps already exist
            var hasSteps = await _questStepRepository.QuestContainsSteps(questId, CancellationToken.None);
            if (hasSteps)
            {
                _logger.LogWarning("Quest {QuestId} already has steps generated", questId);
                return BadRequest("Quest steps have already been generated for this quest");
            }

            // ========== SCHEDULE BACKGROUND JOB ==========

            // Schedule background job with immediate execution (no delay)
            var jobId = _backgroundJobClient.Schedule<IQuestStepGenerationService>(
                service => service.GenerateQuestStepsAsync(authUserId, questId),
                TimeSpan.Zero); // Immediate scheduling

            _logger.LogInformation(
                "Successfully scheduled background job {JobId} for quest step generation. " +
                "Quest: {QuestId}, User: {AuthUserId}",
                jobId, questId, authUserId);

            // ========== RETURN 202 ACCEPTED WITH JOB ID ==========

            return AcceptedAtAction(
                nameof(GetQuestGenerationStatus),
                new { jobId = jobId },
                new GeneratedQuestStepsResponse
                {
                    JobId = jobId,
                    Status = "Processing",
                    Message = "Quest step generation has been scheduled. You will receive a notification when completed.",
                    QuestId = questId
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling quest step generation for quest {QuestId}", questId);
            return StatusCode(500, "Failed to schedule quest step generation");
        }
    }

    /// <summary>
    /// Checks the status of a quest step generation background job.
    /// Use this to poll and track job progress.
    /// 
    /// Returns job state information including:
    /// - Processing: Job is still running
    /// - Succeeded: Job completed successfully
    /// - Failed: Job failed (check Error property for details)
    /// - Scheduled: Job is scheduled but not yet running
    /// - Deleted: Job was deleted
    /// </summary>
    [HttpGet("generation-status/{jobId}")]
    [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestGenerationStatus(string jobId)
    {
        try
        {
            // FIXED: Use proper Hangfire API - JobStorage.Current.GetConnection()
            using (var connection = JobStorage.Current.GetConnection())
            {
                var jobData = connection.GetJobData(jobId);

                if (jobData == null)
                {
                    _logger.LogWarning("Job {JobId} not found in Hangfire storage", jobId);
                    return NotFound($"Job {jobId} not found or has expired");
                }

                var response = new JobStatusResponse
                {
                    JobId = jobId,
                    Status = jobData.State ?? "Unknown",
                    CreatedAt = DateTime.UtcNow
                };

                // FIXED: Properly access exception details from FailedState
                if (jobData.State == FailedState.StateName)
                {
                    // Get the detailed state information
                    var stateData = connection.GetStateData(jobId);
                    if (stateData != null && stateData.Data.ContainsKey("Exception"))
                    {
                        response.Error = stateData.Data["Exception"];
                    }
                    else
                    {
                        response.Error = "Job failed - exception details not available";
                    }
                    _logger.LogWarning("Job {JobId} failed. Error: {Error}", jobId, response.Error);
                }
                // Check if job is in Succeeded state
                else if (jobData.State == SucceededState.StateName)
                {
                    response.Message = "Quest step generation completed successfully!";
                    _logger.LogInformation("Job {JobId} succeeded", jobId);
                }
                // Check if job is still processing
                else if (jobData.State == ProcessingState.StateName || jobData.State == EnqueuedState.StateName)
                {
                    response.Status = "Processing";
                    _logger.LogInformation("Job {JobId} is processing", jobId);
                }

                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking status for job {JobId}", jobId);
            return StatusCode(500, "Failed to check job status");
        }
    }

    // MODIFIED: This endpoint is now more specific. It targets a specific activity within a step.
    /// <summary>
    /// Updates the progress status of a specific activity within a quest step (weekly module) for the authenticated user.
    /// </summary>
    [HttpPost("{questId:guid}/steps/{stepId:guid}/activities/{activityId:guid}/progress")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuestActivityProgress(Guid questId, Guid stepId, Guid activityId, [FromBody] UpdateQuestActivityProgressRequest body)
    {
        var authUserId = User.GetAuthUserId();
        // MODIFIED: We now use the new, more descriptive command.
        var command = new UpdateQuestActivityProgressCommand
        {
            AuthUserId = authUserId,
            QuestId = questId,
            StepId = stepId,
            ActivityId = activityId,
            Status = body.Status
        };
        await _mediator.Send(command);
        return NoContent();
    }
}

// ========== RESPONSE DTOs ==========

/// <summary>
/// Response returned when a background job is scheduled (202 Accepted).
/// Use the JobId to poll the GetQuestGenerationStatus endpoint.
/// </summary>
public class GeneratedQuestStepsResponse
{
    /// <summary>
    /// Unique identifier for the background job.
    /// Use this to check job status via GET /api/quests/generation-status/{jobId}
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Current status: "Processing", "Succeeded", "Failed", etc.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable message about the job.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The quest ID this job is generating steps for.
    /// </summary>
    public Guid QuestId { get; set; }
}

/// <summary>
/// Response for checking the status of a quest step generation job.
/// </summary>
public class JobStatusResponse
{
    /// <summary>
    /// Unique identifier for the background job.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Current job state: "Processing", "Succeeded", "Failed", "Scheduled", etc.
    /// Uses Hangfire's built-in state names.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the job was created/scheduled.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Error details if the job failed. Null if successful.
    /// Contains the full exception details from the FailedState.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string? Message { get; set; }
}

// MODIFIED: Renamed request DTO for clarity.
public class UpdateQuestActivityProgressRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StepCompletionStatus Status { get; set; }
}