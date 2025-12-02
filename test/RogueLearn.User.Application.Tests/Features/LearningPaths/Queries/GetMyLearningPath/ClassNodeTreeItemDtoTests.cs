using FluentAssertions;
using RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Tests.Features.LearningPaths.Queries.GetMyLearningPath;

public class ClassNodeTreeItemDtoTests
{
    [Fact]
    public void FromModel_MapsAllFields()
    {
        var node = new ClassNode
        {
            Id = Guid.NewGuid(),
            ClassId = Guid.NewGuid(),
            ParentId = Guid.NewGuid(),
            Title = "Node",
            NodeType = "type",
            Description = "desc",
            Sequence = 3,
            IsActive = true,
            IsLockedByImport = false,
            Metadata = new Dictionary<string, object> { ["k"] = "v" },
            CreatedAt = DateTimeOffset.UtcNow
        };

        var dto = ClassNodeTreeItemDto.FromModel(node);
        dto.Id.Should().Be(node.Id);
        dto.ClassId.Should().Be(node.ClassId);
        dto.ParentId.Should().Be(node.ParentId);
        dto.Title.Should().Be("Node");
        dto.NodeType.Should().Be("type");
        dto.Description.Should().Be("desc");
        dto.Sequence.Should().Be(3);
        dto.IsActive.Should().BeTrue();
        dto.IsLockedByImport.Should().BeFalse();
        dto.Metadata!["k"].Should().Be("v");
        dto.CreatedAt.Should().BeCloseTo(node.CreatedAt, TimeSpan.FromSeconds(1));
        dto.Children.Should().NotBeNull();
        dto.Children.Should().BeEmpty();
    }
}