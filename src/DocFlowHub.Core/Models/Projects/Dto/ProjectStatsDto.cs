using System;

namespace DocFlowHub.Core.Models.Projects.Dto;

public class ProjectStatsDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    
    // Document statistics
    public int TotalDocuments { get; set; }
    public int ActiveDocuments { get; set; }
    public int DeletedDocuments { get; set; }
    public long TotalFileSize { get; set; }
    
    // Folder statistics
    public int TotalFolders { get; set; }
    public int RootFolders { get; set; }
    public int MaxFolderDepth { get; set; }
    
    // Activity statistics
    public DateTime? LastActivity { get; set; }
    public DateTime? LastDocumentUpload { get; set; }
    public DateTime? LastFolderCreation { get; set; }
    
    // Collaboration statistics
    public int SharedWithTeams { get; set; }
    public int CollaboratorCount { get; set; }
} 