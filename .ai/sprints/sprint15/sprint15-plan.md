# Sprint 15: Interactions, states, responsive, cleanup

Part of the IDE redesign — see [`../../redesign-ide-plan.md`](../../redesign-ide-plan.md) for scope,
decisions and the sprint map. Design source: `DocFlowHub States.dc.html` (7d AI states, 7e banners,
7j context menu, 7k command palette) and `DocFlowHub Responsive.dc.html` (breakpoints, mobile 9a–9d)
in `.ai/design/Doc-Flow-Hub redesign direction/design_handoff_docflowhub_ui/`.

Status: **in progress** · Branch: `redesign/sprint15-interactions-responsive` → PR into `redesign/ide` (D1).
Merging `redesign/ide` into `master` (which redeploys the demo) needs the user's separate approval.
User decisions 2026-11-14: palette on a small JSON endpoint, the full responsive spec, an honest Privacy page (all as recommended).

## State before the sprint

- No `alert()` / `confirm()` left: Sprints 11–14 replaced them (`data-dfh-confirm`, dialogs, toasts).
- Ctrl K only focuses the title-bar search box.
- No responsive rules: the only `@media` is `prefers-reduced-motion`. Below ~1100px the panels squeeze.
- Documents rows have no ⋯ / right-click menu; actions live in the inspector and the bulk bar.
- AI summaries are generated synchronously (form post), so there is no queue to show; the AI section
  has "ready" and "no summary yet" only.
- Legacy CSS still loaded by `_Layout`: `site.css`, `themes.css`, `ux-enhancements.css`,
  `css/components.css` (1,110 lines). They only restyle Bootstrap classes that `dfh/bootstrap.css`
  already covers. `Privacy` is the template page ("Use this page to detail…").
- `EditProfile` / `UploadProfilePicture` are redirect stubs (Sprint 14); `Folders/Index` GET redirects.

## Decisions

| # | Topic | Decision |
|---|---|---|
| S15-1 | Command palette (7k) | Ctrl K / ⌘K or a click on the title-bar search opens a 560px palette. Groups: Documents (title search through the existing document filter, top 6), Folders and projects (names the user can see), Actions (Upload, New project, New team, go to Dashboard / Documents / Projects / Teams / Settings / Admin for admins). One JSON handler `GET /Search?handler=Palette&q=` built on existing services (no new tables). Enter opens, Ctrl+Enter opens in a new tab, "Search all documents for …" as the last row (→ `/Documents?q=`). Without JS the title-bar search box still submits to Documents. |
| S15-2 | Context menu (7j) | One `dfh-menu` component: right click on a Documents row, or its new ⋯ button. Items that exist: Open, Download, Upload new version, Compare versions (≥ 2 versions), Generate summary (no summary), Move to…, Edit (instead of inline rename), Copy link, Delete. Disabled with a tip in the demo / when not the owner. Same component for project and team rows where they have actions. |
| S15-3 | Keyboard shortcuts | Ctrl K palette, `/` focuses search, `U` upload, on a selected document row `↵` open, `M` move, `F2` edit, `Del` delete, `↑/↓` rows (exists), `Esc` closes overlays, `?` shows a shortcut list. Ctrl D and Ctrl L are browser shortcuts (bookmark, address bar): shown as hints only, not bound. Single-letter keys are ignored while typing in a field. |
| S15-4 | AI states (7d) | Card variants in the Details AI tab and the Documents inspector: **generating** (static skeleton while the Generate form posts), **failed · limit** (when today's limit is reached: numbers from the existing limits, Retry disabled, no "Request more"), **off** (Summaries and comparisons turned off in Settings · AI: link to Settings and a manual "Summarize this document"). No "queued": generation is synchronous. |
| S15-5 | Banners (7e) | Generic `dfh-banner` danger / warning / info / ai (exists for warning / info). Demo read-only banner in AI tint under the title bar, dismissible, remembered in `sessionStorage` (try/catch). No storage health banner (no probe). |
| S15-6 | Responsive | ≥1440 inspector 340px; ≤1279 tables drop the Author column first; ≤1024 inspector becomes a right overlay (opens on row click, × / Esc closes), Compare forced to unified, admin analytics one column; ≤768 tool window becomes a left drawer opened from a title-bar button, tables keep Name / Ver / Modified; <600 rail → bottom nav (Home, Documents, Projects, Teams, More → Settings, Admin, Sign out), Documents table → two-line 64px list, Details shows its inspector tabs below the preview, 44px targets, 14px text, status bar hidden. Custom media queries in `dfh/responsive.css`. |
| S15-7 | Legacy cleanup | Delete `site.css`, `themes.css`, `ux-enhancements.css`, `css/components.css` and their `<link>`s; fix whatever the screenshots show. Privacy: a short honest page in the new style (what the app stores: account, documents, AI usage log; the demo resets) instead of the template text. |
| S15-8 | Docs | README screenshots and status, `.ai` status docs, final full screenshot pass. |

## Tasks

### 15.1 Palette and shortcuts
- [ ] `Search` page with `Palette` JSON handler; `palette.js` (open/close, debounce 150ms, groups, highlight, ↑/↓, Enter, Ctrl+Enter, focus return, ARIA combobox/listbox).
- [ ] Shortcut handler in `shell.js` + shortcut help dialog.

### 15.2 Context menu
- [ ] `dfh-menu` component (`menu.js`: right click / ⋯, viewport clamping, arrow keys, Esc, outside click).
- [ ] Documents rows: ⋯ button + menu items wired to existing handlers and dialogs.

### 15.3 States
- [ ] AI card variants (generating / failed · limit / off) in Details and the inspector.
- [ ] Banner variants; demo read-only banner.

### 15.4 Responsive
- [ ] `dfh/responsive.css`; inspector overlay, tool-window drawer, bottom nav, two-line list, mobile details.
- [ ] Screenshots at 1440 / 1024 / 768 / 390.

### 15.5 Cleanup and docs
- [ ] Delete legacy CSS; Privacy page.
- [ ] README, `.ai` docs, memory.

### 15.6 Verification
- [ ] Build, unit tests; smoke tests for palette, shortcuts, context menu, responsive overlays; write specs; crawler on a fresh demo DB; full screenshot pass.
