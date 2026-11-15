# Sprint 19 log: PDF pages with PDF.js

Plan: [`sprint19-plan.md`](sprint19-plan.md) · Branch `feat/pdf-preview` → PR into `master`.

## What was built

- **PDF.js, self-hosted:**
  - `pdfjs-dist@6.4.299` comes in through `libman.json` (jsdelivr, glob file lists) into `wwwroot/lib/pdfjs/`. The Docker build already runs `libman restore`.
  - Included: `build/pdf.min.mjs` and `pdf.worker.min.mjs`, `standard_fonts/`, `cmaps/`, `iccs/`, the jbig2 / openjpeg / qcms `.wasm` files and the licences.
  - Not included: the viewer UI, scripting (quickjs), the editor, source maps.
  - The restored files have the same SHA-256 as the npm package.
- **Static files:** `Program.cs` maps `.mjs` → text/javascript, `.bcmap` and `.pfb` → application/octet-stream, `.icc` → application/vnd.iccprofile. Without the map, `UseStaticFiles` answers 404 for unknown types.
- **Core / Infrastructure:**
  - `DocumentPreviewKind.Pdf`.
  - `GetPreviewAsync(…, pdfText)`: a PDF is not read on the server unless the text view asks for it. A PDF with no text keeps the `Pdf` kind and gets a note ("The Pages view shows it as it looks").
  - `GetInlineFileAsync` serves `.pdf` as `application/pdf` with the same headers as images: nosniff, a sandbox CSP, private cache, ETag.
- **Details page:**
  - **Preview bar:** "Pages / Text" switch (`?view=text`). In the Pages view, mono-font controls: "‹ page n / m ›", "− 94% +" and "Fit width" (`aria-pressed`). The controls stay hidden until the PDF is open.
  - **Body:** an 84px thumbnail strip next to the pages (design 3b).
  - **No JS:** a `<noscript>` note links to the text view.
  - **PDF.js cannot open the file:** "This PDF could not be shown here (the file may be damaged)" (or "password-protected"), with links to the text view and Download.
- **`js/dfh/pdf-preview.js`** (ES module, loaded only for PDFs):
  - **Loading:** PDF.js is imported dynamically. It runs with `isEvalSupported: false` and absolute `cMapUrl`, `standardFontDataUrl`, `wasmUrl` and `iccUrl`.
  - **Page sizes up front:** every page gets its size first, so the scrollbar is right before anything is drawn.
  - **Lazy drawing:** pages draw when they come near the view (IntersectionObserver, one view-height ahead) at `devicePixelRatio`, capped at 16M canvas pixels. During a zoom change the old drawing is stretched until the new one is ready, and stale renders are cancelled.
  - **Text layer:** a PDF.js `TextLayer` over each page makes the text selectable. The needed rules from `pdf_viewer.css` are copied flat into `components.css`.
  - **Zoom:**
    - Fit width is the default and is capped at 150%. It follows the pane's width through a ResizeObserver.
    - The steps are 50 to 300%.
    - A zoom change keeps the same spot of the current page in view.
  - **Page number:**
    - It follows the scroll: the current page is the last one whose top is above a third of the view; at the end of the document it is the last page.
    - "Go to page" pins the chosen page until the user scrolls again, so a click on the last thumbnail shows "3 / 3" even when page 3 cannot scroll to the top.
  - **Thumbnails:**
    - The images are 48px wide (as in the mock). They draw one at a time after the first page, lazily within the strip.
    - The current page has an accent border and `aria-current="page"`.
    - The strip is hidden for one-page PDFs.
  - **Phones:** no strip. The pages flow with the page; the module switches its scroll and observer root when the 600px breakpoint changes.
- **Demo seed:** demo PDFs now have three pages:
  - the content page;
  - "Details": the other body texts;
  - "Appendix: version history": the document's real versions up to this one.

  `DemoFileFactory.CreatePdf` paginates (41 lines a page, overflow goes to a new page). It understands `PageBreak` and `HeadingPrefix` (bold Helvetica), and puts "Page n of m" at the bottom of each page. TXT / MD / DOCX leave both markers out.
- **Disabled ghost buttons** (`.btn-ghost`) no longer show a border.

## Tests

- **Unit (10 new, 187 total):**
  - a PDF preview is `Pdf` and does not read the file;
  - the text view has the text;
  - a PDF without text keeps the Pages view and explains;
  - a PDF is served inline with `%PDF-` and an ETag;
  - TXT / MD / DOCX / DOC have no inline file;
  - demo PDFs: page breaks and overflow give four pages that PdfPig reads ("Page 2 of 4", line 60 on page 4);
  - the other formats leave out the markers.
- **E2E smoke (demo):**
  - **"PDF pages render…":**
    - state ready, 3 pages, more than 200 dark pixels on page 1;
    - the text layer holds "Page 1 of 3";
    - the inline file has `application/pdf`, nosniff and the sandbox CSP;
    - 3 thumbnails drawn; a click on the third gives "3", `aria-current` and the appendix text; ‹ gives "2";
    - zoom in changes the percentage and the page width; Fit width restores both;
    - Text and Pages switch the view;
    - no console errors.
  - **"PDF.js files are served…":** the module, the worker, `.pfb`, `.ttf`, `.bcmap`, `.icc` and `.wasm` return 200 with their content types.
  - **Phone (390px):** the PDF draws, there is no strip, the page fits the width and nothing scrolls sideways.
- **E2E write spec (non-demo):** a damaged PDF ("%PDF-1.4" and nonsense) shows the error with the controls hidden. "Show the text" gives the "No text could be found" note, and "Show the pages" goes back. The test deletes its document at the end, so the demo-data PDF tests that run next take a real PDF.
- **Screenshots:** `preview screens` adds pdf, pdf-text, pdf-page3, pdf-zoom and pdf-390 (`settlePdf` waits for the first page and the visible thumbnails). The README shots now prefer a PDF with an AI summary.

## Verification

- Release build: 0 errors. Unit tests: 187/187.
- **Demo mode, fresh database:**
  - smoke: 40 passed, 2 skipped (non-demo only);
  - screenshots: 16 passed, 2 skipped (on demand);
  - crawler: 756 pages (364 user + 392 admin), 0 errors. There are more pages than before because of the PDF Text views.
- **Demo mode off, after a fresh seed:**
  - write specs: 16/16 (documents, workspace, admin);
  - preview, PDF, landing, sign-in and register tests: 11 passed, 1 skipped.
  - The one `fail` log line is PdfPig rejecting the deliberately damaged PDF in `TextExtractionService`; it is expected.
- **Test interference:** the first non-demo run failed two PDF tests because the damaged-PDF upload was the first PDF in the list. The write test now cleans up after itself.
- Port 5199 was free after each run.
- **README:**
  - the feature list and "Not there yet" are updated (Word layout and PDF links remain);
  - the tech stack names PDF.js and Markdig;
  - `document-ai.png` and `mobile-document.png` show a 3-page PDF.
