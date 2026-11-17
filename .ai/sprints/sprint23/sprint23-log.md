# Sprint 23 log: An upload screen that shows what the app can do

Plan: [`sprint23-plan.md`](sprint23-plan.md) · Branch `feat/versions-upload` (with Sprint 22) → PR into `master`.

## What was built

- **The old screen, looked at again:** the pre-redesign upload page (commit `beedb1c`) was built in a
  scratch worktree and screenshotted, so the user could compare it with the current one. It had:
  - the "AI-Powered Features" panel: a summary switch, a model picker, a 0–100% quality slider, a cost /
    time / quality estimate and "Your AI preferences";
  - the `.doc` tip;
  - progress steps.
- **Layout:**
  - **Wide screens:** the file and the details on the left, the **"AI on upload"** panel on the right.
    The panel is sticky and violet, like the other AI parts of the app.
  - **Narrow containers (and phones):** file, then the AI panel, then the details.
  - The layout follows a container query on the form, not the window, so the inspector's width counts.
- **AI panel:**
  - the "Generate an AI summary" switch, with a sentence on what happens;
  - **Model:** every model, grouped by provider (★ marks the default), with a line on what it is good
    at;
  - **Summary detail:** Short / Standard / Detailed, with what each gives;
  - **Estimate:** cost, time and tokens. It is computed for a typical one-page document, then for the
    picked file (from its size and type);
  - **"Remember as my defaults":** saves the switch, the model and the detail to the AI settings;
  - AI requests today, where the API key comes from, and a link to Settings · AI.
  - When AI summaries are off for the account, the panel says so and links to the settings.
- **Made real** (the old model choice and slider were cosmetic):
  - **Model:** `CreateDocumentRequest.SummaryModel` and `SummaryDetail` reach the background summary
    through `IDocumentSummaryService.RegenerateSummaryAsync(id, model, detail)`.
  - **Detail:** `SummaryDetail` (Short / Standard / Detailed) changes the prompt
    (`DocumentSummaryService.BuildPrompt`). Standard is the prompt the app always used, and the answer
    keeps the shape the parser reads.
  - **Defaults:** they come from the AI settings: `PreferredModel`, `AutoSummarizeOnUpload`, and the
    detail mapped from `QualityPreference` (below 0.45 Short, below 0.8 Standard, from 0.8 Detailed).
    The old default 0.7 stays Standard.
  - **The server** asks for no summary for a file without readable text: images, `.doc`.
  - **Estimate figures:** `AIModelFacts` holds the list prices per million tokens, the typical seconds
    and a strengths line per model. `SummaryDetailHelper` holds the typical output tokens.
- **"What will happen"** appears once a file is picked:
  - the type and size against the limit;
  - whether its text can be read;
  - the AI plan ("An AI summary with Claude Haiku 4.5, standard: < $0.01, ≈ 5 s.").

  For an image or a `.doc`, the switch turns itself off and is disabled, with the reason. Another file
  brings the user's own choice back.
- **Upload with progress:**
  - `upload.js` sends the form itself (XHR, `X-DFH-Upload: 1`) and shows:
    - a progress bar with the real percentage;
    - the steps "Upload → Check and store the file → AI summary (continues in the background / not
      requested)".
  - **The server's answers to this upload:**
    - success: JSON `{ url }`, and the toast is kept for the next page;
    - a refused file: JSON `{ error }` with 400. The error shows on the form, nothing navigates away,
      and the form keeps its values.
  - Without JS the form posts as before. The demo keeps the Upload button disabled, but the panel can
    be tried in full.
- **Small things:**
  - the dropzone shows the `.doc` tip;
  - Settings · AI says the upload screen starts with the default model, and explains what the Quality
    slider now drives.

## Differences from the plan

- **The detail changes the prompt only, not the output token limit.** `IAIService` has no per-call
  limit, and adding one touches both providers and the router. The prompt asks for a length, and the
  models keep to it.
- **The steps are "Upload → Check and store the file → AI summary"**, not "Upload → Text extraction →
  AI summary". Text extraction is part of the background summary, so the upload request cannot report
  it.
- **The Details AI card does not show "Summarising…".** It still says "No summary yet" until the
  summary is ready; the toast says it will appear in a moment.

## Tests

- **Unit (12 new, 261 total):**
  - `QualityPreference` → detail at the borders, round trips, and the old default stays Standard;
  - every model has prices, time and strengths, and the output tokens grow with the detail;
  - the prompt for each detail, and that it keeps the parser's shape;
  - an upload with Sonnet / Detailed calls `RegenerateSummaryAsync` with exactly that model and detail.
- **E2E smoke (demo):**
  - **"the AI panel shows…":**
    - the defaults (Haiku, Standard, on, System API) and the typical-document estimate;
    - a model change updates the strengths; the detail note follows;
    - a 195 KB text file: "For this file (195 KB)" and a cost above a cent, and the three checks with
      Opus / detailed;
    - a PNG disables the switch ("Images have no text to summarise."); a `.doc` gives the re-save tip;
      a `.md` brings the choice back; the switch off shows "the switch is off";
    - Upload is disabled in the demo; no page errors.
  - **Phone (390px):** the AI panel comes before the details, and nothing scrolls sideways.
- **E2E write specs (non-demo):**
  - **Remember:** the switch off, Sonnet, Detailed and "Remember", then upload. The upload page then
    starts with those choices. "Reset to defaults" in Settings · AI brings back Haiku / Standard / on.
    The test deletes its document.
  - **Refused file:** a fake PNG shows the server's "does not match" error on the form; the page stays,
    the progress hides, and the button and title are back.
  - All earlier write specs now upload through the progress upload and still pass.
- **Screenshots:** `upload screens` adds upload-empty, upload-file, upload-image and upload-390.

## Verification

- Release build: 0 errors. Unit tests: 261/261.
- **Demo mode, fresh database:**
  - smoke: 48 passed, 2 skipped (non-demo only);
  - screenshots: 19 passed, 2 skipped (on demand);
  - crawler: 1,112 pages (522 user + 590 admin), 0 errors;
  - no `fail` or `crit` log lines.
- **Demo mode off, after a fresh seed:** write specs 22/22. The two `fail` log lines are the text
  extractor rejecting the deliberately damaged PDF and DOCX; they are expected.
- Port 5199 was free after each run.
