using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Application.Features.ClassNodes.Queries.GetFlatClassNodes;
using RogueLearn.User.Application.Features.ClassNodes.Queries.GetTreeClassNodes;
using RogueLearn.User.Application.Features.ClassNodes.Commands.CreateClassNode;
using RogueLearn.User.Application.Features.ClassNodes.Commands.UpdateClassNode;
using RogueLearn.User.Application.Features.ClassNodes.Commands.MoveClassNode;
using RogueLearn.User.Application.Features.ClassNodes.Commands.ReorderClassNodes;
using RogueLearn.User.Application.Features.ClassNodes.Commands.SoftDeleteClassNode;
using RogueLearn.User.Application.Features.ClassNodes.Commands.ToggleLockClassNode;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/classes/{classId:guid}/nodes")]
[AdminOnly]
public class ClassNodesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClassNodesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get a flat list of nodes for a class.
    /// </summary>
    [HttpGet("flat")]
    [ProducesResponseType(typeof(List<ClassNodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ClassNodeDto>>> GetFlat(Guid classId, [FromQuery] bool onlyActive = false, CancellationToken cancellationToken = default)
    {
        var nodes = await _mediator.Send(new GetFlatClassNodesQuery(classId, onlyActive), cancellationToken);
        var result = nodes.Select(ClassNodeDto.FromEntity).ToList();
        return Ok(result);
    }

    /// <summary>
    /// Get a hierarchical tree of nodes for a class.
    /// </summary>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(List<ClassNodeTreeItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ClassNodeTreeItemDto>>> GetTree(Guid classId, [FromQuery] bool onlyActive = false, CancellationToken cancellationToken = default)
    {
        var tree = await _mediator.Send(new GetTreeClassNodesQuery(classId, onlyActive), cancellationToken);
        var result = tree.Select(ClassNodeTreeItemDto.FromModel).ToList();
        return Ok(result);
    }

    /// <summary>
    /// Create a new node under a parent (or as root if parentId is null).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClassNodeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassNodeDto>> Create(Guid classId, [FromBody] CreateNodeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required.");

        var node = await _mediator.Send(new CreateClassNodeCommand(
            classId,
            request.Title.Trim(),
            request.NodeType,
            request.Description,
            request.ParentId,
            request.Sequence
        ), cancellationToken);

        var dto = ClassNodeDto.FromEntity(node);
        return CreatedAtAction(nameof(GetFlat), new { classId }, dto);
    }

    /// <summary>
    /// Update a node's attributes or sequence.
    /// </summary>
    [HttpPut("{nodeId:guid}")]
    [ProducesResponseType(typeof(ClassNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassNodeDto>> Update(Guid classId, Guid nodeId, [FromBody] UpdateNodeRequest request, CancellationToken cancellationToken = default)
    {
        var node = await _mediator.Send(new UpdateClassNodeCommand(
            classId,
            nodeId,
            request.Title,
            request.NodeType,
            request.Description,
            request.Sequence
        ), cancellationToken);
        return Ok(ClassNodeDto.FromEntity(node));
    }

    /// <summary>
    /// Move a node to a new parent and sequence.
    /// </summary>
    [HttpPost("{nodeId:guid}/move")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Move(Guid classId, Guid nodeId, [FromBody] MoveNodeRequest request, CancellationToken cancellationToken = default)
    {
        if (request.NewSequence <= 0)
            return BadRequest("newSequence must be >= 1.");

        await _mediator.Send(new MoveClassNodeCommand(classId, nodeId, request.NewParentId, request.NewSequence), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Bulk reorder direct children under a parent.
    /// </summary>
    [HttpPost("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reorder(Guid classId, [FromBody] ReorderNodesRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items is null || request.Items.Count == 0)
            return BadRequest("items must contain at least one entry.");

        var items = request.Items.Select(i => (i.NodeId, i.Sequence)).ToList();
        await _mediator.Send(new ReorderClassNodesCommand(classId, request.ParentId, items), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft delete a node (sets is_active=false and compacts sibling sequence).
    /// </summary>
    [HttpDelete("{nodeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDelete(Guid classId, Guid nodeId, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new SoftDeleteClassNodeCommand(classId, nodeId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Toggle lock flag on a node (used for import protection).
    /// </summary>
    [HttpPost("{nodeId:guid}/lock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleLock(Guid classId, Guid nodeId, [FromBody] ToggleLockRequest request, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new ToggleLockClassNodeCommand(classId, nodeId, request.IsLocked, request.Reason), cancellationToken);
        return NoContent();
    }

    // DTOs and Requests
    public record ClassNodeDto(
        Guid Id,
        Guid ClassId,
        Guid? ParentId,
        string Title,
        string? NodeType,
        string? Description,
        int Sequence,
        bool IsActive,
        bool IsLockedByImport,
        Dictionary<string, object>? Metadata,
        DateTimeOffset CreatedAt)
    {
        public static ClassNodeDto FromEntity(ClassNode n) => new(
            n.Id,
            n.ClassId,
            n.ParentId,
            n.Title,
            n.NodeType,
            n.Description,
            n.Sequence,
            n.IsActive,
            n.IsLockedByImport,
            n.Metadata,
            n.CreatedAt
        );
    }

    public record ClassNodeTreeItemDto(ClassNodeDto Node, List<ClassNodeTreeItemDto> Children)
    {
        public static ClassNodeTreeItemDto FromModel(ClassNodeTreeItem item)
        {
            return new ClassNodeTreeItemDto(ClassNodeDto.FromEntity(item.Node), item.Children.Select(FromModel).ToList());
        }
    }

    public class CreateNodeRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? NodeType { get; set; }
        public string? Description { get; set; }
        public Guid? ParentId { get; set; }
        public int? Sequence { get; set; }
    }

    public class UpdateNodeRequest
    {
        public string? Title { get; set; }
        public string? NodeType { get; set; }
        public string? Description { get; set; }
        public int? Sequence { get; set; }
    }

    public class MoveNodeRequest
    {
        public Guid? NewParentId { get; set; }
        public int NewSequence { get; set; }
    }

    public class ReorderNodesRequest
    {
        public Guid? ParentId { get; set; }
        public List<ReorderItem> Items { get; set; } = new();
    }

    public class ReorderItem
    {
        public Guid NodeId { get; set; }
        public int Sequence { get; set; }
    }

    public class ToggleLockRequest
    {
        public bool IsLocked { get; set; }
        public string? Reason { get; set; }
    }
}