using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassNodes.Commands.ReorderClassNodes;

public class ReorderClassNodesCommandHandler : IRequestHandler<ReorderClassNodesCommand, Unit>
{
    private readonly IClassNodeRepository _classNodeRepository;
    private readonly ILogger<ReorderClassNodesCommandHandler> _logger;

    public ReorderClassNodesCommandHandler(IClassNodeRepository classNodeRepository, ILogger<ReorderClassNodesCommandHandler> logger)
    {
        _classNodeRepository = classNodeRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(ReorderClassNodesCommand request, CancellationToken cancellationToken)
    {
        // Validate parent lock
        if (request.ParentId.HasValue)
        {
            var parent = await _classNodeRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent == null || parent.ClassId != request.ClassId)
                throw new NotFoundException($"Parent node {request.ParentId} not found in class {request.ClassId}.");
            if (parent.IsLockedByImport)
                throw new ForbiddenException("Cannot reorder children of a locked (imported) parent node.");
        }

        var siblings = (await _classNodeRepository.FindAsync(x => x.ClassId == request.ClassId && x.ParentId == request.ParentId, cancellationToken))
            .OrderBy(s => s.Sequence).ToList();
        var siblingIds = siblings.Select(s => s.Id).ToHashSet();

        // Validate items only include current siblings and none are locked
        foreach (var (nodeId, _) in request.Items)
        {
            if (!siblingIds.Contains(nodeId))
                throw new BadRequestException("Reorder contains a node that is not a direct child of the given parent.");
            var n = siblings.First(s => s.Id == nodeId);
            if (n.IsLockedByImport)
                throw new ForbiddenException("Cannot reorder a locked (imported) node. Unlock first.");
        }

        // Normalize requested order
        var normalized = request.Items
            .OrderBy(i => i.sequence)
            .Select((i, index) => new { i.nodeId, seq = index + 1 })
            .ToList();

        // Apply
        foreach (var change in normalized)
        {
            var node = siblings.First(s => s.Id == change.nodeId);
            if (node.Sequence != change.seq)
            {
                node.Sequence = change.seq;
                await _classNodeRepository.UpdateAsync(node, cancellationToken);
            }
        }

        _logger.LogInformation("Reordered {Count} nodes under parent {ParentId} in class {ClassId}", normalized.Count, request.ParentId, request.ClassId);
        return Unit.Value;
    }
}
