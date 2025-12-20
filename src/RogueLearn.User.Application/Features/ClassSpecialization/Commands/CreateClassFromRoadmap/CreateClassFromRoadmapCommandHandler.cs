using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.CreateClassFromRoadmap;

public class CreateClassFromRoadmapCommandHandler : IRequestHandler<CreateClassFromRoadmapCommand, ClassDto>
{
    private readonly IClassRepository _classRepository;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IRoadmapExtractionPlugin _roadmapExtractionPlugin;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateClassFromRoadmapCommandHandler> _logger;

    public CreateClassFromRoadmapCommandHandler(
        IClassRepository classRepository,
        IPdfTextExtractor pdfTextExtractor,
        IRoadmapExtractionPlugin roadmapExtractionPlugin,
        IMapper mapper,
        ILogger<CreateClassFromRoadmapCommandHandler> logger)
    {
        _classRepository = classRepository;
        _pdfTextExtractor = pdfTextExtractor;
        _roadmapExtractionPlugin = roadmapExtractionPlugin;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ClassDto> Handle(CreateClassFromRoadmapCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting roadmap import from file: {FileName}", request.FileName);

        if (request.FileStream == null || request.FileStream.Length == 0)
        {
            throw new BadRequestException("File is empty.");
        }

        // 1. Extract Text from PDF
        string rawText;
        try
        {
            rawText = await _pdfTextExtractor.ExtractTextAsync(request.FileStream, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from PDF.");
            throw new BadRequestException("Could not read text from the provided PDF file.");
        }

        if (string.IsNullOrWhiteSpace(rawText))
        {
            throw new BadRequestException("PDF contains no extractable text.");
        }

        // 2. Use AI to extract structured Class data
        var json = await _roadmapExtractionPlugin.ExtractClassRoadmapJsonAsync(rawText, cancellationToken);

        // 3. Deserialize
        RoadmapImportData? data;
        try
        {
            data = JsonSerializer.Deserialize<RoadmapImportData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize AI response: {Json}", json);
            throw new BadRequestException("AI extraction failed to produce valid JSON.");
        }

        if (data?.Class == null)
        {
            throw new BadRequestException("AI extraction returned empty class data.");
        }

        // 4. Map to Entity & Save
        var newClass = new Class
        {
            Id = Guid.NewGuid(),
            Name = data.Class.Name,
            Description = data.Class.Description,
            RoadmapUrl = data.Class.RoadmapUrl,
            DifficultyLevel = (RogueLearn.User.Domain.Enums.DifficultyLevel)Math.Clamp(data.Class.DifficultyLevel - 1, 0, 3), // Map 1-5 to enum 0-3 roughly
            SkillFocusAreas = data.Class.SkillFocusAreas?.ToArray(),
            EstimatedDurationMonths = data.Class.EstimatedDurationMonths,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _classRepository.AddAsync(newClass, cancellationToken);

        _logger.LogInformation("Created new class '{ClassName}' from roadmap import.", newClass.Name);

        return _mapper.Map<ClassDto>(newClass);
    }

    // Helper classes for JSON deserialization
    private class RoadmapImportData
    {
        public ClassData? Class { get; set; }
    }

    private class ClassData
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RoadmapUrl { get; set; }
        public List<string>? SkillFocusAreas { get; set; }
        public int DifficultyLevel { get; set; }
        public int? EstimatedDurationMonths { get; set; }
    }
}