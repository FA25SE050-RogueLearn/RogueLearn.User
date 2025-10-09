using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;

public class ValidateSyllabusQuery : IRequest<ValidateSyllabusResponse>
{
    public string RawText { get; set; } = string.Empty;
}