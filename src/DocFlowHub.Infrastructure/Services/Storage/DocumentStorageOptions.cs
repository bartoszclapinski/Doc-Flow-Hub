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
    /// <summary>"Azure" (default) or "Local" — files on the server's disk, used by the self-contained demo.</summary>
    public string Provider { get; set; } = "Azure";
    /// <summary>Folder for the Local provider.</summary>
    public string LocalPath { get; set; } = Path.Combine(Path.GetTempPath(), "docflowhub-files");
    public string AccountName { get; set; } = string.Empty;
    public string AccountKey { get; set; } = string.Empty;
} 