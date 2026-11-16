# Sprint 20: Word documents as formatted pages (DOCX → HTML)

Sprint 18 shows a DOCX as plain text with line numbers and the badge "Text only — page layout preview is
coming". This sprint replaces it with a formatted view: headings, paragraphs, bold and italic, lists,
tables, links and pictures, on a sheet like the rendered Markdown. The plain text stays one click away.

Status: **done** (2026-11-16, see [`sprint20-log.md`](sprint20-log.md)) · Branch: `feat/docx-preview` → PR into `master`.
Merging into `master` redeploys the public demo, so it needs the user's separate approval.

## State before the sprint

- A DOCX shows `DocumentPreviewKind.ExtractedText`: the text from `ITextExtractionService` (OpenXml, one
  paragraph per line), in the mono view.
- `DocumentFormat.OpenXml` 3.5.1 is already a dependency (text extraction, demo files).
- `PreviewRendering` renders Markdown to sanitised HTML; the `.dfh-md` rules style it.
- Demo DOCX files (`DemoFileFactory.CreateDocx`) are a bold title and plain paragraphs: nothing to show
  off a formatted view with.
- A `.doc` file has no preview (download card).

## Decisions

| # | Topic | Decision |
|---|---|---|
| S20-1 | Converter | Our own OpenXml → HTML writer, `DocxRendering` in Infrastructure (next to `PreviewRendering`). No new package: the existing .NET converters are either unmaintained (OpenXmlPowerTools) or copy the document's inline styles, which we do not want. The writer only ever emits a fixed set of tags and attributes, and every piece of text is HTML-encoded, so the output is safe by construction: nothing from the file reaches the page as markup. |
| S20-2 | What is shown | **Blocks:** headings (h1–h6), paragraphs, bullet and numbered lists with nesting, tables, page breaks (a thin rule). **Inline:** bold, italic, underline, strikethrough, superscript, subscript, line breaks, tabs. **Alignment:** centre and right (a class, not a style). **Links:** `http`, `https` and `mailto` only, opened in a new tab with `rel="noopener noreferrer nofollow"` (the Markdown rules); any other link becomes plain text. Content controls and tracked insertions show their text; tracked deletions and comments are left out. |
| S20-3 | Headings | Word stores a style id that depends on the language of the Word that wrote the file ("Heading1", "Nagwek1", "berschrift1"). The level is therefore read from the outline level (the paragraph's own, or its style chain's), then from the style *name* ("heading 1" … "heading 6", "Title"). "Title" becomes h1. |
| S20-4 | Lists | A paragraph with numbering becomes a list item. The numbering definition decides the list type: bullet → `ul`; decimal, letters, roman → `ol` with the matching `type`. The indent level gives the nesting. Numbering that continues after a break in the list restarts in this version (noted in "Not there yet"). |
| S20-5 | Tables | `table / tr / td` with merged cells (`gridSpan` → `colspan`, vertical merge → `rowspan`), a header row when Word marks one, nested tables. Each table sits in a wrapper that scrolls sideways on its own, so a wide table never makes the page scroll. No cell colours or widths. |
| S20-6 | Pictures | PNG, JPG and GIF pictures are embedded in the page as `data:` images, with the alt text from the document. Budget: 1.5 MB of pictures per document; a picture over the budget, or of another type (EMF, WMF, SVG), becomes a "[picture]" placeholder with its alt text. No new endpoint is needed. The user kept pictures in this sprint. |
| S20-7 | Limits and failures | The file is opened with a cap on the size of each part (`OpenSettings.MaxCharactersInPart`), so a zip bomb cannot exhaust memory. The HTML is capped at 1 MB; past it the view stops and says "Showing the first part — download the file for the rest". A file that cannot be opened (damaged, password-protected) falls back to the text view when text can be read, otherwise to the download card with a note. |
| S20-8 | Model and service | New `DocumentPreviewKind.Docx` with `Html`. `ExtractedText` goes away (nothing else uses it). The `pdfText` parameter is renamed `textView` and applies to both PDF and DOCX: the text is only extracted when the text view asks for it. |
| S20-9 | Views | A "Document / Text" switch in the preview bar (`?view=text`), like "Pages / Text" for a PDF. Document is the default. The badge becomes "Simplified layout", with a tooltip: fonts, colours, columns, headers and footers are not shown. Rendered on the server; no JavaScript. |
| S20-10 | Look | A sheet in the theme's colours (the same as rendered Markdown) on the sunken background, at most 816px wide (Letter at 96 dpi) with page-like padding, typography shared with Markdown (`.dfh-md`) plus `.dfh-docx` rules for the sheet, tables, lists and the page-break rule. On phones the sheet takes the full width with small padding. |
| S20-11 | Demo | Demo DOCX files get real structure, so the demo shows the view: the "Title" style, "Details" and "Version history" headings, a bullet list of action points, a version-history table (version, date, author, summary) and some bold and italic. `CreateDocx` writes a styles part and a numbering part. The PDF, TXT and MD demo files do not change. |
| S20-12 | `.doc` | Stays download-only, as planned in Sprint 18. |

## Tasks

1. `DocxRendering.Render(bytes)` → HTML, truncated flag: styles and numbering lookups, blocks, inline
   runs, lists, tables, links, pictures, limits.
2. Core / Infrastructure: `DocumentPreviewKind.Docx`, `textView`, the fallback chain; remove
   `ExtractedText`. `DemoFileFactory.CreateDocx` with styles, numbering, a list and a table; the seeder
   builds the DOCX content.
3. Details: "Document / Text" switch, the `.dfh-docx` sheet, the "Simplified layout" badge; CSS in
   `components.css` and the phone rules in `responsive.css`.
4. Unit tests:
   - headings from the outline level, from a localised style id and from the style name;
   - bold, italic, underline, strikethrough, superscript, line break;
   - bullet and numbered lists, nesting, two lists in a row;
   - a table with `colspan` and `rowspan`, a header row, a nested table;
   - an `https` link is kept; a `javascript:` link becomes text;
   - text with `<script>` and `&` comes out encoded;
   - tracked deletions are left out, insertions kept;
   - a PNG is embedded; a picture over the budget and an EMF become placeholders;
   - the HTML cap sets `Truncated`;
   - a damaged file falls back to text, then to the download card;
   - the preview service: `Docx` with HTML and no text; the text view has the text;
   - demo DOCX files contain a heading, a list and a table.
5. E2E:
   - demo (smoke): a DOCX shows its h1, an h2, list items and a table with the version rows; "Text" shows
     the line-numbered text and "Document" goes back; no sideways scroll at 390px; no console errors.
   - non-demo (write spec): upload a fixture DOCX with `<script>` in its text and a `javascript:` link:
     the text is shown as text and there is no such link. A damaged DOCX shows the note and Download.
     Both tests delete their documents.
   - screenshots: docx, docx-text, docx-390.
6. Docs: README (feature list, "Not there yet", tech stack), the sprint log, the `redesign-ide-plan.md`
   D5 note.

## Out of scope

Fonts, colours and sizes from the document; columns; headers and footers; footnotes and endnotes;
comments; text boxes and shapes; charts; EMF / WMF pictures; real pagination (a DOCX has no fixed
pages); numbering that continues across a break; `.doc`, `.odt`, `.rtf`.

## Verification

Release build, unit tests, smoke + screenshots + crawler (demo), write specs (non-demo), CI on the PR.
After the master merge (separate approval): live smoke against the demo.
