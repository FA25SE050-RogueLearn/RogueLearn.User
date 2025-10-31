// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Commands/ForgeLearningPath/ForgeLearningPathCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LearningPaths.Commands.ForgeLearningPath;

public class ForgeLearningPathCommandHandler : IRequestHandler<ForgeLearningPathCommand, ForgedLearningPath>
{
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestChapterRepository _chapterRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILogger<ForgeLearningPathCommandHandler> _logger;

    public ForgeLearningPathCommandHandler(
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository chapterRepository,
        IUserProfileRepository userProfileRepository,
        ILogger<ForgeLearningPathCommandHandler> logger)
    {
        _learningPathRepository = learningPathRepository;
        _chapterRepository = chapterRepository;
        _userProfileRepository = userProfileRepository;
        _logger = logger;
    }

    public async Task<ForgedLearningPath> Handle(ForgeLearningPathCommand request, CancellationToken cancellationToken)
    {
        var user = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        if (user?.RouteId is null)
        {
            throw new BadRequestException("User has no assigned curriculum route.");
        }

        // For simplicity, we create one learning path per user route.
        // This could be made more complex later.
        var learningPath = new LearningPath
        {
            Name = $"Main Quest for {user.Username}",
            Description = "Your personalized learning journey forged from your academic record and career goals.",
            PathType = PathType.Course,
            CurriculumVersionId = user.RouteId, // Assuming RouteId is the CurriculumVersionId
            IsPublished = true,
            CreatedBy = request.AuthUserId
        };

        var createdPath = await _learningPathRepository.AddAsync(learningPath, cancellationToken);
        _logger.LogInformation("Forged new LearningPath {LearningPathId} for user {AuthUserId}", createdPath.Id, request.AuthUserId);

        // Here you would create the QuestChapter shells based on the curriculum structure.
        // For now, we'll assume a simple structure or leave it for the quest generation step.

        return new ForgedLearningPath
        {
            Id = createdPath.Id,
            Name = createdPath.Name,
            Description = createdPath.Description
        };
    }
}