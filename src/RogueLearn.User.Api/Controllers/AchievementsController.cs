using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Achievements.Commands.CreateAchievement;
using RogueLearn.User.Application.Features.Achievements.Commands.UpdateAchievement;
using RogueLearn.User.Application.Features.Achievements.Commands.DeleteAchievement;
using RogueLearn.User.Application.Features.Achievements.Queries.GetAllAchievements;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/achievements")]
[AdminOnly]
public class AchievementsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAchievementImageStorage _imageStorage;

    public AchievementsController(IMediator mediator, IAchievementImageStorage imageStorage)
    {
        _mediator = mediator;
        _imageStorage = imageStorage;
    }

    /// <summary>
    /// Get all achievements in the catalog
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllAchievementsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetAllAchievementsResponse>> GetAll()
    {
        var result = await _mediator.Send(new GetAllAchievementsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Create a new achievement
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateAchievementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateAchievementResponse>> Create([FromBody] CreateAchievementCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    /// <summary>
    /// Create a new achievement with icon upload (multipart/form-data)
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(CreateAchievementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateAchievementResponse>> CreateFromUpload([FromForm] CreateAchievementCommand command, IFormFile? icon)
    {
        if (icon is not null && icon.Length > 0)
        {
            using var stream = icon.OpenReadStream();
            // Use stable achievement key for storage path slugging
            var iconUrl = await _imageStorage.SaveIconAsync(command.Key, stream, icon.FileName, icon.ContentType);
            command.IconUrl = iconUrl;
        }

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing achievement
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdateAchievementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateAchievementResponse>> Update(Guid id, [FromBody] UpdateAchievementCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Update an existing achievement with icon upload (multipart/form-data)
    /// </summary>
    [HttpPut("{id:guid}/upload")]
    [ProducesResponseType(typeof(UpdateAchievementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateAchievementResponse>> UpdateFromUpload(Guid id, [FromForm] UpdateAchievementCommand command, IFormFile? icon)
    {
        command.Id = id;

        if (icon is not null && icon.Length > 0)
        {
            using var stream = icon.OpenReadStream();
            // Use stable achievement key for storage path slugging
            var iconUrl = await _imageStorage.SaveIconAsync(command.Key, stream, icon.FileName, icon.ContentType);
            command.IconUrl = iconUrl;
        }

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete an achievement
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteAchievementCommand { Id = id });
        return NoContent();
    }
}