using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;

public class GetAllSubjectsHandler : IRequestHandler<GetAllSubjectsQuery, PaginatedSubjectsResponse>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllSubjectsHandler> _logger;

    public GetAllSubjectsHandler(ISubjectRepository subjectRepository, IMapper mapper, ILogger<GetAllSubjectsHandler> logger)
    {
        _subjectRepository = subjectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedSubjectsResponse> Handle(GetAllSubjectsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetAllSubjectsQuery. Page: {Page}, Size: {Size}, Search: {Search}", request.Page, request.PageSize, request.Search);

        var (subjects, totalCount) = await _subjectRepository.GetPagedSubjectsAsync(request.Search, request.Page, request.PageSize, cancellationToken);

        var dtos = _mapper.Map<List<SubjectDto>>(subjects) ?? new List<SubjectDto>();

        _logger.LogInformation("Retrieved {Count} subjects out of {Total}", dtos.Count, totalCount);

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new PaginatedSubjectsResponse
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}