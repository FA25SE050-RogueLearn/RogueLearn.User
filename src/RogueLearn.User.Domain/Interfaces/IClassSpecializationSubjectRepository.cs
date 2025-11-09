using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IClassSpecializationSubjectRepository : IGenericRepository<ClassSpecializationSubject>
{
    Task<IEnumerable<Subject>> GetSubjectByClassIdAsync(Guid classId, CancellationToken cancellationToken);
}