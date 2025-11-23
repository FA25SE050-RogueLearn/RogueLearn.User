using MediatR;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubjectContent;

/// <summary>
/// Command to update the content (syllabus JSON) of a subject.
/// Takes a SyllabusContent model and serializes it to JSON for storage.
/// Uses existing SyllabusContent from CurriculumImportSchema.cs
/// </summary>
public class UpdateSubjectContentCommand : IRequest<SyllabusContent>
{
    /// <summary>
    /// The ID of the subject to update.
    /// </summary>
    public Guid SubjectId { get; set; }

    /// <summary>
    /// The new content object (syllabus).
    /// This will be serialized to JSON and stored in Subject.Content.
    /// </summary>
    public SyllabusContent Content { get; set; } = new();
}
