// RogueLearn.User/src/RogueLearn.User.Application/Features/Onboarding/Commands/CompleteOnboarding/CompleteOnboardingCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Onboarding.Commands.CompleteOnboarding;

public class CompleteOnboardingCommandHandler : IRequestHandler<CompleteOnboardingCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly IClassRepository _classRepository;
    private readonly ILogger<CompleteOnboardingCommandHandler> _logger;

    public CompleteOnboardingCommandHandler(
        IUserProfileRepository userProfileRepository,
        ICurriculumProgramRepository curriculumProgramRepository,
        IClassRepository classRepository,
        ILogger<CompleteOnboardingCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _curriculumProgramRepository = curriculumProgramRepository;
        _classRepository = classRepository;
        _logger = logger;
    }

    public async Task Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), request.AuthUserId);

        if (userProfile.OnboardingCompleted)
        {
            throw new BadRequestException("User has already completed onboarding.");
        }

        // 1. Validate the provided Program ID exists.
        if (!await _curriculumProgramRepository.ExistsAsync(request.CurriculumProgramId, cancellationToken))
        {
            throw new BadRequestException($"Invalid CurriculumProgramId: {request.CurriculumProgramId}");
        }

        // 2. Validate the Career Roadmap (Class) ID exists.
        if (!await _classRepository.ExistsAsync(request.CareerRoadmapId, cancellationToken))
        {
            throw new BadRequestException($"Invalid CareerRoadmapId: {request.CareerRoadmapId}");
        }

        // 3. Update the user's profile with their final choices.
        // This handler no longer creates an enrollment record. That is now the responsibility
        // of the ProcessAcademicRecord command handler upon first transcript sync.
        userProfile.RouteId = request.CurriculumProgramId;
        userProfile.ClassId = request.CareerRoadmapId;
        userProfile.OnboardingCompleted = true;
        userProfile.UpdatedAt = DateTimeOffset.UtcNow;

        await _userProfileRepository.UpdateAsync(userProfile, cancellationToken);

        _logger.LogInformation("User {AuthUserId} completed onboarding with RouteId {RouteId} and ClassId {ClassId}",
            request.AuthUserId, userProfile.RouteId, userProfile.ClassId);
    }
}