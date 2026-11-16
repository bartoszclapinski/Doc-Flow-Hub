# Sprint 20 log: Word documents as formatted pages

Plan: [`sprint20-plan.md`](sprint20-plan.md) · Branch `feat/docx-preview` → PR into `master`.

## What was built

- **`DocxRendering`** (Infrastructure, next to `PreviewRendering`): our own OpenXml → HTML writer, no new
  package.
  - **Safe by construction:** it writes a fixed set of tags and attributes and HTML-encodes every piece of
    text, so nothing from the file reaches the page as markup.
  - **Blocks:** headings, paragraphs, bullet and numbered lists with nesting, tables, page breaks (a dashed
    rule). Empty paragraphs are left out.
  - **Headings:** the level comes from the outline level (the paragraph's own, or its style chain's), then
    from the style name ("heading 1", "Title"). The style id is not used when a styles part exists, because
    it depends on Word's language ("Nagwek1"). "Title" becomes `h1.dfh-docx-title`.
  - **Inline:** bold, italic, underline, strikethrough, superscript, subscript, line breaks, tabs. On/off
    properties also come from the character style and the paragraph style. Neighbouring runs with the same
    formatting are joined, so Word's split runs do not produce a tag each.
  - **Lists:** the numbering definition gives `ul`, or `ol` with `type` (a, A, i, I) and `start`. Two
    different lists in a row stay two lists. An unknown numbering id is a bullet list.
  - **Tables:** `colspan` from `gridSpan`, `rowspan` from vertical merges, `th scope="col"` for a header
    row, nested tables and lists inside cells. Each table sits in a `.dfh-docx-table` wrapper that scrolls
    sideways on its own.
  - **Links:** `http`, `https` and `mailto` only; web links open in a new tab with
    `rel="noopener noreferrer nofollow"`. Any other target, and bookmarks, become plain text.
  - **Pictures:** PNG, JPG and GIF are embedded as `data:` images with the alt text and the width from the
    document. All pictures of one document share a 1.5 MB budget; past it, and for other types (EMF, WMF)
    or a missing part, the page shows "[picture: alt]".
  - **Tracked changes and controls:** insertions and content controls show their text; deletions, moved-away
    text and hidden text are left out.
  - **Limits:** each part of the package is capped at 20 million characters while it is read; the HTML
    stops after 1 MB (pictures not counted) at a block boundary; nesting deeper than 12 levels is not
    followed.
- **Core / Infrastructure:**
  - `DocumentPreviewKind.Docx` replaces `ExtractedText`.
  - `GetPreviewAsync(…, textView)` (was `pdfText`): the plain text of a PDF or a DOCX is only extracted
    for the text view.
  - **Fallbacks:** a file the writer cannot open shows its plain text with a note; when there is no text
    either, the download card says the file may be damaged or password-protected. An empty document gets
    the download card ("nothing to show").
- **Details page:**
  - "Document / Text" switch (`?view=text`), like "Pages / Text" for a PDF.
  - The badge "Simplified layout" (tooltip: what is and is not shown) replaces "Text only — page layout
    preview is coming".
  - The document is rendered on the server into `article.dfh-md.dfh-docx`: the Markdown sheet at Letter
    width (816px) with page-like padding. No JavaScript.
  - A cut-off document says "Showing the first part of the document".
- **CSS:** `.dfh-docx*` in `components.css` (sheet, title, alignment classes, table wrapper, pictures,
  page-break rule, the fallback banner); the phone rule for the page-break rule in `responsive.css`.
- **Demo seed:** demo DOCX files now have real structure:
  - the title in the "Title" style, then the description and bold-labelled facts (version, author, date);
  - "Summary", "Action points" (a bullet list of three) and "Version history" (a table with the real
    versions up to this one) as "Heading 1";
  - an italic closing line.

  `DemoFileFactory.CreateDocx` writes a styles part and a numbering part and understands `HeadingPrefix`,
  `BulletPrefix`, `RowPrefix` / `CellSeparator` and `BoldMark`. `DemoDataSeeder.BuildDocxLines` builds the
  content. PDF, TXT and MD demo files are unchanged.

## Differences from the plan

- The sheet follows the theme (dark in the dark theme), like rendered Markdown, instead of being white.
  A document's own colours are not shown, so a white sheet would only clash with the rest of the page.
- `ExtractedText` was removed, as planned; the text view of a DOCX is `Docx` with `Text` and no `Html`.

## Tests

- **Unit (28 new, 215 total):**
  - headings: from the style name with a localised id, from the outline level of the chain or the
    paragraph, from the English ids when there is no styles part;
  - inline formatting, joined runs, a character style, line breaks, tabs, hidden text;
  - text with `<script>` and `<img onerror>` comes out encoded;
  - tracked changes, alignment, empty paragraphs, page breaks, content controls;
  - lists: nesting, type and start, two lists in a row, a list that starts nested, unknown numbering;
  - tables: header row, `colspan`, `rowspan`, a list and a table inside a cell;
  - links: `https` and `mailto` kept; `javascript:`, `file:` and bookmarks become text;
  - pictures: embedded PNG with alt text and width; over the budget, EMF and a missing part become
    placeholders; pictures do not count towards the HTML limit;
  - the HTML limit cuts at a block and closes the open list;
  - an empty document has no HTML; a damaged file throws;
  - the preview service: formatted HTML without text; the text view; an empty document; a damaged file;
    a document over the part cap falls back to its text with a note;
  - a demo DOCX renders to the expected HTML (title, headings, list, table, bold, italic).
- **E2E smoke (demo):**
  - **"a Word document is formatted…":** the badge, the title, the "Action points" heading, three list
    items, the bold "Author:", the four table headers, "v1" in the first row, the italic line; Text shows
    the numbered lines and keeps `view=text` when wrapping is switched; Document goes back; no console
    errors.
  - **Phone (390px):** the sheet and the table stay inside the screen and nothing scrolls sideways.
- **E2E write specs (non-demo):**
  - `fixtures/tricky.docx` (built by hand: a Polish heading style id, `<script>` and `<img onerror>` as
    text, a `javascript:` link, an `https` link, a table): the markup is shown as text, there is no
    `script` or `img` element, only the `https` link exists, and `window.__docx_xss` stays undefined in
    both views.
  - a damaged DOCX shows the note and "Download DOCX".
  - Both tests delete their document.
- **Screenshots:** `preview screens` adds docx-text, docx-390 and docx-table.

## Verification

- Release build: 0 errors. Unit tests: 215/215.
- **Demo mode, fresh database:**
  - smoke: 41 passed, 2 skipped (non-demo only);
  - screenshots: 16 passed, 2 skipped (on demand);
  - crawler: 876 pages (424 user + 452 admin), 0 errors. There are more pages than in Sprint 19 because
    of the DOCX Text views.
  - no `fail` or `crit` log lines.
- **Demo mode off, after a fresh seed:**
  - write specs: 18/18 (documents, workspace, admin);
  - preview, PDF, landing, sign-in and register tests: 12 passed, 1 skipped.
  - The two `fail` log lines are the text extractor rejecting the deliberately damaged PDF and DOCX; they
    are expected.
- Port 5199 was free after each run.
- **README:** the feature list, "Not there yet" (the Word view is simplified) and the tech stack (Open XML
  SDK) are updated.
