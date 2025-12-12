using System.Text.Json;
using FluentAssertions;
using Microsoft.SemanticKernel;
using NSubstitute;
using RogueLearn.User.Application.Features.GameSessions.Commands.GenerateQuestionPack;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.GameSessions;

public class GenerateQuestionPackCommandHandlerTests
{
  private static Kernel CreateKernel() => Kernel.CreateBuilder().Build();

  [Fact]
  public async Task Falls_back_to_quest_steps_when_no_syllabus()
  {
    // Arrange
    var kernel = CreateKernel();
    var subjectRepo = Substitute.For<ISubjectRepository>();
    subjectRepo.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns((Subject?)null);

    var questStepRepo = Substitute.For<IQuestStepRepository>();
    questStepRepo.GetPagedAsync(1, 25, Arg.Any<CancellationToken>())
        .Returns(new List<QuestStep>
        {
                new()
                {
                    CreatedAt = DateTimeOffset.UtcNow,
                    Content = """
                    {
                      "activities":[
                        {
                          "type":"quiz",
                          "payload":{
                            "questions":[
                              {"prompt":"p1","options":["a","b"],"answerIndex":1,"topic":"t1"}
                            ]
                          }
                        }
                      ]
                    }
                    """
                }
        });

    var handler = new GenerateQuestionPackCommandHandler(kernel, subjectRepo, questStepRepo);
    var sessionId = Guid.NewGuid();

    // Act
    var result = await handler.Handle(new GenerateQuestionPackCommand(sessionId, null, null, null, 3), CancellationToken.None);

    // Assert
    result.PackId.Should().NotBeNullOrWhiteSpace();
    result.QuestionPackJson.Should().NotBeNullOrWhiteSpace();

    using var doc = JsonDocument.Parse(result.QuestionPackJson);
    doc.RootElement.TryGetProperty("questions", out var questions).Should().BeTrue();
    questions.GetArrayLength().Should().Be(3);
  }

  [Fact]
  public async Task Clamps_count_and_sets_pack_id_when_missing()
  {
    // Arrange
    var kernel = CreateKernel();
    var subjectRepo = Substitute.For<ISubjectRepository>();
    subjectRepo.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns((Subject?)null);

    var questStepRepo = Substitute.For<IQuestStepRepository>();
    questStepRepo.GetPagedAsync(1, 25, Arg.Any<CancellationToken>())
        .Returns(new List<QuestStep>()); // forces synthetic fallback

    var handler = new GenerateQuestionPackCommandHandler(kernel, subjectRepo, questStepRepo);
    var sessionId = Guid.NewGuid();

    // Act
    var result = await handler.Handle(new GenerateQuestionPackCommand(sessionId, "math", "algebra", "hard", 50), CancellationToken.None);

    // Assert
    result.PackId.Should().Be($"pack_{sessionId}");
    using var doc = JsonDocument.Parse(result.QuestionPackJson);
    var questions = doc.RootElement.GetProperty("questions");
    questions.GetArrayLength().Should().BeLessThanOrEqualTo(20);
  }
}
