using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.SubjectSkillMappings.Commands.RemoveSubjectSkillMapping;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.SubjectSkillMappings.Commands.RemoveSubjectSkillMapping;

public class RemoveSubjectSkillMappingCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = Substitute.For<ISubjectSkillMappingRepository>();
        var sut = new RemoveSubjectSkillMappingCommandHandler(repo);
        var cmd = new RemoveSubjectSkillMappingCommand { SubjectId = Guid.NewGuid(), SkillId = Guid.NewGuid() };
        repo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SubjectSkillMapping, bool>>>(), Arg.Any<CancellationToken>()).Returns((SubjectSkillMapping?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Found_Deletes()
    {
        var repo = Substitute.For<ISubjectSkillMappingRepository>();
        var sut = new RemoveSubjectSkillMappingCommandHandler(repo);
        var mapping = new SubjectSkillMapping { Id = Guid.NewGuid(), SubjectId = Guid.NewGuid(), SkillId = Guid.NewGuid() };
        repo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SubjectSkillMapping, bool>>>(), Arg.Any<CancellationToken>()).Returns(mapping);
        await sut.Handle(new RemoveSubjectSkillMappingCommand { SubjectId = mapping.SubjectId, SkillId = mapping.SkillId }, CancellationToken.None);
        await repo.Received(1).DeleteAsync(mapping.Id, Arg.Any<CancellationToken>());
    }
}