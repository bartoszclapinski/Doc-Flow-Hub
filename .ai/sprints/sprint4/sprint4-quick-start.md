# Sprint 4 Quick Start Guide - Team Management Features

## 🎯 Sprint 4 Goal: Complete Team Management System

### Current Status: Sprint 3 ✅ COMPLETED
- Document management fully implemented end-to-end ✅
- Professional UX with loading states and animations ✅
- Dynamic welcome page with real user data ✅
- All core document workflows working ✅

### ✅ SPRINT 4 MAJOR PROGRESS: Document-Team Integration COMPLETED
- **Security Fix**: Resolved critical issue where users could see other users' documents ✅
- **Team Document Filtering**: Users can filter documents by team membership ✅
- **Document Sharing**: Document owners can share/unshare with teams ✅
- **Navigation Enhancement**: Team breadcrumbs and statistics implemented ✅
- **Team UI Pages**: All team management pages exist and ready for testing ✅

### Sprint 4 Remaining Tasks: Core Team Functionality Verification

### Sprint 4 Priority Tasks: Team Collaboration Features

## 🏗️ Foundation Status ✅ READY

### Backend Infrastructure ✅ PREPARED
All team foundations are already in place:
- **Team Models**: `Team.cs` and `TeamMember.cs` entities exist ✅
- **Database Schema**: All team tables and relationships configured ✅
- **Service Interface**: `ITeamService` completely defined ✅
- **Document Integration**: Services already support team sharing ✅

### Frontend Infrastructure ✅ READY
- **Bootstrap 5.3**: Professional UI framework established ✅
- **UX Patterns**: Loading states, toast notifications ready ✅
- **Page Layouts**: Responsive design patterns established ✅
- **Navigation**: Layout structure ready for team navigation ✅

## 📋 Week 1: TeamService Implementation (HIGH PRIORITY)

### Task 1: Complete TeamService Class
**File**: `src/DocFlowHub.Infrastructure/Services/Teams/TeamService.cs`

#### Key Methods to Implement:
```csharp
public class TeamService : ITeamService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TeamService> _logger;

    // Core CRUD Operations
    public async Task<ServiceResult<TeamDto>> CreateTeamAsync(CreateTeamRequest request, string ownerId)
    public async Task<ServiceResult<TeamDto>> GetTeamByIdAsync(int id, string userId)
    public async Task<ServiceResult<PagedResult<TeamDto>>> GetUserTeamsAsync(string userId, TeamFilter filter)
    public async Task<ServiceResult<TeamDto>> UpdateTeamAsync(int id, UpdateTeamRequest request, string userId)
    public async Task<ServiceResult> DeleteTeamAsync(int id, string userId)

    // Member Management
    public async Task<ServiceResult<TeamMemberDto>> AddMemberAsync(int teamId, AddTeamMemberRequest request, string userId)
    public async Task<ServiceResult> RemoveMemberAsync(int teamId, string memberUserId, string userId)
    public async Task<ServiceResult<TeamMemberDto>> UpdateMemberRoleAsync(int teamId, string memberUserId, TeamRole role, string userId)
    public async Task<ServiceResult<PagedResult<TeamMemberDto>>> GetTeamMembersAsync(int teamId, TeamMemberFilter filter, string userId)

    // Invitation System
    public async Task<ServiceResult<IEnumerable<TeamDto>>> GetUserInvitationsAsync(string userId)
    public async Task<ServiceResult> AcceptInvitationAsync(int teamId, string userId)
    public async Task<ServiceResult> DeclineInvitationAsync(int teamId, string userId)
}
```

#### Implementation Guidelines:
1. **Permission Validation**: Always check if user is team owner for management operations
2. **Error Handling**: Use ServiceResult pattern for consistent error responses
3. **Async Patterns**: Use async/await throughout with proper ConfigureAwait(false)
4. **Transaction Handling**: Use database transactions for multi-step operations
5. **Logging**: Add comprehensive logging for debugging and monitoring

### Task 2: Register TeamService
**File**: `src/DocFlowHub.Infrastructure/DependencyInjection.cs`
```csharp
services.AddScoped<ITeamService, TeamService>();
```

## 📋 Week 2: Team UI Pages (MEDIUM PRIORITY)

### Task 1: Team Listing Page
**Files**: 
- `src/DocFlowHub.Web/Pages/Teams/Index.cshtml`
- `src/DocFlowHub.Web/Pages/Teams/Index.cshtml.cs`

#### Page Model Structure:
```csharp
[Authorize]
public class IndexModel : PageModel
{
    private readonly ITeamService _teamService;
    
    public PagedResult<TeamDto> Teams { get; set; } = new();
    public string? ErrorMessage { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public TeamFilter Filter { get; set; } = new();
    
    public async Task OnGetAsync()
    {
        var userId = User.GetUserId();
        var result = await _teamService.GetUserTeamsAsync(userId, Filter);
        
        if (result.Succeeded)
        {
            Teams = result.Data;
        }
        else
        {
            ErrorMessage = result.Error;
        }
    }
}
```

#### UI Features:
- Card-based layout showing team information
- Member count and recent activity
- "Create Team" button
- Filter by role (Owner/Member)
- Pagination support

### Task 2: Team Creation Page
**Files**:
- `src/DocFlowHub.Web/Pages/Teams/Create.cshtml`
- `src/DocFlowHub.Web/Pages/Teams/Create.cshtml.cs`

#### Page Model Structure:
```csharp
[Authorize]
public class CreateModel : PageModel
{
    private readonly ITeamService _teamService;
    
    [BindProperty]
    public CreateTeamRequest Team { get; set; } = new();
    
    public string? ErrorMessage { get; set; }
    
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        
        var userId = User.GetUserId();
        var result = await _teamService.CreateTeamAsync(Team, userId);
        
        if (result.Succeeded)
        {
            return RedirectToPage("Details", new { id = result.Data.Id });
        }
        
        ErrorMessage = result.Error;
        return Page();
    }
}
```

### Task 3: Team Details and Member Management
**Files**:
- `src/DocFlowHub.Web/Pages/Teams/Details.cshtml`  
- `src/DocFlowHub.Web/Pages/Teams/Details.cshtml.cs`

#### Key Features:
- Team information display
- Member list with roles
- Add member functionality (email invitation)
- Remove member buttons (owner only)
- Role management (promote/demote)
- Team document listing

## 📋 Week 3: Document Integration (MEDIUM PRIORITY)

### Task 1: Enhance Document Upload
**File**: `src/DocFlowHub.Web/Pages/Documents/Upload.cshtml`

#### Add Team Selection:
```csharp
// In UploadModel
public IEnumerable<TeamDto> UserTeams { get; set; } = new List<TeamDto>();

[BindProperty]
public int? TeamId { get; set; }

public async Task OnGetAsync()
{
    // Load user teams
    var teamsResult = await _teamService.GetUserTeamsAsync(User.GetUserId(), new TeamFilter());
    if (teamsResult.Succeeded)
    {
        UserTeams = teamsResult.Data.Items;
    }
    
    // ... existing code
}
```

#### UI Enhancement:
```html
<div class="mb-3">
    <label for="teamSelect" class="form-label">Share with Team (Optional)</label>
    <select asp-for="TeamId" class="form-select" id="teamSelect">
        <option value="">Private Document</option>
        @foreach (var team in Model.UserTeams)
        {
            <option value="@team.Id">@team.Name</option>
        }
    </select>
</div>
```

### Task 2: Enhance Document Listing
**File**: `src/DocFlowHub.Web/Pages/Documents/Index.cshtml`

#### Add Team Filters:
- Team filter dropdown
- Show team name in document cards
- Filter documents by team membership
- Team-specific document views

### Task 3: Enhance Document Details
**File**: `src/DocFlowHub.Web/Pages/Documents/Details.cshtml`

#### Show Team Information:
- Display team name if document is shared
- Show team member access information
- Team sharing/unsharing buttons

## 🎯 Quick Implementation Tips

### Service Implementation Pattern:
```csharp
public async Task<ServiceResult<TeamDto>> CreateTeamAsync(CreateTeamRequest request, string ownerId)
{
    try
    {
        // 1. Validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult<TeamDto>.Failure("Team name is required");
        }
        
        // 2. Check for duplicate names (optional)
        var existingTeam = await _context.Teams
            .FirstOrDefaultAsync(t => t.Name == request.Name && t.OwnerId == ownerId);
        if (existingTeam != null)
        {
            return ServiceResult<TeamDto>.Failure("Team name already exists");
        }
        
        // 3. Create team
        var team = new Team
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();
        
        // 4. Convert to DTO and return
        var teamDto = team.ToDto();
        return ServiceResult<TeamDto>.Success(teamDto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating team for user {UserId}", ownerId);
        return ServiceResult<TeamDto>.Failure("An error occurred while creating the team");
    }
}
```

### Permission Validation Helper:
```csharp
private async Task<bool> IsTeamOwnerAsync(int teamId, string userId)
{
    return await _context.Teams
        .AnyAsync(t => t.Id == teamId && t.OwnerId == userId);
}

private async Task<bool> IsTeamMemberAsync(int teamId, string userId)
{
    return await _context.TeamMembers
        .AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId) ||
           await IsTeamOwnerAsync(teamId, userId);
}
```

## ✅ Success Criteria for Sprint 4

### User Stories to Complete:
- [ ] Users can create teams and become owners
- [ ] Team owners can invite members by email  
- [ ] Users can accept/decline team invitations
- [ ] Team members can view shared team documents
- [ ] Users can share documents with their teams
- [ ] Team owners can manage member roles and permissions

### Technical Goals:
- [ ] TeamService fully implemented with all methods
- [ ] Team UI pages responsive and follow design patterns
- [ ] Team functionality integrated with document system
- [ ] Proper authorization and security measures
- [ ] Unit tests for team service (90%+ coverage)
- [ ] Integration tests for major workflows

## 🚀 Quick Start Commands

### Run the Application:
```bash
cd src/DocFlowHub.Web
dotnet run
```

### Access Team Features:
- Navigate to `/Teams` for team listing
- Use `/Teams/Create` for team creation
- Access `/Teams/Details/{id}` for team management

### Testing:
```bash
cd tests/DocFlowHub.Tests
dotnet test --filter "TeamService"
```

## 📝 Implementation Notes

### Database Considerations ✅
- All team tables already exist
- No new migrations required
- Relationships properly configured

### UI/UX Considerations:
- Follow established Bootstrap patterns
- Use existing loading states and toast notifications
- Maintain mobile responsiveness
- Consistent error handling

### Security Considerations:
- Validate team ownership for management operations
- Ensure proper access control for team documents
- Secure invitation system with token validation

This sprint builds upon the solid foundation established in Sprints 1-3, focusing purely on team collaboration features without requiring any infrastructure changes. 