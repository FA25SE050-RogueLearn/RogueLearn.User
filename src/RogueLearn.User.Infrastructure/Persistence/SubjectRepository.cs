// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/SubjectRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase.Postgrest;
using Supabase.Postgrest.Interfaces;
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

        var subjects = await _supabaseClient
            .From<Subject>()
            .Filter("id", Operator.In, subjectIds)
            .Get(cancellationToken);

        return subjects.Models;
    }

    public async Task<Subject?> GetSubjectForUserContextAsync(string subjectCode, Guid authUserId, CancellationToken cancellationToken = default)
    {
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(authUserId, cancellationToken);
        if (userProfile?.RouteId == null || userProfile.ClassId == null)
        {
            return null;
        }

        var allowedSubjectIds = new HashSet<Guid>();

        var programSubjects = await _programSubjectRepository.FindAsync(p => p.ProgramId == userProfile.RouteId.Value, cancellationToken);
        foreach (var subject in programSubjects)
        {
            allowedSubjectIds.Add(subject.SubjectId);
        }

        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(userProfile.ClassId.Value, cancellationToken);
        foreach (var subject in classSubjects)
        {
            allowedSubjectIds.Add(subject.Id);
        }

        if (!allowedSubjectIds.Any())
        {
            return null;
        }

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

    public async Task<(IEnumerable<Subject> Items, int TotalCount)> GetPagedSubjectsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Helper to apply filters consistently
        IPostgrestTable<Subject> ApplyFilters(IPostgrestTable<Subject> table)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                var filter = $"subject_name.ilike.%{search}%,subject_code.ilike.%{search}%";
                return table.Filter((string)null, Operator.Or, filter);
            }
            return table;
        }

        // 1. Get Count (Explicitly typed to match return of ApplyFilters)
        IPostgrestTable<Subject> countQuery = _supabaseClient.From<Subject>();
        countQuery = ApplyFilters(countQuery);
        var count = await countQuery.Count(CountType.Exact, cancellationToken);

        // 2. Get Data (Explicitly typed)
        IPostgrestTable<Subject> dataQuery = _supabaseClient.From<Subject>();
        dataQuery = ApplyFilters(dataQuery);

        var offset = (page - 1) * pageSize;
        var response = await dataQuery
            .Order("created_at", Ordering.Descending)
            .Range(offset, offset + pageSize - 1)
            .Get(cancellationToken);

        return (response.Models, count);
    }
}
