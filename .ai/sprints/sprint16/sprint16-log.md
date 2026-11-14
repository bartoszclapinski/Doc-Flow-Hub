# Sprint 16 log: Public landing page

Plan: [`sprint16-plan.md`](sprint16-plan.md) · Branch `feat/landing-page` → PR into `master`.

## What was built

- **Routing (L1):** `IndexModel.OnGetAsync` returns `Partial("_Landing")` for signed-out visitors
  instead of redirecting to Sign in. Signed-in users (admins included) still get the Dashboard.
- **`Pages/Shared/_Landing.cshtml`:** a standalone HTML document (no app chrome, no Bootstrap CSS).
  It contains: sticky nav with section anchors, hero, "built on" strip, sections 01–05 (versioning, AI, teams + keyboard,
  architecture + run locally, changelog), final call to action, footer with the assembly version and
  "MIT". Skip link, `aria-labelledby` sections, the roles matrix is a real `<table>` with hidden
  yes/no text, illustration cards are `role="img"` with a description.
- **`css/dfh/landing.css`:** built on the existing tokens and self-hosted fonts. Layout follows the handoff:
  max 1200px, auto-fit grids, `clamp()` headings. Two-column sections sit side by side from 936px.
  The AI section has its text first in the markup and moves the card left only when the columns sit
  side by side. Below 900px the anchors are hidden; below 600px the gutters are 16px, the CTAs go full width and the
  history card drops the time column.
- **Copy (decision: corrected):** every claim checked against the code (table in the plan). Notable
  changes from the mock: ASP.NET Core / EF Core 10, three team roles (Owner, Admin, Member) with a
  matrix taken from `TeamService`. Two claims were replaced: the line diff became the AI comparison card, and the "monthly limits set by admins"
  became the fixed 1,000/day and 100/hour limits. These were dropped: restoring a version, matching uploads by
  name, Ctrl D, people search and v2.3.0.
- **Hero (L3):** `wwwroot/img/landing/documents.jpg` is a real screenshot of the Documents view on the demo seed, 1200×740 at 2×, JPEG q82, 282 KB.
  It was made by the new `landing hero` test (`LANDING_SHOTS=… npx playwright test screenshots.spec.ts -g "landing hero"`).
- **Changelog (L4):** four real milestones with dates from the git history (Jul 2025 admin panel,
  Jul 2026 Anthropic + text extraction + demo mode, Nov 2026 public demo, Nov 2026 new interface).
  "Full history on GitHub" links to the commit list.
- **CTA (L2):** in demo mode, "Try the demo" links to `/Account/Login?demo=1`, and `auth.js` fills in the
  demo user account; the final card shows the demo credentials. With demo mode off, the page shows
  "Create account" / "Sign in" and no credentials.
- **Licence (L5):** `LICENSE` (MIT, 2025-2026). The README's licence section links to it.
- **Docs:** README (landing line under Interface, licence link), `redesign-ide-plan.md` (D2 superseded,
  Sprint 16 entry). No landing screenshot was added to the README: the README already opens with the same Documents view.

## Fixes found by the screenshot pass

- Below 600px, `.dfh-lp-wrap { padding: 0 16px }` reset the sections' top padding, so section
  headings stuck to the card above. The rule now sets the side padding only.
- The architecture tiles wrapped 4 + 2 at full width; the grid minimum is now 300px, giving 3 × 2.

## Tests

- smoke: "`/` goes straight to Sign in" replaced by: landing renders (h1, five sections, hero image
  loaded, MIT in the footer) and the CTA works in both modes; anchors scroll and there is no
  horizontal scroll at 390px; Sign in from the nav.
- screenshots: `landing page` (1280, full page, 1024 and 390 full page), `landing hero` (on demand).

## Verification

- Release build 0 errors; unit tests 150/150.
- Demo mode: smoke 32 passed + 1 skipped (registration); screenshots 15 passed + 2 skipped (on-demand
  readme / landing hero); crawler 372 pages (163 user + 209 admin), 0 errors.
- Demo mode off (same database): write specs 14/14; landing tests 3/3 (Create account path); `/` has
  "Create account" and no demo credentials.
- App logs: 0 `fail`/`crit` lines in every run; port 5199 free after each run.
