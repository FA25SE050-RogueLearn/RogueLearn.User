using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Commands.CreateParty;

public class CreatePartyCommandHandler : IRequestHandler<CreatePartyCommand, CreatePartyResponse>
{
    private readonly IPartyRepository _partyRepository;
    private readonly IPartyMemberRepository _partyMemberRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;

    public CreatePartyCommandHandler(IPartyRepository partyRepository, IPartyMemberRepository partyMemberRepository, IUserRoleRepository userRoleRepository, IRoleRepository roleRepository)
    {
        _partyRepository = partyRepository;
        _partyMemberRepository = partyMemberRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
    }

    public async Task<CreatePartyResponse> Handle(CreatePartyCommand request, CancellationToken cancellationToken)
    {
        var party = new Party
        {
            Name = request.Name,
            Description = string.Empty,
            PartyType = PartyType.StudyGroup,
            MaxMembers = request.MaxMembers,
            IsPublic = request.IsPublic,
            CreatedBy = request.CreatorAuthUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        party = await _partyRepository.AddAsync(party, cancellationToken);

        // Add creator as Leader
        var member = new PartyMember
        {
            PartyId = party.Id,
            AuthUserId = request.CreatorAuthUserId,
            Role = PartyRole.Leader,
            Status = MemberStatus.Active,
            JoinedAt = DateTimeOffset.UtcNow
        };

        await _partyMemberRepository.AddAsync(member, cancellationToken);

        // Assign the global "Party Leader" role to the user who created the party
        var leaderRole = await _roleRepository.GetByNameAsync("Party Leader", cancellationToken);
        if (leaderRole == null)
        {
            // If the role is not configured, surface a clear error so the environment can be corrected
            throw new Exceptions.NotFoundException("Role", "Party Leader");
        }

        // Avoid duplicate assignment if the user already has the role
        var existingUserRoles = await _userRoleRepository.GetRolesForUserAsync(request.CreatorAuthUserId, cancellationToken);
        if (!existingUserRoles.Any(ur => ur.RoleId == leaderRole.Id))
        {
            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                AuthUserId = request.CreatorAuthUserId,
                RoleId = leaderRole.Id,
                AssignedAt = DateTimeOffset.UtcNow,
                AssignedBy = request.CreatorAuthUserId
            };

            await _userRoleRepository.AddAsync(userRole, cancellationToken);
        }

        return new CreatePartyResponse
        {
            PartyId = party.Id,
            RoleGranted = "PartyLeader"
        };
    }
}