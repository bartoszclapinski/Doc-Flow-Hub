# Sprint 10 log: Sign in, Register, Dashboard, Admin overview

Date: 2026-11-13 · Branch `redesign/sprint10-auth-dashboard` → PR into `redesign/ide` · Plan: [`sprint10-plan.md`](sprint10-plan.md)

## Done

**Shared components** — new `wwwroot/css/dfh/components.css` (loaded by `_Layout` and `_AuthLayout`):
page header + stat line (stats that don't fit are hidden whole, never cut mid-word), section header,
`dfh-table` (real `<table>`, sticky header, rows with `data-dfh-href` open their page via `shell.js`),
file-type labels, chips, dots, meter, stacked bar + legend, bottom strip, team list, activity feed,
empty state, filter dropdown, auth card / fields / demo box / password rules / footer. Links are no
longer underlined until hovered (design system); `.input-validation-error` styled like `.is-invalid`.

**Data** — `IDashboardService` (`Core`) + `DashboardService` (`Infrastructure`): every number is a
COUNT/SUM in the database. Replaces the old page code that loaded up to 1000 documents per user and,
on the admin page, looped over every user with two queries each. Visibility rule identical to
`DocumentService.GetDocumentsAsync`. Activity = versions (with short MD5 hash), AI summaries, AI
comparisons, team joins (+ new accounts on the admin page).

**Pages**
- `Account/Login` (3g) on `_AuthLayout`; demo box with click-to-fill; "Create one" hidden in demo;
  Forgot-password link removed (no page); validation scripts now actually load (they never did with
  the old standalone page). 514 → 62 lines.
- `Account/Register` (8d): live rules + 4-segment strength meter (`wwwroot/js/dfh/auth.js`), confirm
  password kept, submit disabled until the rules pass and the confirmation matches (enabled without
  JS). Minimum length 6 → 8 to match Identity. 469 → 65 lines.
- `Account/Logout`: "You're signed out" card; a signed-in GET redirects to the Dashboard.
- `Index` (3a): signed-out `/` → Sign in, landing page deleted; admins are no longer redirected to
  `/Admin` (S10-1). Overview + stat line + period dropdown, Recent documents, AI summaries / Storage
  / Teams strip, Activity panel (360px). 1,739 + 304 → 196 + 66 lines.
- `Admin/Index`: same structure, system-wide; Newest users table → user details; AI usage, storage,
  largest teams; System activity panel. `_AdminLayout` forwards the `Inspector` section.
- `_AuthFooter` (version · GitHub · Privacy), `_PeriodPicker`, `_StoragePanel`, `_ActivityFeed`
  partials; `Ui/Dfh.cs` (file size, relative time, initials, team colour, file-type label, version)
  and `Ui/DashboardUi.cs`. App version moved out of the status bar component.
- Deleted the unused `_LoginPartial.cshtml`; removed unused external-login plumbing from the Login and
  Register models (no providers configured, no ExternalLogin page).

**Bugs fixed along the way**
- Demo seed: versions were clamped to "now" (several versions "just now") and `Document.UpdatedAt`
  stayed at the creation date, so "Modified" contradicted the history. Versions now stop at the
  present, `UpdatedAt` = last version, summaries never dated in the future, and creation dates lean
  towards recent so "last 7 days" has activity.
- `AISettingsService.CreateDefaultSettingsAsync`: parallel first requests of a new user all inserted
  default settings and hit the unique index (12 `fail:` lines on a fresh demo DB under the crawler).
  Creation is now serialized in-process, and a lost race reads the existing row.

**CI** — the `docker` job now starts the built demo image and runs the Playwright smoke suite against
it (S10-8); traces are uploaded on failure.

## Verification
- `dotnet build -c Release`: 0 errors, no warnings in changed files. Unit tests **121/121**
  (`Category!=LiveStorage`), incl. 5 new visibility/counting tests (InMemory) and 3 on the SQLite
  demo fixture (both dashboard methods run on SQLite; document count = `DocumentService` count).
- Playwright smoke on the demo build: **11 passed, 1 skipped** (register: disabled in demo); on a
  non-demo build the register test and signed-out redirect pass.
- Crawler (fresh demo DB, both accounts): **306 pages, 0 errors**, **0 `fail:`/`crit:` lines**, 104 s.
- Screenshots at 1280×800 reviewed: dashboard, dashboard 30d, admin overview, admin's own dashboard,
  sign in (empty, required errors, invalid login), register (live rules), signed out, 404.

## Differences from the mocks (intentional)
- Dashboard "AI queue" → "AI summaries": documents without a summary + today's requests vs the daily
  limit (no queue or monthly limit in the backend).
- Storage shows the used total only (no quota); demo files are small, so sizes are in KB.
- Stat line "+N" counts versions (incl. first uploads) in the period; "All time" hides it.
- Activity has no filter icon and no move/share events (not recorded).
- Register has no consent checkbox (no terms page) and keeps Confirm password.
- Admin overview has no mock; it reuses the 3a structure.

## Deferred / notes
- Privacy page still uses the old content in the shell (Sprint 15 leftovers).
- Docker image not run locally; CI now runs it and the smoke suite on every push.
