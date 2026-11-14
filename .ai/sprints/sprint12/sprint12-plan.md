# Sprint 12: Details, Compare, Upload, Edit

Part of the IDE redesign — see [`../../redesign-ide-plan.md`](../../redesign-ide-plan.md) for scope,
decisions and the sprint map. Design source: screens 3b (Details), 3i (Version compare) and 3c (Upload)
in `.ai/design/Doc-Flow-Hub redesign direction/design_handoff_docflowhub_ui/`; Edit has no mock and
follows Upload.

Status: **done 2026-11-14** — see [`sprint12-log.md`](sprint12-log.md) · Branch: `redesign/sprint12-details-compare-upload` → PR into `redesign/ide` (D1)

## Goal
The document screens use the IDE layout: editor tabs, a toolbar, a sunken content area and a tabbed
inspector. Version comparison gets its own page. Upload and Edit become plain forms in the same shell.
No page-level CSS remains on these four pages.

## Decisions taken for this sprint

| # | Topic | Decision |
|---|---|---|
| S12-1 | Editor tabs | The current document's tab is rendered by the server; other recently opened documents (max 6) are kept in `sessionStorage` and shown as inactive tabs. × closes a tab; closing the active one goes to the previous tab or to Documents. No backend. |
| S12-2 | Preview (D5) | Placeholder in the sunken area: file-type label, title, size and version, and Download. No PDF.js yet. |
| S12-3 | Inspector tabs | Info (as designed: description, details, short AI summary, recent versions), AI (full summary, key points, model / confidence / date, **Generate summary** on the existing `IDocumentSummaryService`), Versions (all versions: download, compare with current, delete an old version), Sharing (team assignment, the only sharing model). |
| S12-4 | Who can do what | Same rules as the backend: anyone who can open the document may edit its details, upload a new version and generate a summary; only the owner may move, share, delete it or delete versions. The Sprint 11 inspector's Edit item follows this too. Everything that writes is disabled in the demo (D6). |
| S12-5 | Compare page | New `/Documents/Compare/{id}?from=&to=` (GET). Version pickers in the header row; the AI comparison in the inspector (summary, chips from `ChangeType` + `Significance`, changes list from `DetailedChanges`, model / tokens / date). The line diff column is an empty state "Line-by-line diff isn't available yet" (master plan §6). No saved comparison → "Generate comparison" (POST, the user's preferred model). The old in-page compare form and `CompareVersions` handler on Details are removed. |
| S12-6 | Compare in the demo (D4) | The demo seed saves an AI comparison for every pair of consecutive versions, so the demo shows comparisons with GET only. Generating stays disabled in the demo. |
| S12-7 | Upload | One file (no multi-file backend): dropzone + browse, file row, title prefilled from the file name. Details grid: Project, Folder (existing JSON handler), Workspace (team or Private), Categories as toggle chips, Description. AI: "Generate summary after upload" — now honoured end to end (new `CreateDocumentRequest.GenerateSummary`). The model / quality pickers move out (they are Settings · AI preferences). Inspector "Destination": the project tree with the chosen folder and its count "n → n+1", plus storage used and AI requests today. "Existing document" and "scanning" are not shown (no backend). |
| S12-8 | Edit | Editor tab "Edit · title", two sections with their own buttons: Details (title, description, category chips) and New version (dropzone + change note). Results are toasts after a redirect (no more re-rendered page with a message). Inspector: current version and recent history. |
| S12-9 | Status bar | Path "project / folder / title.ext" on Details and Compare (design 3b, 3i). |
| S12-10 | Shared dialogs | Delete (7g) and Move (7i) dialogs become partials used by Documents and Details; their JS moves from `documents.js` into `document-dialogs.js`. |

## Bugs to fix
- Upload: the "Generate AI summary" choice is ignored (the service always summarizes) and the page
  starts a second summary on a request scope that is already disposed.
- Inspector copy "Open the document to generate one" pointed to a feature that did not exist.

## Tasks

### 12.0 Branch
- [x] `redesign/sprint12-details-compare-upload` from `redesign/ide`.

### 12.1 Data
- [x] `IDocumentBrowserService.GetDetailsAsync` (all versions with authors, summary with key points, team, categories, saved comparison pairs) and `GetComparisonAsync` (versions + the saved comparison of a pair, read only).
- [x] `CreateDocumentRequest.GenerateSummary` honoured by `DocumentService.CreateDocumentAsync`.
- [x] Demo seed: comparisons for consecutive versions.
- [x] Unit tests (InMemory) + SQLite demo tests.

### 12.2 Components (`components.css`)
- [x] Editor tabs, compact toolbar buttons, preview placeholder, inspector tabs, chips, key points.
- [x] Diff header row, compare chips, change list, inspector footer.
- [x] Form page: section headings, field grid, dropzone + file row, toggle chips, sticky footer.

### 12.3 Pages
- [x] Details (3b) rewritten; handlers Download (version), Delete, DeleteVersion, Share, Unshare, GenerateSummary; old CSS/JS deleted.
- [x] Compare (3i) new page; handler Generate.
- [x] Upload (3c) rewritten; Edit rewritten.
- [x] `_DeleteDialog`, `_MoveDialog` partials + `document-dialogs.js`; `editor-tabs.js`; `upload.js` (dropzone, folders, chips).

### 12.4 Verification
- [x] Build, unit tests, SQLite tests.
- [x] Playwright smoke: details tabs, compare page with a seeded comparison, editor tabs, upload type check; write tests (non-demo): upload, edit details, new version, delete a version, sharing, generate summary.
- [x] Crawler on a fresh demo DB: no errors, clean log.
- [x] Screenshots vs mocks; docs, memory.
