using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IStudentSemesterSubjectRepository : IGenericRepository<StudentSemesterSubject>
{

    Task<IEnumerable<Subject>> GetSubjectsByUserAsync(Guid authUserId, CancellationToken cancellationToken = default);
}
    