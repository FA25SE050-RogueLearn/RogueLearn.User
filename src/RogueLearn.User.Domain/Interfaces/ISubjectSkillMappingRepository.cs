using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface ISubjectSkillMappingRepository : IGenericRepository<SubjectSkillMapping>
{
    Task<IEnumerable<SubjectSkillMapping>> GetMappingsBySubjectIdsAsync(IEnumerable<Guid> subjectIds, CancellationToken cancellationToken = default);
}
