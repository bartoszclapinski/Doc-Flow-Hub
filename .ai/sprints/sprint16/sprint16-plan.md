# Sprint 16: Public landing page

Design source: `DocFlowHub Landing.dc.html` and the "Landing page" section of the README in the
updated handoff `.ai/design/Landing-Page/Doc-Flow-Hub redesign direction.zip` (extracted to
`.ai/design/Landing-Page/handoff/`, git-ignored). Apart from that file and README section, the new
handoff is identical to the one used in Sprints 9–15.

The handoff reverses decision D2 (Sprint 10: anonymous `/` → Sign in, landing page deleted). Its
README: "Public marketing page at `/` for anonymous users (signed-in users are redirected to the
Dashboard) ... Copy is final English. Verify the licence (MIT is assumed), the changelog dates and
the local-run commands against the repo."

Status: **done** (2026-11-14, see [`sprint16-log.md`](sprint16-log.md)) · Branch: `feat/landing-page` → PR into `master`.
Merging into `master` redeploys the public demo, so it needs the user's separate approval.
User decisions 2026-11-14: corrected copy (fact-check table below), changelog from real git
milestones (L4), add an MIT `LICENSE` file (L5), CTA depends on demo mode (L2).

## Fact check of the mock copy

| Mock says | Repo reality | Proposed copy |
|---|---|---|
| `v2.3.0 released`, footer `v2.3.0` | No git tags; assembly version 1.0.0 | Pill "new interface · Nov 2026"; footer shows the real assembly version (as the status bar does) |
| ASP.NET Core 9, EF Core 9 | .NET 10, EF Core 10 | ASP.NET Core 10, EF Core 10 |
| Built on: SQL Server | SQL Server, SQLite on the demo | add SQLite |
| Upload the same file again → recognised by name and folder, stored as the next version | New versions are uploaded explicitly (Edit / row menu "Upload new version"); no name matching | "Upload a new version of a document; every previous one is kept" |
| Full history with author, date and short hash | True (`FileHash`, shown as short hash) | keep |
| Side-by-side or unified line diff between any two versions | No line diff (backlog); Compare shows two versions' metadata + AI comparison | "Compare any two versions, with an AI summary of what changed" |
| Restore any version in one click | No version restore (`RestoreDocumentAsync` is undelete) | drop |
| Mini diff under the history card | No diff | replace with the real AI comparison card (as in Compare) |
| Summary generated in the background on upload; "uploads never wait for AI" | True on upload (`Task.Run` after save); manual Generate on Details is synchronous | keep, wording "on upload" |
| Change list "linked to the exact lines in the diff" | No line links | "a plain-language list of what changed" |
| Per-user monthly limits set by admins | Daily request/cost limits per user (fixed defaults) | "Daily per-user AI limits" |
| Can be turned off per user | True (Settings · AI, `SummarizationEnabled`) | keep |
| `gpt-4o-mini · cached by file hash · 412 / 1000 req this month` | gpt-4o-mini default true; no hash cache claim verified; numbers invented | `gpt-4o-mini · generated on upload`, no usage numbers |
| Four team roles: Owner / Admin / Member / Viewer | `TeamRole` = Member, Admin; plus the team owner → 3 | three columns, matrix checked against the services |
| Palette: documents, folders, **people** and actions | Documents, projects, folders, actions | drop "people" |
| Keys: Space = Preview, Ctrl D = Download, F2 = Rename | Space = select, no Ctrl D binding, F2 = Edit details | Ctrl K, /, U, M, F2 Edit, Del, ? |
| Audit log: failed logins, new devices, role changes, bulk downloads | `UserActivityLog` exists; those event types not verified | describe only what is logged, or replace the tile with "Admin panel" |
| "works with JavaScript off for core flows" | Not tested | drop |
| Run locally: user-secrets + `dotnet ef database update` + `dotnet run` | README: clone, `dotnet run --project src/DocFlowHub.Web`, migrations on start, LocalDB or SQLite | copy the README commands |
| Changelog v2.1–v2.3 with dates | No releases | see L4 |
| Footer `MIT` | README says MIT, no `LICENSE` file | see L5 |
| Demo credentials `demo@docflowhub.com · Demo123!` | True (`DemoAccounts`) | keep, only when demo mode is on |

## Decisions

| # | Topic | Proposal |
|---|---|---|
| L1 | Routing | Anonymous `/` renders the landing (own layout, no IDE shell); signed-in users get the Dashboard as today. Sign in stays at `/Account/Login`. |
| L2 | "Try the demo" | Demo mode on: CTA goes to Sign in with the demo account pre-filled (existing click-to-fill box). Demo mode off (self-hosted): the CTA becomes "Sign in" / "Create account" and the demo credentials line is hidden. |
| L3 | Hero visual | A real 1200×740 screenshot (WebP + PNG fallback) of the Documents view on the demo seed, made by a new Playwright `landing hero` shot, `<img width=100%>` as the README asks. No HTML mock in production. |
| L4 | Changelog | Real milestones with real dates from git history (e.g. first release May 2025, admin panel Jul 2025, live demo, IDE redesign Nov 2026), no invented version numbers; "All releases" links to the GitHub commit history. Alternative: hide the section (the mock has a `showChangelog` switch). |
| L5 | Licence | Add an MIT `LICENSE` file (copyright holder: repo owner), or remove "MIT" from the README and the landing. Owner's call. |
| L6 | Styling | `css/dfh/landing.css` on the existing tokens and self-hosted fonts (`dfh/fonts.css`); no inline styles, no CDN. Dark only (the app is dark only). Fluid layout per the handoff: max 1200px, `auto-fit` grids, `clamp()` headings, sections stack below ~900px. |

## Tasks

1. `Index`: anonymous → landing view (`_LandingLayout`), signed-in unchanged. Remove the redirect.
2. `Pages/Shared/_LandingLayout.cshtml` + `css/dfh/landing.css`: nav (sticky, anchors, GitHub,
   Sign in, CTA), hero, built-on strip, sections 01–05, final CTA, footer (logo, version · licence,
   GitHub / Changelog / Privacy / Sign in).
3. Static illustration blocks in HTML/CSS with the corrected copy: version history card, AI summary
   card, AI comparison card, roles matrix, palette + shortcut grid, architecture tiles, run-locally
   terminal.
4. Hero screenshot: Playwright shot → `wwwroot/img/landing/hero.webp|png`.
5. Login: "Try the demo" link pre-fills the demo box (`?demo=1`), only in demo mode.
6. Demo middleware: make sure anonymous `/` and the landing assets are allowed (GET only, should be).
7. Tests: smoke `/` anonymous shows the landing and its CTAs (replaces "`/` → Login"),
   signed-in `/` still Dashboard, landing at 390px has no horizontal scroll; screenshots spec
   gets landing desktop + mobile; crawler run.
8. Docs: README (landing screenshot, LICENSE per L5), `redesign-ide-plan.md` D2 note, sprint log.

## Verification

Release build, unit tests, smoke + screenshots + write specs on the local runner, crawler 0 errors,
then CI on the PR. After the master merge (separate approval): live smoke against the demo.
