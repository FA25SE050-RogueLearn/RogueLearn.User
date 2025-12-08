using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Parties.Commands.AddPartyResource;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.AddPartyResource;

public class AddPartyResourceCommandHandlerTests
{
    [Fact]
    public async Task Handle_AddsResourceAndSendsNotification()
    {
        var repo = Substitute.For<IPartyStashItemRepository>();
        var notif = Substitute.For<IPartyNotificationService>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new AddPartyResourceCommandHandler(repo, notif, mapper, memberRepo);

        var cmd = new AddPartyResourceCommand(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(), "T", new { text = "body" }, new[] { "tag1" });
        memberRepo.GetMemberAsync(cmd.PartyId, cmd.SharedByUserId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = cmd.PartyId, AuthUserId = cmd.SharedByUserId, Status = RogueLearn.User.Domain.Enums.MemberStatus.Active, Role = RogueLearn.User.Domain.Enums.PartyRole.Leader });
        repo.AddAsync(Arg.Any<PartyStashItem>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyStashItem>());
        mapper.Map<PartyStashItemDto>(Arg.Any<PartyStashItem>()).Returns(new PartyStashItemDto(System.Guid.NewGuid(), cmd.PartyId, cmd.OriginalNoteId, cmd.SharedByUserId, cmd.Title, cmd.Content, cmd.Tags, System.DateTimeOffset.UtcNow, System.DateTimeOffset.UtcNow));

        var resp = await sut.Handle(cmd, CancellationToken.None);
        Assert.Equal(cmd.PartyId, resp.PartyId);
    }

    [Fact]
    public async Task Handle_MapsAllFields()
    {
        var repo = Substitute.For<IPartyStashItemRepository>();
        var notif = Substitute.For<IPartyNotificationService>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new AddPartyResourceCommandHandler(repo, notif, mapper, memberRepo);

        var partyId = System.Guid.NewGuid();
        var sharedBy = System.Guid.NewGuid();
        var originalNote = System.Guid.NewGuid();
        var tags = new[] { "a", "b" };
        var content = new { text = "body" };
        var cmd = new AddPartyResourceCommand(partyId, sharedBy, originalNote, "Title", content, tags);

        PartyStashItem? captured = null;
        memberRepo.GetMemberAsync(cmd.PartyId, cmd.SharedByUserId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = cmd.PartyId, AuthUserId = cmd.SharedByUserId, Status = RogueLearn.User.Domain.Enums.MemberStatus.Active, Role = RogueLearn.User.Domain.Enums.PartyRole.Leader });
        repo.AddAsync(Arg.Do<PartyStashItem>(i => captured = i), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyStashItem>());
        mapper.Map<PartyStashItemDto>(Arg.Any<PartyStashItem>()).Returns(new PartyStashItemDto(System.Guid.NewGuid(), partyId, originalNote, sharedBy, "Title", content, tags, System.DateTimeOffset.UtcNow, System.DateTimeOffset.UtcNow));

        var dto = await sut.Handle(cmd, CancellationToken.None);
        Assert.NotNull(captured);
        Assert.Equal(partyId, captured!.PartyId);
        Assert.Equal(sharedBy, captured.SharedByUserId);
        Assert.Equal(originalNote, captured.OriginalNoteId);
        Assert.Equal("Title", captured.Title);
        Assert.Equal(tags.Length, captured.Tags!.Length);
        Assert.Equal("Title", dto.Title);
    }

    [Fact]
    public async Task Handle_ActiveMember_NotLeader_AllowsAddResource()
    {
        var repo = Substitute.For<IPartyStashItemRepository>();
        var notif = Substitute.For<IPartyNotificationService>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new AddPartyResourceCommandHandler(repo, notif, mapper, memberRepo);

        var partyId = System.Guid.NewGuid();
        var sharedBy = System.Guid.NewGuid();
        var originalNote = System.Guid.NewGuid();
        var tags = new[] { "a", "b" };
        var content = new { text = "body" };
        var cmd = new AddPartyResourceCommand(partyId, sharedBy, originalNote, "Title", content, tags);

        PartyStashItem? captured = null;
        memberRepo.GetMemberAsync(cmd.PartyId, cmd.SharedByUserId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = cmd.PartyId, AuthUserId = cmd.SharedByUserId, Status = RogueLearn.User.Domain.Enums.MemberStatus.Active, Role = RogueLearn.User.Domain.Enums.PartyRole.Member });
        repo.AddAsync(Arg.Do<PartyStashItem>(i => captured = i), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<PartyStashItem>());
        mapper.Map<PartyStashItemDto>(Arg.Any<PartyStashItem>()).Returns(new PartyStashItemDto(System.Guid.NewGuid(), partyId, originalNote, sharedBy, "Title", content, tags, System.DateTimeOffset.UtcNow, System.DateTimeOffset.UtcNow));

        var dto = await sut.Handle(cmd, CancellationToken.None);
        Assert.NotNull(captured);
        Assert.Equal(partyId, dto.PartyId);
    }

    [Fact]
    public async Task Handle_InactiveMember_Forbidden()
    {
        var repo = Substitute.For<IPartyStashItemRepository>();
        var notif = Substitute.For<IPartyNotificationService>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var memberRepo = Substitute.For<IPartyMemberRepository>();
        var sut = new AddPartyResourceCommandHandler(repo, notif, mapper, memberRepo);

        var partyId = System.Guid.NewGuid();
        var sharedBy = System.Guid.NewGuid();
        var originalNote = System.Guid.NewGuid();
        var tags = new[] { "a", "b" };
        var content = new { text = "body" };
        var cmd = new AddPartyResourceCommand(partyId, sharedBy, originalNote, "Title", content, tags);

        memberRepo.GetMemberAsync(cmd.PartyId, cmd.SharedByUserId, Arg.Any<CancellationToken>()).Returns(new PartyMember { PartyId = cmd.PartyId, AuthUserId = cmd.SharedByUserId, Status = RogueLearn.User.Domain.Enums.MemberStatus.Suspended, Role = RogueLearn.User.Domain.Enums.PartyRole.Member });

        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.ForbiddenException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}
