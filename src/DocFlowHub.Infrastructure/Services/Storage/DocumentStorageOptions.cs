namespace DocFlowHub.Infrastructure.Services.Storage;

public class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "documents";
    public long MaxFileSizeBytes { get; set; } = 31457280; // 30MB
    public string[] AllowedFileTypes { get; set; } = 
    {
        ".md", ".txt", ".doc", ".docx", 
        ".pdf", ".png", ".jpg", ".jpeg", 
        ".gif", ".bmp"
    };
} 