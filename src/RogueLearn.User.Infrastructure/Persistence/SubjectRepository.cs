using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase.Postgrest.Interfaces;
using Client = Supabase.Client;
using static Supabase.Postgrest.Constants;
using Supabase.Postgrest;

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

    public async Task<IEnumerable<Subject>> GetByCodesAsync(IEnumerable<string> subjectCodes, CancellationToken cancellationToken = default)
    {
        var codesList = subjectCodes.Distinct().ToList();
        if (!codesList.Any()) return Enumerable.Empty<Subject>();

        var response = await _supabaseClient
            .From<Subject>()
            .Filter("subject_code", Operator.In, codesList)
            .Get(cancellationToken);

        return response.Models;
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
        var searchPattern = !string.IsNullOrWhiteSpace(search) ? $"%{search}%" : null;
        var offset = (page - 1) * pageSize;

        if (searchPattern != null)
        {
            var orFilters = new List<IPostgrestQueryFilter>
            {
                new QueryFilter("subject_name", Operator.ILike, searchPattern),
                new QueryFilter("subject_code", Operator.ILike, searchPattern)
            };

            var count = await _supabaseClient.From<Subject>()
                .Or(orFilters)
                .Count(CountType.Exact, cancellationToken);

            var response = await _supabaseClient.From<Subject>()
                .Or(orFilters)
                .Order("created_at", Ordering.Descending)
                .Range(offset, offset + pageSize - 1)
                .Get(cancellationToken);

            return (response.Models, count);
        }
        else
        {
            var count = await _supabaseClient.From<Subject>()
                .Count(CountType.Exact, cancellationToken);

            var response = await _supabaseClient.From<Subject>()
                .Order("created_at", Ordering.Descending)
                .Range(offset, offset + pageSize - 1)
                .Get(cancellationToken);

            return (response.Models, count);
        }
    }
}