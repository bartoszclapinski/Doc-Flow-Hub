# DocFlowHub – Product Requirements Document (PRD)

Version: 1.0
Date: 2025-01-22
Owner: Bartosz Cłapiński
Repository: `https://github.com/bartoszclapinski/Doc-Flow-Hub`

## 1. Summary

DocFlowHub is an enterprise-grade, web-based document management platform built with ASP.NET Core 9, Entity Framework Core 9, SQL Server, and Azure Blob Storage. It enables authenticated users to upload, organize, version, search, and manage documents across projects and folders. The system integrates AI features using OpenAI (`gpt-4o-mini`) to automatically generate document summaries and compare versions. The UI follows a modern glass-morphism design with full light/dark themes and responsive layout.

## 2. Objectives and Non‑Goals

### Objectives
- Provide secure user authentication and profile management.
- Deliver core document management: upload, view, update metadata/content, delete, move, and versioning.
- Offer organizational hierarchy: Projects → Folders → Documents.
- Add AI capabilities: auto document summarization and version comparison.
- Ensure a modern, responsive, and theme‑aware UI for desktop and mobile.
- Provide unit tests and CI pipeline (GitHub Actions) to validate quality.

### Non‑Goals (for this release)
- Enterprise SSO (SAML/LDAP) integrations.
- Custom workflow automation and webhooks.
- Full text search across binaries (beyond metadata and stored summary text).
- Advanced analytics beyond current admin dashboards.

## 3. Users and Personas

- Regular User: uploads and manages own documents, views summaries, compares versions.
- Team Member: collaborates within a team/project, organizes folders and documents.
- Admin: manages users, roles, system settings, analytics, and usage limits.

## 4. Problem Statement

Teams need a secure, simple yet powerful place to store and manage documents with version history. Traditional solutions are either too heavy or lack modern UX and AI assistance. DocFlowHub fills this gap with a clean architecture, modern UI, and practical AI features.

## 5. Scope (MVP)

### 5.1 Authentication & Accounts
- Email/password registration and login using ASP.NET Core Identity.
- Password policies, lockout, and role-based access (Admin/User).

### 5.2 Document Management
- Upload documents with metadata (name, description, categories, tags, project/folder location).
- List, filter, and search documents by name, type, project, folder, and owner.
- Edit metadata and replace content (new version).
- Version history with ability to download specific version and delete versions safely.
- Move single or bulk documents between folders/projects.
- Delete documents (single and bulk) with owner/admin authorization.

### 5.3 Organization
- Projects with overview screens.
- Nested folders with stats, move operations, and archiving flag.

### 5.4 AI Features
- Auto‑generate document summary on upload (stored in `DocumentSummaries`).
- Compare two versions using OpenAI and store comparison results (`VersionComparison`).

### 5.5 UI/UX
- Modernized Documents page: statistics cards, professional filter/search, colorful file‑type icons, responsive grid (4→2×2→1), and theme support.
- Main layout and admin area with consistent styles; header, sidebar, and glass‑morphism components.

### 5.6 Quality & Tooling
- Unit tests (xUnit, Moq) covering document, storage, and team services.
- CI pipeline with GitHub Actions: restore, build, and test on push/PR.

## 6. Out of Scope (Now, planned later)
- Workflow automation, approvals, notifications.
- External storage integrations (SharePoint/Google Drive).
- SSO and enterprise directory sync.

## 7. Functional Requirements & Acceptance Criteria

### FR‑1: User Authentication
- Users can register and log in.
- AC: Login succeeds with valid credentials and denies invalid ones; authenticated navigation visible.

### FR‑2: Upload Document
- User uploads a file with metadata to a chosen project/folder.
- AC: New document appears in list with correct metadata; storage has the binary; initial version created.

### FR‑3: List, Filter, Search Documents
- Filter by project, folder, type, owner; free‑text search by name.
- AC: Results update instantly; filters persist during pagination; both themes remain legible.

### FR‑4: Edit Document Metadata & Content
- Update name/description/categories or upload a new version.
- AC: Changes are persisted; version number increments; previous versions remain downloadable.

### FR‑5: Delete Documents (Single & Bulk)
- Owner/admin can delete; shows confirmation; updates UI dynamically.
- AC: Items removed from DB and storage; bulk deletion reports per‑item results.

### FR‑6: Move Documents
- Move single/bulk documents across folders/projects.
- AC: New locations reflected in list; permissions enforced.

### FR‑7: AI Summary on Upload
- Background process generates summary; failures don’t block upload.
- AC: Summary stored and visible in details once ready; errors logged gracefully.

### FR‑8: Version Comparison
- Compare two selected versions and present differences.
- AC: Comparison text stored and retrievable; handles large files via background service if needed.

### FR‑9: UI/UX Themes and Responsiveness
- Fully responsive; consistent light/dark themes.
- AC: Documents page passes visual checks: stats grid breakpoints, filter panel parity, checkbox visibility, and green button theme.

### FR‑10: Admin Controls
- Manage users, roles, system settings, analytics, usage limits.
- AC: Admin pages load and operations complete successfully under authorized roles.

## 8. Non‑Functional Requirements

- Performance: document lists paginate; common actions < 1s; heavy AI tasks async.
- Security: ASP.NET Identity; role/claim‑based authorization; server‑side validation; virus scanning hook in storage service design.
- Reliability: background services use scoped DbContext patterns; resilient logs.
- Accessibility: keyboard‑navigable forms; sufficient contrast in both themes.
- Maintainability: Clean Architecture (Core/Infrastructure/Web), DI, MediatR pattern optional for future.

## 9. Success Metrics (MVP)

- 100% green build + tests on CI.
- < 1% upload failures (non‑network).
- < 2s first contentful render on Documents page on mid‑tier laptop.
- 90%+ user tasks completed without confusion in informal test.

## 10. Release Plan

- Sprint 8 focus: Modern UI/UX; Documents page complete; next: main dashboard modernization.
- Tagged GitHub release after dashboard update; deployment to demo environment optional.

## 11. Risks & Mitigations

- AI provider failures → fail‑soft strategy; upload proceeds; retries/backoff; clear logs.
- Large files/storage costs → configurable size limits; Azure Blob lifecycle policies.
- Theme regressions → shared variables and component styles; visual test checklist.

## 12. Dependencies

- ASP.NET Core 9, EF Core 9, SQL Server, Azure Blob Storage.
- OpenAI model `gpt-4o-mini` via `OpenAIService` implementation.
- Bootstrap 5.3; xUnit + Moq; GitHub Actions.

## 13. Glossary

- Document: Metadata + stored binary + versions.
- Version: Immutable content snapshot with sequential number.
- Project/Folder: Organizational containers for documents.
- Summary: AI‑generated text describing a document’s contents.
- Version Comparison: AI‑assisted diff of two versions.


