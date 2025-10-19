using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Classes.Commands.SoftDeleteClass;

public class SoftDeleteClassCommandHandler : IRequestHandler<SoftDeleteClassCommand, Unit>
{
    private readonly IClassRepository _classRepository;

    public SoftDeleteClassCommandHandler(IClassRepository classRepository)
    {
        _classRepository = classRepository;
    }

    public async Task<Unit> Handle(SoftDeleteClassCommand request, CancellationToken cancellationToken)
    {
        var entity = await _classRepository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
            throw new NotFoundException($"Class {request.Id} not found.");

        entity.IsActive = false;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _classRepository.UpdateAsync(entity, cancellationToken);
        return Unit.Value;
    }
}
