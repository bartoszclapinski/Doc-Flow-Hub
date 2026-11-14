# Sprint 11 log: Documents list

Date: 2026-11-14 · Branch `redesign/sprint11-documents-list` → PR into `redesign/ide` · Plan: [`sprint11-plan.md`](sprint11-plan.md)

## Done

**Data** — `IDocumentBrowserService` (`Core`) + `DocumentBrowserService` (`Infrastructure`), S11-1:
- `GetTreeAsync`: the user's active projects with nested, non-archived folders, document counts per
  project and per folder (GROUP BY), the user's teams with member counts, file types present (for the
  Type filter) and used storage.
- `GetDocumentsAsync`: one page of projected rows (version, owner name, team, location, AI flag) with
  search / type / owner / category / project / folder / team filters and a stable sort. The old list
  loaded every version and category of every row.
- `GetInspectorAsync`: one document with its categories, latest AI summary, last 4 versions (authors
  by name) and version totals; fails for documents the user cannot see.
- Shared `DisplayFormat` (person name, short hash, "Project / folder" location) now used by the
  dashboard too.

**Page** — `Documents/Index` rewritten (3,308 → 1,044 lines of Razor/C# plus `documents.js`; the
1,608 lines of page CSS and 458 lines of inline JS are gone, as is the cards view):
- Tool window: "All documents", project tree (chevrons only where a folder has subfolders, path to the
  selection expanded), Teams section, storage footer.
- View tab, toolbar with breadcrumb + count + removable filter tokens + Type / Owner / Category menus.
- Table with a checkbox column, file-type labels, AI star, workspace (team or Private), sortable
  Name / Size / Modified headers; pager "1–25 of 40" with Prev / Next.
- Inspector (`_DocumentInspector` partial): identity, Download / Open / ⋯ menu (Edit, Move, Compare,
  Delete — disabled with a reason for non-owners and in the demo), details, AI summary, history.
- States: no documents (7a box), empty project/folder with Upload here + New folder (7a), no results
  with "search all documents" / remove-token / clear links (7b), inspector skeleton while loading (7c).
- Dialogs: delete (7g, posts the existing handler), bulk delete with per-document results (7h), move
  with a searchable project/folder picker, ↑/↓/Enter, match highlighting and a "from → to" line (7i).
- `wwwroot/js/dfh/documents.js`: row selection loads the inspector (cached), ↑/↓ move, Enter /
  double-click open, Space checks; checkboxes + bulk bar; Escape clears the selection unless it closes
  a dialog or a menu.
- `shell.js`: `DocFlowHub.postJson` (antiforgery header, always resolves with `success`/`message`,
  marks the demo filter's 403 as `readOnly`), `reportFailure`, `toastAfterReload`.
- `_Layout`: `TempData["ToastSuccess"]` / `["ToastError"]` become toasts (redesigned pages' form posts).
- Title-bar search sends `q` and shows the current search on the Documents page.
- Upload accepts `projectId` / `folderId` on GET (empty-folder "Upload" preselects the destination).

**Bugs fixed along the way**
- Pagination dropped the filters and the sort: every link is now built from one `DocumentListState`.
- Demo: AJAX writes showed "HTTP error 403" / `alert('Move failed')`; they now show the server's
  read-only message as a toast (and the buttons are disabled in the demo anyway, D6).
- Demo seed stored folder paths with the project name (`/Product Launch/Archive`) while
  `FolderService` stores `/Archive`; locations showed the project twice. The seeder now matches.
- Bulk move returned only a count; it now returns a result per document like bulk delete.
- Search on SQLite is case-insensitive through the existing `instr` interceptor (the new queries use
  `Contains`).

## Verification
- `dotnet build -c Release`: 0 errors, no warnings in changed files. Unit tests **132/132**
  (`Category!=LiveStorage`), incl. 10 new InMemory tests (visibility, filters, sort, page clamping,
  tree, inspector access) and 1 on the SQLite demo fixture (tree, list, all filters, case-insensitive
  search and inspector run on SQLite; counts match `DocumentService`).
- Playwright smoke on the demo build: **15 passed, 4 skipped** (register + 3 write tests are disabled
  in the demo). New: links keep filters and sort, tree scopes the list, row selection and arrows fill
  the inspector, bulk bar + Escape, blocked AJAX move reports the read-only message.
- Same database restarted without demo mode: `documents-actions.spec.ts` **3 passed** — bulk delete
  (1 deleted · 1 skipped "not owner"), move into a folder via the picker, single delete.
- Crawler (demo DB, both accounts, query variants capped at 40 per path): **358 pages, 0 errors**,
  **0 `fail:`/`crit:` lines**.
- Screenshots at 1280×800 reviewed: list, selected row, folder, filtered, no results, bulk bar,
  move, bulk delete, delete, inspector menu, dashboard.

## Differences from the mocks (intentional)
- Only the "Documents" view tab (Versions / Activity views are not designed); no grid toggle.
- Storage footer shows the used total without a meter (no quota).
- "Preview" in the inspector is "Open" (the preview arrives with Details in Sprint 12).
- Delete copy says the document is removed and can't be restored from the app (soft delete, no
  restore UI); the counts of AI summaries/comparisons are not listed.
- Move has no "members will lose access" warning (no access model behind it).
- An "All documents" root node and a team filter in the tree (needed to leave a project/team scope).

## Deferred / notes
- Compare links go to Details until the Compare page exists (Sprint 12).
- Context menu (7j) and keyboard shortcuts beyond the table (Sprint 15).
