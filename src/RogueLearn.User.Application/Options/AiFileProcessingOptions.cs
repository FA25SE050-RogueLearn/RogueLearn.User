namespace RogueLearn.User.Application.Options;

public class AiFileProcessingOptions
{
    public bool EnableLocalTextFallback { get; set; } = true;
    public int MaxFileSizeMB { get; set; } = 50;
    public string[] AllowedMimeTypes { get; set; } = new[]
    {
        "application/pdf",
        "text/plain",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
        "application/vnd.openxmlformats-officedocument.presentationml.presentation" // .pptx
    };
    public bool SingleFileOnly { get; set; } = true;
}