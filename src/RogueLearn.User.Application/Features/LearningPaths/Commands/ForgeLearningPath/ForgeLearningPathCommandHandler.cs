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
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;

    public ForgeLearningPathCommandHandler(
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository chapterRepository,
        IUserProfileRepository userProfileRepository,
        ILogger<ForgeLearningPathCommandHandler> logger,
        ICurriculumVersionRepository curriculumVersionRepository)
    {
        _learningPathRepository = learningPathRepository;
        _chapterRepository = chapterRepository;
        _userProfileRepository = userProfileRepository;
        _logger = logger;
        _curriculumVersionRepository = curriculumVersionRepository;
    }

    public async Task<ForgedLearningPath> Handle(ForgeLearningPathCommand request, CancellationToken cancellationToken)
    {
        var user = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        if (user?.RouteId is null)
        {
            throw new BadRequestException("User has no assigned curriculum route (ProgramId). Onboarding must be completed first.");
        }

        var versions = await _curriculumVersionRepository.FindAsync(
            v => v.ProgramId == user.RouteId.Value && v.IsActive == true,
            cancellationToken);

        var latestVersion = versions.OrderByDescending(v => v.EffectiveYear).ThenByDescending(v => v.CreatedAt).FirstOrDefault();
        if (latestVersion == null)
        {
            _logger.LogError("No active curriculum version found for ProgramId {ProgramId} assigned to user {AuthUserId}", user.RouteId.Value, request.AuthUserId);
            throw new BadRequestException($"No active curriculum version could be found for the user's assigned academic program.");
        }

        // MODIFIED: 1. Add a pre-condition check for an existing learning path.
        var existingPath = await _learningPathRepository.FirstOrDefaultAsync(
            lp => lp.CreatedBy == request.AuthUserId && lp.CurriculumVersionId == latestVersion.Id,
            cancellationToken);

        // MODIFIED: 2. If a path already exists, throw a ConflictException.
        if (existingPath != null)
        {
            _logger.LogWarning("Attempted to forge a duplicate learning path for AuthUserId {AuthUserId} and CurriculumVersionId {CurriculumVersionId}",
                request.AuthUserId, latestVersion.Id);
            throw new ConflictException("A learning path for this user and curriculum already exists.");
        }

        var learningPath = new LearningPath
        {
            Name = $"Main Quest for {user.Username}",
            Description = "Your personalized learning journey forged from your academic record and career goals.",
            PathType = PathType.Course,
            CurriculumVersionId = latestVersion.Id,
            IsPublished = true,
            CreatedBy = request.AuthUserId
        };

        var createdPath = await _learningPathRepository.AddAsync(learningPath, cancellationToken);
        _logger.LogInformation("Forged new LearningPath {LearningPathId} for user {AuthUserId} using CurriculumVersion {CurriculumVersionId}",
            createdPath.Id, request.AuthUserId, latestVersion.Id);

        // The rest of the logic to create chapters and quests would follow here.
        // For now, we are just creating the learning path itself.

        return new ForgedLearningPath
        {
            Id = createdPath.Id,
            Name = createdPath.Name,
            Description = createdPath.Description
        };
    }
}