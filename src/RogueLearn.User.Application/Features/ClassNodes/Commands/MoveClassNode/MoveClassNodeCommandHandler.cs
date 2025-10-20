using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassNodes.Commands.MoveClassNode;

public class MoveClassNodeCommandHandler : IRequestHandler<MoveClassNodeCommand, Unit>
{
    private readonly IClassNodeRepository _classNodeRepository;
    private readonly ILogger<MoveClassNodeCommandHandler> _logger;

    public MoveClassNodeCommandHandler(IClassNodeRepository classNodeRepository, ILogger<MoveClassNodeCommandHandler> logger)
    {
        _classNodeRepository = classNodeRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(MoveClassNodeCommand request, CancellationToken cancellationToken)
    {
        var node = await _classNodeRepository.GetByIdAsync(request.NodeId, cancellationToken);
        if (node == null || node.ClassId != request.ClassId)
            throw new NotFoundException($"Node {request.NodeId} not found in class {request.ClassId}.");
        if (node.IsLockedByImport)
            throw new ForbiddenException("Cannot move a locked (imported) node. Unlock it first.");

        ClassNode? newParent = null;
        if (request.NewParentId.HasValue)
        {
            newParent = await _classNodeRepository.GetByIdAsync(request.NewParentId.Value, cancellationToken);
            if (newParent == null || newParent.ClassId != request.ClassId)
                throw new NotFoundException($"Destination parent {request.NewParentId} not found in class {request.ClassId}.");
            if (newParent.IsLockedByImport)
                throw new ForbiddenException("Cannot move under a locked (imported) parent node.");
        }

        // Cycle detection
        var allNodes = await _classNodeRepository.FindAsync(x => x.ClassId == request.ClassId, cancellationToken);
        // Build children lookup only for non-null parents to avoid null dictionary keys
        var childrenLookup = allNodes
            .Where(n => n.ParentId.HasValue)
            .GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var stack = new Stack<Guid>();
        stack.Push(node.Id);
        var descendants = new HashSet<Guid>();
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (childrenLookup.TryGetValue(current, out var kids))
            {
                foreach (var kid in kids)
                {
                    if (descendants.Add(kid.Id)) stack.Push(kid.Id);
                }
            }
        }
        if (request.NewParentId.HasValue && (request.NewParentId.Value == node.Id || descendants.Contains(request.NewParentId.Value)))
            throw new BadRequestException("Cannot move a node under itself or its descendant (cycle detected).");

        // Reorder old siblings to fill the gap
        var oldSiblings = (await _classNodeRepository.FindAsync(x => x.ClassId == request.ClassId && x.ParentId == node.ParentId, cancellationToken))
            .OrderBy(s => s.Sequence).ToList();
        foreach (var sib in oldSiblings.Where(s => s.Sequence > node.Sequence))
        {
            sib.Sequence -= 1;
            await _classNodeRepository.UpdateAsync(sib, cancellationToken);
        }

        // Prepare target siblings
        var targetSiblings = (await _classNodeRepository.FindAsync(x => x.ClassId == request.ClassId && x.ParentId == request.NewParentId, cancellationToken))
            .OrderBy(s => s.Sequence).ToList();
        int insertSequence = Math.Max(1, Math.Min(request.NewSequence, targetSiblings.Count + 1));
        foreach (var sib in targetSiblings.Where(s => s.Sequence >= insertSequence))
        {
            sib.Sequence += 1;
            await _classNodeRepository.UpdateAsync(sib, cancellationToken);
        }

        node.ParentId = request.NewParentId;
        node.Sequence = insertSequence;
        await _classNodeRepository.UpdateAsync(node, cancellationToken);

        _logger.LogInformation("Moved ClassNode {NodeId} to parent {ParentId} at sequence {Sequence}", node.Id, request.NewParentId, insertSequence);
        return Unit.Value;
    }
}
