using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassNodes.Queries.GetTreeClassNodes;

public class GetTreeClassNodesQueryHandler : IRequestHandler<GetTreeClassNodesQuery, IReadOnlyList<ClassNodeTreeItem>>
{
    private readonly IClassNodeRepository _classNodeRepository;

    public GetTreeClassNodesQueryHandler(IClassNodeRepository classNodeRepository)
    {
        _classNodeRepository = classNodeRepository;
    }

    public async Task<IReadOnlyList<ClassNodeTreeItem>> Handle(GetTreeClassNodesQuery request, CancellationToken cancellationToken)
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

        // Build lookup for non-root parents to avoid null dictionary keys
        var byParent = nodes
            .Where(n => n.ParentId.HasValue)
            .GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Sequence).ToList());

        List<ClassNodeTreeItem> Build(Guid? parentId)
        {
            List<ClassNode> children;
            if (!parentId.HasValue)
            {
                // Root-level nodes
                children = nodes.Where(n => n.ParentId == null).OrderBy(n => n.Sequence).ToList();
            }
            else
            {
                if (!byParent.TryGetValue(parentId.Value, out children!)) return new List<ClassNodeTreeItem>();
            }

            return children.Select(child => new ClassNodeTreeItem
            {
                Node = child,
                Children = Build(child.Id)
            }).ToList();
        }

        return Build(null);
    }
}
