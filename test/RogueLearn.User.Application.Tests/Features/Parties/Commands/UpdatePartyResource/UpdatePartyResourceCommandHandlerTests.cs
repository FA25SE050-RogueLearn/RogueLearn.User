using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.UpdatePartyResource;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.UpdatePartyResource;

public class UpdatePartyResourceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WrongParty_Throws()
    {
        var cmd = new UpdatePartyResourceCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(), new UpdatePartyResourceRequest("T", new object(), new[] { "tag" }));
        var repo = Substitute.For<IPartyStashItemRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new UpdatePartyResourceCommandHandler(repo, memberRepo);
        var item = new PartyStashItem { Id = cmd.StashItemId, PartyId = System.Guid.NewGuid() };
        repo.GetByIdAsync(cmd.StashItemId, Arg.Any<CancellationToken>()).Returns(item);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Updates()
    {
        var cmd = new UpdatePartyResourceCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(), new UpdatePartyResourceRequest("T", new object(), new[] { "tag" }));
        var repo = Substitute.For<IPartyStashItemRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new UpdatePartyResourceCommandHandler(repo, memberRepo);
        var item = new PartyStashItem { Id = cmd.StashItemId, PartyId = cmd.PartyId, SharedByUserId = cmd.ActorAuthUserId };
        repo.GetByIdAsync(cmd.StashItemId, Arg.Any<CancellationToken>()).Returns(item);
        memberRepo.GetMemberAsync(cmd.PartyId, cmd.ActorAuthUserId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = cmd.PartyId, AuthUserId = cmd.ActorAuthUserId, Status = MemberStatus.Active, Role = PartyRole.Member });
        repo.UpdateAsync(Arg.Any<PartyStashItem>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyStashItem>());

        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<PartyStashItem>(s => s.Title == "T"), Arg.Any<CancellationToken>());
    }



    [Fact]
    public async Task Handle_LeaderCanUpdate_NotSharer_Succeeds()
    {
        var cmd = new UpdatePartyResourceCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(), new UpdatePartyResourceRequest("NT", new object(), new[] { "x" }));
        var repo = Substitute.For<IPartyStashItemRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new UpdatePartyResourceCommandHandler(repo, memberRepo);
        var item = new PartyStashItem { Id = cmd.StashItemId, PartyId = cmd.PartyId, SharedByUserId = System.Guid.NewGuid(), Title = "Old" };
        repo.GetByIdAsync(cmd.StashItemId, Arg.Any<CancellationToken>()).Returns(item);
        memberRepo.GetMemberAsync(cmd.PartyId, cmd.ActorAuthUserId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = cmd.PartyId, AuthUserId = cmd.ActorAuthUserId, Status = MemberStatus.Active, Role = PartyRole.Leader });
        repo.UpdateAsync(Arg.Any<PartyStashItem>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyStashItem>());
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).UpdateAsync(Arg.Is<PartyStashItem>(s => s.Title == "NT"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotActiveMember_ThrowsForbidden()
    {
        var cmd = new UpdatePartyResourceCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(), new UpdatePartyResourceRequest("T", new object(), new[] { "tag" }));
        var repo = Substitute.For<IPartyStashItemRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new UpdatePartyResourceCommandHandler(repo, memberRepo);
        var item = new PartyStashItem { Id = cmd.StashItemId, PartyId = cmd.PartyId };
        repo.GetByIdAsync(cmd.StashItemId, Arg.Any<CancellationToken>()).Returns(item);
        memberRepo.GetMemberAsync(cmd.PartyId, cmd.ActorAuthUserId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = cmd.PartyId, AuthUserId = cmd.ActorAuthUserId, Status = MemberStatus.Left, Role = PartyRole.Member });
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}
