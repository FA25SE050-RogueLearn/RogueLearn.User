using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class StudentTermSubjectRepository : GenericRepository<StudentTermSubject>, IStudentTermSubjectRepository
{
    public StudentTermSubjectRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}
