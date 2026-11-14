# Sprint 12 log: Details, Compare, Upload, Edit

Date: 2026-11-14 · Branch `redesign/sprint12-details-compare-upload` → PR into `redesign/ide` · Plan: [`sprint12-plan.md`](sprint12-plan.md)

## Done

**Data**
- `IDocumentBrowserService.GetDetailsAsync`: the document with all versions (authors by name, size,
  short hash), the latest AI summary with key points split into items, categories, team and the
  version pairs that have a saved comparison.
- `GetComparisonAsync`: two versions (defaults: the newest and the one before; reversed or equal
  numbers are normalised) and their saved AI comparison. It only reads; it never calls the AI.
- `CreateDocumentRequest.GenerateSummary`, honoured by `DocumentService.CreateDocumentAsync`.
- Demo seed: an AI comparison for every pair of consecutive versions (D4), so the demo shows
  Compare with GET only.

**Pages**
- `Documents/Details` (3b), rewritten (957 + 533 → 369 + 248 lines): editor tabs, toolbar (path,
  New version, Move, Share, Download, ⋯ with Edit / Compare / Copy link / Delete), preview placeholder
  (D5), inspector tabs Info (as designed) / AI (full summary, key points, **Generate summary**, saved
  comparisons) / Versions (download, compare with current, delete an old version) / Sharing (team
  assignment). Status bar path "project / folder / file".
- `Documents/Compare/{id}?from=&to=` (3i), new: compare tab, version pickers with hash and author,
  AI comparison inspector (summary, chips from change kinds + significance, numbered changes, model ·
  tokens · confidence · date), "Compare with AI" when nothing is saved (disabled in the demo), and an
  empty state for the line diff with downloads of both versions. The in-page compare form and the
  `CompareVersions` handler of the old Details page are gone.
- `Documents/Upload` (3c), rewritten (1,420 + 556 → 184 + 211 lines): dropzone + file row with a
  client-side type/size check, title from the file name, Project / Folder / Workspace, category
  toggle chips, description, "Generate a summary after upload", sticky footer; inspector
  "Destination" tree (click to choose, count "n → n+1") and usage. The model / quality pickers are
  gone (Settings · AI preferences).
- `Documents/Edit`, rewritten (302 + 257 → 164 + 188 lines): editor tab, Details form, New version
  form with the same dropzone; results are toasts after a redirect.
- Shared: `_EditorTabs` + `editor-tabs.js` (session tabs, S12-1), `_DocumentDialogs` +
  `document-dialogs.js` (delete / bulk delete / move, used by Documents and Details), `details.js`,
  `compare.js`, `upload.js`. Documents list inspector: Edit available to everyone who can open the
  document (S12-4).

**Bugs fixed along the way**
- Upload ignored "Generate AI summary" (the service always summarized) and the page started a second
  summary on a request scope that was already disposed (it always failed silently).
- The Documents inspector said "Open the document to generate one" although there was no way to
  generate a summary; the AI tab now has Generate summary (it never replaces an existing summary).
- Upload defaulted to the first project; documents can be uploaded without a project again, and a
  project or folder that is not the user's is rejected.

## Verification
- `dotnet build -c Release`: 0 errors, no warnings in changed files. Unit tests **138/138**
  (`Category!=LiveStorage`), incl. 5 new InMemory tests (details, key points, comparison pairs and
  normalisation, access) and 1 on the SQLite demo fixture (details + seeded comparison run on SQLite).
- Playwright smoke on the demo build: **19 passed, 6 skipped** (writes are disabled in the demo).
  New: inspector tabs survive a reload, seeded comparison + pickers, editor tabs open/close,
  upload type check.
- Same database without demo mode: `documents-actions.spec.ts` all passed — upload into a project,
  edit details, new version, delete the old version, share and unshare, generate summary (no AI key
  locally: an error toast, nothing saved, no `fail:` log lines).
- Crawler (demo DB, both accounts): **444 pages, 0 errors**, **0 `fail:`/`crit:` lines**.
- Screenshots at 1280×800 reviewed: details (Info / AI / Versions / Sharing), compare, upload with a
  chosen file, edit.

## Differences from the mocks (intentional)
- Preview is a placeholder card (D5): no page thumbnails, page count or zoom.
- Compare: no line diff, ignore-whitespace, change navigation or "Restore v3" (no backend); the
  changes list has numbers instead of line anchors. Version pickers are native selects.
- Upload takes one file; no "existing document → new version" detection and no "scanning" state;
  storage and AI usage show used amounts only (no quota / monthly limit).
- Editor tabs are per browser session, not per account.

## Deferred / notes
- Real preview (PDF.js) and a line diff need backend work (inline file endpoint, text diff).
