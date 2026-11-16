# Sprint 21: Line-by-line version diff

The Compare page (design 3i) shows the AI comparison in the inspector, but its main column is an empty
state: "Line-by-line diff isn't available yet". This sprint fills it with a real text diff of the two
versions: side by side or unified, with changed words marked inside changed lines.

Status: **done** (2026-11-16, see [`sprint21-log.md`](sprint21-log.md)) · Branch: `feat/line-diff` → PR into `master`.
Merging into `master` redeploys the public demo, so it needs the user's separate approval.

## State before the sprint

- `/Documents/Compare/{id}?from=&to=` has the two version pickers, the AI comparison in the inspector and
  an empty state with two Download buttons in place of the diff.
- The diff colours already exist as tokens (`--dfh-diff-del`, `--dfh-diff-add`, `--dfh-diff-text`).
- Text is available for every previewable type: `PreviewRendering.DecodeText` for TXT / MD, and
  `ITextExtractionService` for PDF / DOCX (the text the AI compares).
- Demo versions of one document differ in a few lines (version line, body text, "Changes since…"), so
  the demo has something to show without new seed data.

## Decisions

| # | Topic | Decision |
|---|---|---|
| S21-1 | Diff engine | The **DiffPlex** package (Apache-2.0, the usual .NET diff library) in Infrastructure: line diff for the page, word diff inside changed lines. Writing our own Myers diff is about 200 lines plus edge cases; a tested library is the safer choice here. The user chose DiffPlex (1.9.0, which has a .NET 6+ build). |
| S21-2 | What is compared | The text of each version: TXT and MD as decoded text; PDF and DOCX as their extracted text (the same text as the "Text" view). The two versions may have different types. Images, `.doc` and files with no text keep an empty state that says why, with the Download buttons. |
| S21-3 | Limits | Each side is read up to 256 KB of text (the preview cap) and 5,000 lines. Past that the page says the versions are too large to diff here and offers the downloads. Identical text says "No text differences between v2 and v3" (the files may still differ in layout). |
| S21-4 | Views | **Side by side** (default) and **Unified** (`?view=unified`), as in the mock: grid `40px 18px 1fr 40px 18px 1fr`, mono 12/22, line numbers of both versions, `+` / `−` markers, deleted lines on `--dfh-diff-del`, added on `--dfh-diff-add`, filler rows opposite a one-sided change. A changed line pair marks the changed words with a stronger background. Long lines wrap. |
| S21-5 | Context | Unchanged stretches are folded: 3 lines of context around each change, the rest behind a row "⋯ 42 unchanged lines". "Show all lines" (`?context=all`) unfolds everything. Server-rendered, so it works without JavaScript. |
| S21-6 | Toolbar | "Side by side / Unified" switch, "Ignore whitespace" toggle (`?ws=ignore`: leading, trailing and repeated spaces do not count), the counter "+6 −4 · change 2 of 5" and ↑ / ↓ buttons that jump between changes (JS; also keys `n` / `p`). The counter and jumps are progressive: without JS the totals still show. |
| S21-7 | Service | `IVersionDiffService.GetDiffAsync(userId, documentId, from, to, options)` in Core, implemented in Infrastructure. Access goes through `IDocumentBrowserService.GetComparisonAsync`, the same check the Compare page uses. Read-only and AI-free, so it works in the demo. Computed per request; no caching in this sprint. |
| S21-8 | Phones | Unified view only (two columns do not fit 390px); the switch is hidden. No sideways scroll. |
| S21-9 | Not in this sprint | "Restore v3" (the version-restore backlog item), AI changes linked to diff lines (the AI list has no line numbers), a rendered diff of Markdown or Word formatting, syntax highlighting. |

## Tasks

1. Core: `VersionDiff` models (rows, change blocks, totals, the "why not" reasons) and `IVersionDiffService`.
2. Infrastructure: `VersionDiffService` (text of both versions, limits, DiffPlex, folding, ignore
   whitespace, word-level pieces). DI.
3. Compare page: toolbar, side-by-side and unified markup, folded rows, empty states; `compare.js` gets
   the change counter and jumps. CSS `.dfh-diff*` in `components.css`, phone rules in `responsive.css`.
4. Unit tests:
   - added, removed and changed lines; a change at the start and at the end; an empty side;
   - line numbers on both sides and filler rows in side-by-side rows;
   - word pieces inside a changed line;
   - ignore whitespace;
   - folding with 3 lines of context, neighbouring changes merged into one block, `context=all`;
   - totals and the number of change blocks;
   - identical text; a side with no text; over the size and line limits;
   - PDF and DOCX use the extracted text; a user without access is refused;
   - text with `<script>` stays text (the page encodes it).
5. E2E:
   - demo (smoke): Compare shows added and removed lines with the right totals; Unified and Ignore
     whitespace change the URL and the layout; "Show all lines" unfolds; ↓ moves the counter; no
     sideways scroll at 390px; no console errors.
   - non-demo (write spec): upload a TXT, add a new version with known edits, and check the exact added,
     removed and changed lines; the test deletes its document.
   - screenshots: compare side by side, unified, 390px.
6. Docs: README (feature list, remove "No line-by-line diff"), the sprint log, the
   `redesign-ide-plan.md` 3i note.

## Verification

Release build, unit tests, smoke + screenshots + crawler (demo), write specs (non-demo), CI on the PR.
After the master merge (separate approval): live smoke against the demo.
