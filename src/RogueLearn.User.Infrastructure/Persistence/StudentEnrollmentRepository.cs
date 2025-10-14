using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class StudentEnrollmentRepository : GenericRepository<StudentEnrollment>, IStudentEnrollmentRepository
{
    public StudentEnrollmentRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}
