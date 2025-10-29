// src/RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateMyQuestLine/GenerateMyQuestLineCommand.cs
using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateMyQuestLine;

public class GenerateMyQuestLineCommand : IRequest<GenerateMyQuestLineResponse>
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }

    public string RawCurriculumText { get; set; } = string.Empty;
}