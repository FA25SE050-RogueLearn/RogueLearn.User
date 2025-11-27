// src/RogueLearn.User.Application/Plugins/IQuestGenerationPlugin.cs
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Interface for quest step generation plugin using AI.
/// Generates activities for a SINGLE week per call.
/// This plugin is called multiple times (once per week) to generate all quest steps.
/// </summary>
public interface IQuestGenerationPlugin
{
    /// <summary>
    /// Generates quest steps for a SINGLE week using AI.
    /// Called once per week to generate all quest steps sequentially.
    /// </summary>
    /// <param name="weekContext">The aggregated context for the week (topics + pooled resources)</param>
    /// <param name="userContext">User/class context for personalization</param>
    /// <param name="relevantSkills">List of skills linked to the subject</param>
    /// <param name="subjectName">Name of the subject</param>
    /// <param name="courseDescription">Course description for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON response with activities array: { "activities": [...] }</returns>
    Task<string?> GenerateQuestStepsJsonAsync(
        WeekContext weekContext,
        string userContext,
        List<Skill> relevantSkills,
        string subjectName,
        string courseDescription,
        CancellationToken cancellationToken = default);
}