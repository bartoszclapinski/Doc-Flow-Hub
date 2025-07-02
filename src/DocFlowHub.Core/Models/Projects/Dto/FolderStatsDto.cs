using System;

namespace DocFlowHub.Core.Models.Projects.Dto;

public class FolderStatsDto
{
    public int FolderId { get; set; }
    public string FolderName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Level { get; set; }
    
    // Content statistics
    public int DirectDocumentCount { get; set; }
    public int TotalDocumentCount { get; set; } // Including subfolders
    public int DirectSubfolderCount { get; set; }
    public int TotalSubfolderCount { get; set; } // Including nested folders
    
    // Size statistics
    public long DirectFileSize { get; set; }
    public long TotalFileSize { get; set; } // Including subfolders
    
    // Activity statistics
    public DateTime? LastActivity { get; set; }
    public DateTime? LastDocumentAdded { get; set; }
    public DateTime? LastSubfolderCreated { get; set; }
    
    // Hierarchy information
    public int MaxDepth { get; set; } // Maximum depth from this folder
    public bool HasSubfolders { get; set; }
    public bool HasDocuments { get; set; }
    public bool IsEmpty { get; set; }
} 