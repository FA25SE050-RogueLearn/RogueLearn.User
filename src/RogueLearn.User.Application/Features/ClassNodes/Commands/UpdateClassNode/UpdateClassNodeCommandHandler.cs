using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassNodes.Commands.UpdateClassNode;

public class UpdateClassNodeCommandHandler : IRequestHandler<UpdateClassNodeCommand, ClassNode>
{
    private readonly IClassNodeRepository _classNodeRepository;
    private readonly ILogger<UpdateClassNodeCommandHandler> _logger;

    public UpdateClassNodeCommandHandler(IClassNodeRepository classNodeRepository, ILogger<UpdateClassNodeCommandHandler> logger)
    {
        _classNodeRepository = classNodeRepository;
        _logger = logger;
    }

    public async Task<ClassNode> Handle(UpdateClassNodeCommand request, CancellationToken cancellationToken)
    {
        var node = await _classNodeRepository.GetByIdAsync(request.NodeId, cancellationToken);
        if (node == null || node.ClassId != request.ClassId)
            throw new NotFoundException($"Node {request.NodeId} not found in class {request.ClassId}.");
        if (node.IsLockedByImport)
            throw new ForbiddenException("Cannot update a locked (imported) node. Unlock it first.");

        if (!string.IsNullOrWhiteSpace(request.Title)) node.Title = request.Title!;
        if (request.NodeType != null) node.NodeType = request.NodeType;
        if (request.Description != null) node.Description = request.Description;

        // Handle sequence change within same parent
        if (request.Sequence.HasValue && request.Sequence.Value != node.Sequence)
        {
            var siblings = (await _classNodeRepository.FindAsync(x => x.ClassId == request.ClassId && x.ParentId == node.ParentId, cancellationToken))
                .OrderBy(s => s.Sequence).ToList();

            int oldIndex = node.Sequence; // one-based
            int newIndex = Math.Max(1, Math.Min(request.Sequence.Value, siblings.Count));

            if (newIndex > oldIndex)
            {
                // Shift left siblings between oldIndex+1..newIndex down by 1
                foreach (var sib in siblings.Where(s => s.Sequence > oldIndex && s.Sequence <= newIndex))
                {
                    sib.Sequence -= 1;
                    await _classNodeRepository.UpdateAsync(sib, cancellationToken);
                }
            }
            else if (newIndex < oldIndex)
            {
                // Shift right siblings between newIndex..oldIndex-1 up by 1
                foreach (var sib in siblings.Where(s => s.Sequence >= newIndex && s.Sequence < oldIndex))
                {
                    sib.Sequence += 1;
                    await _classNodeRepository.UpdateAsync(sib, cancellationToken);
                }
            }

            node.Sequence = newIndex;
        }

        node = await _classNodeRepository.UpdateAsync(node, cancellationToken);
        _logger.LogInformation("Updated ClassNode {NodeId} in class {ClassId}", node.Id, request.ClassId);
        return node;
    }
}
