# Sprint 17: Clear demo sign-in

The user's feedback: on the demo, the sign-in page is unclear. Nothing says that clicking an e-mail fills in
the form. Nothing says which account is for what. Registration silently redirects to Sign in, with no reason
given. Document previews move to Sprint 18.

Status: **done** (2026-11-14, see [`sprint17-log.md`](sprint17-log.md)) · Branch: `feat/demo-sign-in` → PR into `master`.
Merging into `master` redeploys the public demo, so it needs the user's separate approval.
User decisions 2026-11-14: demo accounts **above** the form; buttons **sign in at once**; a short
note on Sign in **plus** an explanation on `/Account/Register` instead of the silent redirect.

## State before the sprint

- `Pages/Account/Login.cshtml`: the e-mail form, and under it a small "Read-only demo" box with
  two e-mails as link-style buttons (`.dfh-demo-fill`, `auth.js` fills the form) and "password: Demo123!".
- `Register.cshtml.cs`: in demo mode GET and POST redirect to Sign in with no message.
- `DemoModePageFilter` allows every POST to `/Account/Login`, so a new handler there needs no
  filter change.
- Landing "Try the demo" → `/Account/Login?demo=1` (pre-fills the user account, Sprint 16).

## Decisions

| # | Topic | Decision |
|---|---|---|
| S17-1 | Layout (demo mode only) | Heading "Explore the read-only demo" + "Pick an account — no sign-up needed.", two account cards, a collapsible "Why read-only? Why no sign-up?", then a divider "or sign in with e-mail" and the existing form. Without demo mode, Sign in is unchanged. |
| S17-2 | Account cards | **Demo user**: what it shows, checked against the seed (dashboard, projects with folders, team documents, version history and saved AI comparisons, AI summaries, command palette). **Demo administrator**: everything above plus the admin panel (users, roles, AI usage and limits, analytics). Each card: icon, title, 2-line description, primary button "Sign in as user" / "Sign in as admin" (full width on phones), credentials in small mono text for people who prefer typing. |
| S17-3 | One-click sign-in | Each card is a small form: `POST /Account/Login?handler=Demo` with `account=user|admin` and the antiforgery token. The server signs in with the `DemoAccounts` constants (the password is not in the form). Works without JavaScript. The handler returns 404 when demo mode is off. It honours a local `returnUrl` and logs the sign-in. |
| S17-4 | Why read-only, why no sign-up | `<details>` on Sign in. Points: the demo is public and shared, so changes are blocked and everyone sees the same data. It is reset when the server restarts, and the free server sleeps when idle. AI generation is off because it costs money; the saved summaries and comparisons are shown instead. To try uploads and your own account, run your own copy (GitHub link, `dotnet run`). |
| S17-5 | Register in demo mode | `/Account/Register` shows the same explanation in the auth card (no form), with "Explore the demo" (→ Sign in) and "Run your own copy" (GitHub). POST still redirects (defence in depth). |
| S17-6 | Clean-up | Remove `.dfh-demo-fill` / the old demo box and the fill code in `auth.js`. Landing "Try the demo" links to plain `/Account/Login` (the cards are the choice now). |

## Tasks

1. `LoginModel.OnPostDemoAsync(string account, string? returnUrl)` + view: cards, details, divider.
2. CSS in `components.css` (auth section): `.dfh-demo-accounts`, `.dfh-demo-account`, divider; phone
   layout (390px).
3. `Register` in demo mode: explanation view instead of the redirect on GET.
4. Remove the old demo box, its CSS and the `auth.js` fill code; landing link.
5. Tests: smoke: the cards are visible and explained, "Sign in as user" → Dashboard,
   "Sign in as admin" → the Admin rail item is visible, `/Account/Register` explains instead of redirecting,
   the landing CTA → Sign in with the cards (replaces the "clicking a demo account fills" test and the
   `?demo=1` check). With demo mode off: no demo section, `?handler=Demo` → 404, Register form unchanged.
   Screenshots: sign-in (demo) at 1280 and 390, register (demo).
6. Docs: sprint log, README demo line if needed.

## Verification

Release build, unit tests, smoke + screenshots (demo), write specs + the non-demo checks, crawler,
CI on the PR. After the master merge (separate approval): live smoke against the demo.
