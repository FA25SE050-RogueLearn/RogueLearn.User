using System.Threading;

namespace RogueLearn.User.Application.Interfaces
{
    public interface IWebSearchService
    {
        Task<IEnumerable<string>> SearchAsync(
            string query,
            int count = 10,
            int offset = 0,
            CancellationToken cancellationToken = default);
    }
}