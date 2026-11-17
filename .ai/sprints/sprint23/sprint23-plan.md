# Sprint 23: An upload screen that shows what the app can do

The upload screen from before the redesign (Sprint 12) offered more than the current one. In the
current IDE layout the screen looks empty, and its strongest part, the AI summary, is a single checkbox
below the form.

Status: **plan, waiting for the user's go** · Branch: `feat/versions-upload` (after Sprint 22) → PR into `master`.

## What the old screen had (commit `eef3618`)

- An "AI-Powered Features" panel:
  - a "Generate AI Summary" switch;
  - an AI model picker;
  - a quality slider;
  - cost / time / quality indicators per model;
  - a box with "Your AI preferences".
- **Progress:** "Upload file → AI processing → Complete", with the AI stage explained while it ran.
- **Hints:** the allowed types and size, and a tip that `.doc` cannot be summarised (re-save as `.docx`).

A screenshot of the old screen (taken from a build of commit `beedb1c`) confirmed this on 2026-11-17.

## What the current screen has

A dropzone and file row, then title, project, folder, workspace, categories and description, then the
"Generate summary" checkbox. The inspector shows the destination tree and usage (storage, AI requests
today).

The old model choice and quality slider were partly cosmetic:

- uploads always summarise with the default model;
- `QualityPreference` is stored but nothing reads it.

## Decisions

| # | Topic | Decision |
|---|---|---|
| S23-1 | Layout | Two parts side by side on wide screens: **the file and its details** on the left as now, and an **"AI on upload" panel** on the right, in the same violet AI style as the Details AI card. The panel takes the place of the checkbox, so the AI part is visible without scrolling. On phones the panel comes after the details. |
| S23-2 | AI panel | It holds: <ul><li>the "Generate AI summary" switch;</li><li>the **model** (the models available on this server, as in Settings · AI), each with a short line: speed, cost per summary (estimated from the file size and the model's price) and strengths;</li><li>the **summary detail**: Short / Standard / Detailed;</li><li>an **estimate** card, as on the old screen: cost, time and detail for the chosen model and file;</li><li>"AI requests today: 1 / 1,000";</li><li>where the key comes from ("System API" or "Your own key");</li><li>"Defaults from Settings · AI" with a link there.</li></ul> |
| S23-3 | Made real | <ul><li>The chosen model is the model the upload summary uses.</li><li>The summary detail changes the prompt and the output limit, so it is a real choice; the old 0–1 slider meant nothing.</li><li>The defaults come from the user's AI settings: model, auto-summarise, and the detail mapped from `QualityPreference`.</li><li>An option "Remember as my default" saves them back.</li></ul> |
| S23-4 | Before the upload | When a file is picked, a "What will happen" card: <ul><li>the type check;</li><li>the text that can be read (PDF / DOCX / TXT / MD: yes; images: no AI summary; `.doc`: no AI summary, with the re-save tip);</li><li>the estimated AI cost.</li></ul> The summary switch turns itself off, with the reason, for types that cannot be summarised. |
| S23-5 | During the upload | A progress card with three steps, "Upload → Text extraction → AI summary", and a real upload percentage (XHR progress). The summary still runs after the page moves on, so the third step says "continues in the background" and the Details AI card shows "Summarising…" until it is ready. Without JavaScript the form posts as now. |
| S23-6 | Demo | The screen is read-only in the demo (the upload is blocked), but every option is visible and can be tried, including the cost estimate. Only the final Upload stays disabled, so the demo shows the screen in full. |

## Questions for the user

1. **Choices on the screen:** are model choice + summary detail (instead of the old quality slider) the
   right ones? Is there anything else from the old screen you want back?
2. **Order:** this sprint comes after Sprint 22 (restore and compare any pair). Should it go first
   instead?

## Out of scope

Uploading several files at once, uploading a folder, OCR for scanned PDFs and images.
