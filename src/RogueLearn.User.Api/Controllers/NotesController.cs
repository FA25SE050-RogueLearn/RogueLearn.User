// RogueLearn.User/src/RogueLearn.User.Api/Controllers/NotesController.cs
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNote;
using RogueLearn.User.Application.Features.Notes.Commands.DeleteNote;
using RogueLearn.User.Application.Features.Notes.Commands.UpdateNote;
using RogueLearn.User.Application.Features.Notes.Queries.GetMyNotes;
using RogueLearn.User.Application.Features.Notes.Queries.GetPublicNotes;
using RogueLearn.User.Application.Features.Notes.Queries.GetNoteById;
using RogueLearn.User.Application.Features.Notes.Commands.CreateNoteFromUpload;
using RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTags;
using RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTagsFromUpload;
using RogueLearn.User.Application.Features.AiTagging.Commands.CommitNoteTagSelections;
using RogueLearn.User.Application.Models;

using RogueLearn.User.Application.Features.Notes.Commands.CreateNoteWithAiTags;
using BuildingBlocks.Shared.Authentication;

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
    /// Creates a new note by uploading and processing a file.
    /// </summary>
    /// <param name="file">The file to upload (PDF, DOCX, TXT).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("upload-and-create")]
    [ProducesResponseType(typeof(CreateNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateNoteResponse>> CreateNoteFromUpload(IFormFile file, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

        if (file == null || file.Length == 0)
            return BadRequest("A valid file is required.");

        var command = new CreateNoteFromUploadCommand
        {
            AuthUserId = authUserId,
            FileStream = file.OpenReadStream(),
            FileName = file.FileName,
            ContentType = file.ContentType
        };

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetNoteById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get notes for the authenticated user.
    /// </summary>
    /// <param name="search">Optional search term for filtering by title.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("me")]
    [ProducesResponseType(typeof(List<NoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<NoteDto>>> GetMyNotes([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

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
    [ProducesResponseType(typeof(List<NoteDto>), StatusCodes.Status200OK)]
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
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteDto>> GetNoteById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetNoteByIdQuery { Id = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (result is null)
            return NotFound();

        // Only return private notes if the requester is the owner
        if (!result.IsPublic)
        {
            Guid requesterId;
            try
            {
                requesterId = User.GetAuthUserId();
            }
            catch
            {
                return NotFound();
            }
            if (requesterId != result.AuthUserId)
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
    [ProducesResponseType(typeof(CreateNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateNoteResponse>> CreateNote([FromBody] CreateNoteCommand command, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

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
    [ProducesResponseType(typeof(UpdateNoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateNoteResponse>> UpdateNote(Guid id, [FromBody] UpdateNoteCommand command, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNote(Guid id, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

        var command = new DeleteNoteCommand { Id = id, AuthUserId = authUserId };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Generate AI-assisted tag suggestions for a note or raw text.
    /// </summary>
    /// <remarks>
    /// Provide either <c>noteId</c> to analyze an existing note's content, or <c>rawText</c> to analyze arbitrary text.
    /// Optionally set <c>maxTags</c> (1-20) to control how many tags to suggest.
    /// </remarks>
    /// <param name="request">The query payload containing either <c>noteId</c> or <c>rawText</c> along with optional <c>maxTags</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("/api/ai/tagging/suggest")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(SuggestNoteTagsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Suggest([FromBody] SuggestNoteTagsQuery request, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

        request.AuthUserId = authUserId;
        var result = await _mediator.Send(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Generate AI-assisted tag suggestions from an uploaded file (PDF, DOCX, TXT).
    /// </summary>
    /// <remarks>
    /// Upload a file using multipart/form-data. The server will extract its text content in the handler and generate tag suggestions.
    /// </remarks>
    /// <param name="file">The uploaded file (PDF, DOCX, or TXT).</param>
    /// <param name="maxTags">Optional maximum number of tags to suggest (1-20). Default is 10.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("/api/ai/tagging/suggest-upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SuggestNoteTagsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SuggestFromUpload(IFormFile file, [FromForm] int maxTags = 10, CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
    if (file is null || file.Length == 0) return BadRequest("No file uploaded.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        var query = new SuggestNoteTagsFromUploadQuery
        {
            AuthUserId = authUserId,
            FileContent = bytes,
            ContentType = file.ContentType ?? string.Empty,
            FileName = file.FileName ?? string.Empty,
            MaxTags = maxTags
        };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Commit selected tags (existing and/or new) to the specified note.
    /// </summary>
    /// <remarks>
    /// Use this endpoint after selecting which suggested tags to apply.
    /// You may include existing tag IDs, new tag names, or both. The service will deduplicate by slug and create any missing tags.
    /// </remarks>
    /// <param name="request">The command payload containing the target <c>noteId</c>, selected existing tag IDs, and/or new tag names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("/api/ai/tagging/commit")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CommitNoteTagSelectionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Commit([FromBody] CommitNoteTagSelectionsCommand request, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

        request.AuthUserId = authUserId;
        var result = await _mediator.Send(request, cancellationToken);
        return Ok(result);
    }


    /// <summary>
    /// Create a new note (raw text) and generate AI tag suggestions, optionally auto-applying them.
    /// </summary>
    /// <param name="request">Payload containing rawText or title, with maxTags and applySuggestions options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("create-with-ai-tags")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CreateNoteWithAiTagsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateNoteWithAiTagsResponse>> CreateWithAiTags([FromBody] CreateNoteWithAiTagsCommand request, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

        request.AuthUserId = authUserId;
        var result = await _mediator.Send(request, cancellationToken);
        return CreatedAtAction(nameof(GetNoteById), new { id = result.NoteId }, result);
    }

    /// <summary>
    /// Create a new note from an uploaded file (PDF, DOCX, TXT), generate AI tag suggestions, optionally auto-applying them.
    /// </summary>
    /// <param name="file">The uploaded file.</param>
    /// <param name="maxTags">Maximum number of tag suggestions (1-20).</param>
    /// <param name="applySuggestions">Whether to auto-apply suggestions to the created note.</param>
    /// <param name="title">Optional title override for the note.</param>
    /// <param name="isPublic">Whether the created note should be public (default false).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("create-with-ai-tags/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateNoteWithAiTagsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateNoteWithAiTagsResponse>> CreateWithAiTagsFromUpload(
        IFormFile file,
        [FromForm] int maxTags = 10,
        [FromForm] bool applySuggestions = true,
        [FromForm] string? title = null,
        [FromForm] bool isPublic = false,
        CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
    if (file is null || file.Length == 0) return BadRequest("No file uploaded.");

        var command = new CreateNoteWithAiTagsCommand
        {
            AuthUserId = authUserId,
            FileStream = file.OpenReadStream(),
            FileLength = file.Length,
            ContentType = file.ContentType ?? string.Empty,
            FileName = file.FileName ?? string.Empty,
            Title = title,
            IsPublic = isPublic,
            MaxTags = maxTags,
            ApplySuggestions = applySuggestions
        };

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetNoteById), new { id = result.NoteId }, result);
    }
}