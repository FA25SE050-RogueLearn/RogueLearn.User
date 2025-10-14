using System.Threading;
using System.Threading.Tasks;

namespace RogueLearn.User.Application.Plugins;

public interface IFlmExtractionPlugin
{
    Task<string> ExtractCurriculumJsonAsync(string rawText, CancellationToken cancellationToken = default);
    Task<string> ExtractSyllabusJsonAsync(string rawText, CancellationToken cancellationToken = default);
}