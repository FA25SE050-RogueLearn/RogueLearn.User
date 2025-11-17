// RogueLearn.User/src/RogueLearn.User.Application/Features/Specialization/Commands/SetUserSpecialization/SetUserSpecializationCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
// ADDED: We now need to trigger the quest line generation.
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;

namespace RogueLearn.User.Application.Features.Specialization.Commands.SetUserSpecialization;

public class SetUserSpecializationCommandHandler : IRequestHandler<SetUserSpecializationCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IClassRepository _classRepository;
    // REMOVED: The following repositories are no longer needed as this handler's responsibility is simplified.
    // private readonly IClassSpecializationSubjectRepository _specSubjectRepository;
    // private readonly IQuestRepository _questRepository;
    // private readonly IUserQuestProgressRepository _questProgressRepository;
    private readonly ILogger<SetUserSpecializationCommandHandler> _logger;
    // ADDED: The mediator is now required to dispatch the GenerateQuestLine command.
    private readonly IMediator _mediator;

    public SetUserSpecializationCommandHandler(
        IUserProfileRepository userProfileRepository,
        IClassRepository classRepository,
        // REMOVED: Obsolete dependencies.
        // IClassSpecializationSubjectRepository specSubjectRepository,
        // IQuestRepository questRepository,
        // IUserQuestProgressRepository questProgressRepository,
        ILogger<SetUserSpecializationCommandHandler> logger,
        IMediator mediator) // ADDED
    {
        _userProfileRepository = userProfileRepository;
        _classRepository = classRepository;
        // _specSubjectRepository = specSubjectRepository;
        // _questRepository = questRepository;
        // _questProgressRepository = questProgressRepository;
        _logger = logger;
        _mediator = mediator; // ADDED
    }

    public async Task Handle(SetUserSpecializationCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate inputs and retrieve user profile. This remains unchanged.
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), request.AuthUserId);

        var selectedClass = await _classRepository.GetByIdAsync(request.ClassId, cancellationToken)
            ?? throw new BadRequestException($"Invalid ClassId: {request.ClassId}");

        // 2. Update the user's profile with the new specialization (class_id). This also remains.
        userProfile.ClassId = request.ClassId;
        await _userProfileRepository.UpdateAsync(userProfile, cancellationToken);
        _logger.LogInformation("User {AuthUserId} selected specialization ClassId {ClassId}", request.AuthUserId, request.ClassId);

        // 3. ARCHITECTURAL REFACTOR: Instead of manually creating progress records,
        // we now trigger the authoritative command responsible for building the user's entire quest structure.
        // This command will automatically reconcile the learning path by adding quests for the new
        // specialization and archiving quests from the old one.
        _logger.LogInformation("Triggering learning path reconciliation for user {AuthUserId} due to specialization change.", request.AuthUserId);
        await _mediator.Send(new GenerateQuestLine { AuthUserId = request.AuthUserId }, cancellationToken);
        _logger.LogInformation("Learning path reconciliation complete for user {AuthUserId}.", request.AuthUserId);
    }
}