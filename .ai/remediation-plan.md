# DocFlowHub — Remediation Plan

**Created**: 2026-07-02
**Branch**: `feature/sprint8-modern-ui-design`
**Basis**: Verified against current code (not status docs). Cross-checks the honest
code review (`honest-code-review-2026-05-09.md`) against today's source and adds a
page-by-page UI modernization audit.

> Status docs claim "100% complete / all pages modernized / production ready".
> Direct code inspection shows this is not accurate. This plan is the real backlog
> to close the gap between the claims and the code.

---

## Progress log

**2026-07-02 — Workstreams A, B, C implemented (not yet committed):**
- **A done.** Added `.gitattributes` (fixes the LF/CRLF churn — the "20 modified files"
  were EOL-only noise, zero content change). Backup cshtml was already gitignored (never in
  repo) — deleted the disk copy. Fixed `.NET 8→9` in `technology-stack.md`; reconciled the
  75%/100% contradiction; added verified-status banners pointing here. `.claude/settings.local.json`
  is tracked — flagged for untracking (user's call). Run `git add --renormalize .` before committing.
- **B done (code).** Upload validation already had size + extension whitelist in
  `DocumentStorageService`; added `FileSignatureValidator` (magic-byte content sniffing) so a
  renamed file can't bypass the whitelist. B1 secret handling was already config-based +
  user-secrets wired — **rotation is a pending USER action**; `.ai/openai-key.md` to be deleted after.
  B3 (rate limiting, `CustomApiKey` encryption) deferred.
- **C mostly done.** PDF via **PdfPig**, DOCX via **DocumentFormat.OpenXml** — both implemented,
  return empty on failure (no more garbage fed to the summarizer). Retry/backoff added to
  `OpenAIService`. **Legacy binary .doc: no maintained OSS extractor exists** (NPOI ships no
  HWPF) — currently degrades gracefully (logs + returns empty). DECISION PENDING: accept the
  limitation, or add a commercial/limited lib (Aspose paid / Spire.Doc.Free watermark).
- Full solution builds clean (0 errors). Extraction **verified at runtime**: added
  `TextExtractionServiceTests` (real DOCX round-trip + real minimal PDF + legacy-.doc
  degradation). Full suite green: **27/27 passing** (was 23 + 4 new). Legacy .doc decision:
  **accept the limitation** (graceful degradation, no paid lib).
- **D in progress.** Created shared `wwwroot/css/components.css` (theme-aware page-header /
  form / card / button system) and wired it into `_Layout` before the Styles section (zero
  regression risk to the 17 already-modernized pages; admin pages inherit it). Modernized the
  **6 CRUD forms**: Projects/Create+Edit, Teams/Create+Edit, Folders/Create+Edit — old Bootstrap
  `card` + hardcoded `#f8f9fa` (dark-mode-broken) → tokenized modern components. Web builds clean.
  REMAINING in D: Document pages (Upload, Details, Edit) and Admin area (Analytics, UserLimits,
  Roles ×5, Users ×4) — ~14 pages. Not yet visually verified (needs the app running).

**2026-07-03 — D continued: Document pages done (not yet committed):**
- Modernized the **3 Document pages** (Upload, Details, Edit): `container`/`container-fluid` +
  `card-header bg-primary/bg-success text-white` shells → `page-container`/`page-header`/
  `form-card`/`content-card`; modern breadcrumbs. Tokenized every dark-mode-breaking hardcoded
  color: Upload's `<style>` block (`#dee2e6`/`#f8f9fa`/`#6c757d`/`#0d6efd`/`#198754`/`#fafafa`/
  `#e3f2fd` → `var(--border-color)`/`--surface-secondary`/`--text-secondary`/`--accent-color`/
  `--success-color`), Details' `bg-light` panels → `.token-panel`, `.list-group-item-primary`
  hardcoded rgba → tokens. All form fields, IDs, and the ~700-line Upload JS preserved verbatim.
  Web builds clean (0 errors); div balance verified (Upload 82/82, Edit 33/33, Details 106/106).
  REMAINING in D: Admin area only — Analytics, UserLimits, Roles ×5, Users ×4 (~11 pages).

**2026-07-03 — D complete: Admin area theme-corrected (not yet committed):**
- All 11 deep admin pages share `_AdminLayout` (which sits under `_Layout`, so `components.css`
  and the token system are already available). Their only real defect was **dark-mode breakage**:
  raw Bootstrap `.card`/`.table`/`.form-control`/`.list-group-item` default to white.
- **DRY fix, not 11 rewrites:** added a theme-aware override block to `_AdminLayout.cshtml` that
  swaps white → design tokens for every legacy component at once. Light mode is unchanged (tokens
  resolve to the same near-white values); dark mode is fixed everywhere in one place. Added targeted
  overrides for `bg-light` PANELS (`.card.bg-light`, `.card-header.bg-light`, `.form-control-plaintext.bg-light`)
  while deliberately leaving `bg-light` badges alone (they must stay light for their dark text).
- Page-specific cleanups: Analytics + UserLimits headers → modern `.admin-deep-header`; Users/Details
  role chip and Users/Index `.bulk-actions-panel`/`.filter-panel` hardcoded `#fff`/`#f8f9fa`/`#dee2e6`
  → tokens. `card-header bg-primary/bg-success` accents on Roles/Users forms kept (solid accents read
  fine in both themes; their card bodies are now themed).
- Web builds clean (0 errors); scan confirms **no remaining hardcoded white backgrounds** in Admin.
- **Workstream D is now functionally complete** (all pages theme-aware). Still not visually verified
  in a running app — recommend a light/dark pass before committing. F (monolith refactor) remains optional.

---

**2026-07-03 — E: security + extraction tests added (not yet committed):**
- **E3 done (the achievable, high-value part):** added `FileSignatureValidatorTests` — 17 pure,
  dependency-free cases covering the B2 magic-byte check: valid signatures for every accepted format
  (pdf/png/jpg/jpeg/gif/docx/doc), the core attacks (text→.pdf, exe→.png, gif→.png), text/NUL-byte
  handling for .txt/.md, empty files, case-insensitive extensions, docx alternate ZIP marker, and the
  "defer to whitelist for unknown signatures" contract. Plus one **wiring** test in the storage suite
  (`UploadDocument_WithExtensionSpoofedContent_ShouldFail`) proving the validator is actually invoked
  inside `UploadDocumentAsync`. Extraction (C) already has `TextExtractionServiceTests`.
  **Full suite: 45/45 passing** (was 27).
- **E1 (upload→summarize→download) partial:** upload↔download round-trip is already covered
  (`UploadAndDownload_ShouldReturnSameContent`). The **summarize** leg can't be tested end-to-end here:
  `OpenAIService` news up its `ChatClient` internally, so there's no seam to mock the AI call, and a
  live key isn't available to tests. Closing E1 needs a small refactor to inject the chat client (or an
  `IChatCompletion` seam) — flagged, not done.
- **E2 (authorization on Razor POST handlers) blocked on harness:** the test project references only
  Core + Infrastructure (no Web, no `Microsoft.AspNetCore.Mvc.Testing`). Proper handler/authorization
  tests need a `WebApplicationFactory` host — a deliberate harness expansion (new package + Web ref).
  Flagged as a decision, not silently skipped.

---

## Verified findings (2026-07-02)

- **AI text extraction is stubbed** — `TextExtractionService.cs` still has `TODO: Implement`
  for PDF (L220), DOC (L244), DOCX (L266). It returns placeholder strings / raw-byte
  "fallback", so AI summaries for PDF/DOC/DOCX are meaningless or hallucinated.
- **Tests are minimal** — exactly 4 test files (Documents×2, Storage, Teams). No AI,
  admin, auth, upload-validation, or Razor-page tests.
- **Single migration** — one migration file for an 8-sprint project (history squashed).
- **OpenAI key on disk** — `.ai/openai-key.md` holds a real-looking key (gitignored, but treat as leaked).
- **UI modernization incomplete** — ~20 pages still on old Bootstrap (see map below).
- **Docs contradict themselves** — "75%" and "100%" in the same file; `technology-stack.md` says .NET 8 (actually 9).

---

## UI modernization audit

### Modernized — Sprint 8 design applied (17)
Login, Register, Landing (Index/anon), Dashboard (Index), Documents/Index,
Projects/Index, Projects/Details, Folders/Index, Folders/Details, Teams/Index,
Teams/Details, Admin/Index, Admin/Settings/Index, Account/Manage/{Index, EditProfile,
ChangePassword, UploadProfilePicture}, Settings/AI.

### NOT modernized — status after remediation (all now addressed ✅)
| Group | Pages | Status |
|---|---|---|
| Document forms | Documents/Upload, Details, Edit | ✅ Modernized (page-container/form-card/content-card, tokenized styles) |
| CRUD forms | Projects/Create+Edit, Folders/Create+Edit, Teams/Create+Edit | ✅ Modernized (shared components.css) |
| Admin deep | Analytics, UserLimits | ✅ Theme-corrected + modern headers |
| Admin Roles | Index, Create, Edit, Delete, Details | ✅ Theme-corrected via `_AdminLayout` override |
| Admin Users | Index, Create, Edit, Details | ✅ Theme-corrected via `_AdminLayout` override |

**Original pattern**: listing/hub pages were modernized; nearly every form/create/edit page and
the deep admin area were skipped. **Now closed** — every page is theme-aware in light + dark.
Remaining: visual verification in a running app, and optional F (monolith split).

---

## Workstreams

### A — Repo hygiene & docs truth  *(~0.5 day, do first)*
- **A1** Resolve the ~20 uncommitted Sprint 8 files (commit or stash) — stop working on a moving target.
- **A2** Delete `Documents/Index_CardLayout_Backup.cshtml`; add `*_Backup.*`, `*.bak` to `.gitignore`.
- **A3** Collapse status docs to one source of truth; fix 75%-vs-100%; correct `technology-stack.md` to .NET 9.
- **A4** Rewrite "Production Ready Enterprise" framing → "strong MVP / portfolio".

### B — Security  *(~1–1.5 days, highest real risk)*
- **B1** 🔴 Rotate OpenAI key in `.ai/openai-key.md`; move to `dotnet user-secrets` / Key Vault; delete the file.
- **B2** Upload validation — extension whitelist + hard size cap + MIME/magic-byte sniffing in `Upload.cshtml.cs`.
- **B3** (optional) Rate limiting on AI/upload endpoints; verify `AISettings.CustomApiKey` not stored plaintext.

### C — Make AI actually work  *(~2–3 days, biggest functional gap)*
- **C1** 🔴 DOCX extraction via `DocumentFormat.OpenXml`.
- **C2** 🔴 PDF extraction via `PdfPig`.
- **C3** Legacy DOC — force-convert or explicitly mark unsupported; stop feeding garbage bytes to OpenAI.
- **C4** Retry/backoff in `OpenAIService`; remove placeholder-string returns.

### D — Finish UI modernization  *(~3–5 days, the gap above)*
- **D1** Extract repeated glass-morphism/`page-header` patterns into shared partials + `wwwroot/css` classes FIRST.
- **D2** Document forms: Upload, Details, Edit.
- **D3** CRUD forms: Projects, Folders, Teams (Create/Edit) — one shared form pattern.
- **D4** Admin deep pages: Analytics, UserLimits, Roles (5), Users (4).

### E — Tests  *(~2–3 days)*
- **E1** Integration test: upload → summarize → download.
- **E2** Authorization tests on POST handlers (owner/admin enforcement).
- **E3** Unit tests for new extraction (C) and upload validation (B2).

### F — Refactor monoliths  *(optional, folds into D)*
- Break up `Documents/Index.cshtml` (~2,650 lines) and peers into partials; move inline CSS/JS to `wwwroot/`.

---

## Recommended sequence
A → B → C → D → E  (F folded into D). A/B are quick and high-value; C fixes the core
feature; D is the largest but lowest-risk; E can partly run in parallel.
