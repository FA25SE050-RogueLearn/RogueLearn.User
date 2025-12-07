using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.JoinPublicParty;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.JoinPublicParty;

public class JoinPublicPartyCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotPublic_ThrowsBadRequest()
    {
        var partyId = System.Guid.NewGuid();
        var authId = System.Guid.NewGuid();
        var cmd = new JoinPublicPartyCommand(partyId, authId);
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new JoinPublicPartyCommandHandler(partyRepo, memberRepo);

        partyRepo.GetByIdAsync(partyId, Arg.Any<CancellationToken>()).Returns(new Party { Id = partyId, IsPublic = false, MaxMembers = 3 });

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NewMember_Succeeds()
    {
        var partyId = System.Guid.NewGuid();
        var authId = System.Guid.NewGuid();
        var cmd = new JoinPublicPartyCommand(partyId, authId);
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var sut = new JoinPublicPartyCommandHandler(partyRepo, memberRepo, invRepo);

        partyRepo.GetByIdAsync(partyId, Arg.Any<CancellationToken>()).Returns(new Party { Id = partyId, IsPublic = true, MaxMembers = 3 });
        memberRepo.CountActiveMembersAsync(partyId, Arg.Any<CancellationToken>()).Returns(1);
        memberRepo.GetMemberAsync(partyId, authId, Arg.Any<CancellationToken>()).Returns((PartyMember?)null);
        invRepo.GetPendingInvitationsByInviteeAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<PartyInvitation>());

        await sut.Handle(cmd, CancellationToken.None);
        await memberRepo.Received(1).AddAsync(Arg.Is<PartyMember>(m => m.PartyId == partyId && m.AuthUserId == authId && m.Status == MemberStatus.Active), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PartyNotFound_ThrowsBadRequest()
    {
        var partyId = System.Guid.NewGuid();
        var authId = System.Guid.NewGuid();
        var cmd = new JoinPublicPartyCommand(partyId, authId);
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new JoinPublicPartyCommandHandler(partyRepo, memberRepo);

        partyRepo.GetByIdAsync(partyId, Arg.Any<CancellationToken>()).Returns((Party?)null);

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingActiveMember_Throws()
    {
        var partyId = System.Guid.NewGuid();
        var authId = System.Guid.NewGuid();
        var cmd = new JoinPublicPartyCommand(partyId, authId);
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new JoinPublicPartyCommandHandler(partyRepo, memberRepo);

        partyRepo.GetByIdAsync(partyId, Arg.Any<CancellationToken>()).Returns(new Party { Id = partyId, IsPublic = true, MaxMembers = 3 });
        memberRepo.CountActiveMembersAsync(partyId, Arg.Any<CancellationToken>()).Returns(1);
        memberRepo.GetMemberAsync(partyId, authId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = partyId, AuthUserId = authId, Status = MemberStatus.Active });

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingInactiveMember_ActivatesAndDeclinesInvites()
    {
        var partyId = System.Guid.NewGuid();
        var authId = System.Guid.NewGuid();
        var cmd = new JoinPublicPartyCommand(partyId, authId);
        var partyRepo = Substitute.For<IPartyRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var invRepo = Substitute.For<IPartyInvitationRepository>();
        var sut = new JoinPublicPartyCommandHandler(partyRepo, memberRepo, invRepo);

        partyRepo.GetByIdAsync(partyId, Arg.Any<CancellationToken>()).Returns(new Party { Id = partyId, IsPublic = true, MaxMembers = 3 });
        memberRepo.CountActiveMembersAsync(partyId, Arg.Any<CancellationToken>()).Returns(1);
        var existing = new PartyMember { PartyId = partyId, AuthUserId = authId, Status = MemberStatus.Left };
        memberRepo.GetMemberAsync(partyId, authId, Arg.Any<CancellationToken>()).Returns(existing);
        invRepo.GetPendingInvitationsByInviteeAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<PartyInvitation> { new() { PartyId = partyId, Status = InvitationStatus.Pending } });

        await sut.Handle(cmd, CancellationToken.None);
        await memberRepo.Received(1).UpdateAsync(Arg.Is<PartyMember>(m => m.PartyId == partyId && m.AuthUserId == authId && m.Status == MemberStatus.Active), Arg.Any<CancellationToken>());
        await invRepo.Received(1).UpdateRangeAsync(Arg.Is<List<PartyInvitation>>(l => l.All(i => i.Status == InvitationStatus.Declined && i.RespondedAt.HasValue)), Arg.Any<CancellationToken>());
    }
}
