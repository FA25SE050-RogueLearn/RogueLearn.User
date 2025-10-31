using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;

public class ValidateCurriculumQuery : IRequest<ValidateCurriculumResponse>
{
    // MODIFIED: Removed the [Required] attribute. The controller now handles the presence check.
    public string RawText { get; set; } = string.Empty;
}