using Hangfire;
using Hangfire.Server;
using RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;

namespace RogueLearn.User.Application.Services;

public interface ISubjectImportService
{
    [AutomaticRetry(Attempts = 0)]
    Task ImportSubjectAsync(ImportSubjectFromTextCommand command, PerformContext context);
}