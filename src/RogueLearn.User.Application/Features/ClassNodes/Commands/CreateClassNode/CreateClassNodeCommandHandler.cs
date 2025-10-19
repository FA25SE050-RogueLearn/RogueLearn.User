using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassNodes.Commands.CreateClassNode;

public class CreateClassNodeCommandHandler : IRequestHandler<CreateClassNodeCommand, ClassNode>
{
    private readonly IClassNodeRepository _classNodeRepository;
    private readonly ILogger<CreateClassNodeCommandHandler> _logger;

    public CreateClassNodeCommandHandler(IClassNodeRepository classNodeRepository, ILogger<CreateClassNodeCommandHandler> logger)
    {
        _classNodeRepository = classNodeRepository;
        _logger = logger;
    }

    public async Task<ClassNode> Handle(CreateClassNodeCommand request, CancellationToken cancellationToken)
    {
        // Validate parent
        if (request.ParentId.HasValue)
        {
            var parent = await _classNodeRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent == null || parent.ClassId != request.ClassId)
                throw new NotFoundException($"Parent node {request.ParentId} not found in class {request.ClassId}.");
            if (parent.IsLockedByImport)
                throw new ForbiddenException("Cannot add child to a locked (imported) node. Unlock the parent first.");
        }

        var siblings = await _classNodeRepository.FindAsync(x => x.ClassId == request.ClassId && x.ParentId == request.ParentId, cancellationToken);
        var siblingsOrdered = siblings.OrderBy(s => s.Sequence).ToList();

        int insertSequence;
        if (request.Sequence.HasValue)
        {
            insertSequence = Math.Max(1, Math.Min(request.Sequence.Value, siblingsOrdered.Count + 1));
            int zeroIndex = insertSequence - 1;
            // Shift existing siblings at or after position up by 1
            for (int i = zeroIndex; i < siblingsOrdered.Count; i++)
            {
                var sib = siblingsOrdered[i];
                sib.Sequence = i + 2; // i (zero-based) becomes sequence (one-based) shifted up
                await _classNodeRepository.UpdateAsync(sib, cancellationToken);
            }
        }
        else
        {
            insertSequence = siblingsOrdered.Count + 1;
        }

        var node = new ClassNode
        {
            ClassId = request.ClassId,
            ParentId = request.ParentId,
            Title = request.Title,
            NodeType = request.NodeType,
            Description = request.Description,
            Sequence = insertSequence,
            IsActive = true,
            IsLockedByImport = false,
            Metadata = null,
        };

        node = await _classNodeRepository.AddAsync(node, cancellationToken);
        _logger.LogInformation("Created ClassNode {NodeId} in class {ClassId} under parent {ParentId} at sequence {Sequence}", node.Id, request.ClassId, request.ParentId, insertSequence);
        return node;
    }
}
