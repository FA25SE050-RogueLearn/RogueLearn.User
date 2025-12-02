using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.ApplyJoinRequest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ApplyJoinRequest;

public class ApplyGuildJoinRequestCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_Success_AddsPendingRequest(ApplyGuildJoinRequestCommand cmd)
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo);

        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50 });
        memberRepo.CountActiveMembersAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(10);
        requestRepo.GetRequestsByRequesterAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<GuildJoinRequest>());
        requestRepo.AddAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildJoinRequest>());

        await sut.Handle(cmd, CancellationToken.None);
        await requestRepo.Received(1).AddAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>());
    }
}