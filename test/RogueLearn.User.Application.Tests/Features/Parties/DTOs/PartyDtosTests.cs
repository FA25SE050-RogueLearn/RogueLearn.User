using System.Text.Json;
using RogueLearn.User.Application.Features.Guilds.Commands.InviteMember;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Enums;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Parties.DTOs;

public class PartyDtosTests
{
    [Fact]
    public void PartyMemberDto_ConstructsAndSerializes()
    {
        var dto = new PartyMemberDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PartyRole.Member,
            MemberStatus.Active,
            DateTimeOffset.UtcNow,
            "user",
            "user@example.com",
            "First",
            "Last",
            "http://img",
            3,
            100,
            "bio");

        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<PartyMemberDto>(json);
        Assert.NotNull(back);
        Assert.Equal(dto.AuthUserId, back!.AuthUserId);
        Assert.Equal(dto.Role, back.Role);
        Assert.Equal(dto.Status, back.Status);
        Assert.Equal(dto.Username, back.Username);
    }

    [Fact]
    public void PartyInvitationDto_ConstructsAndSerializes()
    {
        var dto = new PartyInvitationDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            InvitationStatus.Pending,
            "msg",
            "join-link",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            null,
            DateTimeOffset.UtcNow.AddDays(1),
            "Party",
            "Invitee");

        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<PartyInvitationDto>(json);
        Assert.NotNull(back);
        Assert.Equal(dto.PartyId, back!.PartyId);
        Assert.Equal(dto.InviterId, back.InviterId);
        Assert.Equal(dto.InviteeId, back.InviteeId);
        Assert.Equal(dto.Status, back.Status);
        Assert.Equal(dto.PartyName, back.PartyName);
        Assert.Equal(dto.InviteeName, back.InviteeName);
    }

    [Fact]
    public void CreatePartyResponse_Serializes()
    {
        var dto = new RogueLearn.User.Application.Features.Parties.Commands.CreateParty.CreatePartyResponse { PartyId = Guid.NewGuid(), RoleGranted = "PartyLeader" };
        var json = JsonSerializer.Serialize(dto);
        var back = JsonSerializer.Deserialize<RogueLearn.User.Application.Features.Parties.Commands.CreateParty.CreatePartyResponse>(json);
        Assert.NotNull(back);
        Assert.Equal(dto.PartyId, back!.PartyId);
        Assert.Equal(dto.RoleGranted, back.RoleGranted);
    }

    [Fact]
    public void AddPartyResourceDto_ConstructsAndSerializes()
    {
        var dto = new AddPartyResourceRequest(
            Guid.NewGuid(),
            "Title",
            "Content",
            new[] { "Tag1", "Tag2" });
        Assert.Equal("Content", dto.Content);
        Assert.Equal("Title", dto.Title);
        Assert.Equal(new[] { "Tag1", "Tag2" }, dto.Tags);
    }

    [Fact]
    public void InvitePartyMemberDto_ConstructsAndSerializes()
    {
        var dto = new InviteMemberRequest(
            [],
            "msg",
            DateTimeOffset.UtcNow.AddDays(1)
            );
        Assert.Equal([], dto.Targets);
        Assert.Equal("msg", dto.Message);
    }
}
