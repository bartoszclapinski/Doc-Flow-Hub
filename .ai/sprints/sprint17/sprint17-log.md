# Sprint 17 log: Clear demo sign-in

Plan: [`sprint17-plan.md`](sprint17-plan.md) · Branch `feat/demo-sign-in` → PR into `master`.

## What was built

- **Sign in in demo mode** (`Pages/Account/Login.cshtml`):
  - heading "Explore the read-only demo";
  - two account cards (Demo user, Demo administrator), each saying what you can see there, with the credentials in small mono text;
  - a collapsed "Why read-only? Why no sign-up?";
  - a divider "or sign in with e-mail", then the existing form, now with a secondary button.

  Without demo mode the page is unchanged ("Sign in" + "Create one").
- **One-click sign-in:** `LoginModel.OnPostDemoAsync(account, returnUrl)`:
  - each card is a small form (`?handler=Demo`, `account=user|admin`, antiforgery), so it works without JavaScript;
  - the password comes from `DemoAccounts`, not from the form;
  - returns 404 when demo mode is off;
  - follows `returnUrl` only when it is local, clears the e-mail form's model state, updates `LastLoginAt` like the normal sign-in, and logs the account.

  `DemoModePageFilter` already allows every POST to `/Account/Login`.
- **`Pages/Shared/_DemoExplainer.cshtml`** (`model bool` = open) explains:
  - why the demo is public and read-only;
  - the reset on restart and the sleeping free server;
  - why AI generation is off (cost) and that saved summaries are shown instead;
  - how to run your own copy (`dotnet run`, link to the README section).
- **Register in demo mode:** GET shows "No sign-up in the demo" with the explanation open, plus "Explore the demo" and "Run your own copy", instead of a silent redirect. POST still redirects, as defence in depth.
- **Removed:** the old dashed demo box, `.dfh-demo-fill` and its CSS, the fill code in `auth.js`, and the landing's `?demo=1` link. "Try the demo" now opens plain Sign in.
- **CSS** (`components.css`, auth section):
  - `.dfh-auth-card--demo` is 460px wide;
  - styles for the account cards, the "why" section and the divider;
  - under 600px: icons hidden, full-width 40px buttons.

## Tests

- **`helpers.signIn`** clicks `#login-submit`. The first submit button on the demo Sign in page is now a demo card.
- **smoke:**
  - the old "clicking a demo account fills the form" test is replaced by "accounts are explained; one click signs in as user or admin" (the explanation opens, the user has no Admin rail item, the admin has one, the e-mail form still works);
  - new: "demo: /Account/Register explains why there is no sign-up";
  - new: demo sign-in on a phone (full-width button, no sideways scroll);
  - new: "without demo mode: no demo accounts, and the demo sign-in handler does not exist" (POST with a valid antiforgery token → 404);
  - the landing CTA test and the Register skip condition were updated.
- **screenshots:** `sign-in-390.png` (full page) added to the signed-out screens. The register form shot skips when there is no form.

## Verification

- Release build: 0 errors. Unit tests: 150/150.
- Demo mode:
  - smoke: 34 passed, 2 skipped (both non-demo only);
  - screenshots: 15 passed, 2 skipped (on demand);
  - crawler: 375 pages (167 user + 208 admin), 0 errors.
- Demo mode off (same database):
  - write specs: 14/14;
  - landing, sign-in and register tests: 5 passed, 3 skipped (demo only).
- App logs: 0 `fail`/`crit` lines. Port 5199 was free after each run.
