using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Commands.RemoveMember;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.RemoveMember;

public class RemovePartyMemberCommandHandlerTests
{
    [Fact]
    public async Task Handle_CannotRemoveLeader()
    {
        var cmd = new RemovePartyMemberCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), "reason");
        var repo = Substitute.For<IPartyMemberRepository>();
        var notification = Substitute.For<IPartyNotificationService>();
        var sut = new RemovePartyMemberCommandHandler(repo, notification);

        var member = new PartyMember { Id = cmd.MemberId, PartyId = cmd.PartyId, Role = PartyRole.Leader };
        repo.GetByIdAsync(cmd.MemberId, Arg.Any<CancellationToken>()).Returns(member);

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Deletes()
    {
        var cmd = new RemovePartyMemberCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), null);
        var repo = Substitute.For<IPartyMemberRepository>();
        var notification = Substitute.For<IPartyNotificationService>();
        var sut = new RemovePartyMemberCommandHandler(repo, notification);

        var member = new PartyMember { Id = cmd.MemberId, PartyId = cmd.PartyId, Role = PartyRole.Member };
        repo.GetByIdAsync(cmd.MemberId, Arg.Any<CancellationToken>()).Returns(member);

        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(member.Id, Arg.Any<CancellationToken>());
    }
}