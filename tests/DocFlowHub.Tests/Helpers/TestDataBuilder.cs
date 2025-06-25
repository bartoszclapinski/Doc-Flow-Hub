using DocFlowHub.Core.Models;
using DocFlowHub.Core.Models.Documents.Dto;
using DocFlowHub.Core.Models.Teams.Dto;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace DocFlowHub.Tests.Helpers;

/// <summary>
/// Helper class to build test data objects using the Builder pattern
/// </summary>
public static class TestDataBuilder
{
    public static class Documents
    {
        public static CreateDocumentRequestBuilder CreateRequest() => new();
        public static UpdateDocumentRequestBuilder UpdateRequest() => new();
        public static DocumentDtoBuilder Dto() => new();
        public static IFormFileBuilder File() => new();
    }

    public static class Teams
    {
        public static CreateTeamRequestBuilder CreateRequest() => new();
        public static TeamDtoBuilder Dto() => new();
        public static TeamMemberDtoBuilder MemberDto() => new();
    }
}

// Document Builders
public class CreateDocumentRequestBuilder
{
    private readonly CreateDocumentRequest _request = new();

    public CreateDocumentRequestBuilder WithTitle(string title)
    {
        _request.Title = title;
        return this;
    }

    public CreateDocumentRequestBuilder WithDescription(string description)
    {
        _request.Description = description;
        return this;
    }

    public CreateDocumentRequestBuilder WithCategories(params int[] categoryIds)
    {
        _request.CategoryIds = categoryIds.ToList();
        return this;
    }

    public CreateDocumentRequestBuilder WithValidData()
    {
        return WithTitle("Test Document")
               .WithDescription("Test description")
               .WithCategories(1, 2);
    }

    public CreateDocumentRequest Build() => _request;
}

public class UpdateDocumentRequestBuilder
{
    private readonly UpdateDocumentRequest _request = new();

    public UpdateDocumentRequestBuilder WithTitle(string title)
    {
        _request.Title = title;
        return this;
    }

    public UpdateDocumentRequestBuilder WithDescription(string description)
    {
        _request.Description = description;
        return this;
    }

    public UpdateDocumentRequestBuilder WithValidData()
    {
        return WithTitle("Updated Document")
               .WithDescription("Updated description");
    }

    public UpdateDocumentRequest Build() => _request;
}

public class DocumentDtoBuilder
{
    private readonly DocumentDto _dto = new();

    public DocumentDtoBuilder WithId(int id)
    {
        _dto.Id = id;
        return this;
    }

    public DocumentDtoBuilder WithTitle(string title)
    {
        _dto.Title = title;
        return this;
    }

    public DocumentDtoBuilder WithDescription(string description)
    {
        _dto.Description = description;
        return this;
    }

    public DocumentDtoBuilder WithOwner(string ownerId, string ownerName)
    {
        _dto.OwnerId = ownerId;
        _dto.OwnerName = ownerName;
        return this;
    }

    public DocumentDtoBuilder WithFile(string fileType, long fileSize)
    {
        _dto.FileType = fileType;
        _dto.FileSize = fileSize;
        return this;
    }

    public DocumentDtoBuilder WithValidData()
    {
        return WithId(1)
               .WithTitle("Test Document")
               .WithDescription("Test description")
               .WithOwner("user123", "Test User")
               .WithFile(".pdf", 1024);
    }

    public DocumentDto Build() => _dto;
}

public class IFormFileBuilder
{
    private string _fileName = "test.txt";
    private string _content = "Test content";
    private string _contentType = "text/plain";

    public IFormFileBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    public IFormFileBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    public IFormFileBuilder WithContentType(string contentType)
    {
        _contentType = contentType;
        return this;
    }

    public IFormFileBuilder AsPdf()
    {
        return WithFileName("test.pdf")
               .WithContentType("application/pdf");
    }

    public IFormFileBuilder AsDocx()
    {
        return WithFileName("test.docx")
               .WithContentType("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    }

    public IFormFileBuilder AsInvalidType()
    {
        return WithFileName("test.exe")
               .WithContentType("application/x-msdownload");
    }

    public IFormFile Build()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(_content));
        return new FormFile(stream, 0, stream.Length, "test", _fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = _contentType
        };
    }
}

// Team Builders
public class CreateTeamRequestBuilder
{
    private readonly CreateTeamRequest _request = new();

    public CreateTeamRequestBuilder WithName(string name)
    {
        _request.Name = name;
        return this;
    }

    public CreateTeamRequestBuilder WithDescription(string description)
    {
        _request.Description = description;
        return this;
    }

    public CreateTeamRequestBuilder WithValidData()
    {
        return WithName("Test Team")
               .WithDescription("Test team description");
    }

    public CreateTeamRequest Build() => _request;
}

public class TeamDtoBuilder
{
    private readonly TeamDto _dto = new();

    public TeamDtoBuilder WithId(int id)
    {
        _dto.Id = id;
        return this;
    }

    public TeamDtoBuilder WithName(string name)
    {
        _dto.Name = name;
        return this;
    }

    public TeamDtoBuilder WithDescription(string description)
    {
        _dto.Description = description;
        return this;
    }

    public TeamDtoBuilder WithOwner(string ownerId)
    {
        _dto.OwnerId = ownerId;
        return this;
    }

    public TeamDtoBuilder WithValidData()
    {
        return WithId(1)
               .WithName("Test Team")
               .WithDescription("Test description")
               .WithOwner("user123");
    }

    public TeamDto Build() => _dto;
}

public class TeamMemberDtoBuilder
{
    private readonly TeamMemberDto _dto = new();

    public TeamMemberDtoBuilder WithUserId(string userId)
    {
        _dto.UserId = userId;
        return this;
    }

    public TeamMemberDtoBuilder WithTeamId(int teamId)
    {
        _dto.TeamId = teamId;
        return this;
    }

    public TeamMemberDtoBuilder WithRole(TeamRole role)
    {
        _dto.Role = role;
        return this;
    }
    
    public TeamMemberDtoBuilder WithRole(string role)
    {
        _dto.Role = Enum.Parse<TeamRole>(role);
        return this;
    }

    public TeamMemberDtoBuilder WithValidData()
    {
        return WithUserId("user456")
               .WithTeamId(1)
               .WithRole("Member");
    }

    public TeamMemberDto Build() => _dto;
} 