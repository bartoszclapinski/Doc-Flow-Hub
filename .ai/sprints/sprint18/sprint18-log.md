# Sprint 18 log: Document preview

Plan: [`sprint18-plan.md`](sprint18-plan.md) · Branch `feat/document-preview` → PR into `master`.

## What was built

- **Core:** `DocumentPreview` / `DocumentPreviewKind` (None, Text, Markdown, ExtractedText, Image),
  `DocumentPreviewFile`, `IDocumentPreviewService`. The service has three methods:
  `GetPreviewAsync(userId, id, version?)`, an overload that takes the already access-checked
  `DocumentDetails` (the Details page doesn't load the document twice), and `GetInlineFileAsync`.
- **Infrastructure:**
  - `DocumentPreviewService`: access check through `IDocumentBrowserService.GetDetailsAsync`, picks the version, then:
    - TXT / MD: decoded;
    - PDF / DOCX: text from the existing `ITextExtractionService`;
    - images: kind only (the file comes from the inline handler);
    - DOC and others: "No preview for … files yet";
    - an unreadable file, or a PDF without text, gives a note instead of an error.
  - `PreviewRendering`, pure and unit tested:
    - **Text decoding:** BOM (UTF-8 / UTF-16 LE / BE), otherwise strict UTF-8, otherwise Windows-1250 (`CodePagesEncodingProvider`). The 256 KB cap never splits a UTF-8 character. Line endings are normalised.
    - **Markdown rendering:** Markdig 1.6.0 with `DisableHtml()`, pipe tables, task lists, autolinks and emphasis extras. After parsing, the AST is rewritten:
      - links keep only `http`, `https`, `mailto` and `#anchor` (the check runs after removing whitespace and control characters, so `java\tscript:` and entity-encoded `javascript:` are caught);
      - other links become plain text;
      - web links get `rel="noopener noreferrer nofollow" target="_blank"`;
      - images are never loaded: an http(s) image becomes a link "image: alt", any other image "[image: alt]".
- **Details page:**
  - The preview pane replaces the D5 placeholder.
  - **Preview bar:** type label, version, size, encoding; for PDF / DOCX a "Text only — page layout preview is coming" badge; "Rendered / Source" for Markdown; "Wrap lines" (`?wrap=0`); "Actual size" for images. That button is shown by JS only when the image is larger than the pane.
  - **Older versions:** `?version=N` shows a "Viewing vN of M · Back to current" banner. The Versions tab has a Preview (eye) action per version.
  - **Text views:** text, Markdown source and extracted text are a `<pre>` with CSS-counter line numbers. A file's final newline does not add an empty line. Rendered Markdown sits on a page up to 820px wide (`.dfh-md` typography in the dark tokens).
  - **Images:** `OnGetFileAsync` (`?handler=File&version=`) serves jpg / jpeg / png / gif inline with `X-Content-Type-Options: nosniff`, `Content-Security-Policy: default-src 'none'; sandbox`, `Content-Disposition: inline`, `Cache-Control: private` and an ETag (with 304 support). Other types → 404.
  - **Phones:** the preview flows with the page, the bar wraps, and the "no preview" card shows its note.
  - The version size in the Versions tab no longer wraps ("329 B").
- **Demo seed:** the Markdown demo files now have a blank line between lines, so they render as paragraphs. A single newline joins lines in Markdown.

## Tests

- **Unit (27 new, 177 total).**
  - `PreviewRenderingTests`:
    - encodings, BOM, Windows-1250 fallback, the cut at a character boundary;
    - Markdown: headings, tables and task lists render; raw HTML and `<script>` stay text; seven unsafe link forms are dropped (`javascript:` in mixed case, with a tab or as entities, `data:`, a reference link, an autolink); web, mailto and anchor links survive; images are not loaded.
  - `DocumentPreviewServiceTests`: no access; latest vs older version; unknown version; Markdown; PDF and DOCX text (demo factory files); `.doc`; an unreadable file; an image inline; a non-image refused.
- **E2E smoke (demo):**
  - Markdown rendered, then Source / Rendered;
  - TXT encoding, lines and wrap toggle;
  - PDF and DOCX "Text only";
  - an older version banner (finds a document that still has an older version);
  - a phone at 390px.
- **E2E write spec (non-demo):** a PNG upload is previewed (natural width 1). Its response has `image/png`, `nosniff` and a CSP with `sandbox`. `?handler=File` on a TXT → 404.
- **Screenshots:** `preview screens` (md, md source, txt, pdf, docx, older version, md at 390).
- **Test selectors** use `data-ext` (TXT / MD / PDF / DOCX), not `data-kind`: `data-kind` is the label colour, and PNG shares "txt".

## Verification

- Release build: 0 errors. Unit tests: 177/177.
- **Demo mode, fresh database:**
  - smoke: 38 passed, 2 skipped (non-demo only);
  - screenshots: 16 passed, 2 skipped (on demand);
  - crawler: 682 pages (332 user + 350 admin), 0 errors. There are more pages than before because of the version, Source and Wrap links.
- **Demo mode off:**
  - write specs: 15/15, including the PNG preview;
  - preview, landing, sign-in and register tests: 9 passed, 1 skipped.
- App logs: 0 `fail`/`crit` lines. Port 5199 was free after each run.
- README: features and "Not there yet" updated. `document-ai.png` and `mobile-document.png` now show the text preview.
