using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.RoadmapImport.Commands.ImportClassRoadmap;

public class ImportClassRoadmapCommandHandler : IRequestHandler<ImportClassRoadmapCommand, ImportClassRoadmapResult>
{
    private readonly IRoadmapExtractionPlugin _extractionPlugin;
    private readonly IClassRepository _classRepository;
    private readonly IClassNodeRepository _classNodeRepository;
    private readonly ILogger<ImportClassRoadmapCommandHandler> _logger;

    public ImportClassRoadmapCommandHandler(
        IRoadmapExtractionPlugin extractionPlugin,
        IClassRepository classRepository,
        IClassNodeRepository classNodeRepository,
        ILogger<ImportClassRoadmapCommandHandler> logger)
    {
        _extractionPlugin = extractionPlugin;
        _classRepository = classRepository;
        _classNodeRepository = classNodeRepository;
        _logger = logger;
    }

    public async Task<ImportClassRoadmapResult> Handle(ImportClassRoadmapCommand request, CancellationToken cancellationToken)
    {
        var result = new ImportClassRoadmapResult();
        try
        {
            if (string.IsNullOrWhiteSpace(request.RoadmapJson) && string.IsNullOrWhiteSpace(request.RawText))
            {
                result.IsSuccess = false;
                result.Message = "Either RoadmapJson or RawText must be provided";
                return result;
            }

            // Compute raw text hash for idempotent caching if raw text provided
            if (!string.IsNullOrWhiteSpace(request.RawText))
            {
                result.RawTextHash = ComputeSha256Hash(request.RawText!);
            }

            // 1) Extract JSON if not provided
            var roadmapJson = request.RoadmapJson;
            if (string.IsNullOrWhiteSpace(roadmapJson))
            {
                roadmapJson = await _extractionPlugin.ExtractClassRoadmapJsonAsync(request.RawText!, cancellationToken);
                if (string.IsNullOrWhiteSpace(roadmapJson))
                {
                    result.IsSuccess = false;
                    result.Message = "AI extraction returned empty JSON";
                    return result;
                }
            }
            result.RoadmapJson = roadmapJson;

            // 2) Deserialize
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<ClassRoadmapData>(roadmapJson, options);
            if (data == null || data.Class == null)
            {
                result.IsSuccess = false;
                result.Message = "Invalid roadmap JSON format";
                return result;
            }

            // 3) Upsert Class
            var classEntity = await UpsertClassAsync(data.Class, request, cancellationToken);
            result.Class = new ClassSummary { Id = classEntity.Id.ToString(), Name = classEntity.Name, RoadmapUrl = classEntity.RoadmapUrl };

            // 4) Upsert Nodes
            int created = 0, updated = 0;
            if (data.Nodes != null && data.Nodes.Count > 0)
            {
                foreach (var rootNode in data.Nodes.Select((n, i) => (node: n, index: i)))
                {
                    var counts = await UpsertNodeRecursiveAsync(classEntity.Id, null, rootNode.node, rootNode.index + 1, cancellationToken);
                    created += counts.created;
                    updated += counts.updated;
                }
            }

            result.IsSuccess = true;
            result.Message = "Roadmap imported successfully";
            result.CreatedNodes = created;
            result.UpdatedNodes = updated;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import class roadmap");
            result.IsSuccess = false;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
            return result;
        }
    }

    private async Task<Class> UpsertClassAsync(ClassData cls, ImportClassRoadmapCommand request, CancellationToken ct)
    {
        // Match by roadmapUrl first if provided; else by Name
        Class? existing = null;
        if (!string.IsNullOrWhiteSpace(cls.RoadmapUrl))
        {
            existing = (await _classRepository.FindAsync(c => c.RoadmapUrl == cls.RoadmapUrl!, ct)).FirstOrDefault();
        }
        if (existing == null)
        {
            existing = (await _classRepository.FindAsync(c => c.Name == cls.Name, ct)).FirstOrDefault();
        }

        if (existing != null)
        {
            if (request.OverwriteClassMetadata)
            {
                existing.Description = cls.Description;
                existing.RoadmapUrl = request.RoadmapUrl ?? cls.RoadmapUrl ?? existing.RoadmapUrl;
                existing.SkillFocusAreas = cls.SkillFocusAreas;
                existing.DifficultyLevel = cls.DifficultyLevel;
                existing.EstimatedDurationMonths = cls.EstimatedDurationMonths;
                existing.IsActive = cls.IsActive;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                existing = await _classRepository.UpdateAsync(existing, ct);
            }
            else
            {
                // Optionally update only RoadmapUrl if provided
                if (!string.IsNullOrWhiteSpace(request.RoadmapUrl) && request.RoadmapUrl != existing.RoadmapUrl)
                {
                    existing.RoadmapUrl = request.RoadmapUrl;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    existing = await _classRepository.UpdateAsync(existing, ct);
                }
            }
            return existing;
        }

        var entity = new Class
        {
            Name = cls.Name,
            Description = cls.Description,
            RoadmapUrl = request.RoadmapUrl ?? cls.RoadmapUrl,
            SkillFocusAreas = cls.SkillFocusAreas,
            DifficultyLevel = cls.DifficultyLevel,
            EstimatedDurationMonths = cls.EstimatedDurationMonths,
            IsActive = cls.IsActive,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        return await _classRepository.AddAsync(entity, ct);
    }

    private async Task<(int created, int updated)> UpsertNodeRecursiveAsync(Guid classId, Guid? parentId, RoadmapNodeData node, int siblingSequence, CancellationToken ct)
    {
        int created = 0, updated = 0;
        // Compute idempotent lookup: match by ClassId + ParentId + Title
        var existing = (await _classNodeRepository.FindAsync(n => n.ClassId == classId && n.ParentId == parentId && n.Title == node.Title, ct)).FirstOrDefault();

        if (existing != null)
        {
            existing.NodeType = node.NodeType;
            existing.Description = node.Description;
            existing.Sequence = siblingSequence; // keep latest computed order
            existing.CreatedAt = existing.CreatedAt; // unchanged
            await _classNodeRepository.UpdateAsync(existing, ct);
            updated++;
            // Recurse children
            if (node.Children != null && node.Children.Count > 0)
            {
                int i = 1;
                foreach (var child in node.Children)
                {
                    var childCounts = await UpsertNodeRecursiveAsync(classId, existing.Id, child, i++, ct);
                    created += childCounts.created;
                    updated += childCounts.updated;
                }
            }
        }
        else
        {
            var entity = new ClassNode
            {
                ClassId = classId,
                ParentId = parentId,
                Title = node.Title,
                NodeType = node.NodeType,
                Description = node.Description,
                Sequence = siblingSequence,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            var inserted = await _classNodeRepository.AddAsync(entity, ct);
            created++;
            // Recurse children
            if (node.Children != null && node.Children.Count > 0)
            {
                int i = 1;
                foreach (var child in node.Children)
                {
                    var childCounts = await UpsertNodeRecursiveAsync(classId, inserted.Id, child, i++, ct);
                    created += childCounts.created;
                    updated += childCounts.updated;
                }
            }
        }

        return (created, updated);
    }

    private static string ComputeSha256Hash(string rawData)
    {
        using var sha256Hash = SHA256.Create();
        var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
    }
}