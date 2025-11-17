using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Tags.Commands.CreateTag;
using RogueLearn.User.Application.Features.Tags.Commands.DeleteTag;
using RogueLearn.User.Application.Features.Tags.Commands.AttachTagToNote;
using RogueLearn.User.Application.Features.Tags.Commands.RemoveTagFromNote;
using RogueLearn.User.Application.Features.Tags.Commands.CreateTagAndAttachToNote;
using RogueLearn.User.Application.Features.Tags.Queries.GetMyTags;
using RogueLearn.User.Application.Features.Tags.Queries.GetTagsForNote;
using RogueLearn.User.Application.Features.Tags.Commands.UpdateTag;
using BuildingBlocks.Shared.Authentication;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
public class TagsController : ControllerBase
{
  private readonly IMediator _mediator;

  public TagsController(IMediator mediator)
  {
    _mediator = mediator;
  }

  /// <summary>
  /// Get tags owned by the authenticated user. Optional search by name.
  /// </summary>
  [HttpGet("me")]
  [ProducesResponseType(typeof(GetMyTagsResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<GetMyTagsResponse>> GetMyTags([FromQuery] string? search, CancellationToken cancellationToken)
  {
    var authUserId = User.GetAuthUserId();
    var result = await _mediator.Send(new GetMyTagsQuery(authUserId, search), cancellationToken);
    return Ok(result);
  }

  /// <summary>
  /// Create a tag for the authenticated user.
  /// </summary>
  [HttpPost]
  [ProducesResponseType(typeof(CreateTagResponse), StatusCodes.Status201Created)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<CreateTagResponse>> CreateTag([FromBody] CreateTagCommand command, CancellationToken cancellationToken)
  {
    var authUserId = User.GetAuthUserId();
    command.AuthUserId = authUserId;
    var result = await _mediator.Send(command, cancellationToken);
    return Created($"/api/tags/{result.Tag.Id}", result);
  }

  /// <summary>
  /// Delete a tag owned by the authenticated user (detaches from all notes).
  /// </summary>
  [HttpDelete("{id:guid}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> DeleteTag(Guid id, CancellationToken cancellationToken)
  {
    var authUserId = User.GetAuthUserId();
    await _mediator.Send(new DeleteTagCommand { TagId = id, AuthUserId = authUserId }, cancellationToken);
    return NoContent();
  }

  /// <summary>
  /// Get tags attached to my note.
  /// </summary>
  [HttpGet("~/api/notes/{noteId:guid}/tags")]
  [ProducesResponseType(typeof(GetTagsForNoteResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<GetTagsForNoteResponse>> GetTagsForNote(Guid noteId, CancellationToken cancellationToken)
  {
    var authUserId = User.GetAuthUserId();
    var result = await _mediator.Send(new GetTagsForNoteQuery { NoteId = noteId, AuthUserId = authUserId }, cancellationToken);
    return Ok(result);
  }

  /// <summary>
  /// Attach an existing tag to my note.
  /// </summary>
  [HttpPost("~/api/notes/{noteId:guid}/tags/attach")]
  [ProducesResponseType(typeof(AttachTagToNoteResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<AttachTagToNoteResponse>> AttachTag(Guid noteId, [FromBody] Guid tagId, CancellationToken cancellationToken)
  {
    var authUserId = User.GetAuthUserId();
    var command = new AttachTagToNoteCommand { NoteId = noteId, TagId = tagId, AuthUserId = authUserId };
    var result = await _mediator.Send(command, cancellationToken);
    return Ok(result);
  }

  /// <summary>
  /// Create a tag (if not exists) and attach to my note.
  /// </summary>
  [HttpPost("~/api/notes/{noteId:guid}/tags/create-and-attach")]
  [ProducesResponseType(typeof(CreateTagAndAttachToNoteResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<CreateTagAndAttachToNoteResponse>> CreateAndAttach(Guid noteId, [FromBody] CreateTagAndAttachToNoteCommand command, CancellationToken cancellationToken)
  {
    var authUserId = User.GetAuthUserId();
    command.NoteId = noteId;
    command.AuthUserId = authUserId;
    var result = await _mediator.Send(command, cancellationToken);
    return Ok(result);
  }

  /// <summary>
  /// Remove a tag from my note.
  /// </summary>
  [HttpDelete("~/api/notes/{noteId:guid}/tags/{tagId:guid}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> RemoveTag(Guid noteId, Guid tagId, CancellationToken cancellationToken)
  {
    var authUserId = User.GetAuthUserId();
    await _mediator.Send(new RemoveTagFromNoteCommand { NoteId = noteId, TagId = tagId, AuthUserId = authUserId }, cancellationToken);
    return NoContent();
  }

  [HttpPut("{id:guid}")]
  [ProducesResponseType(typeof(UpdateTagResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<ActionResult<UpdateTagResponse>> UpdateTag(Guid id, [FromBody] UpdateTagCommand command, CancellationToken cancellationToken)
  {
    var authUserId = User.GetAuthUserId();
    command.TagId = id;
    command.AuthUserId = authUserId;
    var result = await _mediator.Send(command, cancellationToken);
    return Ok(result);
  }
}