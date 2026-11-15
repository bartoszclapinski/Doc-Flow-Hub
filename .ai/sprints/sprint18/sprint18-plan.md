# Sprint 18: Document preview (text, Markdown, images; text view for PDF and DOCX)

The user's feedback: opening a document shows a big empty window that only offers a download. This
sprint replaces the Details placeholder (decision D5) with a real preview for the simple formats.
Every document in the demo gets at least a text view. Real page rendering follows later:
PDF.js in Sprint 19, a DOCX → HTML view in Sprint 20.

Status: **done** (2026-11-15, see [`sprint18-log.md`](sprint18-log.md)) · Branch: `feat/document-preview` → PR into `master`.
Merging into `master` redeploys the public demo, so it needs the user's separate approval.
User decisions 2026-11-15: text view for PDF / DOCX now (S18-1); Markdown rendered with a Source
toggle (S18-3); remote images in Markdown are shown as links, not loaded (S18-3); images are part of this
sprint (S18-5).

## State before the sprint

- `Documents/Details` shows a card (type, title, size, "A preview isn't available yet", Download).
  On phones it is a 220px band.
- Download is `POST ?handler=Download&versionId=` after an access check through `IDocumentBrowserService`.
  The demo filter allows that handler.
- `ITextExtractionService` (PdfPig for PDF, OpenXml for DOCX, plain read for TXT / MD) already feeds the AI.
- Upload accepts pdf, doc, docx, txt, md, jpg, jpeg, png, gif (30 MB). The demo seed has real pdf,
  docx, txt and md files (`DemoFileFactory`), and no images.

## Decisions

| # | Topic | Decision |
|---|---|---|
| S18-1 | What gets which preview | TXT → text. MD → rendered Markdown. JPG / PNG / GIF → image. PDF and DOCX → **text view** from `ITextExtractionService`, labelled "Text only — page layout preview is coming". DOC and anything else → the existing download card with the text "No preview for .doc files". |
| S18-2 | Where it is built | A new read-only `IDocumentPreviewService.GetPreviewAsync(userId, documentId, versionNumber?)` in Core / Infrastructure. It checks access the same way as Details and returns `DocumentPreview { Kind, Text or Html, Truncated, Encoding, Version }`. Details renders text and Markdown **server-side in the page**, so no extra request and it works without JS. Images come from a GET handler (S18-5). |
| S18-3 | Markdown | Markdig (BSD-2) with `DisableHtml()`, so raw HTML in the file is shown as text. Extensions: tables, task lists, autolinks, emphasis extras. Links are rewritten: only `http`, `https`, `mailto` and `#anchors` stay, everything else becomes plain text. External links get `rel="noopener noreferrer nofollow"` and `target="_blank"`. Remote images are **not loaded**: they are shown as a link `image: alt → url`. A "Rendered / Source" switch uses `?view=source`, a plain link that works without JS. Typography CSS `.dfh-md` uses the dark tokens: headings, lists, tables, code blocks, blockquotes. |
| S18-4 | Text (TXT, and PDF / DOCX text) | Monospace with line numbers (CSS counters), a wrap switch (`?wrap=0`, default on). Encoding: BOM (UTF-8 / UTF-16), otherwise strict UTF-8, otherwise Windows-1250 (`CodePagesEncodingProvider`); the encoding is shown in the preview bar. Shows at most 256 KB of text: "Showing the first 256 KB — download the file for the rest". |
| S18-5 | Images | `GET /Documents/Details/{id}?handler=File&versionId=` serves the file **inline** only for jpg / jpeg / png / gif. The content type comes from the extension, which the upload already checked against the file signature. Headers: `X-Content-Type-Options: nosniff`, `Content-Security-Policy: default-src 'none'; sandbox`, `Cache-Control: private`, and an `ETag` from the version hash. The preview fits the image in the window, with "Actual size" / "Fit" (JS, the default fit works without JS). Other types → 404 on this handler. |
| S18-6 | Versions | The current version by default. `?version=N` previews an older one with a bar: "Viewing v2 of 4 · Back to current". The Versions tab gets a "Preview" button per version. |
| S18-7 | Layout | The preview fills the editor column on the sunken background and scrolls inside it. A preview bar shows type label, version, size, encoding, the view switches and Download. Markdown sits on a readable "page" (max 820px). Text and extracted text are full width. On phones the preview flows in the page (no 220px band) and the inspector tabs come after it, as today. |
| S18-8 | Demo | All preview routes are GET, so they work in the demo. AI-free. No new seed data: the demo already has txt, md, pdf and docx. Images are tested on the non-demo run. |

## Tasks

1. Core: `DocumentPreview` model + `IDocumentPreviewService`. Infrastructure: `DocumentPreviewService`
   (access check, version pick, size cap, encoding detection, Markdown pipeline + link/image rewriter,
   extracted text for PDF / DOCX). Markdig package. DI.
2. Details: `OnGetAsync` loads the preview (`version`, `view`, `wrap`). `OnGetFileAsync` serves images inline.
   Markup for the preview bar and the four kinds. Versions tab "Preview" buttons.
3. CSS: `.dfh-preview` rework (fill, scroll), `.dfh-code` with line numbers, `.dfh-md` typography, image
   fit / actual size, phone rules. JS: image fit toggle only.
4. Unit tests (DocumentPreviewService):
   - encodings: UTF-8 with and without BOM, UTF-16, Windows-1250;
   - the 256 KB cap;
   - Markdown: `<script>` and raw HTML stay text; `javascript:` and `data:` links are dropped; remote images become links; tables render;
   - PDF / DOCX text from demo-style files;
   - no access → failure.
5. E2E:
   - demo (smoke): an MD document shows a rendered heading and Source shows numbered lines; a TXT document shows its text; a PDF shows the "Text only" label and text; an older version opens with the "Viewing vN" bar; no sideways scroll at 390px.
   - non-demo (write spec): upload a PNG and the preview shows the image (natural width > 0); `?handler=File` on a TXT returns 404; the image response has the `nosniff` and CSP headers.
6. Docs: README ("Not there yet": no file preview → PDF/DOCX page layout preview still missing),
   `redesign-ide-plan.md` D5 note, sprint log, screenshots (README `document-ai` shot may change).

## Later sprints (outline, not part of this plan)

- **Sprint 19: PDF.js.** Pages rendered on the sunken background, an 84px thumbnail strip (design 3b), page navigation,
  zoom, lazy rendering. PDF.js self-hosted (no CDN), fed by the same inline `?handler=File` (PDF added
  to its allowed types).
- **Sprint 20: DOCX.** An OpenXml → HTML view with headings, paragraphs, bold and italic, lists and tables, through the same
  sanitising rules. `.doc` stays download-only.

## Verification

Release build, unit tests, smoke + screenshots + crawler (demo), write specs + image checks (non-demo),
CI on the PR. After the master merge (separate approval): live smoke against the demo.
