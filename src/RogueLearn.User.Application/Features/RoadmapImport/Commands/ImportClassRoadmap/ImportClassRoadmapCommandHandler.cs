using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.RoadmapImport.Commands.ImportClassRoadmap;

public class ImportClassRoadmapCommandHandler : IRequestHandler<ImportClassRoadmapCommand, ImportClassRoadmapResponse>
{
    private readonly IRoadmapExtractionPlugin _extractionPlugin;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IClassRepository _classRepository;
    private readonly IClassNodeRepository _classNodeRepository;
    private readonly IRoadmapImportStorage _storage;
    private readonly ILogger<ImportClassRoadmapCommandHandler> _logger;

    public ImportClassRoadmapCommandHandler(
        IRoadmapExtractionPlugin extractionPlugin,
        IPdfTextExtractor pdfTextExtractor,
        IClassRepository classRepository,
        IClassNodeRepository classNodeRepository,
        IRoadmapImportStorage storage,
        ILogger<ImportClassRoadmapCommandHandler> logger)
    {
        _extractionPlugin = extractionPlugin;
        _pdfTextExtractor = pdfTextExtractor;
        _classRepository = classRepository;
        _classNodeRepository = classNodeRepository;
        _storage = storage;
        _logger = logger;
    }

    public async Task<ImportClassRoadmapResponse> Handle(ImportClassRoadmapCommand request, CancellationToken cancellationToken)
    {
        var result = new ImportClassRoadmapResponse();
        try
        {
            // Validate PDF input only
            if (request.PdfAttachmentStream == null)
            {
                result.IsSuccess = false;
                result.Message = "A PDF file must be provided";
                return result;
            }

            // Read PDF stream into memory for repeated use
            byte[] pdfBytes;
            using (var ms = new MemoryStream())
            {
                await request.PdfAttachmentStream.CopyToAsync(ms, cancellationToken);
                pdfBytes = ms.ToArray();
            }

            // Extract raw text from PDF
            string rawText;
            using (var extractStream = new MemoryStream(pdfBytes))
            {
                rawText = await _pdfTextExtractor.ExtractTextAsync(extractStream, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(rawText))
            {
                result.IsSuccess = false;
                result.Message = "Failed to extract text from PDF";
                return result;
            }

            // Compute raw text hash for idempotent caching
            result.RawTextHash = ComputeSha256Hash(rawText);

            // 1) Extract JSON from raw text via AI
            var roadmapJson = await _extractionPlugin.ExtractClassRoadmapJsonAsync(rawText, cancellationToken);
            if (string.IsNullOrWhiteSpace(roadmapJson))
            {
                result.IsSuccess = false;
                result.Message = "AI extraction returned empty JSON";
                return result;
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

            // Upload PDF attachment to Supabase storage after a successful import
            if (result.Class != null)
            {
                try
                {
                    const string bucketName = "roadmap-imports";
                    var className = result.Class.Name;

                    using var uploadStream = new MemoryStream(pdfBytes);
                    await _storage.SavePdfAttachmentAsync(
                        bucketName,
                        className,
                        result.RawTextHash!,
                        uploadStream,
                        request.PdfAttachmentFileName ?? "attachment.pdf",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    // Do not fail the import if upload fails; just log error and continue
                    _logger.LogError(ex, "PDF upload failed after successful roadmap import for class {Class}", result.Class.Name);
                    result.Errors.Add($"PDF upload failed: {ex.Message}");
                }
            }

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
        var candidates = await _classNodeRepository.GetByClassAndTitleAsync(classId, node.Title, ct);
        var existing = candidates.FirstOrDefault(n => n.ParentId == parentId);

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