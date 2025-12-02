using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Commands.RemoveMember;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.RemoveMember;

public class RemovePartyMemberCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_CannotRemoveLeader(RemovePartyMemberCommand cmd)
    {
        var repo = Substitute.For<IPartyMemberRepository>();
        var sut = new RemovePartyMemberCommandHandler(repo);

        var member = new PartyMember { Id = cmd.MemberId, PartyId = cmd.PartyId, Role = PartyRole.Leader };
        repo.GetByIdAsync(cmd.MemberId, Arg.Any<CancellationToken>()).Returns(member);

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Deletes(RemovePartyMemberCommand cmd)
    {
        var repo = Substitute.For<IPartyMemberRepository>();
        var sut = new RemovePartyMemberCommandHandler(repo);

        var member = new PartyMember { Id = cmd.MemberId, PartyId = cmd.PartyId, Role = PartyRole.Member };
        repo.GetByIdAsync(cmd.MemberId, Arg.Any<CancellationToken>()).Returns(member);

        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(member.Id, Arg.Any<CancellationToken>());
    }
}