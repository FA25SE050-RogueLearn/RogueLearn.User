// RogueLearn.User/src/RogueLearn.User.Application/Features/Onboarding/Commands/CompleteOnboarding/CompleteOnboardingCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Onboarding.Commands.CompleteOnboarding;

public class CompleteOnboardingCommandHandler : IRequestHandler<CompleteOnboardingCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly IClassRepository _classRepository;
    private readonly ILogger<CompleteOnboardingCommandHandler> _logger;

    public CompleteOnboardingCommandHandler(
        IUserProfileRepository userProfileRepository,
        ICurriculumVersionRepository curriculumVersionRepository,
        IClassRepository classRepository,
        ILogger<CompleteOnboardingCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _curriculumVersionRepository = curriculumVersionRepository;
        _classRepository = classRepository;
        _logger = logger;
    }

    public async Task Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        if (userProfile == null)
        {
            throw new NotFoundException("UserProfile", request.AuthUserId);
        }

        if (userProfile.OnboardingCompleted)
        {
            throw new BadRequestException("User has already completed onboarding.");
        }

        // Validate the provided IDs
        var curriculumVersion = await _curriculumVersionRepository.GetByIdAsync(request.CurriculumVersionId, cancellationToken);
        if (curriculumVersion == null)
        {
            throw new BadRequestException($"Invalid CurriculumVersionId: {request.CurriculumVersionId}");
        }

        if (!await _classRepository.ExistsAsync(request.CareerRoadmapId, cancellationToken))
        {
            throw new BadRequestException($"Invalid CareerRoadmapId: {request.CareerRoadmapId}");
        }

        // Update the user's profile with their choices
        userProfile.RouteId = curriculumVersion.ProgramId; // Store the program ID for the chosen route
        userProfile.ClassId = request.CareerRoadmapId;
        userProfile.OnboardingCompleted = true;
        userProfile.UpdatedAt = DateTimeOffset.UtcNow;

        await _userProfileRepository.UpdateAsync(userProfile, cancellationToken);

        _logger.LogInformation("User {AuthUserId} completed onboarding with RouteId {RouteId} and ClassId {ClassId}",
            request.AuthUserId, userProfile.RouteId, userProfile.ClassId);

        // In a future step, this handler would publish a "UserOnboardingCompleted" event
        // to trigger the QuestMicroservice to start quest generation.
    }
}