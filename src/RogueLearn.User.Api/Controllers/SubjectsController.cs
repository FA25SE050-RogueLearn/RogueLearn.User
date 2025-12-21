using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.DeleteSubject;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectById;
using RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;
using RogueLearn.User.Application.Features.SubjectSkillMappings.Queries.GetSubjectSkillMappings;
using RogueLearn.User.Application.Features.SubjectSkillMappings.Commands.AddSubjectSkillMapping;
using RogueLearn.User.Application.Features.SubjectSkillMappings.Commands.RemoveSubjectSkillMapping;
using Hangfire;
using System.Text.Json;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/subjects")]
[AdminOnly]
public class SubjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Starts a background job to import a subject from raw syllabus text.
    /// Returns a Job ID that can be used to poll for progress.
    /// </summary>
    [HttpPost("import-from-text")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportSubjectFromTextResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ImportSubjectFromTextResponse>> ImportFromText([FromForm] ImportSubjectFromTextRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawText))
        {
            return BadRequest("The 'rawText' form field is required and cannot be empty.");
        }

        var command = new ImportSubjectFromTextCommand
        {
            RawText = request.RawText,
            Semester = request.Semester
        };

        // Mediator now returns a Job ID immediately
        var result = await _mediator.Send(command, cancellationToken);

        return AcceptedAtAction(nameof(GetImportStatus), new { jobId = result.JobId }, result);
    }

    [HttpPut("import-from-text")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportSubjectFromTextResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ImportSubjectFromTextResponse>> UpdateFromText([FromForm] ImportSubjectFromTextRequest request, CancellationToken cancellationToken)
    {
        return await ImportFromText(request, cancellationToken);
    }

    /// <summary>
    /// Checks the progress of a subject import background job.
    /// </summary>
    [HttpGet("import-status/{jobId}")]
    [ProducesResponseType(typeof(ImportJobStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetImportStatus(string jobId)
    {
        var connection = JobStorage.Current.GetConnection();
        var jobData = connection.GetJobData(jobId);

        if (jobData == null)
        {
            return NotFound($"Job {jobId} not found");
        }

        var response = new ImportJobStatusResponse
        {
            JobId = jobId,
            Status = jobData.State ?? "Unknown",
            CreatedAt = jobData.CreatedAt
        };

        // Retrieve custom progress data stored by the service
        var progressJson = connection.GetJobParameter(jobId, "ImportProgress");
        if (!string.IsNullOrEmpty(progressJson))
        {
            try
            {
                var progress = JsonSerializer.Deserialize<JobProgressData>(progressJson);
                if (progress != null)
                {
                    response.Percent = progress.Percent;
                    response.Message = progress.Message;
                    response.UpdatedAt = progress.Timestamp;
                }
            }
            catch { /* Ignore deserialization errors */ }
        }

        // Handle specific states
        if (jobData.State == "Failed")
        {
            // Hangfire stores exception details in state data
            // Note: Retrieving exact exception message requires accessing state data directly
            response.Message = "Job failed. Check logs or retry.";
            response.Status = "Failed";
        }
        else if (jobData.State == "Succeeded")
        {
            response.Percent = 100;
            if (string.IsNullOrEmpty(response.Message)) response.Message = "Import completed successfully.";
        }

        return Ok(response);
    }

    /// <summary>
    /// Retrieves paginated subjects with optional search.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedSubjectsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedSubjectsResponse>> GetAllSubjects(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var query = new GetAllSubjectsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SubjectDto>> GetSubjectById(Guid id)
    {
        var query = new GetSubjectByIdQuery { Id = id };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateSubjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateSubjectResponse>> CreateSubject([FromBody] CreateSubjectCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetSubjectById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdateSubjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateSubjectResponse>> UpdateSubject(Guid id, [FromBody] UpdateSubjectCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteSubject(Guid id)
    {
        var command = new DeleteSubjectCommand { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpGet("{subjectId:guid}/skills")]
    [ProducesResponseType(typeof(List<SubjectSkillMappingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSkillMappings(Guid subjectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSubjectSkillMappingsQuery { SubjectId = subjectId }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{subjectId:guid}/skills")]
    [ProducesResponseType(typeof(AddSubjectSkillMappingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddSkillMapping(Guid subjectId, [FromBody] AddSubjectSkillMappingCommand command, CancellationToken cancellationToken)
    {
        command.SubjectId = subjectId;
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetSkillMappings), new { subjectId = result.SubjectId }, result);
    }

    [HttpDelete("{subjectId:guid}/skills/{skillId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSkillMapping(Guid subjectId, Guid skillId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveSubjectSkillMappingCommand { SubjectId = subjectId, SkillId = skillId }, cancellationToken);
        return NoContent();
    }
}

public class ImportJobStatusResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Percent { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class JobProgressData
{
    public int Percent { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}