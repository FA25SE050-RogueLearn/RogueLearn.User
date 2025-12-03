using FluentAssertions;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Tests.Models;

public class RoadmapImportModelsTests
{
    [Fact]
    public void ClassRoadmapData_Can_Add_Nodes()
    {
        var roadmap = new ClassRoadmapData
        {
            Class = new ClassData { Name = "CS", DifficultyLevel = 3, RoadmapUrl = "http://example" }
        };
        roadmap.Nodes.Add(new RoadmapNodeData { Title = "Intro", Sequence = 1, FullPath = "root/intro" });
        roadmap.Nodes.Count.Should().Be(1);
        roadmap.Class.Name.Should().Be("CS");
    }
}