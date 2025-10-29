using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class StudentSemesterSubjectRepository : GenericRepository<StudentSemesterSubject>, IStudentSemesterSubjectRepository
{
    public StudentSemesterSubjectRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}
