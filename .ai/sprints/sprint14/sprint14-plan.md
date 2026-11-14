# Sprint 14: Admin, Settings, Profile

Part of the IDE redesign — see [`../../redesign-ide-plan.md`](../../redesign-ide-plan.md) for scope,
decisions and the sprint map. Design source: screens 3e (Admin · Users), 8c (Admin · Analytics),
3f (Settings · AI) and 8e (Profile) in `.ai/design/Doc-Flow-Hub redesign direction/design_handoff_docflowhub_ui/`;
Roles, User limits, System settings, user Create / Edit / Details and Change password have no mock
and follow these screens.

Status: **done** (2026-11-14, see [`sprint14-log.md`](sprint14-log.md)) · Branch: `redesign/sprint14-admin-settings-profile` → PR into `redesign/ide` (D1)

## Findings before the sprint (user decision 2026-11-14: "look + honesty")

The inventory showed settings screens that store values the app never reads:
- **System settings**: none of the 10 settings (AI daily limit, max upload size, public registration,
  maintenance mode, session timeout, password rules, cache expiry, AI model / max tokens) is read
  anywhere outside the settings service.
- **Settings · AI**: only the preferred model (Details "Generate summary", Compare "Compare with AI")
  and the on/off of summaries / comparisons (Upload's AI checkbox) have an effect. Auto-summarize,
  quality, max tokens, sensitivity, caching, cache duration and the custom API key do not; the
  summary after an upload always uses the default model.
- **User limits**: "Reset usage" and "Update default limits" returned success without doing
  anything; the limits are constants in `AIUsageTrackingService` (1000 a day, 100 an hour, $10 a day).

Decision: redesign the pages, remove the fake actions, and mark every setting that has no effect
with a clear "Saved, not applied yet" note. Wiring the settings is a separate piece of work for the
backlog (see the log).

## Decisions taken for this sprint

| # | Topic | Decision |
|---|---|---|
| S14-1 | Data | New read-only `IAdminBrowserService`: admin nav counts and mini stats, per-user aggregates for the users table (roles, teams, storage, AI requests in 30 days), user details, and the whole Analytics screen (uploads per day, AI usage per operation with cost, cache hit rate, failures, p95 latency, top users by storage, admin actions). Projected queries; the old Analytics service stays for anything else that uses it. |
| S14-2 | Admin nav | Overview, Users (count), Roles (count), User limits, Analytics, System settings, plus the 3e mini stats (active in 30 days, suspended, sign-ups in 7 days). No Audit log item: there is no page; admin actions appear on Analytics. |
| S14-3 | Users (3e) | Search box + Status / Role filters shown as tokens (like Documents), table with checkboxes, bulk bar (Assign role, Lock, Unlock, Message, Delete; Activate / Suspend dropped as duplicates of Unlock / Lock, see the log) on the existing bulk operation, row menu with the existing quick actions, numbered pager. Results are toasts after a redirect. No Export and no "Add to team" (no backend). |
| S14-4 | User pages | Details: header with status and roles, numbers from one aggregate query (the old page loaded up to 1000 documents and showed every user as active), teams, recent admin actions about the user, the quick actions and "Send message". Create / Edit on the form-page layout. |
| S14-5 | Roles | Table (name, description, users, created) + Create / Edit / Details (users in the role) / Delete (the Administrator role stays protected). |
| S14-6 | User limits | Table of today's usage against the real limits, with a note that the limits are fixed defaults. The fake actions go. |
| S14-7 | Analytics (8c) | 7d / 30d / 90d, stat line, four panels made of CSS bars and tables. The "Security events" panel shows the admin actions the app records (`UserActivityLogs`); failed logins, devices and downloads are not recorded. Chart.js is removed from the app (library, LibMan entry and both pages). |
| S14-8 | System settings | One form per category with typed controls (switch / number / text), reset per category, and the "Saved, not applied yet" banner. |
| S14-9 | Settings shell (3f / 8e) | Settings tree tool window: Account (Profile, Password), AI (Models & features). Profile merges Manage/Index, EditProfile and UploadProfilePicture into one page (avatar with Upload / Remove, name, bio, e-mail read-only with "verified"); the two old URLs redirect. Language, time zone, theme, sessions and delete account are not shown (no backend; dark theme only). |
| S14-10 | Settings · AI (3f) | Model, features, generation and API key sections in the 3f row layout; settings without effect carry the note; usage this month and a 14-day bar chart from the user's AI log. |
| S14-11 | Messages | `TempData["StatusMessage"]` is shown as a toast by the layout (error when it starts with "Error"); the pages no longer render it themselves. `css/legacy/admin.css` is deleted. |

## Tasks

### 14.0 Branch
- [x] `redesign/sprint14-admin-settings-profile` from `redesign/ide`.

### 14.1 Data
- [x] `IAdminBrowserService` + implementation + models; unit tests (InMemory) and SQLite demo tests.

### 14.2 Components
- [x] Admin nav counts + mini stats, numbered pager, bar charts (vertical / horizontal), settings tree, form rows (200px label column), section divider, note chip "not applied yet", avatar 64.

### 14.3 Pages
- [x] Admin: Users Index / Details / Create / Edit, Roles ×5, User limits, Analytics, System settings.
- [x] Settings · AI; Profile (merged), Change password; redirects for EditProfile / UploadProfilePicture.
- [x] Remove Chart.js and `css/legacy/admin.css`.

### 14.4 Verification
- [x] Build, unit tests, SQLite tests.
- [x] Playwright smoke (admin + user screens); write tests (non-demo): bulk suspend / activate, role create / edit / delete, user create / edit, profile save, password change, AI settings save, system settings save.
- [x] Crawler on a fresh demo DB; screenshots; docs; memory.
