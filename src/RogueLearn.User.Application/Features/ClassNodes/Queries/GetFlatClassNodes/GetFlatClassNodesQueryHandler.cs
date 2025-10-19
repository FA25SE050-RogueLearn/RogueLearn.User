using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassNodes.Queries.GetFlatClassNodes;

public class GetFlatClassNodesQueryHandler : IRequestHandler<GetFlatClassNodesQuery, IReadOnlyList<ClassNode>>
{
    private readonly IClassNodeRepository _classNodeRepository;

    public GetFlatClassNodesQueryHandler(IClassNodeRepository classNodeRepository)
    {
        _classNodeRepository = classNodeRepository;
    }

    public async Task<IReadOnlyList<ClassNode>> Handle(GetFlatClassNodesQuery request, CancellationToken cancellationToken)
    {
        var nodes = await _classNodeRepository.FindAsync(x => x.ClassId == request.ClassId && (!request.OnlyActive || x.IsActive), cancellationToken);
        var ordered = nodes
            .OrderBy(x => x.ParentId.HasValue ? 1 : 0)
            .ThenBy(x => x.ParentId)
            .ThenBy(x => x.Sequence)
            .ToList();
        return ordered;
    }
}
