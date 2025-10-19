using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Classes.Commands.RestoreClass;

public class RestoreClassCommandHandler : IRequestHandler<RestoreClassCommand, Unit>
{
    private readonly IClassRepository _classRepository;

    public RestoreClassCommandHandler(IClassRepository classRepository)
    {
        _classRepository = classRepository;
    }

    public async Task<Unit> Handle(RestoreClassCommand request, CancellationToken cancellationToken)
    {
        var entity = await _classRepository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
            throw new NotFoundException($"Class {request.Id} not found.");

        entity.IsActive = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _classRepository.UpdateAsync(entity, cancellationToken);
        return Unit.Value;
    }
}
