// RogueLearn.User/src/RogueLearn.User.Application/Features/Onboarding/Queries/GetAllClasses/GetAllClassesQueryHandler.cs
using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;

public class GetAllClassesQueryHandler : IRequestHandler<GetAllClassesQuery, List<ClassDto>>
{
    private readonly IClassRepository _classRepository;
    private readonly IMapper _mapper;

    public GetAllClassesQueryHandler(IClassRepository classRepository, IMapper mapper)
    {
        _classRepository = classRepository;
        _mapper = mapper;
    }

    public async Task<List<ClassDto>> Handle(GetAllClassesQuery request, CancellationToken cancellationToken)
    {
        // FIXED: The Supabase LINQ provider cannot parse an implicit boolean predicate (c => c.IsActive).
        // It requires an explicit comparison (c => c.IsActive == true) to correctly translate it
        // into the required URL filter parameter (is_active=eq.true).
        var classes = await _classRepository.FindAsync(c => c.IsActive == true, cancellationToken);
        return _mapper.Map<List<ClassDto>>(classes);
    }
}