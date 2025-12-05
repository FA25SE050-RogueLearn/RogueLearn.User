// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Queries/GetQuestById/GetQuestByIdQueryHandler.cs
using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById; // Fix namespace resolution
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;

public class GetQuestByIdQueryHandler : IRequestHandler<GetQuestByIdQuery, QuestDetailsDto>
{
    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly IUserQuestAttemptRepository _userQuestAttemptRepository;
    private readonly IMapper _mapper;

    public GetQuestByIdQueryHandler(
        IQuestRepository questRepository,
        IQuestStepRepository questStepRepository,
        IUserQuestAttemptRepository userQuestAttemptRepository,
        IMapper mapper)
    {
        _questRepository = questRepository;
        _questStepRepository = questStepRepository;
        _userQuestAttemptRepository = userQuestAttemptRepository;
        _mapper = mapper;
    }

    public async Task<QuestDetailsDto> Handle(GetQuestByIdQuery request, CancellationToken cancellationToken)
    {
        var quest = await _questRepository.GetByIdAsync(request.Id, cancellationToken);
        if (quest == null)
        {
            // MediatR pipeline might expect null or exception. Returning null allows Controller to 404.
            return null!;
        }

        // Default difficulty strategy
        string targetDifficulty = "Standard";

        // 1. Determine the difficulty filter based on user context
        if (request.AuthUserId != Guid.Empty)
        {
            // Check if the user has an existing attempt (Active or Completed)
            // The attempt holds the "AssignedDifficulty" which locks the user to a specific track
            var attempt = await _userQuestAttemptRepository.FirstOrDefaultAsync(
                x => x.QuestId == request.Id && x.AuthUserId == request.AuthUserId,
                cancellationToken);

            if (attempt != null && !string.IsNullOrEmpty(attempt.AssignedDifficulty))
            {
                targetDifficulty = attempt.AssignedDifficulty;
            }
            else if (!string.IsNullOrEmpty(quest.ExpectedDifficulty))
            {
                // If no attempt yet, preview the difficulty tailored to their academic history
                // This 'ExpectedDifficulty' is calculated during GenerateQuestLine
                targetDifficulty = quest.ExpectedDifficulty;
            }
        }

        // 2. Fetch all steps for this Master Quest
        var allSteps = await _questStepRepository.GetByQuestIdAsync(request.Id, cancellationToken);

        // 3. Filter steps to show ONLY the track matching the user's difficulty
        // The Master Quest contains steps for all 3 variants (Standard, Supportive, Challenging)
        // We case-insensitive match just to be safe
        var filteredSteps = allSteps
            .Where(s => string.Equals(s.DifficultyVariant, targetDifficulty, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.StepNumber)
            .ToList();

        // 4. Map to DTO
        var questDto = _mapper.Map<QuestDetailsDto>(quest);
        questDto.Steps = _mapper.Map<List<QuestStepDto>>(filteredSteps);

        return questDto;
    }
}