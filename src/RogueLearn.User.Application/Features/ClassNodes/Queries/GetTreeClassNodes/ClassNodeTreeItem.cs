using System.Collections.Generic;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.ClassNodes.Queries.GetTreeClassNodes;

public class ClassNodeTreeItem
{
    public ClassNode Node { get; set; } = default!;
    public List<ClassNodeTreeItem> Children { get; set; } = new();
}