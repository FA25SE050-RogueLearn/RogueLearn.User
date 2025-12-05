using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Roles.Commands.CreateRole;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_DuplicateName_Throws()
    {
        var repo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateRoleCommandHandler>>();
        var sut = new CreateRoleCommandHandler(repo, mapper, logger);
        var cmd = new CreateRoleCommand { Name = "role", Description = "desc" };
        repo.GetByNameAsync(cmd.Name, Arg.Any<CancellationToken>()).Returns(new Role { Name = cmd.Name });
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_ReturnsResponse()
    {
        var repo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateRoleCommandHandler>>();
        var sut = new CreateRoleCommandHandler(repo, mapper, logger);
        var cmd = new CreateRoleCommand { Name = "role", Description = "desc" };
        repo.GetByNameAsync(cmd.Name, Arg.Any<CancellationToken>()).Returns((Role?)null);
        var created = new Role { Id = System.Guid.NewGuid(), Name = cmd.Name, Description = cmd.Description };
        repo.AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>()).Returns(created);
        mapper.Map<CreateRoleResponse>(created).Returns(new CreateRoleResponse { Id = created.Id, Name = created.Name });

        var resp = await sut.Handle(cmd, CancellationToken.None);
        Assert.Equal(created.Id, resp.Id);
        await repo.Received(1).AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }
}