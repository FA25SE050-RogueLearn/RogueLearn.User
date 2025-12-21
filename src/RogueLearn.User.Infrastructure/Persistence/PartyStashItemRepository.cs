using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using System.Text.Json;

namespace RogueLearn.User.Infrastructure.Persistence;

public class PartyStashItemRepository : GenericRepository<PartyStashItem>, IPartyStashItemRepository
{
    public PartyStashItemRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<PartyStashItem>> GetResourcesByPartyAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<PartyStashItem>()
            .Where(r => r.PartyId == partyId)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<PartyStashItem>> GetResourcesByPartyAndSubjectAsync(Guid partyId, string subject, CancellationToken cancellationToken = default)
    {
        // Subject may be stored within the Content string as JSON; filter client-side for now
        var response = await _supabaseClient
            .From<PartyStashItem>()
            .Where(r => r.PartyId == partyId)
            .Get(cancellationToken);

        return response.Models.Where(m =>
        {
            if (m.Content is null)
                return false;

            // Handle different content representations
            if (m.Content is string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return false;
                try
                {
                    using var doc = JsonDocument.Parse(s);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("subject", out var subjectProp))
                    {
                        return string.Equals(subjectProp.GetString(), subject, StringComparison.OrdinalIgnoreCase);
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
            else if (m.Content is JsonElement el)
            {
                if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("subject", out var subjectProp))
                {
                    return string.Equals(subjectProp.GetString(), subject, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }
            else if (m.Content is IDictionary<string, object> dict)
            {
                if (dict.TryGetValue("subject", out var val) && val is string subj)
                {
                    return string.Equals(subj, subject, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }

            return false;
        });
    }
}