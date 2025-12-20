using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.DeleteClass;

public class DeleteClassCommandHandler : IRequestHandler<DeleteClassCommand>
{
    private readonly IClassRepository _classRepository;
    private readonly ILogger<DeleteClassCommandHandler> _logger;

    public DeleteClassCommandHandler(IClassRepository classRepository, ILogger<DeleteClassCommandHandler> logger)
    {
        _classRepository = classRepository;
        _logger = logger;
    }

    public async Task Handle(DeleteClassCommand request, CancellationToken cancellationToken)
    {
        var existingClass = await _classRepository.GetByIdAsync(request.Id, cancellationToken);
        if (existingClass == null)
        {
            throw new NotFoundException("Class", request.Id);
        }

        await _classRepository.DeleteAsync(request.Id, cancellationToken);
        _logger.LogInformation("Deleted Class {ClassId}", request.Id);
    }
}