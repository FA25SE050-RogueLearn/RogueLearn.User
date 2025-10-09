using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class LecturerVerificationRequestRepository : GenericRepository<LecturerVerificationRequest>, ILecturerVerificationRequestRepository
{
    public LecturerVerificationRequestRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}
