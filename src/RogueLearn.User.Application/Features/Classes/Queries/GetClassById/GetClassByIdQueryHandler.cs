using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Classes.Queries.GetClassById;

public class GetClassByIdQueryHandler : IRequestHandler<GetClassByIdQuery, Class?>
{
    private readonly IClassRepository _classRepository;

    public GetClassByIdQueryHandler(IClassRepository classRepository)
    {
        _classRepository = classRepository;
    }

    public async Task<Class?> Handle(GetClassByIdQuery request, CancellationToken cancellationToken)
    {
        return await _classRepository.GetByIdAsync(request.Id, cancellationToken);
    }
}
