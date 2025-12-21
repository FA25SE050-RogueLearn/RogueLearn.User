using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;
using RogueLearn.User.Application.Features.Quests.Commands.StartQuest;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;
using RogueLearn.User.Application.Features.Quests.Queries.GetMyQuestsWithSubjects;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestSkills;
using RogueLearn.User.Application.Features.QuestSubmissions.Commands.SubmitQuizAnswer;
using RogueLearn.User.Application.Features.QuestSubmissions.Commands.SubmitCodingActivity;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json.Serialization;

[ApiController]
[Route("api/quests")]
[Authorize]
public class QuestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ILogger<QuestsController> _logger;

    public QuestsController(
        IMediator mediator,
        IQuestRepository questRepository,
        IQuestStepRepository questStepRepository,
        ILogger<QuestsController> logger)
    {
        _mediator = mediator;
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

    [HttpPost("{questId:guid}/steps/{stepId:guid}/activities/{activityId:guid}/submit-code")]
    [ProducesResponseType(typeof(SubmitCodingActivityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitCodingActivity(
    Guid questId,
    Guid stepId,
    Guid activityId,
    [FromBody] SubmitCodingRequest body)
    {
        var authUserId = User.GetAuthUserId();

        try
        {
            var command = new SubmitCodingActivityCommand
            {
                AuthUserId = authUserId,
                QuestId = questId,
                StepId = stepId,
                ActivityId = activityId,
                Code = body.Code,
                Language = body.Language
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing coding submission");
            return StatusCode(500, new { message = "Failed to process coding submission", error = ex.Message });
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

public class SubmitCodingRequest
{
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}

public class UpdateQuestActivityProgressRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StepCompletionStatus Status { get; set; }
}