// RogueLearn.User/src/RogueLearn.User.Application/Services/ISubjectImportService.cs
using Hangfire;
using Hangfire.Server;
using RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

namespace RogueLearn.User.Application.Services;

public interface ISubjectImportService
{
    [AutomaticRetry(Attempts = 0)] // Don't retry automatically; let the user see the error and retry manually
    Task ImportSubjectAsync(ImportSubjectFromTextCommand command, PerformContext context);
}