using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Classes.Queries.GetClasses;

public class GetClassesQueryHandler : IRequestHandler<GetClassesQuery, IReadOnlyList<Class>>
{
    private readonly IClassRepository _classRepository;

    public GetClassesQueryHandler(IClassRepository classRepository)
    {
        _classRepository = classRepository;
    }

    public async Task<IReadOnlyList<Class>> Handle(GetClassesQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<Class> classes;
        if (request.Active.HasValue)
        {
            classes = await _classRepository.FindAsync(c => c.IsActive == request.Active.Value, cancellationToken);
        }
        else
        {
            classes = await _classRepository.GetAllAsync(cancellationToken);
        }

        return classes.OrderBy(c => c.Name).ToList();
    }
}
