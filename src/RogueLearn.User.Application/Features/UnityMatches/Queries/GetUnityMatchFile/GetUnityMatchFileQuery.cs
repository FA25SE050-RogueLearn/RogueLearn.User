using MediatR;
using System.Text;

namespace RogueLearn.User.Application.Features.UnityMatches.Queries.GetUnityMatchFile
{
    public record GetUnityMatchFileQuery(string MatchId) : IRequest<string?>;

    public class GetUnityMatchFileQueryHandler : IRequestHandler<GetUnityMatchFileQuery, string?>
    {
        public Task<string?> Handle(GetUnityMatchFileQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var resultsRoot = Environment.GetEnvironmentVariable("RESULTS_LOG_ROOT") ?? "/var/log/unity/matches";
                if (!Directory.Exists(resultsRoot))
                {
                    return Task.FromResult<string?>(null);
                }

                foreach (var dateDir in Directory.GetDirectories(resultsRoot))
                {
                    var matchFiles = Directory.GetFiles(dateDir, $"match_*_{request.MatchId}.json");
                    if (matchFiles.Length > 0)
                    {
                        var json = File.ReadAllText(matchFiles[0], Encoding.UTF8);
                        return Task.FromResult<string?>(json);
                    }
                }
            }
            catch
            {
                // ignore
            }

            return Task.FromResult<string?>(null);
        }
    }
}
