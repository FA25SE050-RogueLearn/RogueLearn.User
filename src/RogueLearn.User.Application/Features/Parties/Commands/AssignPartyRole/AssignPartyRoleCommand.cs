using MediatR;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Parties.Commands.ManageRoles;

public record AssignPartyRoleCommand(Guid PartyId, Guid MemberAuthUserId, PartyRole RoleToAssign, Guid ActorAuthUserId, bool IsAdminOverride = false) : IRequest<Unit>;