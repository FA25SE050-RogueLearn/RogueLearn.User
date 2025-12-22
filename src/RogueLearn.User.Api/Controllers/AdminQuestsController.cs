using BuildingBlocks.Shared.Authentication;
using Hangfire;
using Hangfire.States;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Quests.Commands.EnsureMasterQuests;
using RogueLearn.User.Application.Features.Quests.Queries.GetAdminQuestDetails;
using RogueLearn.User.Application.Features.Quests.Queries.GetAllQuests;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStatus; // Added reference
using RogueLearn.User.Domain.Enums; // Added reference

namespace RogueLearn.User.Api.Controllers;

/// <summary>
/// Admin-only endpoints for managing Master Quests and content generation.
/// </summary>
[ApiController]
[Route("api/admin/quests")]
[AdminOnly] // Applies to all methods in this controller
public class AdminQuestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ILogger<AdminQuestsController> _logger;

    public AdminQuestsController(
        IMediator mediator,
        IBackgroundJobClient backgroundJobClient,
        IQuestRepository questRepository,
        IQuestStepRepository questStepRepository,
        ILogger<AdminQuestsController> logger)
    {
        _mediator = mediator;
        _backgroundJobClient = backgroundJobClient;
        _questRepository = questRepository;
        _questStepRepository = questStepRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of all quests for admin management.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedQuestsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedQuestsResponse>> GetAllQuests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var query = new GetAllQuestsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Status = status
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves full details of a specific quest, including ALL steps grouped by difficulty track.
    /// Use this for the graph/tree visualization of the quest structure.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminQuestDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminQuestDetailsDto>> GetQuestDetails(Guid id)
    {
        var result = await _mediator.Send(new GetAdminQuestDetailsQuery { QuestId = id });
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Scans all Subjects and creates a Master Quest shell for any that are missing.
    /// Use this to populate the "Quest Pool" before users generate their lines.
    /// </summary>
    [HttpPost("sync-from-subjects")]
    [ProducesResponseType(typeof(EnsureMasterQuestsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncMasterQuests()
    {
        var result = await _mediator.Send(new EnsureMasterQuestsCommand());
        return Ok(result);
    }

    /// <summary>
    /// Updates the lifecycle status of a Master Quest (Draft -> Published -> Archived).
    /// Only Published quests are generated for students.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateQuestStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        if (!Enum.TryParse<QuestStatus>(request.Status, true, out var newStatus))
        {
            return BadRequest("Invalid status. Allowed: Draft, Published, Archived.");
        }

        var command = new UpdateQuestStatusCommand
        {
            QuestId = id,
            NewStatus = newStatus
        };

        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Admin-triggered content generation for a Master Quest.
    /// Schedules a background job to generate steps using the "Three-Lane" architecture.
    /// </summary>
    [HttpPost("{questId:guid}/generate-steps")]
    [ProducesResponseType(typeof(GeneratedQuestStepsResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateQuestSteps(Guid questId)
    {
        var adminId = User.GetAuthUserId();

        _logger.LogInformation(
            "Admin {AdminId} requested content generation for Quest {QuestId}.",
            adminId, questId);

        try
        {
            var quest = await _questRepository.GetByIdAsync(questId, CancellationToken.None);
            if (quest == null)
            {
                return NotFound($"Quest {questId} not found");
            }

            var hasSteps = await _questStepRepository.QuestContainsSteps(questId, CancellationToken.None);
            if (hasSteps)
            {
                return BadRequest("Quest steps have already been generated for this quest");
            }

            var jobId = _backgroundJobClient.Schedule<IQuestStepGenerationService>(
                service => service.GenerateQuestStepsAsync(adminId, questId, null!),
                TimeSpan.Zero);

            return AcceptedAtAction(
                nameof(GetQuestGenerationStatus),
                new { jobId = jobId },
                new GeneratedQuestStepsResponse
                {
                    JobId = jobId,
                    Status = "Processing",
                    Message = "Master Quest content generation scheduled.",
                    QuestId = questId
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling quest generation for quest {QuestId}", questId);
            return StatusCode(500, "Failed to schedule generation");
        }
    }

    /// <summary>
    /// Checks status of a generation job.
    /// </summary>
    [HttpGet("generation-status/{jobId}")]
    [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestGenerationStatus(string jobId)
    {
        try
        {
            using (var connection = JobStorage.Current.GetConnection())
            {
                var jobData = connection.GetJobData(jobId);

                if (jobData == null)
                {
                    return NotFound($"Job {jobId} not found or expired");
                }

                var response = new JobStatusResponse
                {
                    JobId = jobId,
                    Status = jobData.State ?? "Unknown",
                    CreatedAt = DateTime.UtcNow
                };

                if (jobData.State == FailedState.StateName)
                {
                    var stateData = connection.GetStateData(jobId);
                    response.Error = stateData != null && stateData.Data.ContainsKey("Exception")
                        ? stateData.Data["Exception"]
                        : "Job failed";
                }
                else if (jobData.State == SucceededState.StateName)
                {
                    response.Message = "Generation completed successfully!";
                }
                else if (jobData.State == ProcessingState.StateName || jobData.State == EnqueuedState.StateName)
                {
                    response.Status = "Processing";
                }

                return await Task.FromResult(Ok(response));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking status for job {JobId}", jobId);
            return StatusCode(500, "Failed to check job status");
        }
    }

    /// <summary>
    /// Gets detailed progress of a generation job.
    /// </summary>
    [HttpGet("generation-progress/{jobId}")]
    [ProducesResponseType(typeof(QuestGenerationProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetGenerationProgress(string jobId)
    {
        try
        {
            var connection = JobStorage.Current.GetConnection();
            var progressJson = connection.GetJobParameter(jobId, "Progress");

            if (string.IsNullOrEmpty(progressJson))
            {
                return NotFound(new { message = "No progress data available" });
            }

            var progress = System.Text.Json.JsonSerializer.Deserialize<QuestGenerationProgressDto>(progressJson);
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving progress for {JobId}", jobId);
            return StatusCode(500, new { message = "Error retrieving progress" });
        }
    }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class GeneratedQuestStepsResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid QuestId { get; set; }
}

public class JobStatusResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
}

public class QuestGenerationProgressDto
{
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProgressPercentage { get; set; }
    public DateTime UpdatedAt { get; set; }
}