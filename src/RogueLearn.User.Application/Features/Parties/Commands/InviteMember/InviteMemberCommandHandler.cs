using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Newtonsoft.Json;

namespace RogueLearn.User.Application.Features.Parties.Commands.InviteMember;

public class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, Unit>
{
    private readonly IPartyInvitationRepository _invitationRepository;
    private readonly IPartyNotificationService _notificationService;
    private readonly IUserProfileRepository _userProfileRepository;

    public InviteMemberCommandHandler(
        IPartyInvitationRepository invitationRepository,
        IPartyNotificationService notificationService,
        IUserProfileRepository userProfileRepository)
    {
        _invitationRepository = invitationRepository;
        _notificationService = notificationService;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<Unit> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        foreach (var target in request.Targets)
        {
            Guid? inviteeId = target.UserId;

            if (!inviteeId.HasValue && string.IsNullOrWhiteSpace(target.Email))
            {
                throw new Exceptions.BadRequestException("Invite target must include userId or email.");
            }

            if (!inviteeId.HasValue && !string.IsNullOrWhiteSpace(target.Email))
            {
                var userProfile = await _userProfileRepository.GetByEmailAsync(target.Email, cancellationToken);
                if (userProfile != null)
                {
                    inviteeId = userProfile.AuthUserId;
                }
                else
                {
                    throw new Exceptions.BadRequestException($"No user found with email '{target.Email}'.");
                }
            }

            if (inviteeId!.Value == request.InviterAuthUserId)
            {
                throw new Exceptions.BadRequestException("Cannot invite yourself to the party.");
            }

            var existing = await _invitationRepository.GetByPartyAndInviteeAsync(request.PartyId, inviteeId.Value, cancellationToken);
            if (existing is not null)
            {
                if (existing.Status == InvitationStatus.Pending && existing.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    throw new Exceptions.BadRequestException("An invitation is already pending for this user.");
                }

                existing.InviterId = request.InviterAuthUserId;
                existing.Message = SerializeMessage(request.Message, request.JoinLink, request.GameSessionId);
                existing.Status = InvitationStatus.Pending;
                existing.InvitedAt = DateTimeOffset.UtcNow;
                existing.RespondedAt = null;
                existing.ExpiresAt = request.ExpiresAt;

                var updated = await _invitationRepository.UpdateAsync(existing, cancellationToken);
                await _notificationService.SendInvitationNotificationAsync(updated, cancellationToken);
            }
            else
            {
                var invitation = new PartyInvitation
                {
                    PartyId = request.PartyId,
                    InviterId = request.InviterAuthUserId,
                    InviteeId = inviteeId.Value,
                    Message = SerializeMessage(request.Message, request.JoinLink, request.GameSessionId),
                    Status = InvitationStatus.Pending,
                    ExpiresAt = request.ExpiresAt,
                    InvitedAt = DateTimeOffset.UtcNow
                };

                invitation = await _invitationRepository.AddAsync(invitation, cancellationToken);
                await _notificationService.SendInvitationNotificationAsync(invitation, cancellationToken);
            }
        }

        return Unit.Value;
    }

    private static string? SerializeMessage(string? message, string? joinLink, Guid? gameSessionId)
    {
        // If no extra data, keep plain message
        if (string.IsNullOrWhiteSpace(joinLink) && !gameSessionId.HasValue)
        {
            return message;
        }

        var payload = new
        {
            message,
            joinLink,
            gameSessionId
        };

        return JsonConvert.SerializeObject(payload);
    }
}
