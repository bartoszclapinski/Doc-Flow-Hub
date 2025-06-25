# Sprint 5: AI-Powered Features - Quick Start Guide

## 🚀 Sprint 5 Overview
**Goal**: Transform DocFlowHub into an AI-powered intelligent document management platform  
**Duration**: 3 weeks  
**Core Features**: AI Summarization, Version Analysis, Smart Categorization, Document Locking, Project Organization  

## ⚡ Quick Setup Checklist

### Prerequisites ✅
- [ ] .NET 9.0 SDK installed
- [ ] DocFlowHub Sprint 4 complete and tested
- [ ] OpenAI API account and key ready
- [ ] Redis server available (local or cloud)
- [ ] SQL Server with existing DocFlowHub database

### Environment Setup
```bash
# 1. Navigate to project root
cd /path/to/DocFlowHub

# 2. Check current branch (should be feature/sprint4-team-management)
git branch

# 3. Create Sprint 5 branch
git checkout -b feature/sprint5-ai-features

# 4. Verify application runs
cd src/DocFlowHub.Web
dotnet run
```

## 📦 New Dependencies (Week 1)

### Add NuGet Packages
```bash
# Navigate to Core project
cd src/DocFlowHub.Core
dotnet add package Microsoft.Extensions.Http --version 8.0.0

# Navigate to Infrastructure project  
cd ../DocFlowHub.Infrastructure
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis --version 8.0.0
dotnet add package Microsoft.Extensions.Caching.Memory --version 8.0.0
dotnet add package iTextSharp --version 5.5.13.3
dotnet add package DocumentFormat.OpenXml --version 3.0.1
dotnet add package Markdig --version 0.35.0

# Navigate to Web project
cd ../DocFlowHub.Web  
dotnet add package Microsoft.AspNetCore.SignalR --version 1.1.0
dotnet add package Hangfire.AspNetCore --version 1.8.6
dotnet add package Hangfire.SqlServer --version 1.8.6
```

### Configuration Updates
```json
// Add to appsettings.Development.json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here",
    "BaseUrl": "https://api.openai.com/v1",
    "Model": "gpt-4-turbo-preview",
    "MaxTokens": 4000,
    "Temperature": 0.3,
    "TimeoutSeconds": 30
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "DefaultDatabase": 0
  },
  "DocumentLocking": {
    "LockTimeoutMinutes": 30,
    "CleanupIntervalMinutes": 5,
    "MaxLocksPerUser": 5
  },
  "AI": {
    "EnableSummarization": true,
    "EnableVersionComparison": true,
    "EnableSmartCategorization": true,
    "CacheExpirationHours": 24
  }
}
```

## 🗃️ Week 1: Database Schema (Day 1)

### Create Migration for AI Features
```bash
# Navigate to Infrastructure project
cd src/DocFlowHub.Infrastructure

# Create new migration
dotnet ef migrations add AddAIFeatures -s ../DocFlowHub.Web

# Review migration file before applying
# Apply migration
dotnet ef database update -s ../DocFlowHub.Web
```

### SQL Schema for Reference
```sql
-- Document Summaries
CREATE TABLE DocumentSummaries (
    Id int IDENTITY(1,1) PRIMARY KEY,
    DocumentId int NOT NULL,
    Summary nvarchar(max) NOT NULL,
    KeyPoints nvarchar(max) NULL,
    GeneratedAt datetime2 NOT NULL,
    AIModel nvarchar(100) NOT NULL,
    TokensUsed int NOT NULL,
    IsDeleted bit NOT NULL DEFAULT 0,
    FOREIGN KEY (DocumentId) REFERENCES Documents(Id)
);

-- Version Comparisons  
CREATE TABLE VersionComparisons (
    Id int IDENTITY(1,1) PRIMARY KEY,
    FromVersionId int NOT NULL,
    ToVersionId int NOT NULL,
    ChangeSummary nvarchar(max) NOT NULL,
    DetailedChanges nvarchar(max) NOT NULL,
    GeneratedAt datetime2 NOT NULL,
    TokensUsed int NOT NULL,
    FOREIGN KEY (FromVersionId) REFERENCES DocumentVersions(Id),
    FOREIGN KEY (ToVersionId) REFERENCES DocumentVersions(Id)
);

-- Category Suggestions
CREATE TABLE CategorySuggestions (
    Id int IDENTITY(1,1) PRIMARY KEY,
    DocumentId int NOT NULL,
    SuggestedCategoryId int NOT NULL,
    Confidence decimal(3,2) NOT NULL,
    Reason nvarchar(500) NULL,
    AcceptedByUser bit NULL,
    GeneratedAt datetime2 NOT NULL,
    FOREIGN KEY (DocumentId) REFERENCES Documents(Id),
    FOREIGN KEY (SuggestedCategoryId) REFERENCES DocumentCategories(Id)
);

-- Document Locks
CREATE TABLE DocumentLocks (
    Id int IDENTITY(1,1) PRIMARY KEY,
    DocumentId int NOT NULL,
    UserId nvarchar(450) NOT NULL,
    LockedAt datetime2 NOT NULL,
    ExpiresAt datetime2 NOT NULL,
    LockType nvarchar(50) NOT NULL,
    SessionId nvarchar(100) NOT NULL,
    FOREIGN KEY (DocumentId) REFERENCES Documents(Id),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
);

-- Projects
CREATE TABLE Projects (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Description nvarchar(1000) NULL,
    OwnerId nvarchar(450) NOT NULL,
    CreatedAt datetime2 NOT NULL,
    UpdatedAt datetime2 NULL,
    IsDeleted bit NOT NULL DEFAULT 0,
    FOREIGN KEY (OwnerId) REFERENCES AspNetUsers(Id)
);

-- Project Folders
CREATE TABLE ProjectFolders (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ProjectId int NOT NULL,
    ParentFolderId int NULL,
    Name nvarchar(200) NOT NULL,
    Path nvarchar(1000) NOT NULL,
    CreatedAt datetime2 NOT NULL,
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id),
    FOREIGN KEY (ParentFolderId) REFERENCES ProjectFolders(Id)
);

-- Update Documents table
ALTER TABLE Documents ADD ProjectId int NULL;
ALTER TABLE Documents ADD FolderId int NULL;
ALTER TABLE Documents ADD CONSTRAINT FK_Documents_Projects FOREIGN KEY (ProjectId) REFERENCES Projects(Id);
ALTER TABLE Documents ADD CONSTRAINT FK_Documents_ProjectFolders FOREIGN KEY (FolderId) REFERENCES ProjectFolders(Id);
```

## 🏗️ Week 1: Core AI Service (Days 2-3)

### File Structure Setup
```bash
# Create AI service directories
mkdir -p src/DocFlowHub.Core/Models/AI
mkdir -p src/DocFlowHub.Core/Services/Interfaces
mkdir -p src/DocFlowHub.Infrastructure/Services/AI
mkdir -p src/DocFlowHub.Web/Pages/AI
```

### Key Files to Create (Day 2-3)

#### 1. Core Models (src/DocFlowHub.Core/Models/AI/)
- `AIResponse.cs` - Base AI response model
- `DocumentSummary.cs` - Document summary entity
- `VersionComparison.cs` - Version comparison entity
- `CategorySuggestion.cs` - Category suggestion entity

#### 2. Service Interfaces (src/DocFlowHub.Core/Services/Interfaces/)
- `IAIService.cs` - Core AI service interface
- `IDocumentSummaryService.cs` - Document summarization interface
- `IVersionComparisonService.cs` - Version comparison interface
- `IDocumentLockingService.cs` - Document locking interface

#### 3. Service Implementations (src/DocFlowHub.Infrastructure/Services/AI/)
- `OpenAIService.cs` - OpenAI API integration
- `DocumentSummaryService.cs` - Summarization logic
- `TextExtractionService.cs` - Document text extraction
- `CacheService.cs` - AI response caching

### Quick Implementation Template
```csharp
// src/DocFlowHub.Core/Services/Interfaces/IAIService.cs
public interface IAIService
{
    Task<string> GenerateSummaryAsync(string content, string documentType);
    Task<string> CompareVersionsAsync(string oldContent, string newContent);
    Task<IEnumerable<string>> SuggestCategoriesAsync(string content);
}

// src/DocFlowHub.Infrastructure/Services/AI/OpenAIService.cs
public class OpenAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    // Implementation here...
}
```

## 🧪 Week 1: Testing Setup (Days 4-5)

### Test Project Structure
```bash
# Create AI test directories
mkdir -p tests/DocFlowHub.Tests/AI
mkdir -p tests/DocFlowHub.Tests/Mocks
```

### Key Test Files
- `OpenAIServiceTests.cs` - AI service unit tests
- `DocumentSummaryServiceTests.cs` - Summarization tests
- `MockAIService.cs` - Mock AI service for testing
- `AIIntegrationTests.cs` - Integration tests with real API

### Test Configuration
```json
// tests/DocFlowHub.Tests/appsettings.Test.json
{
  "OpenAI": {
    "ApiKey": "test-key",
    "BaseUrl": "https://api.openai.com/v1",
    "Model": "gpt-3.5-turbo",
    "UseMockService": true
  }
}
```

## 📋 Daily Progress Tracking

### Week 1 Daily Goals
- **Day 1**: Database migration, schema setup, environment configuration
- **Day 2**: Core AI models, interfaces, basic OpenAI service
- **Day 3**: Document text extraction, summarization service
- **Day 4**: Caching implementation, error handling
- **Day 5**: Unit tests, integration tests, performance optimization

### Week 1 Success Criteria
- [ ] OpenAI API integration working
- [ ] Document text extraction from PDF, DOCX, TXT, MD
- [ ] Basic summarization generating results
- [ ] Redis caching operational
- [ ] Unit tests passing
- [ ] No existing functionality broken

## 🔧 Development Commands

### Useful Commands During Development
```bash
# Run specific tests
dotnet test tests/DocFlowHub.Tests/AI/

# Run with AI integration tests
dotnet test --filter "Category=AI"

# Check Redis connection
redis-cli ping

# Monitor AI service logs
dotnet run --environment Development | grep "AI"

# Database migration rollback if needed
dotnet ef database update PreviousMigrationName -s src/DocFlowHub.Web
```

### Debugging AI Services
```bash
# Test OpenAI connection
curl -H "Authorization: Bearer YOUR_API_KEY" \
     -H "Content-Type: application/json" \
     -d '{"model":"gpt-3.5-turbo","messages":[{"role":"user","content":"Hello"}]}' \
     https://api.openai.com/v1/chat/completions

# Check Redis
redis-cli info replication
```

## 🚨 Common Issues & Solutions

### Week 1 Troubleshooting

#### OpenAI API Issues
```csharp
// Add retry logic for API calls
services.AddHttpClient<IAIService, OpenAIService>()
    .AddPolicyHandler(GetRetryPolicy());

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
```

#### Redis Connection Issues
```bash
# Local Redis setup (Windows)
# Download and install Redis for Windows
# Start Redis server: redis-server

# Docker alternative
docker run -d -p 6379:6379 redis:alpine
```

#### Text Extraction Issues
```csharp
// Handle different file encodings
public static string ExtractText(Stream fileStream, string fileExtension)
{
    try
    {
        return fileExtension.ToLower() switch
        {
            ".pdf" => ExtractFromPdf(fileStream),
            ".docx" => ExtractFromDocx(fileStream),
            ".txt" => ExtractFromText(fileStream),
            ".md" => ExtractFromMarkdown(fileStream),
            _ => throw new NotSupportedException($"File type {fileExtension} not supported")
        };
    }
    catch (Exception ex)
    {
        // Log error and return empty string or fallback
        return string.Empty;
    }
}
```

## ✅ Week 1 Completion Checklist

### Before Moving to Week 2
- [ ] All new NuGet packages installed successfully
- [ ] Database migration applied without errors
- [ ] OpenAI API key configured and tested
- [ ] Redis connection working
- [ ] Basic document summarization working
- [ ] Text extraction working for all supported file types
- [ ] Unit tests passing (>80% coverage for new code)
- [ ] No regressions in existing functionality
- [ ] Performance acceptable (summaries < 10 seconds)
- [ ] Error handling implemented and tested

### Week 1 Deliverables
1. **AI Service Foundation**: OpenAI integration with proper error handling
2. **Document Summarization**: Working end-to-end summarization pipeline
3. **Text Processing**: Reliable text extraction from all supported formats
4. **Caching System**: Redis-based caching for AI responses
5. **Testing Framework**: Comprehensive unit and integration tests
6. **Database Schema**: All new tables and relationships created

**Ready for Week 2**: Version Analysis & Smart Categorization features

## 🔄 Week 2 Preview

### Week 2 Focus Areas
1. **Version Comparison**: AI-powered diff analysis between document versions
2. **Visual Diff UI**: Side-by-side comparison with highlighting
3. **Smart Categorization**: Content-based category suggestions
4. **Category Learning**: User feedback integration for improved suggestions

### Week 2 Preparation
- Ensure Week 1 AI foundation is solid
- Review existing version management code
- Prepare category management enhancement designs
- Plan user interface for comparison views

**Sprint 5 Goal**: Transform DocFlowHub into an intelligent document platform with cutting-edge AI features that provide real value to users. 