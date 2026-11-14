# Sprint 14 log: Admin, Settings, Profile

Date: 2026-11-14 · Branch `redesign/sprint14-admin-settings-profile` → PR into `redesign/ide` · Plan: [`sprint14-plan.md`](sprint14-plan.md)

## Done

**Data**: `IAdminBrowserService` (`Core`) + `AdminBrowserService` (`Infrastructure`): admin nav
counts and mini stats, user rows with roles / documents / storage / last sign-in, one user with usage,
teams and recorded actions, analytics for 7 / 30 / 90 days (sign-ups, uploads, AI requests per day,
AI operations with p95 latency, storage per user, recorded actions), AI usage today per user, and a
user's own AI usage (month, today, last 14 days). The AI log is aggregated in memory, so it works the
same on SQL Server and SQLite. Unit tests (InMemory) and SQLite demo tests.

**Admin pages** (Chart.js and `css/legacy/admin.css` are gone):
- `_AdminLayout`: nav with counts, mini stats (Active 30d, Locked, Sign-ups 7d).
- Users (3e): search box, Status / Role filters as tokens, table with checkboxes, bulk bar (Assign
  role, Lock, Unlock, Message, Delete) with confirmation dialogs, row menu with quick actions,
  numbered pager. Your own account is left out of lock / delete / remove role.
- User details, Create, Edit; Roles Index / Details / Create / Edit / Delete.
- User limits: AI usage today per user with bars, filters All / Near / Over, an info banner with the
  fixed limits.
- Analytics (8c): period switch, four CSS bar panels.
- System settings: typed controls per category, reset a category, warning banner.

**User settings**: one settings shell with a tree (Account → Profile, Password; AI → Models &
features) and a filter box. Profile (8e) merges Edit profile and Upload picture (picture uploads on
select); the old URLs redirect. Change password reuses the sign-up password rules. Settings · AI (3f)
with dirty tracking, Discard, Reset to defaults and a usage section with a 14-day bar chart.

**Toasts**: `TempData["StatusMessage"]` is shown as a toast by the layout (error when it starts with
"Error").

## Honesty pass (user decision 2026-11-14: "look + honesty")

The app stores more than it reads. Instead of wiring every setting in this sprint, the pages now say
what is real:
- **System settings**: none of the 10 values is read by the app. A warning banner says so.
- **Settings · AI**: only the default model and the Summaries / Version comparisons switches have an
  effect. Everything else carries a "not applied yet" chip.
- **User limits**: "Reset usage" and "Update default limits" returned success without doing anything.
  They are removed; the banner states the fixed limits (1,000 requests / day, 100 / hour, $10 / day,
  constants in `AIUsageTrackingService`).
- **Users**: "Activate / Suspend" and "Unlock / Lock" were the same lockout in
  `UserManagementService`; `ApplicationUser.IsActive` is never set or checked. Only Lock / Unlock are
  offered, and the status column shows Active / Locked / Unconfirmed.
- **Roles**: the forms had a Description field, but Identity roles have no column for it; the value
  was dropped and the save reported success. The field is removed. A new role is only a label: the
  app checks the Administrator role and nothing else (the Create page says so).
- **E-mails**: welcome and password-change e-mails are placeholders; their checkboxes are hidden.

Backlog: wire the system settings (limits, upload size, password rules, maintenance mode) and the AI
preferences, or remove them; add a real suspend if it is needed.

## Bugs fixed

- **Documents pager (introduced in Sprint 11)**: `?page=2` bound to the Razor Pages route value
  "page", so page 2 never opened. Fixed with `[FromQuery]`; smoke test added.
- **Decimal fields on a non-English server**: the form binder used the server culture, so on a Polish
  machine saving Settings · AI failed validation silently (0.7 is not a number in pl-PL). The app now
  uses en-US for requests, and the AI page shows validation errors.
- **Users role filter was ignored** by `SearchUsersAsync`.
- Admin user Create / Edit and Change password accepted 6-character passwords; Identity requires 8.
- The Administrator role could be renamed, which locks everyone out of the admin area.
- An admin could suspend, lock, delete or demote themselves from the users list.
- Admin Analytics never filled `UsageTrends` / `ErrorStats`; user details showed `IsActive` hard-coded.
- `auth.js` failed on forms without a submit button; the busy state ignored `event.submitter`.

## Verification

- Release build: 0 errors. Unit tests: 150 / 150 (`Category!=LiveStorage`).
- Playwright on a fresh demo DB: smoke 26 passed, 13 skipped (writes); screenshots 9 passed.
- Write specs without demo mode: `admin-actions` (new: user create / lock / unlock / role / edit /
  delete, self-protection, role create / rename / delete, system settings save + reset, profile, AI
  settings, wrong current password), `documents-actions`, `workspace-actions`: 12 / 12.
- Crawler: 375 pages (demo user 167, admin 208), 0 errors, no fail / crit lines in the app log.
