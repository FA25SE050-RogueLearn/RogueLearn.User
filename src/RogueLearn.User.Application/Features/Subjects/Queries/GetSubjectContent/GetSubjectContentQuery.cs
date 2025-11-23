using MediatR;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectContent;

/// <summary>
/// Query to retrieve the content (syllabus JSON) of a subject.
/// Returns the raw SyllabusContent model deserialized from JSON.
/// Uses the existing SyllabusContent from CurriculumImportSchema.cs
/// </summary>
public class GetSubjectContentQuery : IRequest<SyllabusContent>
{
    /// <summary>
    /// The ID of the subject to retrieve content for.
    /// </summary>
    public Guid SubjectId { get; set; }
}
