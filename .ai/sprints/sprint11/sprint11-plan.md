# Sprint 11: Documents list

Part of the IDE redesign — see [`../../redesign-ide-plan.md`](../../redesign-ide-plan.md) for scope,
decisions and the sprint map. Design source: screen 3h (Documents) and states 7a (empty), 7b (no
results), 7c (skeleton), 7g (delete), 7h (bulk delete), 7i (move) in
`.ai/design/Doc-Flow-Hub redesign direction/design_handoff_docflowhub_ui/`.

Status: **done 2026-11-14** — see [`sprint11-log.md`](sprint11-log.md) · Branch: `redesign/sprint11-documents-list` → PR into `redesign/ide` (D1)

## Goal
`/Documents` becomes the IDE screen: a project/folder tree on the left, a dense table with
multi-select in the middle, a details inspector on the right. The old page (2,888 lines with
1,608 lines of page CSS and a cards view) is replaced; no page-level CSS remains.

## Decisions taken for this sprint

| # | Topic | Decision |
|---|---|---|
| S11-1 | Data | New read-only `IDocumentBrowserService` (tree, one page of rows, inspector) with projected queries, like `IDashboardService`. The old list loaded every version and category of every row; the new one selects only the columns it shows. `DocumentService.GetDocumentsForUserAsync` stays for its other callers. |
| S11-2 | URL | Short query names: `q, type, owner, category, project, folder, team, sort, dir, page`. Every link (tree, filters, sort, pager, tokens) is built from one `DocumentListState` record, so no link can drop a filter (fixes the pager bug). The title-bar search sends `q`. |
| S11-3 | Selection | Clicking a row selects it and loads the inspector (`?handler=Inspector&id=` returns an HTML partial; a skeleton shows while it loads). The title link, double-click and Enter open Details. ↑/↓ move the selection. The first row's inspector is rendered with the page. |
| S11-4 | View tabs and grid view | Only the "Documents" tab: the Versions and Activity views are not designed and have no page behind them. The list/grid toggle is dropped (grid not designed). |
| S11-5 | Tree | "All documents", then the user's active projects (projects are owner-only in the app) with nested, non-archived folders. A chevron only when a folder has subfolders. Counts are visible documents directly in the node (a project counts all of its documents). The path to the selected node is expanded. Teams section filters the list by team (count = members, as designed). |
| S11-6 | Storage footer | Used bytes only, no meter: there is no quota (same as S10-4). |
| S11-7 | Filters | Type (types present, with counts), Owner (anyone / me / others), Category. Active filters and the search show as removable tokens (`type:pdf ×`). Search uses `Contains`, which the existing SQLite interceptor makes case-insensitive (Polish letters included). |
| S11-8 | Delete | One document: modal 7g posting the existing `Delete` handler. Copy says the document is removed (it is a soft delete; there is no restore UI). Bulk: modal 7h with one request; the existing service already returns per-item results (`not owner · skipped`). |
| S11-9 | Move | Modal 7i with a searchable tree (project root or folder), embedded as JSON. Single and bulk move return per-item results; the 7i "members will lose access" warning is omitted (no access model behind it). |
| S11-10 | Demo | Mutating controls (Upload, New project, Edit, Move, Delete, bulk actions) are disabled with a tooltip (D6). A shared `DocFlowHub.postJson` turns the demo filter's 403 JSON into an info toast with the server's message (fixes "HTTP error 403" / `alert('Move failed')`). |
| S11-11 | Project colour | `Project.Color` is a free hex value; its hue is mapped onto the design formula `oklch(0.74 0.13 h)`. Projects without a colour get a hue derived from the id. |
| S11-12 | Page size | 25 rows. |
| S11-13 | Upload from an empty folder | "Upload" links to `/Documents/Upload?projectId=&folderId=`; Upload's two properties accept GET values (`SupportsGet`) so the destination is preselected. Full Upload redesign is Sprint 12. |

## Tasks

### 11.0 Branch
- [x] `redesign/sprint11-documents-list` from `redesign/ide`.

### 11.1 Data
- [x] Core models (`Models/Documents/Browser`): list query, row, page, tree (projects, folders, teams), inspector.
- [x] `IDocumentBrowserService` + `DocumentBrowserService` (Infrastructure), registered in DI.
- [x] Shared display-name helper with `DashboardService`.
- [x] Unit tests (InMemory): visibility, filters, sort, paging, tree counts, inspector access.
- [x] SQLite demo tests: list, tree and inspector translate and run on SQLite.

### 11.2 Components (`components.css`)
- [x] Tree (`dfh-tree`), toolbar + breadcrumb (`dfh-toolbar`), filter tokens (`dfh-token`), view tabs.
- [x] Table checkbox column, selected row, bulk bar (`dfh-bulkbar`), pager (`dfh-pager`).
- [x] Inspector sections: identity, actions, key/value, AI summary, version list.
- [x] Skeleton bars, empty states (dropzone box 7a, left-aligned no-results 7b).
- [x] Modal parts: info box, per-item result list, progress, tree picker, route line.

### 11.3 Page
- [x] `Documents/Index` rewritten: tool window, tabs, toolbar, table, pager, inspector; old CSS/JS deleted.
- [x] Handlers: GET, `Inspector` (partial), `Download`, `Delete`, `BulkDelete`, `Move` (one or many documents, per-item results; replaces `BulkMove`). `Folders` JSON handler removed (the picker uses the embedded tree).
- [x] `wwwroot/js/dfh/documents.js`: selection + inspector fetch, keyboard, checkboxes + bulk bar, tree expand, delete / bulk delete / move modals.
- [x] `shell.js`: `DocFlowHub.postJson` (antiforgery header, 403 demo message → toast).
- [x] Title bar search uses `q`; Upload accepts `projectId`/`folderId` on GET.

### 11.4 Verification
- [x] Build, unit tests, SQLite tests.
- [x] Playwright smoke: tree filter, select row → inspector, pager keeps filters, bulk bar; screenshots.
- [x] Crawler on a fresh demo DB: no errors, clean log.
- [x] Non-demo run: delete, bulk delete (with a skipped item), move and bulk move.
- [x] Docs: sprint log, master plan bug table, memory.
