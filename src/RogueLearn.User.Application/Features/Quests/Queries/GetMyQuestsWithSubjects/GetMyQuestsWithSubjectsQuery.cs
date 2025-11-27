namespace RogueLearn.User.Application.Features.Quests.Queries.GetMyQuestsWithSubjects;

using MediatR;

public class GetMyQuestsWithSubjectsQuery : IRequest<List<MyQuestWithSubjectDto>>
{
    public Guid AuthUserId { get; set; }
}

