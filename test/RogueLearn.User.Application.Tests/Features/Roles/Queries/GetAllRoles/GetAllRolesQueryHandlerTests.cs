using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Roles.Queries.GetAllRoles;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Roles.Queries.GetAllRoles;

public class GetAllRolesQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_ReturnsMappedRoles(GetAllRolesQuery query)
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetAllRolesQueryHandler>>();

        var roles = new List<Role> { new Role { Id = System.Guid.NewGuid(), Name = "r1" } };
        roleRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(roles);
        mapper.Map<List<RoleDto>>(roles).Returns(new List<RoleDto> { new RoleDto { Id = roles[0].Id, Name = roles[0].Name } });

        var sut = new GetAllRolesQueryHandler(roleRepo, mapper, logger);
        var res = await sut.Handle(query, CancellationToken.None);
        res.Roles.Should().HaveCount(1);
        res.Roles[0].Name.Should().Be("r1");
    }

    [Theory]
    [AutoData]
    public async Task Handle_Empty_ReturnsEmptyList(GetAllRolesQuery query)
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetAllRolesQueryHandler>>();

        roleRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Role>());
        mapper.Map<List<RoleDto>>(Arg.Any<List<Role>>()).Returns((List<RoleDto>?)null);

        var sut = new GetAllRolesQueryHandler(roleRepo, mapper, logger);
        var res = await sut.Handle(query, CancellationToken.None);
        res.Roles.Should().NotBeNull();
        res.Roles.Should().BeEmpty();
    }
}