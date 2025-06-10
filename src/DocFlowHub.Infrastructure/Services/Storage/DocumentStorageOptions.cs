namespace DocFlowHub.Infrastructure.Services.Storage;

public class DocumentStorageOptions
{
    public const string SectionName = "Storage";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "documents";
    public long MaxFileSizeBytes { get; set; } = 31_457_280; // 30MB
    public string[] AllowedFileTypes { get; set; } = 
    {
        ".pdf", ".doc", ".docx", ".txt", ".md",
        ".jpg", ".jpeg", ".png", ".gif"
    };
} 