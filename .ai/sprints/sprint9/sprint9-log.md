# Sprint 9 log: redesign foundation and shell

Date: 2026-11-13 · Branch `redesign/sprint9-foundation` → PR into `redesign/ide`
Plan: [`sprint9-plan.md`](sprint9-plan.md) · Overall: [`../../redesign-ide-plan.md`](../../redesign-ide-plan.md)

## Result
Every page now renders inside the new dark IDE shell: 40px title bar (logo, workspace pill, search
with `Ctrl K`, Upload, account menu), 40px icon rail, 24px status bar, admin navigation as a 208px
tool window. New tokens, Bootstrap mapping, self-hosted fonts and logo apply app-wide. Page *content*
is still the old markup (by design), readable inside the shell.

## Done
- **CI**: runs on `redesign/ide` pushes and PRs too.
- **Assets**: IBM Plex Sans + JetBrains Mono 400/500/600 (latin + latin-ext) via LibMan `jsdelivr`
  (`@fontsource/*@5.3.0`, 12 woff2, 248 KB) + `css/dfh/fonts.css` with `unicode-range`; logo mark as
  `_LogoMark` partial (token colours); `favicon.svg` from the handoff; `favicon.ico` regenerated from it
  (16/32/48).
- **CSS** in `wwwroot/css/dfh/`: `tokens.css` (all design tokens + extra tokens for colours the mocks
  hard-code), `bootstrap.css` (designer's block completed for 5.3.2: link rgb, buttons, forms, checks,
  switches, tables, dropdowns, modals, toasts, alerts, tabs, pagination, cards, tooltips), `base.css`
  (type scale, focus ring, scrollbars, reduced motion), `shell.css` (shell grid, title bar, rail,
  status bar, tool windows, nav list, avatars, toasts, auth layout, error pages).
- **Layouts**: `_Layout` rewritten as the shell (sections `ToolWindow` / `Inspector`, `FullBleed`
  flag for redesigned pages); new `_TitleBar`, `_Rail`, `StatusBar` view component, `_AuthLayout`;
  `_AdminLayout` renders the admin nav tool window and forwards `Styles`/`Scripts` to the head/end of
  body (they used to render mid-body, before jQuery).
- **Antiforgery**: token rendered once in the layout (`<meta name="csrf-token">` + hidden input).
- **Status bar** (D3): storage provider from config, optional path, AI requests today / daily limit
  (cached 60 s per user), `demo · read-only`, version + short commit hash.
- **Demo** (D6): title-bar Upload disabled with a tooltip; the read-only notice is now a toast.
- **Toasts**: new stack (max 3, success/info/AI hide after 5 s, errors stay) behind the existing
  `window.DocFlowHub.showSuccess/showError/showInfo` API (+ `showAi`).
- **Error pages**: new `Account/AccessDenied` (403), `StatusCode/{code}` page wired with
  `UseStatusCodePagesWithReExecute` (404 had no page before), `Error` restyled. The title bar and status
  bar survive a database failure, so the error page still renders when the database is down.
- **Removed**: theme toggle/`theme-manager.js`, ripple and counter scripts, the global "Processing…"
  submit hijack (replaced by opt-in `data-dfh-busy`, added to Upload, Settings save and both Edit
  forms; it respects a cancelled submit), duplicate CDN Bootstrap Icons, dead `upload-progress.js`,
  empty `site.js`, unused `_AdminNav`, template `_Layout.cshtml.css`. Legacy page fade-in animation
  turned off (no motion in the redesign).
- **Legacy kept on purpose**: `themes.css`, `components.css`, `ux-enhancements.css`, `site.css` stay
  loaded (before the new CSS) for pages not yet redesigned; the 401-line admin style block moved
  unchanged to `css/legacy/admin.css` (deleted in Sprint 14).
- **Playwright** (D7): `tests/e2e` (`@playwright/test` 1.60.0) with `smoke.spec.ts` and
  `screenshots.spec.ts`.

## Verification (definition of done)
| Check | Result |
|---|---|
| Build / unit tests | 0 errors · 113/113 |
| Playwright smoke (7 tests) | 7/7 passed: both accounts render every main page in the shell, 403 for the user on `/Admin`, admin nav for the admin, 404 page, layout token accepted (demo AJAX write → 403 read-only, not 400), blocked form post → read-only toast, `Ctrl K` focuses search |
| Crawler, both demo accounts | 291 pages, 0 errors, 99 s (crawler now fetches 8 pages at a time) |
| App log | no `fail:` / `crit:` lines |
| Screenshots 1280×800 | dashboard, documents, document details, upload, projects, teams, team, settings AI, profile, admin overview/users/analytics, 404 (signed in and out), sign in |

Run against the Release build with the demo container's settings (SQLite, local files, demo mode,
Production). The Docker image itself was not run locally; CI builds it (incl. the new `jsdelivr`
font restore).

## Fixed along the way
- Admin Users filter buttons were clipped once the admin nav took 208px: the button row now wraps.
- Buttons use `min-height` instead of a fixed height so old pages' larger buttons aren't cut.

## Known issues (handled in later sprints)
| Issue | Sprint |
|---|---|
| Old page content inside the shell (cards, gradients, emoji headings) | 10–14, per page |
| Sign in / Register / anonymous landing are still the old standalone pages (Login still shows a dead theme-toggle button) | 10 |
| Admin Users shows "100:F1% of total" (format-string bug, pre-existing) | 14 |
| Slowest pages: `Folders/Create` ~4 s, dashboard 2–4 s, Upload / Details ~2 s (local, first hit) | note for 11–13 |
| Bell, workspace switcher and command palette not built (title bar shows a static pill; `Ctrl K` focuses search) | 15 / backend |
| Playwright not in CI yet (browser download time) | decide in 10 |
