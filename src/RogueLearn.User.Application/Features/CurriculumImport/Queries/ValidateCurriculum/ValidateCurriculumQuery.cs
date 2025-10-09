using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;

public class ValidateCurriculumQuery : IRequest<ValidateCurriculumResponse>
{
    public string RawText { get; set; } = string.Empty;
}