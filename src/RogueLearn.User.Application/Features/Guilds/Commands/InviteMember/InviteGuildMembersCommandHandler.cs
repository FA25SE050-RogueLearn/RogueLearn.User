using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.InviteMember;

public class InviteGuildMembersCommandHandler : IRequestHandler<InviteGuildMembersCommand, InviteGuildMembersResponse>
{
    private readonly IGuildInvitationRepository _invitationRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public InviteGuildMembersCommandHandler(
        IGuildInvitationRepository invitationRepository,
        IUserProfileRepository userProfileRepository)
    {
        _invitationRepository = invitationRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<InviteGuildMembersResponse> Handle(InviteGuildMembersCommand request, CancellationToken cancellationToken)
    {
        var createdIds = new List<Guid>();
        var pending = await _invitationRepository.GetPendingInvitationsByGuildAsync(request.GuildId, cancellationToken);

        foreach (var target in request.Targets)
        {
            Guid? inviteeId = target.UserId;

            if (!inviteeId.HasValue && string.IsNullOrWhiteSpace(target.Email))
            {
                throw new Exceptions.BadRequestException("Invite target must include userId or email.");
            }

            if (!inviteeId.HasValue && !string.IsNullOrWhiteSpace(target.Email))
            {
                var profile = await _userProfileRepository.GetByEmailAsync(target.Email, cancellationToken);
                if (profile != null)
                {
                    inviteeId = profile.AuthUserId;
                }
                else
                {
                    throw new Exceptions.BadRequestException($"No user found with email '{target.Email}'.");
                }
            }

            if (!inviteeId.HasValue)
            {
                throw new Exceptions.BadRequestException("Invalid invite target.");
            }

            if (inviteeId.Value == request.InviterAuthUserId)
            {
                throw new Exceptions.BadRequestException("Cannot invite yourself to the guild.");
            }
            if (pending.Any(i => i.InviteeId == inviteeId))
            {
                throw new Exceptions.BadRequestException("An invitation is already pending for this user.");
            }

            var invitation = new GuildInvitation
            {
                GuildId = request.GuildId,
                InviterId = request.InviterAuthUserId,
                InviteeId = inviteeId.Value,
                InvitationType = InvitationType.Invite,
                Status = InvitationStatus.Pending,
                Message = request.Message,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
            };

            invitation = await _invitationRepository.AddAsync(invitation, cancellationToken);
            createdIds.Add(invitation.Id);
        }
        if (createdIds.Count == 0)
        {
            throw new Exceptions.BadRequestException("No valid invite targets.");
        }
        return new InviteGuildMembersResponse(createdIds);
    }
}