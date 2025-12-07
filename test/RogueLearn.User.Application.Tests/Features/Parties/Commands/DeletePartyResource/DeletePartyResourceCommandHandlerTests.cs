using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Parties.Commands.DeletePartyResource;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.DeletePartyResource;

public class DeletePartyResourceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WrongParty_Throws()
    {
        var cmd = new DeletePartyResourceCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid());
        var repo = Substitute.For<IPartyStashItemRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new DeletePartyResourceCommandHandler(repo, memberRepo);
        var item = new PartyStashItem { Id = cmd.StashItemId, PartyId = System.Guid.NewGuid() };
        repo.GetByIdAsync(cmd.StashItemId, Arg.Any<CancellationToken>()).Returns(item);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Deletes()
    {
        var partyId = System.Guid.NewGuid();
        var stashId = System.Guid.NewGuid();
        var cmd = new DeletePartyResourceCommand(partyId, stashId, System.Guid.NewGuid());
        var repo = Substitute.For<IPartyStashItemRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new DeletePartyResourceCommandHandler(repo, memberRepo);
        var item = new PartyStashItem { Id = cmd.StashItemId, PartyId = cmd.PartyId, SharedByUserId = cmd.ActorAuthUserId };
        repo.GetByIdAsync(cmd.StashItemId, Arg.Any<CancellationToken>()).Returns(item);
        memberRepo.GetMemberAsync(cmd.PartyId, cmd.ActorAuthUserId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = cmd.PartyId, AuthUserId = cmd.ActorAuthUserId, Status = MemberStatus.Active, Role = PartyRole.Member });
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnauthorizedActor_Throws()
    {
        var partyId = System.Guid.NewGuid();
        var stashId = System.Guid.NewGuid();
        var cmd = new DeletePartyResourceCommand(partyId, stashId, System.Guid.NewGuid());
        var repo = Substitute.For<IPartyStashItemRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new DeletePartyResourceCommandHandler(repo, memberRepo);
        var item = new PartyStashItem { Id = cmd.StashItemId, PartyId = cmd.PartyId, SharedByUserId = System.Guid.NewGuid() };
        repo.GetByIdAsync(cmd.StashItemId, Arg.Any<CancellationToken>()).Returns(item);
        memberRepo.GetMemberAsync(cmd.PartyId, cmd.ActorAuthUserId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = cmd.PartyId, AuthUserId = cmd.ActorAuthUserId, Status = MemberStatus.Active, Role = PartyRole.Member });
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_LeaderCanDelete_NotSharer_Succeeds()
    {
        var partyId = System.Guid.NewGuid();
        var stashId = System.Guid.NewGuid();
        var actorId = System.Guid.NewGuid();
        var cmd = new DeletePartyResourceCommand(partyId, stashId, actorId);
        var repo = Substitute.For<IPartyStashItemRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new DeletePartyResourceCommandHandler(repo, memberRepo);
        var item = new PartyStashItem { Id = stashId, PartyId = partyId, SharedByUserId = System.Guid.NewGuid() };
        repo.GetByIdAsync(stashId, Arg.Any<CancellationToken>()).Returns(item);
        memberRepo.GetMemberAsync(partyId, actorId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = partyId, AuthUserId = actorId, Status = MemberStatus.Active, Role = PartyRole.Leader });
        await sut.Handle(cmd, CancellationToken.None);
        await repo.Received(1).DeleteAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotActiveMember_ThrowsForbidden()
    {
        var partyId = System.Guid.NewGuid();
        var stashId = System.Guid.NewGuid();
        var cmd = new DeletePartyResourceCommand(partyId, stashId, System.Guid.NewGuid());
        var repo = Substitute.For<IPartyStashItemRepository>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new DeletePartyResourceCommandHandler(repo, memberRepo);
        var item = new PartyStashItem { Id = stashId, PartyId = partyId, SharedByUserId = System.Guid.NewGuid() };
        repo.GetByIdAsync(stashId, Arg.Any<CancellationToken>()).Returns(item);
        memberRepo.GetMemberAsync(partyId, cmd.ActorAuthUserId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = partyId, AuthUserId = cmd.ActorAuthUserId, Status = MemberStatus.Left, Role = PartyRole.Member });
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}
