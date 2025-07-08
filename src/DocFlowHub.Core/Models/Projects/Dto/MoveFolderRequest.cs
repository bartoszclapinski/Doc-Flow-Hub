namespace DocFlowHub.Core.Models.Projects.Dto;

public class MoveFolderRequest
{
    public int FolderId { get; set; }
    public int? NewParentFolderId { get; set; }
} 