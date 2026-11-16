# Sprint 21 log: Line-by-line version diff

Plan: [`sprint21-plan.md`](sprint21-plan.md) · Branch `feat/line-diff` → PR into `master`.

## What was built

- **DiffPlex 1.9.0** (Apache-2.0) in Infrastructure. It finds the changed lines and, inside a changed
  line, the changed words. The layout of the result is ours.
- **`LineDiff`** (Infrastructure): two arrays of lines → the sections the page shows.
  - **Lines:** split on any newline, without the empty line after a final newline. DiffPlex gets the
    lines through our own chunker, so its indexes are exactly our line numbers.
  - **Changes:** each change lists its removed lines, then its added lines. A removed and an added line
    with the same row number are a pair: they sit next to each other in the side-by-side view.
  - **Word marks:** a pair is compared word by word; neighbouring changed words become one mark. A pair
    with less than 30% in common, a line over 2,000 characters and an unpaired line get no marks: the red
    and green lines already say it.
  - **Ignore whitespace:** lines are compared without leading, trailing and repeated whitespace; the page
    still shows the real text of both sides.
  - **Folding:** 3 lines of context around a change; the rest becomes a fold. Fewer than two lines are
    never folded (the fold row would take as much room). `ShowAll` folds nothing.
- **`VersionDiffService`** (`IVersionDiffService` in Core):
  - TXT and MD are compared as decoded text; PDF and DOCX as their extracted text. The two versions may
    have different types.
  - **No diff, with a reason:** an image, a `.doc` or an unknown type (the file is not even read); a PDF
    or DOCX with no text; a file that cannot be read; more than 256 KB of text or 5,000 lines.
  - The same text gives "No text differences" and says the files may still differ in layout.
  - Access goes through `IDocumentBrowserService.GetComparisonAsync`, like the page.
- **Compare page:**
  - **Toolbar:** "Side by side / Unified" (`?view=unified`), "Ignore whitespace" (`?ws=ignore`), "Show
    all lines" (`?context=all`, shown when something is folded), the counter "+9 −7 · change 1 of 5" and
    ↑ / ↓. Every switch is a plain link that keeps the others and the version pair; the version pickers
    carry them too.
  - **Diff:** one markup for both layouts. Unified is the base; side by side is CSS: removed lines go to
    the left column and added lines to the right one, on the grid row the server gives each line, with
    the filler colour behind one-sided rows. Context lines show both line numbers.
  - **Folds** are links to "Show all lines".
  - **Without JavaScript** everything above works; the counter then reads "5 changes" and the arrows stay
    hidden.
  - **Empty states** for identical text, no text and too large, each with the two Download buttons.
- **`compare.js`:** "change n of m", ↑ / ↓ and the keys N / P jump between changes (smooth unless reduced
  motion is set); the current change has an accent bar; `#change-3` in the address opens at that change.
- **CSS:** `.dfh-diff*` in `components.css` (mono 12/22, the diff tokens, word marks, folds, the split
  layout from 600px up); phone rules in `responsive.css` (unified only, narrower number columns, the
  layout switch hidden).

## Differences from the plan

- The "Show all lines" switch is only shown when something is folded.
- A bug found on the way: Razor keeps an empty `data-*` attribute, so every context section counted as
  a change ("change 1 of 11"). The change attributes are now written only for change sections.

## Tests

- **Unit (26 new, 241 total):**
  - `LineDiff`: line splitting; identical text; added, removed and changed lines with both line numbers;
    removed-then-added order with matching rows; an empty side; word marks, neighbouring marks joined,
    no marks for unlike or unpaired lines; ignore whitespace; folding around 3 lines of context, the
    7 / 8 line gap between two changes, `ShowAll`; inserted lines shift the new numbers; repeated and
    empty lines rebuild both texts; 5,000 different lines a side in under 10 seconds.
  - `VersionDiffService`: text files; identical text with and without whitespace; DOCX against PDF by
    extracted text; image, `.doc` and unknown types without reading the file; a PDF without text; a
    missing file; the size and line limits; access through the Compare check; demo versions of one
    document differ.
- **E2E smoke (demo):**
  - **"line diff side by side and unified…":** the counter matches the added and removed lines on the
    page; a pair sits side by side on one row; ↓ and P move the counter and the accent bar; Unified puts
    the pair in one column; the switches keep each other in the address; no console errors.
  - **Phone (390px):** unified, the layout switch hidden, nothing scrolls sideways.
- **E2E write spec (non-demo):** a 40-line TXT and a second version with known edits: "+4 −3", four
  changes, the marked words "100" and "250", the removed line, the two added lines (one with `<b>` shown
  as text), old line 21 = new line 20, the three folds (10, 3 and 4 lines), "Show all lines" (37 context
  lines), ignore whitespace ("+3 −2", three changes) and `#change-3`. The test deletes its document.
- **Screenshots:** `compare diff screens` adds compare-diff, compare-diff-unified and compare-diff-390.

## Verification

- Release build: 0 errors. Unit tests: 241/241.
- **Demo mode, fresh database:**
  - smoke: 43 passed, 2 skipped (non-demo only);
  - screenshots: 17 passed, 2 skipped (on demand);
  - crawler: 1,057 pages (500 user + 557 admin), 0 errors. There are more pages than in Sprint 20
    because of the diff switches.
  - no `fail` or `crit` log lines.
- **Demo mode off, after a fresh seed:** write specs 19/19 (documents, workspace, admin). The two `fail`
  log lines are the text extractor rejecting the deliberately damaged PDF and DOCX; they are expected.
- Port 5199 was free after each run.
- **README:** the feature list, "Not there yet" (text only, limits, no restore) and the tech stack
  (DiffPlex) are updated.
