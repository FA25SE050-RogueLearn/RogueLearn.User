// RogueLearn.User/src/RogueLearn.Quests/RogueLearn.Quests.Application/Features/QuestLines/Commands/GenerateFromCurriculumCommandHandler.cs
using MediatR;
using RogueLearn.Quests.Application.Interfaces;
using RogueLearn.Quests.Domain.Entities;
using RogueLearn.Quests.Domain.Interfaces;

namespace RogueLearn.Quests.Application.Features.QuestLines.Commands;

public class GenerateFromCurriculumCommandHandler : IRequestHandler<GenerateFromCurriculumCommand, Guid>
{
	private readonly IUserServiceClient _userServiceClient;
	private readonly IQuestRepository _questRepository;
	// You will also need an ILearningPathRepository here once that entity is created.

	public GenerateFromCurriculumCommandHandler(
		IUserServiceClient userServiceClient,
		IQuestRepository questRepository)
	{
		_userServiceClient = userServiceClient;
		_questRepository = questRepository;
	}

	public async Task<Guid> Handle(GenerateFromCurriculumCommand request, CancellationToken cancellationToken)
	{
		// 1. Fetch curriculum data from UserService
		var curriculumStructure = await _userServiceClient.GetCurriculumStructureAsync(request.CurriculumVersionId);

		// 2. Create a parent LearningPath (QuestLine)
		// For now, we will just create the quests. A LearningPath entity will be added later.
		var learningPathId = Guid.NewGuid(); // Placeholder

		// 3. Generate a Quest for each subject in the curriculum
		foreach (var subject in curriculumStructure.OrderBy(s => s.TermNumber))
		{
			var newQuest = new Quest
			{
				Title = subject.SubjectName,
				Description = $"Complete the requirements for {subject.SubjectName}.",
				QuestType = QuestType.Project, // Default type
				DifficultyLevel = DifficultyLevel.Beginner, // Default difficulty
				SubjectId = subject.SubjectId,
				CreatedBy = request.UserId,
				// We'll need to link this to the LearningPath later
			};

			await _questRepository.AddAsync(newQuest, cancellationToken);
		}

		// 4. Return the ID of the new QuestLine/LearningPath
		return learningPathId;
	}
}