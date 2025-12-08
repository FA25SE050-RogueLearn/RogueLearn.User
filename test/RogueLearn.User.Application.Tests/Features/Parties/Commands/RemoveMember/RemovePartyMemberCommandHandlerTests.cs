using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.RemoveMember;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.RemoveMember;

public class RemovePartyMemberCommandHandlerTests
{
    [Fact]
    public async Task Handle_Leader_ThrowsBadRequest()
    {
        var repo = Substitute.For<IPartyMemberRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var sut = new RemovePartyMemberCommandHandler(repo, notify);

        var partyId = System.Guid.NewGuid();
        var memberId = System.Guid.NewGuid();
        repo.GetByIdAsync(memberId, Arg.Any<CancellationToken>()).Returns(new PartyMember { Id = memberId, PartyId = partyId, Role = PartyRole.Leader });

        var cmd = new RemovePartyMemberCommand(partyId, memberId, "cleanup");
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_DeletesAndNotifies()
    {
        var repo = Substitute.For<IPartyMemberRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var sut = new RemovePartyMemberCommandHandler(repo, notify);

        var partyId = System.Guid.NewGuid();
        var memberId = System.Guid.NewGuid();
        var authUserId = System.Guid.NewGuid();
        repo.GetByIdAsync(memberId, Arg.Any<CancellationToken>()).Returns(new PartyMember { Id = memberId, PartyId = partyId, Role = PartyRole.Member, AuthUserId = authUserId });

        var cmd = new RemovePartyMemberCommand(partyId, memberId, "rule violation");
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(memberId, Arg.Any<CancellationToken>());
        await notify.Received(1).SendMemberRemovedNotificationAsync(partyId, authUserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongParty_ThrowsBadRequest()
    {
        var repo = Substitute.For<IPartyMemberRepository>();
        var notify = Substitute.For<RogueLearn.User.Application.Interfaces.IPartyNotificationService>();
        var sut = new RemovePartyMemberCommandHandler(repo, notify);

        var partyId = System.Guid.NewGuid();
        var memberId = System.Guid.NewGuid();
        repo.GetByIdAsync(memberId, Arg.Any<CancellationToken>()).Returns(new PartyMember { Id = memberId, PartyId = System.Guid.NewGuid(), Role = PartyRole.Member });

        var cmd = new RemovePartyMemberCommand(partyId, memberId, "cleanup");
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public void Record_Creates_With_Values()
    {
        var partyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var cmd = new RemovePartyMemberCommand(partyId, memberId, "reason");
        cmd.PartyId.Should().Be(partyId);
        cmd.MemberId.Should().Be(memberId);
        cmd.Reason.Should().Be("reason");
    }
}
