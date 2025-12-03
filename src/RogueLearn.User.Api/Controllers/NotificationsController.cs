using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Notifications.Queries.GetMyNotifications;
using RogueLearn.User.Application.Features.Notifications.Queries.GetUnreadCount;
using RogueLearn.User.Application.Features.Notifications.Commands.MarkAsRead;
using RogueLearn.User.Application.Features.Notifications.Commands.MarkAllRead;
using RogueLearn.User.Application.Features.Notifications.Commands.Delete;
using RogueLearn.User.Application.Features.Notifications.Commands.DeleteBatch;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get the latest notifications for the authenticated user.
    /// </summary>
    /// <param name="size">Maximum number of notifications to return (default 20).</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Notification>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<Notification>>> GetMyNotifications([FromQuery] int size = 20, CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
        var items = await _mediator.Send(new GetMyNotificationsQuery(authUserId, size), cancellationToken);
        return Ok(items);
    }

    /// <summary>
    /// Get the unread notifications count for the authenticated user.
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
        var count = await _mediator.Send(new GetMyUnreadNotificationCountQuery(authUserId), cancellationToken);
        return Ok(count);
    }

    /// <summary>
    /// Mark a notification as read (only if it belongs to the authenticated user).
    /// </summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new MarkNotificationReadCommand(id, authUserId), cancellationToken);
        return NoContent();
    }

    [HttpPost("mark-all-read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new MarkAllNotificationsReadCommand(authUserId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Delete a notification (only if it belongs to the authenticated user).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new DeleteNotificationCommand(id, authUserId), cancellationToken);
        return NoContent();
    }

    [HttpPost("batch-delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BatchDelete([FromBody] Guid[] ids, CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new DeleteNotificationsBatchCommand(ids, authUserId), cancellationToken);
        return NoContent();
    }
}