# Sprint 4: Team Management Features - Detailed Plan

## Team Management Implementation

### 1. Complete TeamService Implementation ✅ EXISTS - NEEDS VERIFICATION
- **Location**: `src/DocFlowHub.Infrastructure/Services/Teams/TeamService.cs` ✅ EXISTS
- **Dependencies**: Team and TeamMember models already exist ✅
- **Database**: Team relationships already configured ✅
- **Status**: Implementation exists, needs functional verification

#### Implementation Tasks:
```csharp
public class TeamService : ITeamService
{
    // Implement all methods from ITeamService interface
    Task<ServiceResult<TeamDto>> CreateTeamAsync(CreateTeamRequest request, string ownerId);
    Task<ServiceResult<TeamDto>> GetTeamByIdAsync(int id, string userId);
    Task<ServiceResult<PagedResult<TeamDto>>> GetUserTeamsAsync(string userId, TeamFilter filter);
    Task<ServiceResult<TeamDto>> UpdateTeamAsync(int id, UpdateTeamRequest request, string userId);
    Task<ServiceResult> DeleteTeamAsync(int id, string userId);
    Task<ServiceResult<TeamMemberDto>> AddMemberAsync(int teamId, AddTeamMemberRequest request, string userId);
    Task<ServiceResult> RemoveMemberAsync(int teamId, string memberUserId, string userId);
    Task<ServiceResult<TeamMemberDto>> UpdateMemberRoleAsync(int teamId, string memberUserId, TeamRole role, string userId);
    Task<ServiceResult<PagedResult<TeamMemberDto>>> GetTeamMembersAsync(int teamId, TeamMemberFilter filter, string userId);
    Task<ServiceResult<IEnumerable<TeamDto>>> GetUserInvitationsAsync(string userId);
    Task<ServiceResult> AcceptInvitationAsync(int teamId, string userId);
    Task<ServiceResult> DeclineInvitationAsync(int teamId, string userId);
}
```

#### Key Features to Implement:
- ✅ Team creation with proper ownership
- ✅ Team member management (add, remove, role updates)
- ✅ Team permission validation (only owners can manage)
- ✅ Team invitation system
- ✅ Proper error handling and validation
- ✅ Async operations with proper transaction handling

### 2. ✅ Team UI Pages Implementation COMPLETED
**Required Pages Structure**: ✅ ALL PAGES EXIST
```
src/DocFlowHub.Web/Pages/Teams/
├── Index.cshtml ✅ - Team listing COMPLETE
├── Index.cshtml.cs ✅ - Team listing page model COMPLETE
├── Create.cshtml ✅ - Team creation COMPLETE
├── Create.cshtml.cs ✅ - Team creation page model COMPLETE
├── Details.cshtml ✅ - Team details and member management COMPLETE
├── Details.cshtml.cs ✅ - Team details page model COMPLETE
└── Edit.cshtml ✅ - Team editing COMPLETE
```

#### Page Implementation Details:

**Teams/Index.cshtml** - Team Listing
- Display user's owned teams and member teams
- Show team statistics (member count, document count)
- Create new team button
- Filter teams by role (Owner/Member)
- Responsive card-based layout with Bootstrap
- Pagination support

**Teams/Create.cshtml** - Team Creation
- Team name and description form
- Form validation (required fields, unique names)
- Success/error message handling
- Redirect to team details after creation

**Teams/Details.cshtml** - Team Management
- Team information display
- Member list with roles and actions
- Add member functionality (invite by email)
- Remove member functionality (owner only)
- Role management (promote/demote members)
- Team document listing
- Team settings and edit options

**Teams/Join.cshtml** - Invitation Handling
- Display team invitation details
- Accept/decline invitation buttons
- Show team information before joining
- Handle invitation validation

### 3. ✅ Team Integration with Document System COMPLETED

#### ✅ Document Upload Enhancement COMPLETED
- **File**: `src/DocFlowHub.Web/Pages/Documents/Upload.cshtml` ✅ Team selection exists
- **Enhancement**: Team selection dropdown working ✅

#### ✅ Document Listing Enhancement COMPLETED  
- **File**: `src/DocFlowHub.Web/Pages/Documents/Index.cshtml` ✅ Team filters implemented
- **Enhancement**: Team filter options working ✅
- **Implementation**: ✅ Team filter dropdown, team names in cards, team-specific filtering

#### ✅ Document Details Enhancement COMPLETED
- **File**: `src/DocFlowHub.Web/Pages/Documents/Details.cshtml` ✅ Team sharing implemented
- **Enhancement**: Team sharing/unsharing functionality working ✅
- **Implementation**: ✅ Team sharing UI, unsharing buttons, team context display

### 4. ✅ Team Permission System IMPLEMENTED

#### Permission Implementation:
```csharp
public enum TeamRole
{
    Member = 0,
    Admin = 1
}

// Permission validation in services
private async Task<bool> CanManageTeamAsync(int teamId, string userId)
{
    var team = await _context.Teams
        .FirstOrDefaultAsync(t => t.Id == teamId && t.OwnerId == userId);
    return team != null;
}

private async Task<bool> IsMemberOfTeamAsync(int teamId, string userId)
{
    return await _context.TeamMembers
        .AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
}
```

#### Document Access Control:
- Team members can view team documents
- Team owners can manage team document sharing
- Document owners can share/unshare from teams
- Proper authorization attributes on pages

### 5. Team Dashboard and Statistics ⏳ PENDING

#### Welcome Page Enhancement
- **File**: `src/DocFlowHub.Web/Pages/Index.cshtml`
- **Enhancement**: Team statistics
- **Implementation**:
  ```csharp
  public class IndexModel : PageModel
  {
      public int TotalTeams { get; set; }
      public int TeamsAsOwner { get; set; }
      public int TeamsAsMember { get; set; }
      public int SharedDocuments { get; set; }
      public List<TeamSummary> RecentTeamActivity { get; set; } = new();
  }
  ```

#### Team Activity Feed
- Recent team document uploads
- New team member joins
- Team document shares/unshares
- Team creation and updates

### 6. Navigation and User Experience ⏳ PENDING

#### Layout Enhancement
- **File**: `src/DocFlowHub.Web/Pages/Shared/_Layout.cshtml`
- **Enhancement**: Teams navigation
- **Implementation**:
  ```html
  <li class="nav-item dropdown">
      <a class="nav-link dropdown-toggle" href="#" id="teamsDropdown" role="button" data-bs-toggle="dropdown">
          <i class="fas fa-users me-1"></i> Teams
      </a>
      <ul class="dropdown-menu">
          <li><a class="dropdown-item" href="/Teams">My Teams</a></li>
          <li><a class="dropdown-item" href="/Teams/Create">Create Team</a></li>
          <li><hr class="dropdown-divider"></li>
          <li><a class="dropdown-item" href="/Teams/Join">Join Team</a></li>
      </ul>
  </li>
  ```

#### Breadcrumb Navigation
- Implement breadcrumbs for team pages
- Show team context in document pages
- Clear navigation between teams and documents

### 7. Email Notification System ⏳ PENDING

#### Team Invitation Emails
- **Implementation Location**: `src/DocFlowHub.Infrastructure/Services/Email/`
- **Required Features**:
  - Send invitation emails with team details
  - Include accept/decline links
  - Email templates for different scenarios
  - Track invitation status

#### Email Service Interface:
```csharp
public interface IEmailService
{
    Task<ServiceResult> SendTeamInvitationAsync(string email, TeamDto team, string inviteUrl);
    Task<ServiceResult> SendTeamWelcomeAsync(string email, TeamDto team);
    Task<ServiceResult> SendDocumentSharedNotificationAsync(string email, DocumentDto document, TeamDto team);
}
```

### 8. Testing Implementation ⏳ PENDING

#### Unit Tests for Team Service
- **Location**: `tests/DocFlowHub.Tests/Services/TeamServiceTests.cs`
- **Test Cases**:
  ```csharp
  [Fact]
  public async Task CreateTeamAsync_WithValidData_ShouldCreateTeam()
  
  [Fact]
  public async Task AddMemberAsync_AsOwner_ShouldAddMember()
  
  [Fact]
  public async Task AddMemberAsync_AsNonOwner_ShouldReturnUnauthorized()
  
  [Fact]
  public async Task GetUserTeamsAsync_ShouldReturnOwnerAndMemberTeams()
  ```

#### Integration Tests
- **Location**: `tests/DocFlowHub.Tests/Integration/TeamIntegrationTests.cs`
- **Test Scenarios**:
  - Complete team creation workflow
  - Team member management workflow
  - Document sharing with teams workflow
  - Team invitation and acceptance workflow

### 9. Database Considerations ✅ ALREADY CONFIGURED

#### Existing Schema (Ready):
```sql
-- Teams table already exists with proper relationships
CREATE TABLE Teams (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    Description nvarchar(500),
    CreatedAt datetime2 NOT NULL,
    UpdatedAt datetime2,
    OwnerId nvarchar(450) NOT NULL,
    FOREIGN KEY (OwnerId) REFERENCES AspNetUsers(Id)
);

-- TeamMembers table already exists
CREATE TABLE TeamMembers (
    Id int IDENTITY(1,1) PRIMARY KEY,
    TeamId int NOT NULL,
    UserId nvarchar(450) NOT NULL,
    Role int NOT NULL,
    JoinedAt datetime2 NOT NULL,
    FOREIGN KEY (TeamId) REFERENCES Teams(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
);

-- Documents.TeamId already configured
ALTER TABLE Documents ADD TeamId int;
ALTER TABLE Documents ADD FOREIGN KEY (TeamId) REFERENCES Teams(Id);
```

#### No Migration Required ✅
- All team-related tables already exist
- All relationships properly configured
- All foreign keys and constraints in place

## Implementation Timeline

### Week 1: Core Team Service Implementation
1. **Day 1-2**: Complete TeamService implementation
   - Implement all CRUD operations
   - Add proper validation and error handling
   - Implement permission checks
   - Write unit tests

2. **Day 3-4**: Team invitation system
   - Implement invitation workflow
   - Add email notification service
   - Create invitation acceptance logic
   - Test invitation flow

3. **Day 5**: Integration testing
   - Test team service with database
   - Verify all team operations work
   - Performance testing

### Week 2: Team UI Implementation
1. **Day 1-2**: Basic team pages
   - Create Teams/Index.cshtml (team listing)
   - Create Teams/Create.cshtml (team creation)
   - Implement basic team management

2. **Day 3-4**: Team details and member management
   - Create Teams/Details.cshtml
   - Implement member management UI
   - Add role management functionality

3. **Day 5**: Team invitation UI
   - Create Teams/Join.cshtml
   - Implement invitation acceptance flow
   - Add email invitation features

### Week 3: Document Integration and Polish
1. **Day 1-2**: Document-team integration
   - Enhance document upload with team selection
   - Update document listing with team filters
   - Modify document details to show team info

2. **Day 3-4**: Dashboard and navigation
   - Update welcome page with team statistics
   - Enhance navigation with team links
   - Add team activity feeds

3. **Day 5**: Testing and refinement
   - Complete integration testing
   - UI/UX improvements
   - Bug fixes and optimization

## Success Criteria for Sprint 4

### Functional Requirements ✅
- [ ] Users can create teams and become owners
- [ ] Team owners can invite members by email
- [ ] Users can accept/decline team invitations
- [ ] Team members can view shared team documents
- [ ] Users can share documents with their teams
- [ ] Team owners can manage member roles and permissions
- [ ] Users can view their team memberships and activity

### Technical Requirements ✅
- [ ] TeamService fully implemented with all methods
- [ ] Team UI pages follow established patterns
- [ ] Team functionality integrated with document system
- [ ] Email notification system working
- [ ] Proper authorization and security measures
- [ ] Unit and integration tests with 90%+ coverage
- [ ] Mobile-responsive team management interface

### User Experience Requirements ✅
- [ ] Intuitive team creation and management workflow
- [ ] Clear team membership and role indicators
- [ ] Seamless document sharing with teams
- [ ] Professional email invitations
- [ ] Responsive design on all devices
- [ ] Loading states and error handling
- [ ] Consistent with existing application design

## Definition of Done for Sprint 4

- All team management features implemented and tested
- Team-document integration working seamlessly
- Email notification system operational
- All UI pages responsive and accessible
- Unit tests achieve 90%+ code coverage
- Integration tests cover all major workflows
- Documentation updated with team features
- No critical bugs or security issues
- Performance meets established benchmarks
- Ready for Sprint 5 advanced features 