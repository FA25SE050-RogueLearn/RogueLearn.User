// RogueLearn.User/src/RogueLearn.User.Application/Plugins/ISkillManagementPlugin.cs
using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace RogueLearn.User.Application.Plugins;

public interface ISkillManagementPlugin
{
    [KernelFunction, Description("Analyzes a subject's syllabus, suggests skills, creates any new skills, and maps them all to the subject.")]
    Task<string> CreateAndMapSkillsForSubjectAsync(
        [Description("The UUID of the subject to analyze.")] Guid subjectId);

    [KernelFunction, Description("Analyzes all skills within a curriculum version and suggests prerequisite dependencies between them.")]
    Task<string> AnalyzeAndCreateDependenciesAsync(
        [Description("The UUID of the curriculum version to analyze.")] Guid curriculumVersionId);
}
