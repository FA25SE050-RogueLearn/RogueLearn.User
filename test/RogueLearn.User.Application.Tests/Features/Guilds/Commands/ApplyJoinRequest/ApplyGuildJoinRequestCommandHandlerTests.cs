using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.ApplyJoinRequest;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ApplyJoinRequest;

public class ApplyGuildJoinRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_Success_AddsPendingRequest()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var requestRepo = Substitute.For<IGuildJoinRequestRepository>();
        var notificationService = Substitute.For<RogueLearn.User.Application.Interfaces.IGuildNotificationService>();
        var invitationRepo = Substitute.For<IGuildInvitationRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ApplyGuildJoinRequestCommandHandler(guildRepo, memberRepo, requestRepo, notificationService, invitationRepo, roleRepo, userRoleRepo);

        var cmd = new ApplyGuildJoinRequestCommand(Guid.NewGuid(), Guid.NewGuid(), "msg");
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, MaxMembers = 50, RequiresApproval = true, IsPublic = true });
        memberRepo.CountActiveMembersAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(10);
        requestRepo.GetRequestsByRequesterAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new List<GuildJoinRequest>());
        requestRepo.AddAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<GuildJoinRequest>());

        await sut.Handle(cmd, CancellationToken.None);
        await requestRepo.Received(1).AddAsync(Arg.Any<GuildJoinRequest>(), Arg.Any<CancellationToken>());
    }
}