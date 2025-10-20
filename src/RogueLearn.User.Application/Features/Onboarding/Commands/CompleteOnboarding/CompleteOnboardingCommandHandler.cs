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
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly IClassRepository _classRepository;
    private readonly IStudentEnrollmentRepository _studentEnrollmentRepository;
    private readonly ILogger<CompleteOnboardingCommandHandler> _logger;

    public CompleteOnboardingCommandHandler(
        IUserProfileRepository userProfileRepository,
        ICurriculumProgramRepository curriculumProgramRepository,
        ICurriculumVersionRepository curriculumVersionRepository,
        IClassRepository classRepository,
        IStudentEnrollmentRepository studentEnrollmentRepository,
        ILogger<CompleteOnboardingCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _curriculumProgramRepository = curriculumProgramRepository;
        _curriculumVersionRepository = curriculumVersionRepository;
        _classRepository = classRepository;
        _studentEnrollmentRepository = studentEnrollmentRepository;
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

        // --- MODIFIED LOGIC START ---

        // 1. Validate the provided Program ID.
        var curriculumProgram = await _curriculumProgramRepository.GetByIdAsync(request.CurriculumProgramId, cancellationToken);
        if (curriculumProgram == null)
        {
            throw new BadRequestException($"Invalid CurriculumProgramId: {request.CurriculumProgramId}");
        }

        // 2. Find the latest active version for the selected program.
        // MODIFIED: Changed `v.IsActive` to `v.IsActive == true` for explicit comparison.
        // This resolves the NullReferenceException in the Supabase LINQ provider.
        var versions = await _curriculumVersionRepository.FindAsync(
            v => v.ProgramId == request.CurriculumProgramId && v.IsActive == true,
            cancellationToken);

        var latestVersion = versions.OrderByDescending(v => v.EffectiveYear).ThenByDescending(v => v.CreatedAt).FirstOrDefault();
        if (latestVersion == null)
        {
            throw new BadRequestException($"No active curriculum version found for program: {curriculumProgram.ProgramName}");
        }

        // 3. Validate the Career Roadmap ID.
        if (!await _classRepository.ExistsAsync(request.CareerRoadmapId, cancellationToken))
        {
            throw new BadRequestException($"Invalid CareerRoadmapId: {request.CareerRoadmapId}");
        }

        // 4. Create the student enrollment record using the found version ID.
        var enrollment = new StudentEnrollment
        {
            AuthUserId = request.AuthUserId,
            CurriculumVersionId = latestVersion.Id,
            EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = Domain.Enums.EnrollmentStatus.Active
        };
        await _studentEnrollmentRepository.AddAsync(enrollment, cancellationToken);

        // 5. Update the user's profile with their final choices.
        userProfile.RouteId = curriculumProgram.Id;
        userProfile.ClassId = request.CareerRoadmapId;
        userProfile.OnboardingCompleted = true;
        userProfile.UpdatedAt = DateTimeOffset.UtcNow;

        await _userProfileRepository.UpdateAsync(userProfile, cancellationToken);

        _logger.LogInformation("User {AuthUserId} completed onboarding with RouteId {RouteId} (VersionId: {VersionId}) and ClassId {ClassId}",
            request.AuthUserId, userProfile.RouteId, latestVersion.Id, userProfile.ClassId);

        // --- MODIFIED LOGIC END ---
    }
}