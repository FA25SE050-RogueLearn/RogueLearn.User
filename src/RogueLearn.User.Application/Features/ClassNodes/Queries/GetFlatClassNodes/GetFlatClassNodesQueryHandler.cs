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
        // Avoid complex OR logic in expression trees which the Supabase/Postgrest translator may not support.
        // Build the predicate conditionally to prevent null filters in PrepareFilter.
        IEnumerable<ClassNode> nodes;
        if (request.OnlyActive)
        {
            nodes = await _classNodeRepository.FindAsync(x => x.ClassId == request.ClassId && x.IsActive, cancellationToken);
        }
        else
        {
            nodes = await _classNodeRepository.FindAsync(x => x.ClassId == request.ClassId, cancellationToken);
        }
        var ordered = nodes
            .OrderBy(x => x.ParentId.HasValue ? 1 : 0)
            .ThenBy(x => x.ParentId)
            .ThenBy(x => x.Sequence)
            .ToList();
        return ordered;
    }
}
