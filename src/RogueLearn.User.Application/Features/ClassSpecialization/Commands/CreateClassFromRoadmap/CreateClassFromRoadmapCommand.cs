using MediatR;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses; // For ClassDto

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.CreateClassFromRoadmap;

public class CreateClassFromRoadmapCommand : IRequest<ClassDto>
{
    public required Stream FileStream { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}