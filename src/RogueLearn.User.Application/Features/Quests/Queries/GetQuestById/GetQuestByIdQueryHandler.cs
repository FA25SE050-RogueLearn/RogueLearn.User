using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Services;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;

public class GetQuestByIdQueryHandler : IRequestHandler<GetQuestByIdQuery, QuestDetailsDto>
{
    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly IUserQuestAttemptRepository _userQuestAttemptRepository;
    private readonly IStudentSemesterSubjectRepository _studentSubjectRepository;
    private readonly IQuestDifficultyResolver _difficultyResolver;
    private readonly IMapper _mapper;

    public GetQuestByIdQueryHandler(
        IQuestRepository questRepository,
        IQuestStepRepository questStepRepository,
        IUserQuestAttemptRepository userQuestAttemptRepository,
        IStudentSemesterSubjectRepository studentSubjectRepository,
        IQuestDifficultyResolver difficultyResolver,
        IMapper mapper)
    {
        _questRepository = questRepository;
        _questStepRepository = questStepRepository;
        _userQuestAttemptRepository = userQuestAttemptRepository;
        _studentSubjectRepository = studentSubjectRepository;
        _difficultyResolver = difficultyResolver;
        _mapper = mapper;
    }

    public async Task<QuestDetailsDto> Handle(GetQuestByIdQuery request, CancellationToken cancellationToken)
    {
        var quest = await _questRepository.GetByIdAsync(request.Id, cancellationToken);
        if (quest == null)
        {
            return null!;
        }

        // Determine which difficulty track to show
        string targetDifficulty = "Standard";

        // 1. First, check if the user has an ACTIVE attempt with a locked difficulty
        if (request.AuthUserId != Guid.Empty)
        {
            var attempt = await _userQuestAttemptRepository.FirstOrDefaultAsync(
                a => a.AuthUserId == request.AuthUserId && a.QuestId == request.Id,
                cancellationToken);

            if (attempt != null && !string.IsNullOrEmpty(attempt.AssignedDifficulty))
            {
                // PREFERRED: Use the snapshot from the attempt
                targetDifficulty = attempt.AssignedDifficulty;
            }
            else if (quest.SubjectId.HasValue)
            {
                // FALLBACK: If no attempt (or legacy record), calculate PREVIEW based on current grades
                var gradeRecords = await _studentSubjectRepository.GetSemesterSubjectsByUserAsync(request.AuthUserId, cancellationToken);
                var subjectRecord = gradeRecords.FirstOrDefault(s => s.SubjectId == quest.SubjectId.Value);

                var difficultyInfo = _difficultyResolver.ResolveDifficulty(subjectRecord);
                targetDifficulty = difficultyInfo.ExpectedDifficulty;
            }
        }
        else
        {
            // Anonymous or fallback
            targetDifficulty = "Standard";
        }

        // 2. Fetch all steps for this Master Quest
        var allSteps = await _questStepRepository.GetByQuestIdAsync(request.Id, cancellationToken);

        // 3. Filter steps to show ONLY the track matching the target difficulty
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