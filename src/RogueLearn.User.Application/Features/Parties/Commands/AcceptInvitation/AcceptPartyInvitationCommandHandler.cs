using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Parties.Commands.AcceptInvitation;

public class AcceptPartyInvitationCommandHandler : IRequestHandler<AcceptPartyInvitationCommand, Unit>
{
    private readonly IPartyInvitationRepository _invitationRepository;
    private readonly IPartyMemberRepository _memberRepository;
    private readonly IPartyNotificationService? _notificationService;

    public AcceptPartyInvitationCommandHandler(IPartyInvitationRepository invitationRepository, IPartyMemberRepository memberRepository, IPartyNotificationService notificationService)
    {
        _invitationRepository = invitationRepository;
        _memberRepository = memberRepository;
        _notificationService = notificationService;
    }

    public async Task<Unit> Handle(AcceptPartyInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("PartyInvitation", request.InvitationId.ToString());

        if (invitation.PartyId != request.PartyId)
        {
            throw new Exceptions.BadRequestException("Invitation does not belong to target party.");
        }

        if (invitation.Status != InvitationStatus.Pending || invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new Exceptions.BadRequestException("Invitation is not valid.");
        }

        if (invitation.InviteeId != request.AuthUserId)
        {
            throw new Exceptions.ForbiddenException("Invitation not intended for this user.");
        }

        // Game invite shortcut: if the message payload contains joinLink/gameSessionId,
        // simply mark as accepted without membership mutation to avoid duplicate-member errors.
        if (IsGameInvite(invitation.Message))
        {
            invitation.Status = InvitationStatus.Accepted;
            invitation.RespondedAt = DateTimeOffset.UtcNow;
            await _invitationRepository.UpdateAsync(invitation, cancellationToken);
            if (_notificationService != null)
            {
                await _notificationService.SendInvitationAcceptedNotificationAsync(invitation, cancellationToken);
            }
            return Unit.Value;
        }

        var existingMember = await _memberRepository.GetMemberAsync(request.PartyId, request.AuthUserId, cancellationToken);
        if (existingMember != null && existingMember.Status == MemberStatus.Active)
        {
            throw new Exceptions.BadRequestException("User is already a member of this party.");
        }

        if (existingMember != null)
        {
            existingMember.Status = MemberStatus.Active;
            existingMember.JoinedAt = DateTimeOffset.UtcNow;
            await _memberRepository.UpdateAsync(existingMember, cancellationToken);
        }
        else
        {
            var member = new PartyMember
            {
                PartyId = request.PartyId,
                AuthUserId = request.AuthUserId,
                Role = PartyRole.Member,
                Status = MemberStatus.Active,
                JoinedAt = DateTimeOffset.UtcNow
            };
            await _memberRepository.AddAsync(member, cancellationToken);
        }

        invitation.Status = InvitationStatus.Accepted;
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
        if (_notificationService != null)
        {
            await _notificationService.SendInvitationAcceptedNotificationAsync(invitation, cancellationToken);
        }

        return Unit.Value;
    }

    private static bool IsGameInvite(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            var hasJoinLink = root.TryGetProperty("joinLink", out var jl) && jl.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(jl.GetString());
            var hasSession = root.TryGetProperty("gameSessionId", out var gs) && gs.ValueKind == JsonValueKind.String && Guid.TryParse(gs.GetString(), out _);
            return hasJoinLink || hasSession;
        }
        catch
        {
            return false;
        }
    }
}
