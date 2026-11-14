# Sprint 9: Redesign foundation and shell

Part of the IDE redesign — see [`../../redesign-ide-plan.md`](../../redesign-ide-plan.md) for scope,
decisions and the sprint map. Design source: `.ai/design/Doc-Flow-Hub redesign direction/design_handoff_docflowhub_ui/`
(README.md, `DocFlowHub Design System.dc.html`, `IdeTitleBar/IdeRail/IdeStatusBar.dc.html`, `assets/`).

Status: **done 2026-11-13** — see [`sprint9-log.md`](sprint9-log.md) · Branch: `redesign/sprint9-foundation` → PR into `redesign/ide` (decision D1)

## Goal
Every page renders inside the new dark IDE shell (title bar, icon rail, status bar) with the new
tokens, fonts and logo. Page *content* is still the old markup, but readable and working. Later sprints
replace page content screen by screen.

## Out of scope
Page content redesign (Sprints 10–14), context menu / palette / shortcuts / responsive (Sprint 15),
light theme, backend features.

## Tasks

### 9.0 Branches and CI
- [x] Create `redesign/ide` from `master`; create `redesign/sprint9-foundation` from it.
- [x] `ci.yml`: also run on PRs and pushes to `redesign/ide`.

### 9.1 Assets
- [x] `libman.json`: add `@fontsource/ibm-plex-sans` and `@fontsource/jetbrains-mono` via the
      `jsdelivr` provider (latin + latin-ext, 400/500/600 woff2 + LICENSE); confirm versions; run
      `libman restore` locally and in the Dockerfile build (already runs).
- [x] `wwwroot/css/dfh/fonts.css`: own `@font-face` rules with `unicode-range` (Fontsource per-subset
      CSS has none).
- [x] Logo: `wwwroot/img/logo-mark.svg` (dark variant), inline SVG partial `_LogoMark.cshtml`
      (size parameter, colours from tokens `--dfh-logo-ink` / `--dfh-logo-accent`).
- [x] Favicon: `wwwroot/favicon.svg` from `assets/favicon.svg` + `<link rel="icon" type="image/svg+xml">`;
      keep `favicon.ico` as fallback (regenerate from the SVG).

### 9.2 Tokens and Bootstrap mapping
- [x] `wwwroot/css/dfh/tokens.css`: all dark tokens from the design system (bg, panel, sunken, hover,
      control, borders, selection, text/text-2/muted, accent/hover, link, AI, success, warning, danger,
      danger-solid, diff add/del, file-type hues, shadows, backdrop, focus ring) + extra tokens for
      colours the mocks hard-code (`--dfh-border-button #4e5157`, `--dfh-check-border #5a5d63`,
      `--dfh-filler #232427`, `--dfh-skeleton #2f3135`, `--dfh-ai-fill`, `--dfh-invalid-border`).
- [x] `wwwroot/css/dfh/bootstrap.css`: the designer's Bootstrap 5.3 block, fixed for 5.3.2
      (`--bs-link-color-rgb`, `.btn-primary` / `.btn-secondary` / `.btn-outline-*` variables, focus
      ring, `.form-check-input:checked`, `inline-flex` buttons with fixed heights 24/28/32, table cell
      height and padding, `.dropdown-menu`, `.modal`, `.toast`, `.form-control` / `.form-select`).
- [x] `wwwroot/css/dfh/base.css`: body type (IBM Plex Sans 13/20), mono utility, headings scale,
      focus-visible ring, thin dark scrollbars, `prefers-reduced-motion`, 120ms colour transitions.
- [x] `<html data-bs-theme="dark" data-theme="dark">` — `data-theme` stays only so not-yet-migrated
      page CSS keeps its dark variants until Sprint 15.

### 9.3 Shell
- [x] `_Layout.cshtml` rewritten: head (fonts, bootstrap, icons once, dfh css, then the legacy
      `themes.css`/`components.css`/`ux-enhancements.css` for unmigrated pages), body grid
      `40px minmax(0,1fr) 24px`, workbench row `40px [toolwindow] 1fr [inspector]`, main area
      scrolls on its own, sections `ToolWindow` / `Inspector` / `Styles` / `Scripts` rendered.
- [x] `_TitleBar.cshtml`: logo 22 + wordmark, static workspace pill ("All workspaces" /
      "Administration" in admin), search box 420×26 as a GET form to `/Documents?Filter.SearchTerm=`
      with the `Ctrl K` hint (palette comes in Sprint 15), Upload button (disabled with tooltip in
      demo, D6), avatar 24 with user menu (Profile, Settings, Sign out — Sign out is a POST form).
      Bell hidden (no notifications backend).
- [x] `_Rail.cshtml`: Dashboard `bi-grid-1x2`, Documents `bi-files`, Projects `bi-kanban`, Teams
      `bi-people`; bottom Admin `bi-shield-lock` (Administrator only), Settings `bi-gear`; active state
      from the current path; `title` + `aria-label` + `aria-current`.
- [x] `_StatusBar.cshtml` (decision D3): storage provider label from config, `ViewData["StatusPath"]`
      (mono), spacer, AI requests today / daily limit for the signed-in user (existing
      `IAIUsageTrackingService`; hidden if unavailable), `bi-lock demo · read-only` in demo, app version
      from the assembly informational version.
- [x] `Admin/_AdminLayout.cshtml`: drop the 401-line style; render the admin nav as a 208px tool
      window (Overview, Users, Roles, User limits, Analytics, System settings) using `dfh-nav-list`.
- [x] `_AuthLayout.cshtml` (no chrome, centred on `--dfh-bg`) — used by Login/Register in Sprint 10;
      in this sprint used by the new AccessDenied page and by `Error` for anonymous users.
- [x] Antiforgery token once in the layout (`<meta name="csrf-token">` + hidden input) so page AJAX
      keeps working.

### 9.4 Global behaviour and cleanup
- [x] Remove: theme toggle UI (except the old standalone Login page, rebuilt in Sprint 10) + `theme-manager.js` (and its include on Login/Register/Index), ripple
      script, global submit-button "Processing…" hijack (replace with opt-in `data-dfh-busy`),
      counter animation, CDN Bootstrap Icons link, dead `upload-progress.js`, empty `site.js`,
      unused `_AdminNav.cshtml`, `_Layout.cshtml.css`.
- [x] Toasts: `dfh-toast-stack` bottom-right above the status bar (360px, max 3; success/info hide
      after 5s, errors stay) behind the existing `window.DocFlowHub.showSuccess/showError/showInfo`
      API; the demo read-only TempData flash and `StatusMessage` shown as toasts.
- [x] Modal base restyle (480/560, panel bg, border-strong, radius 8, shadow, backdrop 0.92) via
      Bootstrap modal variables — existing modals keep their ids and markup.
- [x] Dropdown menu base restyle (`dfh-menu` look) for existing dropdowns.

### 9.5 Error pages (7e)
- [x] `Pages/Account/AccessDenied.cshtml` (403) in the new style, Back button; "Request access" hidden.
- [x] 404 via `UseStatusCodePagesWithReExecute("/Error/{0}")` (or a `/NotFound` page) in the new style.
- [x] `Error.cshtml` restyled; demo note kept.

### 9.6 Verification (definition of done)
- [x] Build 0 errors; tests pass.
- [x] Playwright smoke project (decision D7) — sign in as both demo accounts, open each rail
      destination, assert no error page. Runs locally against the demo container; CI integration
      decided when it's added (browsers download adds CI time).
- [x] Crawler as both accounts: 0 errors; no `fail:`/`crit:` in the log — run against the Release build
      with the container's settings. *Not done:* running the Docker image locally (CI builds it).
- [x] Screenshots 1280×800: title bar, rail and status bar next to `IdeTitleBar/IdeRail/IdeStatusBar`;
      one screenshot of every top-level page (Dashboard, Documents, Details, Upload, Projects, Folder,
      Teams, Team, Settings AI, Profile, Admin overview/users/analytics) to confirm old content is
      readable inside the shell — problems listed in the log, not fixed here unless blocking.
- [x] Manual checks: AJAX actions still send the antiforgery token (move, bulk delete, admin user
      details, settings test key); demo POST shows the read-only toast; sign out works; admin rail item
      only for administrators.
- [x] `sprint9-log.md` written; `redesign-ide-plan.md` status updated.

## Files touched (expected)
`Pages/Shared/_Layout.cshtml`, new `_TitleBar`, `_Rail`, `_StatusBar`, `_LogoMark`, `_AuthLayout`;
`Pages/Admin/_AdminLayout.cshtml`; `Pages/Error.cshtml`; new `Pages/Account/AccessDenied.cshtml`;
`Program.cs` (status code pages); `wwwroot/css/dfh/*`; `wwwroot/js/dfh/toasts.js`; `libman.json`;
`wwwroot/favicon.*`, `wwwroot/img/logo-mark.svg`; `.github/workflows/ci.yml`; deletions listed in 9.4.

## Risks in this sprint
- Old page CSS relies on `themes.css` variables and `[data-theme="dark"]`; keeping both loaded until
  Sprint 15 avoids breaking unmigrated pages.
- The body becomes a fixed-height grid with no page scroll; old pages that assume page scroll must
  scroll inside `main` — check long pages (Documents, Upload, Settings AI).
- Removing the submit hijack changes double-submit protection; `data-dfh-busy` must be added to the
  forms that relied on it (upload, settings save).
