// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestLine/GenerateQuestLineCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;

public class GenerateQuestLineCommandHandler : IRequestHandler<GenerateQuestLine, GenerateQuestLineResponse>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly ILogger<GenerateQuestLineCommandHandler> _logger;

    public GenerateQuestLineCommandHandler(
        IUserProfileRepository userProfileRepository,
        ILearningPathRepository learningPathRepository,
        ILogger<GenerateQuestLineCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _learningPathRepository = learningPathRepository;
        _logger = logger;
    }

    public async Task<GenerateQuestLineResponse> Handle(GenerateQuestLine request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating quest line for user {AuthUserId}", request.AuthUserId);

        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        if (userProfile is null) throw new BadRequestException("Invalid User Profile");
        if (userProfile.RouteId is null || userProfile.ClassId is null) throw new BadRequestException("Please select a route and class first.");

        // 1. Get or Create Personal Learning Path (Container)
        var learningPath = await _learningPathRepository.GetLatestByUserAsync(request.AuthUserId, cancellationToken);
        if (learningPath == null)
        {
            learningPath = new LearningPath
            {
                Name = $"{userProfile.Username}'s Path",
                PathType = PathType.Course,
                IsPublished = false,
                CreatedBy = userProfile.AuthUserId
            };
            learningPath = await _learningPathRepository.AddAsync(learningPath, cancellationToken);
        }

        // NOTE: The previous logic that iterated through subjects and pre-created attempts 
        // has been removed. The difficulty calculation now happens "Just-In-Time" 
        // in the StartQuestCommandHandler when the user actually begins a quest.

        return new GenerateQuestLineResponse { LearningPathId = learningPath.Id };
    }
}