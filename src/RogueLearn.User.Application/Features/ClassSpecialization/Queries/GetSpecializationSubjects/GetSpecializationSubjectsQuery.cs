// RogueLearn.User/src/RogueLearn.User.Application/Features/ClassSpecialization/Queries/GetSpecializationSubjects/GetSpecializationSubjectsQuery.cs
using MediatR;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;

public class GetSpecializationSubjectsQuery : IRequest<List<SpecializationSubjectDto>>
{
    public Guid ClassId { get; set; }
}