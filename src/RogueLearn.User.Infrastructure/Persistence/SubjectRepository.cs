// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/SubjectRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase.Postgrest;
using Client = Supabase.Client;
using Supabase;
using static Supabase.Postgrest.Constants;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class SubjectRepository : GenericRepository<Subject>, ISubjectRepository
{
    // ADDED: Inject repositories needed to resolve the user's full context.
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
    private readonly IClassSpecializationSubjectRepository _classSpecializationSubjectRepository;

    public SubjectRepository(
        Client supabaseClient,
        IUserProfileRepository userProfileRepository,
        ICurriculumProgramSubjectRepository programSubjectRepository,
        IClassSpecializationSubjectRepository classSpecializationSubjectRepository) : base(supabaseClient)
    {
        _userProfileRepository = userProfileRepository;
        _programSubjectRepository = programSubjectRepository;
        _classSpecializationSubjectRepository = classSpecializationSubjectRepository;
    }

    public async Task<IEnumerable<Subject>> GetSubjectsByRoute(Guid routeId, CancellationToken cancellationToken = default)
    {
        var routeSubjects = await _supabaseClient
            .From<CurriculumProgramSubject>()
            .Filter("program_id", Operator.Equals, routeId.ToString())
            .Get(cancellationToken);


        if (!routeSubjects.Models.Any())
            return new List<Subject>();

        var subjectIds = routeSubjects.Models.Select(cs => cs.SubjectId).ToList();

        // Fetch the actual subjects
        var subjects = await _supabaseClient
            .From<Subject>()
            .Filter("id", Operator.In, subjectIds)
            .Get(cancellationToken);

        return subjects.Models;
    }

    // ADDED: Implementation for the new contextual query method.
    public async Task<Subject?> GetSubjectForUserContextAsync(string subjectCode, Guid authUserId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get the user's profile to find their program (route) and class.
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(authUserId, cancellationToken);
        if (userProfile?.RouteId == null || userProfile.ClassId == null)
        {
            // If the user's context is incomplete, we cannot perform the lookup.
            return null;
        }

        // Step 2: Build the complete set of valid subject IDs for this user.
        var allowedSubjectIds = new HashSet<Guid>();

        // Add subjects from their main curriculum program.
        var programSubjects = await _programSubjectRepository.FindAsync(p => p.ProgramId == userProfile.RouteId.Value, cancellationToken);
        foreach (var subject in programSubjects)
        {
            allowedSubjectIds.Add(subject.SubjectId);
        }

        // Add subjects from their specialized class.
        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(userProfile.ClassId.Value, cancellationToken);
        foreach (var subject in classSubjects)
        {
            allowedSubjectIds.Add(subject.Id);
        }

        if (!allowedSubjectIds.Any())
        {
            return null;
        }

        // Step 3: Find the subject that matches the code AND is in the user's allowed set.
        var response = await _supabaseClient
            .From<Subject>()
            .Filter("subject_code", Operator.Equals, subjectCode)
            .Filter("id", Operator.In, allowedSubjectIds.Select(id => id.ToString()).ToList())
            .Single(cancellationToken);

        return response;
    }
    public async Task<Subject?> GetByCodeAsync(string subjectCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subjectCode)) return null;
        var response = await _supabaseClient
            .From<Subject>()
            .Filter("subject_code", Operator.Equals, subjectCode)
            .Get(cancellationToken);
        return response.Models.FirstOrDefault();

    }

    public async Task<IEnumerable<Subject>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var list = ids.Select(id => id.ToString()).ToList();
        if (!list.Any()) return Enumerable.Empty<Subject>();

        var response = await _supabaseClient
            .From<Subject>()
            .Filter("id", Operator.In, list)
            .Get(cancellationToken);

        return response.Models;
    }
}
