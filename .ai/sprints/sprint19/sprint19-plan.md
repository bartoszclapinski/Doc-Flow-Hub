# Sprint 19: PDF pages with PDF.js

Sprint 18 gave every document a preview, but PDFs showed only their text. This sprint renders the real
pages in the browser with PDF.js, as in design 3b: an 84px thumbnail strip on the left, the pages on the
sunken background, page navigation and zoom. The text view stays one click away.

Status: **done** (2026-11-15, see [`sprint19-log.md`](sprint19-log.md)) · Branch: `feat/pdf-preview` → PR into `master`.
Merging into `master` redeploys the public demo, so it needs the user's separate approval.

## State before the sprint

- A PDF shows `DocumentPreviewKind.ExtractedText` (PdfPig) with the badge "Text only — page layout preview is coming".
- `?handler=File` serves only jpg / jpeg / png / gif inline (nosniff, sandbox CSP, ETag).
- Client libraries are not in git: `libman.json` restores them (locally and in the Docker build); the jsdelivr
  provider is already used for the fonts.
- Demo PDFs (`DemoFileFactory.CreatePdf`) are one page with the non-embedded Helvetica font.

## Decisions

| # | Topic | Decision |
|---|---|---|
| S19-1 | Library | `pdfjs-dist@6.4.299` (Apache-2.0, released 2026-10-03; 6.5 is only two weeks old). Self-hosted under `wwwroot/lib/pdfjs/` through libman (jsdelivr), so no CDN at runtime. Files: `build/pdf.min.mjs`, `build/pdf.worker.min.mjs`, `standard_fonts/` (needed for PDFs that do not embed fonts, like the demo files), `cmaps/` (CJK text), `iccs/`, the three image-decoder `.wasm` files and the licences. No viewer UI, no scripting (`quickjs`), no editor. |
| S19-2 | Static files | `UseStaticFiles` only serves known types. Add `.mjs` (text/javascript), `.bcmap`, `.pfb` (application/octet-stream) and `.icc` (application/vnd.iccprofile) to the content-type map. |
| S19-3 | Server | New `DocumentPreviewKind.Pdf`. The preview service no longer extracts text for a PDF unless asked (`pdfText: true`, used by the text view), so opening a PDF stays fast. `?handler=File` also serves `.pdf` as `application/pdf` with the same headers. DOCX keeps the text view until Sprint 20. |
| S19-4 | Views | A "Pages / Text" switch in the preview bar (`?view=text`, a plain link like Markdown's "Rendered / Source"). Pages is the default. Without JS, or when PDF.js cannot open the file (damaged, password), the pane says so and links to the text view and Download. |
| S19-5 | Rendering | Continuous vertical scroll of all pages on `--dfh-bg-sunken`, each page a white sheet with a light border. Page placeholders get their real size up front, so the scrollbar is right before rendering. Pages render lazily (IntersectionObserver, one page around the visible ones) at `devicePixelRatio`, capped at about 16 million canvas pixels. A text layer (PDF.js `TextLayer`) makes the text selectable and copyable. `isEvalSupported: false`, no scripting, no annotations layer (links in PDFs are out of scope). |
| S19-6 | Navigation and zoom | In the preview bar, in mono font as in the mock: "‹ page 1 / 12 ›", then "− 100% +" and a "Fit width" button. The default is fit to width (at most 150%). Zoom steps: 50, 75, 100, 125, 150, 200, 300%. 100% means actual size (1 PDF point = 1/72 inch). The page number follows the scroll. The pages take focus, so the keyboard scrolls them as usual (no custom keys). The design draws these controls under the page, but with continuous scroll they would scroll away, so they sit in the bar. |
| S19-7 | Thumbnails | An 84px strip on the left (design 3b): small page images (48px wide as in the mock, rendered lazily at low resolution) with the page number under each. The current page has an accent border. A click or Enter scrolls to the page. The strip is hidden for a one-page PDF and on phones. |
| S19-8 | Phones | No strip, the page fits the width, the bar wraps. The pages flow with the page like the other previews. |
| S19-9 | Demo | Demo PDFs get three pages: the content page, plus "Details" and "Appendix" sections, each starting on a new page. `CreatePdf` paginates, so long text also flows to the next page. The demo can then show the strip and the page navigation. |

## Tasks

1. libman entry for pdfjs-dist; content types; check that the worker, a standard font and a cmap are served.
2. Core / Infrastructure: `DocumentPreviewKind.Pdf`, the `pdfText` option, PDF in `GetInlineFileAsync`.
   `DemoFileFactory` paginates; the demo seed gives PDFs two more sections.
3. Details: "Pages / Text" switch, the PDF container (strip, pages, noscript fallback), the bar controls.
   `js/dfh/pdf-preview.js` (ES module, loaded only for PDFs) and CSS (`.dfh-pdf*`, the text layer rules).
4. Unit tests:
   - a PDF preview is `Pdf` without text, and with text when asked;
   - the inline handler serves a PDF;
   - DOCX is still `ExtractedText`;
   - demo PDFs have three pages that PdfPig can read.
5. E2E:
   - demo (smoke):
     - a PDF renders its first page (a canvas with pixels drawn) and shows "page 1 / 3";
     - clicking the third thumbnail goes to page 3;
     - zoom in changes the percentage and the page width;
     - the text layer holds the document title;
     - "Text" shows the extracted text;
     - no sideways scroll at 390px.
   - non-demo (write spec): `?handler=File` on a PDF returns `application/pdf` with nosniff and the CSP.
   - assets: the worker is served as JavaScript and a `.pfb` font as 200.
6. Docs: README (feature list, "Not there yet"), the sprint log, screenshots (`document-ai` may show the PDF
   pages), the `redesign-ide-plan.md` D5 note.

## Out of scope

Links and form fields inside PDFs (the annotation layer), search inside the PDF, printing, rotation, the
DOCX page view (Sprint 20).

## Verification

Release build, unit tests, smoke + screenshots + crawler (demo), write specs (non-demo), CI on the PR.
After the master merge (separate approval): live smoke against the demo.
