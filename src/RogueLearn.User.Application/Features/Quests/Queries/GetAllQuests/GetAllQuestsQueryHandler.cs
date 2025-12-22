using MediatR;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetAllQuests;

public class GetAllQuestsQueryHandler : IRequestHandler<GetAllQuestsQuery, PaginatedQuestsResponse>
{
    private readonly IQuestRepository _questRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<GetAllQuestsQueryHandler> _logger;

    public GetAllQuestsQueryHandler(
        IQuestRepository questRepository,
        ISubjectRepository subjectRepository,
        ILogger<GetAllQuestsQueryHandler> logger)
    {
        _questRepository = questRepository;
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    public async Task<PaginatedQuestsResponse> Handle(GetAllQuestsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving admin quest list. Page: {Page}, Size: {Size}", request.Page, request.PageSize);

        // Fetch all quests (Supabase generic repository limitations usually require fetching all for complex joins/counts 
        // unless specialized pagination exists. Assuming in-memory paging for now based on other handlers).
        var allQuests = await _questRepository.GetAllAsync(cancellationToken);

        // Apply filters
        var query = allQuests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(q =>
                q.Title.ToLowerInvariant().Contains(term) ||
                (q.Description != null && q.Description.ToLowerInvariant().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(q => q.Status.ToString().Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        }

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var pagedQuests = query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        // Enrich with Subject Data
        var subjectIds = pagedQuests.Where(q => q.SubjectId.HasValue).Select(q => q.SubjectId!.Value).Distinct();
        var subjects = await _subjectRepository.GetByIdsAsync(subjectIds, cancellationToken);
        var subjectMap = subjects.ToDictionary(s => s.Id);

        var dtos = pagedQuests.Select(q => new AdminQuestDto
        {
            Id = q.Id,
            Title = q.Title,
            Description = q.Description,
            QuestType = q.QuestType.ToString(),
            DifficultyLevel = q.DifficultyLevel.ToString(),
            Status = q.Status.ToString(),
            IsActive = q.IsActive,
            IsRecommended = q.IsRecommended,
            ExpectedDifficulty = q.ExpectedDifficulty,
            SubjectCode = q.SubjectId.HasValue && subjectMap.TryGetValue(q.SubjectId.Value, out var s) ? s.SubjectCode : null,
            SubjectName = q.SubjectId.HasValue && subjectMap.TryGetValue(q.SubjectId.Value, out var sn) ? sn.SubjectName : null,
            CreatedAt = q.CreatedAt,
            UpdatedAt = q.UpdatedAt
        }).ToList();

        return new PaginatedQuestsResponse
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}