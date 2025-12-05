// RogueLearn.User/src/RogueLearn.User.Api/Controllers/QuestsController.cs
using BuildingBlocks.Shared.Authentication;
using DocumentFormat.OpenXml.Wordprocessing;
using Hangfire;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;
using RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;
using RogueLearn.User.Application.Features.Quests.Commands.StartQuest;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;
using RogueLearn.User.Application.Features.Quests.Queries.GetMyQuestsWithSubjects;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestSkills;
using RogueLearn.User.Application.Features.QuestSubmissions.Commands.SubmitQuizAnswer;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
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
        var authUserId = User.GetAuthUserId();
        var result = await _mediator.Send(new GetQuestByIdQuery { Id = id, AuthUserId = authUserId });
        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Explicitly starts a quest for the user.
    /// This is required before any progress can be tracked.
    /// Idempotent: returns existing attempt info if already started.
    /// </summary>
    [HttpPost("{questId:guid}/start")]
    [ProducesResponseType(typeof(StartQuestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartQuest(Guid questId)
    {
        var authUserId = User.GetAuthUserId();
        var result = await _mediator.Send(new StartQuestCommand(questId, authUserId));
        return Ok(result);
    }

    [HttpPost("{questId:guid}/generate-steps")]
    [ProducesResponseType(typeof(GeneratedQuestStepsResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateQuestSteps(Guid questId)
    {
        var authUserId = User.GetAuthUserId();

        _logger.LogInformation(
            "GenerateQuestSteps endpoint called for Quest {QuestId} by user {AuthUserId}. Scheduling background job.",
            questId, authUserId);

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
                service => service.GenerateQuestStepsAsync(authUserId, questId, null!),
                TimeSpan.Zero);

            return AcceptedAtAction(
                nameof(GetQuestGenerationStatus),
                new { jobId = jobId },
                new GeneratedQuestStepsResponse
                {
                    JobId = jobId,
                    Status = "Processing",
                    Message = "Quest step generation has been scheduled.",
                    QuestId = questId
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling quest step generation for quest {QuestId}", questId);
            return StatusCode(500, "Failed to schedule quest step generation");
        }
    }

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
                    return NotFound($"Job {jobId} not found or has expired");
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
                    response.Message = "Quest step generation completed successfully!";
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
                return NotFound(new { message = "No progress found for this job" });
            }

            var progress = System.Text.Json.JsonSerializer.Deserialize<QuestGenerationProgressDto>(progressJson);
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job progress for {JobId}", jobId);
            return StatusCode(500, new { message = "Error retrieving progress" });
        }
    }

    [HttpPost("{questId:guid}/steps/{stepId:guid}/activities/{activityId:guid}/progress")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuestActivityProgress(
    Guid questId,
    Guid stepId,
    Guid activityId,
    [FromBody] UpdateQuestActivityProgressRequest body)
    {
        var authUserId = User.GetAuthUserId();

        try
        {
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
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating activity progress");
            return StatusCode(500);
        }
    }

    [HttpPost("{questId:guid}/steps/{stepId:guid}/activities/{activityId:guid}/submit-quiz")]
    [ProducesResponseType(typeof(SubmitQuizAnswerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitQuizAnswer(
    Guid questId,
    Guid stepId,
    Guid activityId,
    [FromBody] SubmitQuizAnswerRequest body)
    {
        var authUserId = User.GetAuthUserId();

        try
        {
            var quest = await _questRepository.GetByIdAsync(questId);
            if (quest == null)
                return NotFound(new { message = $"Quest {questId} not found" });

            var step = await _questStepRepository.GetByIdAsync(stepId);
            if (step == null || step.QuestId != questId)
                return NotFound(new { message = $"Step {stepId} not found in quest {questId}" });

            var command = new SubmitQuizAnswerCommand
            {
                AuthUserId = authUserId,
                QuestId = questId,
                StepId = stepId,
                ActivityId = activityId,
                Answers = body.Answers,
                CorrectAnswerCount = body.CorrectAnswerCount,
                TotalQuestions = body.TotalQuestions
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing quiz submission");
            return StatusCode(500, new { message = "Failed to process quiz submission", error = ex.Message });
        }
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(List<MyQuestWithSubjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyQuestsWithSubjects()
    {
        var authUserId = User.GetAuthUserId();
        var result = await _mediator.Send(new GetMyQuestsWithSubjectsQuery { AuthUserId = authUserId });
        return Ok(result);
    }

    [HttpGet("{questId:guid}/skills")]
    [ProducesResponseType(typeof(GetQuestSkillsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestSkills(Guid questId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetQuestSkillsQuery { QuestId = questId }, cancellationToken);
        return result is not null ? Ok(result) : NotFound();
    }
}

public class SubmitQuizAnswerRequest
{
    public Dictionary<string, string> Answers { get; set; } = new();
    public int CorrectAnswerCount { get; set; }
    public int TotalQuestions { get; set; }
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

public class UpdateQuestActivityProgressRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StepCompletionStatus Status { get; set; }
}