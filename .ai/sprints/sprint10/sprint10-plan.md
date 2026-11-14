# Sprint 10: Sign in, Register, Dashboard, Admin overview

Part of the IDE redesign — see [`../../redesign-ide-plan.md`](../../redesign-ide-plan.md) for scope,
decisions and the sprint map. Design source: screens 3g (Sign in), 8d (Register) and 3a (Dashboard)
in `.ai/design/Doc-Flow-Hub redesign direction/design_handoff_docflowhub_ui/`.

Status: **done 2026-11-13** — see [`sprint10-log.md`](sprint10-log.md) · Branch: `redesign/sprint10-auth-dashboard` → PR into `redesign/ide` (D1)

## Goal
The first screens a visitor sees are fully redesigned: Sign in, Register, the signed-out page and the
Dashboard, plus the Admin overview by analogy. These pages have no page-level CSS left. The shared
components they introduce (`components.css`) are reused by Sprints 11–14.

## Decisions taken for this sprint

| # | Topic | Decision |
|---|---|---|
| S10-1 | Admins opening `/` | No more redirect to `/Admin`. `/` is everyone's personal Dashboard, so the rail's Dashboard icon means the same for every user; the admin overview stays under the Admin rail icon. |
| S10-2 | "AI queue" (no job queue in the backend) | The panel becomes **"AI summaries"**: number of documents without a summary and the newest few of them ("no summary yet", not "queued"), plus a meter of the user's AI requests **today** vs the existing daily limit. Nothing pretends to be a queue. |
| S10-3 | "Last 7 days" filter | Implemented as a plain GET dropdown (`?period=7|30|all`). It changes the "+N" stat and the activity feed range. |
| S10-4 | Storage quota "/ 10 GB" | Not shown (no quota in the backend): used total only, stacked bar relative to the total. |
| S10-5 | Activity feed | Built from data that exists: version uploads (with the first 7 chars of `FileHash`), AI summaries, AI comparisons, team joins (`TeamMember.JoinedAt`). Move/share events are not recorded, so they are not shown. Feed filter icon not shown. |
| S10-6 | Register consent checkbox | Dropped: there are no terms / privacy-policy pages to agree to. |
| S10-7 | Register password length | `InputModel` minimum raised from 6 to 8 so client validation matches Identity (`RequiredLength = 8`). |
| S10-8 | Playwright in CI | Yes: new `e2e` job runs the Release build in demo mode on SQLite and runs the smoke suite. Guards the remaining redesign sprints. |
| S10-9 | Dashboard data | One new read-only `IDashboardService` (aggregate queries) instead of the old page code that loaded up to 1000 documents per user and, on the admin page, looped over every user. No new tables. |

## Tasks

### 10.0 Branch
- [x] `redesign/sprint10-auth-dashboard` from `redesign/ide`.

### 10.1 Shared components (`wwwroot/css/dfh/components.css`, loaded by both layouts)
- [x] Page header (`dfh-page-header`, 48px) + inline stat line (`dfh-statline`).
- [x] Section header (`dfh-section-header`, 34px).
- [x] Grid table (`dfh-table`): header 28, rows 32, hover, link rows, compact variant without checkbox.
- [x] File-type label (`dfh-filetype`), colour square (`dfh-swatch`, exists), chip (`dfh-chip`),
      status dot (`dfh-dot`), meter (`dfh-meter`), stacked bar + legend (`dfh-stackbar`).
- [x] Filter dropdown button (`dfh-filter-btn`).
- [x] Bottom strip panels, team list, activity feed (`dfh-activity`), empty state (`dfh-empty`, simple
      variant).
- [x] Auth card, fields, demo-credentials box, password rules + strength meter, auth footer.
- [x] `.input-validation-error` styled like `.is-invalid` (jquery-validation-unobtrusive).

### 10.2 Data: `IDashboardService`
- [x] Core: `Models/Dashboard/*` DTOs, `IDashboardService` with `GetUserDashboardAsync(userId, period)`
      and `GetAdminOverviewAsync(period)`.
- [x] Infrastructure: `DashboardService` — same visibility rule as `DocumentService.GetDocumentsAsync`
      (owner or a member of the document's team, not deleted); counts, recent documents with location
      (folder path or project), version, size, owner, AI-summary flag; storage by file type; teams
      with the user's role and member count; activity feed (S10-5); AI usage today vs daily limit.
- [x] Register in `DependencyInjection`.
- [x] Tests on the SQLite demo fixture: both methods succeed on SQLite and the document count matches
      `DocumentService` for the demo user.

### 10.3 Sign in (3g), Register (8d), signed out
- [x] `Login.cshtml` on `_AuthLayout`: 380px card, Email/Password, "Keep me signed in", 34px
      submit, "No account? Create one" (hidden in demo), demo box with click-to-fill, footer
      (version · GitHub · Privacy). Forgot-password link removed (no page). Old theme toggle,
      split brand panel and 370 lines of CSS deleted.
- [x] `Register.cshtml` on `_AuthLayout`: 400px card, first/last name grid, email, password with
      live rules (8+, number, uppercase, symbol) and 4-segment strength meter, confirm password
      (decision 17), submit disabled until the rules pass, "Already have an account? Sign in".
      `wwwroot/js/dfh/register.js`.
- [x] `Logout.cshtml`: "You're signed out" card with Sign in button (`_AuthLayout`).
- [x] `_AuthLayout`: footer partial, validation scripts available, `components.css`.

### 10.4 Anonymous `/` and Dashboard (3a)
- [x] `/` signed out → redirect to Sign in; marketing landing page deleted (D2). Admin redirect
      removed (S10-1).
- [x] Dashboard: Overview header with stat line (documents, versions, teams, shared with me, +N in
      period) and period dropdown; Recent documents table (row → Details); bottom strip AI summaries
      / Storage / Teams; Activity panel 360px on the right. Empty states for a new user.
- [x] ~1,400 lines of page CSS/JS in `Index.cshtml` deleted.

### 10.5 Admin overview (by analogy)
- [x] Same structure, system-wide: stat line (users, admins, documents, versions, teams, storage,
      +N in period); "Recent users" table (→ user details); bottom strip AI usage (requests today,
      success rate, tokens in period) / Storage by type / Largest teams; Activity panel with
      system-wide events and new users.
- [x] `_AdminLayout` forwards the `Inspector` section.

### 10.6 Tests, CI, docs
- [x] Playwright smoke: signed-out `/` lands on Sign in; dashboard sections render; admin `/` shows
      the personal dashboard; demo box click fills the form.
- [x] CI `e2e` job (S10-8).
- [x] Definition of done from the master plan (build, unit tests, crawler 0 errors, screenshots vs
      mocks, no page-level CSS on finished pages, demo POSTs still blocked).
- [x] `sprint10-log.md`; master plan status.
