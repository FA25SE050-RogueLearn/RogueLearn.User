// RogueLearn.User/src/RogueLearn.User.Api/Controllers/GuildPostsController.cs
using System.Net.Mime;
using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.GuildPosts.Commands.CreateGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.Commands.DeleteGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.Commands.EditGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.Commands.GuildMasterActions;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPosts;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPostById;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetPinnedGuildPosts;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.CreateGuildPostComment;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.EditGuildPostComment;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Comments.DeleteGuildPostComment;
using RogueLearn.User.Application.Features.GuildPosts.Queries.GetGuildPostComments;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Likes.LikeGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Likes.UnlikeGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.Commands.Images;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
public class GuildPostsController : ControllerBase
{
    private readonly IMediator _mediator;
    public GuildPostsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Public/member endpoints under /api/guilds
    /// <summary>
    /// List guild posts with optional filters (tag, author, pinned, search) and pagination.
    /// </summary>
    [HttpGet("api/guilds/{guildId:guid}/posts")]
    [ProducesResponseType(typeof(IEnumerable<GuildPostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGuildPosts(
        [FromRoute] Guid guildId,
        [FromQuery] string? tag,
        [FromQuery] Guid? authorId,
        [FromQuery] bool? pinned,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken cancellationToken = default)
    {
        var posts = await _mediator.Send(new GetGuildPostsQuery(guildId, tag, authorId, pinned, search, page, size), cancellationToken);
        return Ok(posts);
    }

    /// <summary>
    /// Get pinned posts for a guild.
    /// </summary>
    [HttpGet("api/guilds/{guildId:guid}/posts/pinned")]
    [ProducesResponseType(typeof(IEnumerable<GuildPostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPinnedGuildPosts([FromRoute] Guid guildId, CancellationToken cancellationToken)
    {
        var posts = await _mediator.Send(new GetPinnedGuildPostsQuery(guildId), cancellationToken);
        return Ok(posts);
    }

    /// <summary>
    /// Get a guild post by id.
    /// </summary>
    [HttpGet("api/guilds/{guildId:guid}/posts/{postId:guid}")]
    [ProducesResponseType(typeof(GuildPostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGuildPostById([FromRoute] Guid guildId, [FromRoute] Guid postId, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new GetGuildPostByIdQuery(guildId, postId), cancellationToken);
        return dto is not null ? Ok(dto) : NotFound();
    }

    /// <summary>
    /// Create a guild post (JSON).
    /// </summary>
    [HttpPost("api/guilds/{guildId:guid}/posts")]
    [ProducesResponseType(typeof(CreateGuildPostResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateGuildPost([FromRoute] Guid guildId, [FromBody] CreateGuildPostRequest request, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var result = await _mediator.Send(new CreateGuildPostCommand
        {
            GuildId = guildId,
            AuthorAuthUserId = authUserId,
            Request = request
        }, cancellationToken);
        return CreatedAtAction(nameof(GetGuildPostById), new { guildId, postId = result.PostId }, result);
    }

    [HttpPost("api/guilds/{guildId:guid}/posts/form")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateGuildPostResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGuildPostForm([FromRoute] Guid guildId, [FromForm] string title, [FromForm] string content, [FromForm] string[]? tags, [FromForm] IFormFileCollection files, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var list = new List<GuildPostImageUpload>();
        foreach (var file in files)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, cancellationToken);
            list.Add(new GuildPostImageUpload(ms.ToArray(), file.ContentType, file.FileName));
        }
        var req = new CreateGuildPostRequest
        {
            Title = title,
            Content = content,
            Tags = tags,
            Attachments = null,
            Images = list
        };
        var result = await _mediator.Send(new CreateGuildPostCommand
        {
            GuildId = guildId,
            AuthorAuthUserId = authUserId,
            Request = req
        }, cancellationToken);
        return CreatedAtAction(nameof(GetGuildPostById), new { guildId, postId = result.PostId }, result);
    }

    [HttpPost("api/guilds/{guildId:guid}/posts/{postId:guid}/images")]
    [ProducesResponseType(typeof(UploadGuildPostImagesResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadGuildPostImages([FromRoute] Guid guildId, [FromRoute] Guid postId, [FromForm] IFormFileCollection files, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var list = new List<GuildPostImageUpload>();
        foreach (var file in files)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, cancellationToken);
            list.Add(new GuildPostImageUpload(ms.ToArray(), file.ContentType, file.FileName));
        }
        var result = await _mediator.Send(new UploadGuildPostImagesCommand
        {
            GuildId = guildId,
            PostId = postId,
            AuthUserId = authUserId,
            Images = list
        }, cancellationToken);
        return Created($"/api/guilds/{guildId}/posts/{postId}", result);
    }

    /// <summary>
    /// Edit a guild post (JSON).
    /// </summary>
    [HttpPut("api/guilds/{guildId:guid}/posts/{postId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> EditGuildPost([FromRoute] Guid guildId, [FromRoute] Guid postId, [FromBody] EditGuildPostRequest request, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new EditGuildPostCommand(guildId, postId, authUserId, request), cancellationToken);
        return NoContent();
    }

    [HttpPut("api/guilds/{guildId:guid}/posts/{postId:guid}/form")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EditGuildPostForm([FromRoute] Guid guildId, [FromRoute] Guid postId, [FromForm] string title, [FromForm] string content, [FromForm] string[]? tags, [FromForm] IFormFileCollection files, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var list = new List<GuildPostImageUpload>();
        foreach (var file in files)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, cancellationToken);
            list.Add(new GuildPostImageUpload(ms.ToArray(), file.ContentType, file.FileName));
        }
        var req = new EditGuildPostRequest
        {
            Title = title,
            Content = content,
            Tags = tags,
            Attachments = null,
            Images = list
        };
        await _mediator.Send(new EditGuildPostCommand(guildId, postId, authUserId, req), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Delete a guild post.
    /// </summary>
    [HttpDelete("api/guilds/{guildId:guid}/posts/{postId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteGuildPost([FromRoute] Guid guildId, [FromRoute] Guid postId, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new DeleteGuildPostCommand(guildId, postId, authUserId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Create a comment on a guild post.
    /// </summary>
    [HttpPost("api/guilds/{guildId:guid}/posts/{postId:guid}/comments")]
    [ProducesResponseType(typeof(CreateGuildPostCommentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGuildPostComment([FromRoute] Guid guildId, [FromRoute] Guid postId, [FromBody] CreateGuildPostCommentRequest request, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var result = await _mediator.Send(new CreateGuildPostCommentCommand
        {
            GuildId = guildId,
            PostId = postId,
            AuthorId = authUserId,
            Request = request
        }, cancellationToken);
        return Created($"/api/guilds/{guildId}/posts/{postId}/comments/{result.CommentId}", result);
    }

    /// <summary>
    /// Update own comment content.
    /// </summary>
    [HttpPut("api/guilds/{guildId:guid}/posts/{postId:guid}/comments/{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EditGuildPostComment([FromRoute] Guid guildId, [FromRoute] Guid postId, [FromRoute] Guid commentId, [FromBody] EditGuildPostCommentRequest request, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new EditGuildPostCommentCommand
        {
            GuildId = guildId,
            PostId = postId,
            CommentId = commentId,
            AuthorId = authUserId,
            Request = request
        }, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft-delete own comment.
    /// </summary>
    [HttpDelete("api/guilds/{guildId:guid}/posts/{postId:guid}/comments/{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGuildPostComment([FromRoute] Guid guildId, [FromRoute] Guid postId, [FromRoute] Guid commentId, CancellationToken cancellationToken)
    {
        var requesterId = User.GetAuthUserId();
        await _mediator.Send(new DeleteGuildPostCommentCommand(guildId, postId, commentId, requesterId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Get comments for a guild post.
    /// </summary>
    [HttpGet("api/guilds/{guildId:guid}/posts/{postId:guid}/comments")]
    [ProducesResponseType(typeof(IEnumerable<GuildPostCommentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGuildPostComments([FromRoute] Guid guildId, [FromRoute] Guid postId, [FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] string? sort = null, CancellationToken cancellationToken = default)
    {
        var list = await _mediator.Send(new GetGuildPostCommentsQuery(guildId, postId, page, size, sort), cancellationToken);
        return Ok(list);
    }

    /// <summary>
    /// Idempotently like a guild post.
    /// </summary>
    [HttpPost("api/guilds/{guildId:guid}/posts/{postId:guid}/like")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LikeGuildPost([FromRoute] Guid guildId, [FromRoute] Guid postId, CancellationToken cancellationToken)
    {
        var userId = User.GetAuthUserId();
        await _mediator.Send(new LikeGuildPostCommand(guildId, postId, userId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Remove like from a guild post.
    /// </summary>
    [HttpDelete("api/guilds/{guildId:guid}/posts/{postId:guid}/like")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnlikeGuildPost([FromRoute] Guid guildId, [FromRoute] Guid postId, CancellationToken cancellationToken)
    {
        var userId = User.GetAuthUserId();
        await _mediator.Send(new UnlikeGuildPostCommand(guildId, postId, userId), cancellationToken);
        return NoContent();
    }

    // Admin/moderation endpoints under /api/admin/guilds - allow GuildMaster or Officer or Platform Admin
    /// <summary>
    /// Pin a guild post.
    /// </summary>
    [HttpPost("~/api/guilds/{guildId:guid}/posts/{postId:guid}/pin")]
    [GuildMasterOrOfficerOnly("guildId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PinGuildPost([FromRoute] Guid guildId, [FromRoute] Guid postId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new PinGuildPostCommand(guildId, postId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Unpin a guild post.
    /// </summary>
    [HttpPost("~/api/guilds/{guildId:guid}/posts/{postId:guid}/unpin")]
    [GuildMasterOrOfficerOnly("guildId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnpinGuildPost([FromRoute] Guid guildId, [FromRoute] Guid postId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UnpinGuildPostCommand(guildId, postId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lock a guild post.
    /// </summary>
    [HttpPost("~/api/guilds/{guildId:guid}/posts/{postId:guid}/lock")]
    [GuildMasterOrOfficerOnly("guildId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LockGuildPost([FromRoute] Guid guildId, [FromRoute] Guid postId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new LockGuildPostCommand(guildId, postId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Unlock a guild post.
    /// </summary>
    [HttpPost("~/api/guilds/{guildId:guid}/posts/{postId:guid}/unlock")]
    [GuildMasterOrOfficerOnly("guildId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnlockGuildPost([FromRoute] Guid guildId, [FromRoute] Guid postId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UnlockGuildPostCommand(guildId, postId), cancellationToken);
        return NoContent();
    }
}
