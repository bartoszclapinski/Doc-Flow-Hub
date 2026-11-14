# Sprint 13 log: Projects, Folders, Teams

Date: 2026-11-14 · Branch `redesign/sprint13-projects-folders-teams` → PR into `redesign/ide` · Plan: [`sprint13-plan.md`](sprint13-plan.md)

## Done

**Data** — `IWorkspaceBrowserService` (`Core`) + `WorkspaceBrowserService` (`Infrastructure`), S13-1:
projects table (folder / document counts, size and last change as subqueries), one project with its
folders in tree order and AI coverage, the project / folder view (subfolders with item counts, then
documents with version and last author, "is empty" with the services' delete rules), teams with the
user's role and counts, one team with members and last sign-in. The old Projects list built every
row with separate statistics queries.

**Pages** — the twelve pages are replaced (8,581 → about 2,300 lines across 25 files; all page CSS
and inline scripts are gone):
- `Projects/Index` (8a): Active / Archived, filter box, table with two-line rows and colour squares,
  inspector loaded per row (`_ProjectInspector`: owner, created, storage, AI coverage, folder list,
  Open / Edit / Archive or Restore / Delete when empty).
- `Projects/Details/{id}` and `Folders/Details` (8b): project tree tool window (`_ProjectTree`,
  folders can be dragged onto another folder or the project to move them), toolbar with breadcrumb,
  counts, New folder and Upload here, subfolders above documents (`_FolderBrowser`), empty state
  (7a). Inspector: the project on the project page; on a folder page its details, a settings form
  (name, description, archived) and Delete for an empty folder.
- `Folders/Index` redirects to the project and keeps the `MoveFolder` JSON handler (S13-5).
- `Teams/Index`: table with team badge, your role, members, documents, owner, created.
- `Teams/Details` (3d): header with badge, Settings (owner) and Add member (admins, modal with role);
  tabs Members / Documents (opens the filtered Documents list); members table with an inline role
  select, last sign-in and remove; inspector with the **real** permission matrix (Owner / Admin /
  Member) and the team's documents.
- Create / Edit for projects (colour swatches from the design palette, S13-8), folders (with the
  folder it sits in) and teams (with a danger zone to delete), all on the form-page layout.

**Shared**
- `selectable-table.js` (row selection + inspector for any table: Documents and Projects),
  `tree.js` (expand/collapse + folder drag-and-drop), `row-filter.js`; `documents.js` keeps only the
  multi-select.
- `shell.js`: `data-dfh-confirm` forms open a confirmation dialog (7g) instead of `confirm()`;
  `select[data-dfh-autosubmit]` posts on change.

**Bugs fixed along the way**
- Project details showed a total size of 0 (hard-coded); sizes now come from the documents.
- Project, folder and member deletions on these pages used `window.confirm()`; they use the dialog.

## Verification
- `dotnet build -c Release`: 0 errors, no warnings in changed files. Unit tests **143/143**
  (`Category!=LiveStorage`), incl. 4 new InMemory tests (ownership, counts, tree order, folder view,
  roles) and 1 on the SQLite demo fixture.
- Playwright smoke on the demo build: **22 passed, 8 skipped** (writes are disabled in the demo).
  New: projects table + inspector + archived tab + filter, project → folder pages, team page with the
  permission matrix.
- Same database without demo mode: `workspace-actions.spec.ts` **2 passed** — create a project, two
  folders, drag one into the other, rename + archive it, delete both, archive / restore / delete the
  project; create a team, add a member, make them admin, remove them, rename and delete the team.
- Crawler (demo DB, both accounts): **365 pages, 0 errors**, **0 `fail:`/`crit:` lines**.
- Screenshots reviewed: projects, archived, project, folder, team list, team, project / folder /
  team forms.

## Differences from the mocks (intentional)
- No project ↔ team link in the model (S13-2): no "Team" column, no "Access · inherited", no
  Projects tab on the team page.
- Segmented control has Active / Archived only (projects are owner-only, "Mine" = Active).
- Team page: no Viewer role, no pending invitations, no presence dot ("last sign-in" instead), no
  Projects tab; members are added straight away by e-mail.
- Folder settings have an explicit Save button (the mock shows none).

## Deferred / notes
- Remaining `confirm()` calls are on the admin and profile pages (Sprints 14–15).
