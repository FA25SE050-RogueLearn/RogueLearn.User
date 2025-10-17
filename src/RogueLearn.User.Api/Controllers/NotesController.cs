using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNote;
using RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;
using RogueLearn.User.Application.Features.Notes.Commands.DeleteNote;
using RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;
using RogueLearn.User.Application.Features.Notes.Queries.GetPublicNotes;
using RogueLearn.User.Application.Features.Notes.Queries.GetNoteById;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/notes")]
[Authorize]
public class NotesController : ControllerBase
{
  private readonly IMediator _mediator;

  public NotesController(IMediator mediator)
  {
    _mediator = mediator;
  }

  /// <summary>
  /// Get notes for the authenticated user.
  /// </summary>
  /// <param name="search">Optional search term for filtering by title.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  [HttpGet("me")]
  public async Task<ActionResult<List<NoteDto>>> GetMyNotes([FromQuery] string? search, CancellationToken cancellationToken)
  {
    var authIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(authIdClaim) || !Guid.TryParse(authIdClaim, out var authUserId))
      return Unauthorized();

    var query = new GetMyNotesQuery(authUserId, search);

    var result = await _mediator.Send(query, cancellationToken);
    return Ok(result);
  }

  /// <summary>
  /// Get public notes available to all users.
  /// </summary>
  /// <param name="search">Optional search term for filtering by title.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  [HttpGet("public")]
  [AllowAnonymous]
  public async Task<ActionResult<List<NoteDto>>> GetPublicNotes([FromQuery] string? search, CancellationToken cancellationToken)
  {
    var query = new GetPublicNotesQuery(search);

    var result = await _mediator.Send(query, cancellationToken);
    return Ok(result);
  }

  /// <summary>
  /// Get a note by ID. Returns the note if it is public or owned by the authenticated user.
  /// </summary>
  /// <param name="id">Note ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  [HttpGet("{id:guid}")]
  [AllowAnonymous]
  public async Task<ActionResult<NoteDto>> GetNoteById(Guid id, CancellationToken cancellationToken)
  {
    var query = new GetNoteByIdQuery { Id = id };
    var result = await _mediator.Send(query, cancellationToken);

    if (result is null)
      return NotFound();

    // Only return private notes if the requester is the owner
    if (!result.IsPublic)
    {
      var authIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrWhiteSpace(authIdClaim) || !Guid.TryParse(authIdClaim, out var authUserId) || authUserId != result.AuthUserId)
        return NotFound();
    }

    return Ok(result);
  }

  /// <summary>
  /// Create a new note for the authenticated user.
  /// </summary>
  /// <param name="command">Note creation payload.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  [HttpPost]
  public async Task<ActionResult<CreateNoteResponse>> CreateNote([FromBody] CreateNoteCommand command, CancellationToken cancellationToken)
  {
    var authIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(authIdClaim) || !Guid.TryParse(authIdClaim, out var authUserId))
      return Unauthorized();

    command.AuthUserId = authUserId;

    var result = await _mediator.Send(command, cancellationToken);
    return CreatedAtAction(nameof(GetNoteById), new { id = result.Id }, result);
  }

  /// <summary>
  /// Update an existing note owned by the authenticated user.
  /// </summary>
  /// <param name="id">Note ID.</param>
  /// <param name="command">Note update payload.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  [HttpPut("{id:guid}")]
  public async Task<ActionResult<UpdateNoteResponse>> UpdateNote(Guid id, [FromBody] UpdateNoteCommand command, CancellationToken cancellationToken)
  {
    var authIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(authIdClaim) || !Guid.TryParse(authIdClaim, out var authUserId))
      return Unauthorized();

    command.Id = id;
    command.AuthUserId = authUserId;

    var result = await _mediator.Send(command, cancellationToken);
    return Ok(result);
  }

  /// <summary>
  /// Delete a note owned by the authenticated user.
  /// </summary>
  /// <param name="id">Note ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> DeleteNote(Guid id, CancellationToken cancellationToken)
  {
    var authIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(authIdClaim) || !Guid.TryParse(authIdClaim, out var authUserId))
      return Unauthorized();

    var command = new DeleteNoteCommand { Id = id, AuthUserId = authUserId };
    await _mediator.Send(command, cancellationToken);

    return NoContent();
  }
}