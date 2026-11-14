# Sprint 15 log: Interactions, states, responsive, cleanup

Date: 2026-11-14 · Branch `redesign/sprint15-interactions-responsive` → PR into `redesign/ide` · Plan: [`sprint15-plan.md`](sprint15-plan.md)

User decisions (2026-11-14, all as recommended): palette on a small JSON endpoint, the full responsive
spec, an honest Privacy page, and an honest README rewrite.

## Done

**Command palette (7k, S15-1)**: `Ctrl K` / `⌘K`, a click on the title-bar search, `/`, or the
mobile search button opens a 560px palette (`_Palette.cshtml`, `palette.js`). Documents and
folders/projects come from `GET /Search?handler=Palette&q=` (existing `IDocumentBrowserService`; the
list search matches one phrase, so the longest word is searched and the rest are checked in memory,
so "annual rep" finds "Annual Report"). Actions and navigation are listed in the script (write
actions hidden in the demo, admin pages for admins), plus "Upload to <folder>" and "Search all
documents for …". Typed words are bolded; ↑/↓, Enter, Ctrl+Enter (new tab); Enter right after
typing waits for the results of what was typed. A plain GET of `/Search` redirects to the Documents
search.

**Shortcuts (S15-3)**: `?` opens the list. Global: `Ctrl K`, `/`, `U` (upload; a toast in the demo).
On a focused documents row: `↑/↓`, `↵`, `Space` (existing), `M` move, `F2` edit, `Del` delete,
`Shift F10` / context-menu key for the row menu. Ctrl D and Ctrl L are browser shortcuts and are not
bound. Single-letter keys are ignored while typing or while a dialog/menu is open.

**Context menu (7j, S15-2)**: generic `menu.js` (`DocFlowHub.menu.open(items, position)`), styled as
the existing dropdowns, positioned at the pointer or under a button, kept inside the viewport, arrow
keys / Home / End / Esc / Tab, closes on outside click, scroll, resize. Documents rows get a ⋯ button
(visible on hover / selection / touch) and a right-click menu (`document-menu.js`): Open, Download,
Upload new version, Compare versions (≥ 2 versions), Open AI summary / Generate summary, Move,
Edit details, Copy link, Delete. Items the user can't use are disabled with the reason as a tooltip.

**AI states (7d, S15-4)** in the Details AI tab when there is no summary: **generating** (static
skeleton while the Generate form posts; generation is synchronous, so there is no "queued"),
**failed · limit** (today's requests vs the limits, Retry disabled), **off** (summaries turned off in
Settings · AI; the button reads "Summarize this document" and links to Settings).

**Banners (7e, S15-5)**: `dfh-banner--danger` and `--ai` variants; the demo shows an AI-tinted
"Demo is read-only" banner under the title bar, dismissible for the browser session.

**Responsive (S15-6)**, `css/dfh/responsive.css` plus panel logic in `shell.js`:
- ≥1440 the 288/300px inspectors widen to 340.
- ≤1279 the Documents table drops Author (`dfh-hide-lg`).
- ≤1024 every inspector is a right overlay with a × button, opened by a title-bar button or a row
  click, closed by ×, Esc or the backdrop; admin analytics and detail grids go to one column.
- ≤768 every tool window (project tree, admin nav, settings tree) is a left drawer opened from a
  title-bar ☰ button; the Documents table keeps Name / Ver / Modified (`dfh-hide-md`).
- <600 the rail and status bar give way to a bottom navigation bar (Home, Documents, Projects, Teams,
  More → Profile, Settings, Admin, Search, Sign out); 52px app bar with 44px buttons; 14px text; the
  Documents table becomes a two-line 64px list and a tap opens the document; Details stacks the
  preview (220px band) above its tabs; other tables size by content and scroll sideways; forms
  stack their labels.

**Cleanup (S15-7)**: deleted `site.css`, `themes.css`, `ux-enhancements.css`, `css/components.css`
(1,110 lines; they only restyled Bootstrap classes `dfh/bootstrap.css` already covers) and the link to
the empty scoped-CSS bundle. `.dfh-kbd` no longer inherits Bootstrap's light `kbd` background.
Privacy is a short page written from the code (what is stored, AI providers, cookies, who sees what,
deleting data), with a demo note.

**README (S15-8)**: rewritten to describe what the app does, with screenshots in `docs/screenshots/`
(made by the `readme screenshots` test with `README_SHOTS=`), a "Not there yet" section, the real
stack (.NET 10, SQL Server or SQLite, Azure Blob or a local folder, OpenAI and Anthropic) and the real
configuration keys (`DocumentStorage:*`; the old README said `Storage:ConnectionString`).

## Honesty findings

- **AI limits were never enforced**: `CheckRateLimitsAsync` existed but nothing called it. The manual
  "Generate summary" now checks it (summaries made at upload don't yet).
- **"Send password reset"** (admin user menu) generated a reset token, sent nothing and reported
  success. Removed.
- **Admin messages** are saved in `UserCommunications`, but users have no inbox and no e-mail is sent.
  The dialogs and the toast now say "saved … no inbox or e-mail yet".
- The README promised invitations, comments, notifications, threat detection, device fingerprinting,
  GDPR compliance, end-to-end encryption and real-time updates.

Backlog: an inbox (or e-mail sending) for admin messages and password resets; enforce the AI limits
on upload summaries too; the system settings wiring from Sprint 14.

## Verification

- Release build 0 errors; unit tests 150 / 150 (`Category!=LiveStorage`).
- `node --check` on every `js/dfh` script (an apostrophe in a single-quoted string had broken
  `users-admin.js` mid-sprint; the write specs caught it).
- Playwright on a fresh demo DB: smoke 30 passed, 1 skipped (registration, disabled in the demo);
  screenshots 14 passed (desktop, palette, row menu, shortcuts, 1024 / 768 / 390 widths).
- Write specs without demo mode (admin, workspace, documents, including the new row-shortcut / row-menu
  delete and AI "off" card tests): 14 / 14.
- Crawler: 375 pages (demo user 167, admin 208), 0 errors, no fail / crit lines in the app log.
