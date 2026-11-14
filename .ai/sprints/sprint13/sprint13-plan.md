# Sprint 13: Projects, Folders, Teams

Part of the IDE redesign — see [`../../redesign-ide-plan.md`](../../redesign-ide-plan.md) for scope,
decisions and the sprint map. Design source: screens 8a (Projects), 8b (Project / folder) and 3d
(Team) in `.ai/design/Doc-Flow-Hub redesign direction/design_handoff_docflowhub_ui/`; the Teams list
and the Create / Edit forms have no mock and follow these screens and Upload (3c).

Status: **done 2026-11-14** — see [`sprint13-log.md`](sprint13-log.md) · Branch: `redesign/sprint13-projects-folders-teams` → PR into `redesign/ide` (D1)

## Goal
The project, folder and team screens use the IDE layout: a projects table with an inspector, one
"project / folder" browser with the project tree, subfolders above documents and folder settings in
the inspector, and a team page with a members table and the real role rules. The twelve old pages
(about 6,700 lines, most of them page CSS) are replaced.

## Decisions taken for this sprint

| # | Topic | Decision |
|---|---|---|
| S13-1 | Data | New read-only `IWorkspaceBrowserService`: projects table with folder / document counts, size, last change and AI coverage; the project/folder view (subfolders with item counts, documents with version and last author); teams with role and counts; one team with members and last sign-in. Projected queries like Sprints 10–12. Writes keep the existing services. |
| S13-2 | Project ↔ team | The model has no link between projects and teams, so the 8a "Team" column, the 8b "Access · inherited" row and the 3d "Projects" tab/block are not shown. |
| S13-3 | Projects list (8a) | Table + inspector loaded per row (shared `selectable-table.js`, the Documents mechanism made generic). Segmented **Active / Archived** (projects are owner-only, so "Mine" would equal "Active"). The filter box filters the visible rows in the browser. |
| S13-4 | Project and folder pages (8b) | One browser for both: `Projects/Details/{id}` is the project root, `Folders/Details?Id=` a folder. Project tree on the left (links to these pages, drag a folder onto another folder or project to move it — the old Folders/Index drag-and-drop), subfolders then documents, "New folder" and "Upload here". Inspector: project info + Archive / Restore / Delete (empty only) at the root; folder info + settings form (name, description, archived) + Delete (empty only) in a folder. |
| S13-5 | Folders/Index | Redirects to the project page; its tree is now the tool window. It keeps the `MoveFolder` JSON handler for drag-and-drop. |
| S13-6 | Teams list | No mock: a table like 8a (badge, name, description, your role, members, documents, created) without an inspector; rows open the team. |
| S13-7 | Team page (3d) | Header with badge, Settings (owner) and Add member (admins); tabs Members / Documents (the latter opens the Documents list filtered by the team); members table with an inline role select for admins, last sign-in instead of presence; remove member in a row menu. Inspector "Role permissions" shows the **real** rules: Owner / Admin / Member (no Viewer, no invitations). |
| S13-8 | Project colours | Create / Edit offer the design palette as swatches (hex values whose OKLCH hue matches the design hues) plus "no colour"; the icon picker goes (icons are not in the design; the stored value is kept on edit). |
| S13-9 | Forms | Create / Edit for projects, folders and teams use the form-page layout of Upload (editor tab, sections, footer) with server-side validation and toasts after a redirect. |

## Tasks

### 13.0 Branch
- [x] `redesign/sprint13-projects-folders-teams` from `redesign/ide`.

### 13.1 Data
- [x] `IWorkspaceBrowserService` + implementation + models; unit tests (InMemory) and SQLite demo tests.

### 13.2 Components
- [x] Two-line table rows, segmented links, filter box, team badge, role select, permission matrix, switch row, danger zone, entity header.
- [x] `selectable-table.js` (generic selection + inspector) and `tree.js` (expand/collapse + folder drag-and-drop), used by Documents too.

### 13.3 Pages
- [x] Projects: Index (8a) + `_ProjectInspector`, Details (8b root), Create, Edit.
- [x] Folders: Details (8b), Index (redirect + MoveFolder), Create, Edit; `_FolderTreeNode` replaced by the shared tree.
- [x] Teams: Index, Details (3d), Create, Edit.

### 13.4 Verification
- [x] Build, unit tests, SQLite tests.
- [x] Playwright smoke (demo): projects table + inspector, archived filter, folder browser, team page; write tests (non-demo): create project / folder, folder settings, move folder, team member add / role / remove, create / edit / delete team.
- [x] Crawler on a fresh demo DB; screenshots; docs; memory.
