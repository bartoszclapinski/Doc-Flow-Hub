# DocFlowHub UI redesign ("IDE" direction): implementation plan

Status: **in progress** — Sprints 9–10 done (2026-11-13) · Sprints 9–15 · All recommended decisions (D1–D7) accepted

The designer's handoff lives locally (git-ignored) in
`.ai/design/Doc-Flow-Hub redesign direction/design_handoff_docflowhub_ui/`. `README.md` there is the
spec; the `.dc.html` files are the screens (open them in a browser with `support.js` next to them).
This plan turns that handoff into work on the real Razor Pages app.

---

## 1. Goal and scope

Replace the current glass-morphism / gradient look with the dense, IDE-like interface from the handoff:
40px title bar, 40px icon rail, tool windows (trees), editor tabs, tables, a right inspector, and a
24px status bar.

**In scope**
- Visual redesign of every page (37 routable pages + layouts and partials), **dark theme only**.
- New shell, design tokens, fonts, logo, components, states and overlays (toasts, modals, context
  menu, command palette), keyboard shortcuts, responsive rules.
- Small read-only data plumbing that a screen needs and existing services already provide (e.g. the
  project/folder tree on the Documents list, an "has AI summary" flag, project sizes). No new tables
  or migrations.
- Bugs found during the survey that sit on the redesigned pages (see §8).

**Out of scope (later, only if chosen)**: the 7 backend features the handoff implies — team roles
Owner/Admin/Member/**Viewer** with a permission matrix, pending invitations, upload-by-existing-name
→ new version, multi-file upload, sessions list and security events, AI queue/cost details,
global search across people/actions, line-level text diff, restore version, storage quota, monthly AI
limits, workspaces, notifications. Screens show a fallback or hide these elements (§6).

**Light theme**: not now. Tokens are structured so a `[data-bs-theme="light"]` block can be added
later without touching components.

## 2. How we work

- **One sprint = one branch = one PR**, merged only with green CI.
- **Integration branch (recommended, see decision D1):** sprint PRs merge into `redesign/ide`, not
  `master`. `master` (and therefore the public Render demo) keeps the current UI until the redesign
  is complete and consistent, then `redesign/ide` merges into `master` once. CI must run on PRs to
  `redesign/ide` too.
- **Definition of done for every sprint** (checked, not claimed; the old Sprint 8 status overstated
  progress, so status is only updated after these checks):
  1. `dotnet build` with 0 errors, all tests pass (`--filter "Category!=LiveStorage"`).
  2. The demo container runs locally; the crawler (log in as `demo@` and `demo-admin@`, follow every
     same-site link) reports **0 errors** and the log has no `fail:`/`crit:` lines.
  3. Every page touched in the sprint has a screenshot at 1280×800 next to its mock; differences are
     listed in the sprint log.
  4. Pages finished in the sprint have **no page-level `<style>` / `@section Styles`** left and no
     `[data-theme]` overrides; their JS still works (manual click-through of each interactive element
     in the sprint checklist).
  5. Demo mode still works: POSTs blocked with the flash/toast, downloads work.
  6. Sprint log written (`.ai/sprints/sprintN/sprintN-log.md`): what was done, what was deferred, why.
- Everything runs in the foreground; no background jobs.

## 3. Technical approach

### 3.1 CSS architecture
New stylesheets in `wwwroot/css/dfh/`, loaded in this order by the layouts:

| File | Contents |
|---|---|
| `fonts.css` | `@font-face` for IBM Plex Sans + JetBrains Mono 400/500/600, latin + latin-ext (Polish), `unicode-range` |
| `tokens.css` | `--dfh-*` tokens (dark), incl. the extra tokens for colours the mocks hard-code (§4) |
| `bootstrap.css` | the designer's "Bootstrap 5.3 · themes.css" block, corrected for 5.3.2 (`--bs-link-color-rgb`, own `.btn-primary`/focus/checkbox overrides, `inline-flex` buttons, missing `--dfh-bg-hover`) |
| `base.css` | type scale, focus ring, scrollbars, `prefers-reduced-motion`, utility classes |
| `shell.css` | `dfh-shell` grid, title bar, rail, status bar, tool window, panel header, editor tabs, toolbar, inspector |
| `components.css` | tables, tree, buttons, inputs, token input, chips, file-type labels, dots, avatars, meters, CSS bar charts, banners, empty states, skeletons, toasts, menus, modals, palette, dropzone, auth card, settings rows |

Old files (`themes.css`, `components.css`, `ux-enhancements.css`, `site.css`, `_Layout.cshtml.css`)
and every page-level style block are deleted page by page as pages are migrated; the last sprint
removes whatever is left. Inline page CSS today is ~10,100 lines, so most of this work is deletion.

### 3.2 Layouts and partials
- `_Layout.cshtml` rewritten as the shell: `<html data-bs-theme="dark">`, CSS grid
  `40px 1fr 24px`, partials `_TitleBar`, `_Rail`, `_StatusBar`. Each page declares its columns
  (tool window / inspector) through sections (`@section ToolWindow`, `@section Inspector`) or
  `ViewData` flags.
- New `_AuthLayout.cshtml` for Sign in / Register / AccessDenied / Error-for-anonymous (replaces the
  three standalone `<head>`s).
- `_AdminLayout.cshtml` becomes a thin wrapper that renders the admin nav tool window (208px); its
  401-line style block goes.
- Antiforgery token rendered once in the layout (`<meta name="csrf-token">` + one hidden input) so
  AJAX keeps working when page forms are restructured.
- Removed: theme toggle + `theme-manager.js` (dark only), ripple script, the global "Processing…"
  submit-button hijack (replaced by an explicit `data-dfh-busy` pattern), counters, the CDN copy of
  Bootstrap Icons.

### 3.3 JavaScript
Vanilla JS in `wwwroot/js/dfh/` (jQuery stays only for validation): `toasts.js` (keeps the existing
`window.DocFlowHub.showSuccess/showError/showInfo` API), `table.js` (row select → inspector, checkbox
→ bulk bar, keyboard ↑/↓/↵), `menu.js` (context menu), `palette.js` (Ctrl K), `shortcuts.js`,
`modal.js` helpers (confirm dialogs replacing ~30 `alert()`/`confirm()` calls), `responsive.js`
(drawers/overlays). Existing element ids used by page JS are kept or the JS is ported in the same PR.

### 3.4 Assets
- Fonts: LibMan `jsdelivr` provider, `@fontsource/ibm-plex-sans` and `@fontsource/jetbrains-mono`
  woff2 (latin + latin-ext, 400/500/600, ~230 KB). Not on cdnjs. Exact versions confirmed when
  adding them.
- Logo: final mark "branch" from `assets/` → `wwwroot/img/logo-mark.svg`, `favicon.svg`.
- Chart.js removed once Settings AI and Admin Analytics use CSS bars (Sprint 14).
- PDF preview (Details) uses PDF.js only if decision D5 says so.

## 4. Design decisions (resolving the handoff's ambiguities)

Recommended defaults; the ones marked **D** need your OK.

| # | Topic | Decision |
|---|---|---|
| D1 | Where sprint PRs merge | **`redesign/ide` integration branch**, one merge to `master` at the end, so the public demo never shows a half-old, half-new UI. Alternative: merge each sprint to `master` and accept a mixed demo. |
| D2 | Anonymous `/` | Redirect to Sign in; delete the marketing landing page with its invented stats (handoff says so). |
| D3 | Status bar content | Storage provider from config ("Local storage" on the demo, "Azure Blob" otherwise), current path (mono), signed-in user's AI requests today vs daily limit (existing data), `demo · read-only` in demo, app version from the assembly. No "connected" claim. |
| D4 | Version compare in the demo | Compare is a POST and the demo blocks POSTs, so the demo can't show it. Option: allow the `CompareVersions` handler in demo **only when a cached comparison exists** (the seed can create a few). Otherwise the compare screen shows an explanatory empty state in the demo. |
| D5 | Document preview (Details) | Visual pass: placeholder panel with file type, size, page info we have, and Download. Real PDF.js preview later (needs an inline GET endpoint). |
| D6 | Mutating buttons in demo | Keep them visible but disabled with a tooltip "Read-only demo" (today they fail on click). |
| 7 | Near-identical greys `#9a9da4` / `#9da0a8` | Merge into `--dfh-text-muted` `#9a9da4`. |
| 8 | Secondary button border | `--dfh-border-button: #4e5157` for 28px+ secondary buttons, `#43454a` for 24px compact controls and inputs. |
| 9 | Off-scale heights (26/30/34) | Follow the mocks where they are deliberate: title-bar controls 26, auth submit 34; everything else snaps to 24/28/32. |
| 10 | Hover colours | Tables and trees `--dfh-bg-hover`; menus solid accent with white text; palette active row `--dfh-selection`; rail hover `#393b40`. |
| 11 | Inspector width | Per page as in the mocks: Documents 288, Details 340, Compare/Upload/Team 320, Dashboard activity 360. |
| 12 | Tree indent | 16px per level including the chevron column. |
| 13 | Editor tabs vs view tabs | Two components: `dfh-editor-tabs` (closable, active bg + accent line) and `dfh-view-tabs` (underline only). |
| 14 | Team colour | Derived from the team id (one of the named hues); projects use their stored `Color`. |
| 15 | Logo accent | Keep `#3574f0` in the mark (intentional per assets); UI accent stays `#2f6bde`. Logo SVGs used as delivered (provenance metadata kept). |
| 16 | Profile "Theme" row, language, time zone, sessions, delete account | Hidden (dark only / no backend). |
| 17 | Register "Confirm password" | Keep it (Identity model requires it), styled like the other fields. |
| 18 | Settings AI "Temperature" | Mapped to the existing `QualityPreference`; settings without a design row (custom API key, max tokens, comparison sensitivity, cache duration) are kept in an "Advanced" section in the same style. |
| 19 | 90-day Analytics chart | Weekly bars for 90 days, daily for 7/30. |
| 20 | Shortcuts that clash with the browser (Ctrl D, Ctrl L) | Not bound; shown without those hints. |
| 21 | Folder settings in the inspector (8b) | Explicit Save button (no autosave). |

## 5. Screen → page mapping

| Design screen | Page(s) | Sprint |
|---|---|---|
| Shell: title bar, rail, status bar | `Shared/_Layout`, new `_TitleBar`, `_Rail`, `_StatusBar`, `_AuthLayout`; `Admin/_AdminLayout` | 9 |
| 7e 403 page, banners; Error | new `Account/AccessDenied`, status-code pages (404), `Error` | 9 |
| 7f toasts, 7g modal base | `_Layout` toast stack, global modal restyle | 9 |
| 3g Sign in, 8d Register | `Account/Login`, `Account/Register`, `Account/Logout` | 10 |
| 3a Dashboard | `Index` (authenticated) | 10 |
| (by analogy) Admin overview | `Admin/Index` | 10 |
| 3h Documents + 7a/7b/7c, 7g–7i | `Documents/Index` | 11 |
| 3b Document details | `Documents/Details` | 12 |
| 3i Version compare | new `Documents/Compare` (moved out of Details) | 12 |
| 3c Upload | `Documents/Upload` | 12 |
| (by analogy) Edit document | `Documents/Edit` | 12 |
| 8a Projects | `Projects/Index`, `Projects/Details` | 13 |
| 8b Folder | `Folders/Details`, `Folders/Index` (+ `_FolderTreeNode`) | 13 |
| 3d Team | `Teams/Details`, `Teams/Index` | 13 |
| (by analogy) forms | `Projects/Create/Edit`, `Folders/Create/Edit`, `Teams/Create/Edit` | 13 |
| 3e Admin · Users | `Admin/Users/Index` (+ Create/Edit/Details) | 14 |
| 8c Admin · Analytics | `Admin/Analytics` | 14 |
| (by analogy) admin | `Admin/Roles/*`, `Admin/UserLimits`, `Admin/Settings/Index` | 14 |
| 3f Settings · AI | `Settings/AI` in a new settings shell | 14 |
| 8e Profile | `Account/Manage/*` (+ `_ManageNav` → settings tree) | 14 |
| 7d AI states, 7j context menu, 7k command palette, shortcuts | cross-cutting | 15 |
| Responsive 9a–9d | cross-cutting | 15 |
| Leftovers | `Privacy`, dead files | 15 |

## 6. Elements that need backend: what the visual pass shows instead

| Element | Fallback |
|---|---|
| Workspace switcher (title bar) | Static pill: "All workspaces" (or "Administration" in admin) |
| Storage quota "/ 10 GB" | Used total only |
| Monthly AI limit, AI queue, queue position | Daily usage vs existing daily limit; "Pending" = no summary yet |
| Activity feed beyond versions/AI events | Versions + AI events only |
| Team Viewer role, permission matrix, invitations, team↔project link | Hidden; roles Owner/Admin/Member as today |
| Line diff, change anchors, restore version | Compare screen shows the AI comparison in the new layout; diff column replaced by an empty state "Line diff coming later" |
| Multi-file upload, existing-name detection, "scanning" | Single file; sections hidden |
| Sessions, security events, audit log, CSV export, delete account, language, time zone | Hidden |
| Notifications bell | Hidden |
| Forgot password | Link hidden |
| Global search (people, actions) | Palette: documents (existing search), folders/projects (existing services), static actions and navigation |
| Version hash | Shown from the existing MD5 `FileHash` (first 7 hex chars) |

## 7. Sprint breakdown

Sizes are relative (S/M/L). Each sprint gets its own `sprintN-plan.md` when it starts, so it reflects
what the previous sprint actually delivered. Sprint 9 is planned in detail now.

### Sprint 9 — Foundation and shell (L)
Tokens, Bootstrap mapping, fonts, logo/favicon, new `_Layout` shell with title bar, rail, status
bar; `_AuthLayout`; admin nav tool window; toasts and modal base; AccessDenied + 404; remove
theme toggle, ripple, submit hijack, counters, duplicate icons. **Result:** every page renders inside
the new shell; page content is still old but readable (crawler 0 errors, spot screenshots).
Details: [`sprints/sprint9/sprint9-plan.md`](sprints/sprint9/sprint9-plan.md). **Done** — [log](sprints/sprint9/sprint9-log.md).

### Sprint 10 — Sign in, Register, Dashboard (M)
Sign in (3g) with the demo-credentials box, Register (8d) with live password rules, Logout view,
anonymous `/` → Sign in (landing page deleted), Dashboard (3a): stats line, recent documents table,
AI / storage-by-type / teams strip, activity tool window. Admin overview by analogy.
Details: [`sprints/sprint10/sprint10-plan.md`](sprints/sprint10/sprint10-plan.md). **Done** — [log](sprints/sprint10/sprint10-log.md).

### Sprint 11 — Documents list (L)
Project/folder tree tool window (existing `IFolderService` / `IProjectService`), view tabs, toolbar
with filters, `dfh-table` with check column and file-type labels, AI flag (list query change), pager
that keeps filters (bug fix), inspector filled from the selected row (+ lazy JSON for AI summary and
last 4 versions), bulk bar, delete / bulk delete with per-item results / move with searchable tree
picker, empty and no-results states, skeleton rows. Cards/grid view dropped (not in the design).

### Sprint 12 — Details, Compare, Upload, Edit (L)
Details (3b): editor tab, toolbar, preview placeholder (D5), inspector tabs Info / AI / Versions /
Sharing. Compare (3i) as its own page and editor tab, AI comparison inspector (D4). Upload (3c):
dropzone, file row, details grid, AI options, sticky footer, destination tree inspector. Edit by
analogy.

### Sprint 13 — Projects, Folders, Teams (M)
Projects table (8a) with colour squares and stats; Project details; Folder (8b) with subfolders above
documents and folder settings in the inspector; Folders/Index tree (keeps drag-and-drop move); Teams
list and Team (3d) with members table and role controls; all Create/Edit forms by analogy.

### Sprint 14 — Admin, Settings, Profile (L)
Admin nav, Users (3e) with token search, bulk bar, numbered pager, details modal re-skinned in JS;
Analytics (8c) with CSS bar charts and the never-populated `UsageTrends`/`ErrorStats` fixed; Roles,
UserLimits, Admin Settings by analogy; settings shell with tree; Settings · AI (3f); Profile (8e) +
Edit profile / Change password / Picture. Chart.js removed.

### Sprint 15 — Interactions, states, responsive, cleanup (L)
AI states (7d), banners, context menu (7j), command palette (7k), keyboard shortcuts, all remaining
`alert()`/`confirm()` replaced, TempData/StatusMessage routed to toasts; responsive rules (≤1024
inspector overlay, ≤768 tree drawer, <600 bottom nav + two-line rows + details as tabs); delete old
CSS/JS and dead files; final full screenshot pass; merge `redesign/ide` → `master`; update README
screenshots and `.ai` status docs.

## 8. Bugs fixed along the way

| Bug | Sprint |
|---|---|
| `/Account/AccessDenied` configured but missing; no 404 handling | 9 |
| Bootstrap Icons loaded twice; Register page missing icons | 9 |
| Login links to a non-existent ForgotPassword page | 10 |
| Register validated passwords of 6+ chars while Identity requires 8 | 10 |
| Demo seed: versions clamped to "now", `Document.UpdatedAt` never moved to the last version | 10 |
| Parallel first requests of a new user fail inserting default AI settings (unique index) | 10 |
| Dashboard / admin overview loaded whole document lists (admin: per user) just to count them | 10 |
| Documents pagination drops filters and sort | 11 |
| Demo: mutating AJAX calls show "HTTP error 403" / `alert('Move failed')` instead of the read-only message | 11 |
| Projects/Details TotalSize hard-coded to 0 | 13 |
| Admin Analytics never fills `UsageTrends` / `ErrorStats` | 14 |
| Settings AI loads Chart.js from a CDN instead of the local copy (removed anyway) | 14 |
| `theme-manager.js` posts to a non-existent `/api/profile/theme` | 9 (file removed) |

## 9. Risks

| Risk | Mitigation |
|---|---|
| No UI tests; regressions only found by hand | Crawler + per-sprint click-through checklist + Playwright smoke suite from Sprint 9 (D7) |
| Page JS depends on specific ids, and two pages build HTML in JS strings | Keep ids or port the JS in the same PR; list each page's ids in its sprint checklist |
| AJAX reads the antiforgery token from page forms | Token rendered once in the layout (Sprint 9) |
| Half-migrated pages look broken | Integration branch (D1); old page styles stay until the page is migrated |
| CSS cascade traps (page CSS inside `@section Scripts` or the body) | Delete a page's style blocks wholesale when migrating it |
| Long integration branch drifts from `master` | Merge `master` into `redesign/ide` at the start of every sprint |

## 10. Decisions taken (2026-11-13)

All recommendations accepted:
- **D1** sprint PRs merge into the `redesign/ide` integration branch; one merge to `master` at the end.
- **D2, D3, 7–21** as in §4.
- **D4** the demo allows `CompareVersions` only for comparisons already cached by the seed; otherwise
  the compare screen shows an empty state.
- **D5** Details shows a preview placeholder now; PDF.js preview later.
- **D6** mutating buttons in the demo are disabled with a "Read-only demo" tooltip.
- **D7** a small Playwright smoke suite is added in Sprint 9 (sign in as both demo accounts, open each
  main page, assert no error page).
