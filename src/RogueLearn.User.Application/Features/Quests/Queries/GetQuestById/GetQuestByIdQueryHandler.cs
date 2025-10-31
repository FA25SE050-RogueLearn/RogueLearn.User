// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Queries/GetQuestById/GetQuestByIdQueryHandler.cs
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;

public class GetQuestByIdQueryHandler : IRequestHandler<GetQuestByIdQuery, QuestDetailsDto?>
{
    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetQuestByIdQueryHandler> _logger;

    public GetQuestByIdQueryHandler(
        IQuestRepository questRepository,
        IQuestStepRepository questStepRepository,
        IMapper mapper,
        ILogger<GetQuestByIdQueryHandler> logger)
    {
        _questRepository = questRepository;
        _questStepRepository = questStepRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<QuestDetailsDto?> Handle(GetQuestByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching quest details for QuestId {QuestId}", request.Id);
        var quest = await _questRepository.GetByIdAsync(request.Id, cancellationToken);
        if (quest is null)
        {
            return null;
        }

        var questSteps = await _questStepRepository.FindAsync(s => s.QuestId == request.Id, cancellationToken);

        var dto = _mapper.Map<QuestDetailsDto>(quest);
        dto.Steps = _mapper.Map<List<QuestStepDto>>(questSteps.OrderBy(s => s.StepNumber));

        return dto;
    }
}