using MediatR;
using System.Text;

namespace RogueLearn.User.Application.Features.UnityMatches.Queries.GetLastPlayerSummary
{
    public record GetLastPlayerSummaryQuery(string UserId) : IRequest<string?>;

    public class GetLastPlayerSummaryQueryHandler : IRequestHandler<GetLastPlayerSummaryQuery, string?>
    {
        public Task<string?> Handle(GetLastPlayerSummaryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var overrideDir = Environment.GetEnvironmentVariable("RESULTS_DIR");
                string baseDir = string.IsNullOrWhiteSpace(overrideDir)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "tmp", "match-results", "players")
                    : Path.Combine(overrideDir, "players");
                if (!Directory.Exists(baseDir)) return Task.FromResult<string?>(null);
                var files = Directory.GetFiles(baseDir, $"player_{request.UserId}_*.json");
                if (files == null || files.Length == 0) return Task.FromResult<string?>(null);
                Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
                var path = files[0];
                var json = File.ReadAllText(path, Encoding.UTF8);
                return Task.FromResult<string?>(json);
            }
            catch
            {
                return Task.FromResult<string?>(null);
            }
        }
    }
}
