// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/ICurriculumProgramSubjectRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface ICurriculumProgramSubjectRepository : IGenericRepository<CurriculumProgramSubject>
{
    // This repository can be extended with specialized query methods if needed, for example:
    // Task<IEnumerable<Subject>> GetSubjectsForProgramAsync(Guid programId, CancellationToken cancellationToken = default);
}