using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassNodes.Commands.ToggleLockClassNode;

public class ToggleLockClassNodeCommandHandler : IRequestHandler<ToggleLockClassNodeCommand, Unit>
{
    private readonly IClassNodeRepository _classNodeRepository;
    private readonly ILogger<ToggleLockClassNodeCommandHandler> _logger;

    public ToggleLockClassNodeCommandHandler(IClassNodeRepository classNodeRepository, ILogger<ToggleLockClassNodeCommandHandler> logger)
    {
        _classNodeRepository = classNodeRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(ToggleLockClassNodeCommand request, CancellationToken cancellationToken)
    {
        var node = await _classNodeRepository.GetByIdAsync(request.NodeId, cancellationToken);
        if (node == null || node.ClassId != request.ClassId)
            throw new NotFoundException($"Node {request.NodeId} not found in class {request.ClassId}.");

        node.IsLockedByImport = request.IsLocked;
        node.Metadata ??= new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            node.Metadata["lock_reason"] = request.Reason!;
            node.Metadata["lock_updated_at"] = DateTimeOffset.UtcNow.ToString("o");
        }
        else
        {
            node.Metadata.Remove("lock_reason");
        }

        await _classNodeRepository.UpdateAsync(node, cancellationToken);
        _logger.LogInformation("{Action} lock for ClassNode {NodeId}", request.IsLocked ? "Enabled" : "Disabled", request.NodeId);
        return Unit.Value;
    }
}
