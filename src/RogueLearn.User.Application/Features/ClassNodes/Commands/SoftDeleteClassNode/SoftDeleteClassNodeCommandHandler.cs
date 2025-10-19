using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassNodes.Commands.SoftDeleteClassNode;

public class SoftDeleteClassNodeCommandHandler : IRequestHandler<SoftDeleteClassNodeCommand, Unit>
{
    private readonly IClassNodeRepository _classNodeRepository;
    private readonly ILogger<SoftDeleteClassNodeCommandHandler> _logger;

    public SoftDeleteClassNodeCommandHandler(IClassNodeRepository classNodeRepository, ILogger<SoftDeleteClassNodeCommandHandler> logger)
    {
        _classNodeRepository = classNodeRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(SoftDeleteClassNodeCommand request, CancellationToken cancellationToken)
    {
        var node = await _classNodeRepository.GetByIdAsync(request.NodeId, cancellationToken);
        if (node == null || node.ClassId != request.ClassId)
            throw new NotFoundException($"Node {request.NodeId} not found in class {request.ClassId}.");
        if (node.IsLockedByImport)
            throw new ForbiddenException("Cannot delete a locked (imported) node. Unlock it first.");

        node.IsActive = false;
        await _classNodeRepository.UpdateAsync(node, cancellationToken);

        // Reorder siblings to fill gap
        var siblings = (await _classNodeRepository.FindAsync(x => x.ClassId == request.ClassId && x.ParentId == node.ParentId, cancellationToken))
            .OrderBy(s => s.Sequence).ToList();
        foreach (var sib in siblings.Where(s => s.IsActive && s.Sequence > node.Sequence))
        {
            sib.Sequence -= 1;
            await _classNodeRepository.UpdateAsync(sib, cancellationToken);
        }

        _logger.LogInformation("Soft-deleted ClassNode {NodeId} in class {ClassId}", request.NodeId, request.ClassId);
        return Unit.Value;
    }
}
