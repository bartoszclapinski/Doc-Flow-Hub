# Sprint 22 log: Restore a version, and compare any two versions

Plan: [`sprint22-plan.md`](sprint22-plan.md) · Branch `feat/versions-upload` (with Sprint 23) → PR into `master`.

## What was built

- **`IDocumentService.RestoreVersionAsync(documentId, versionNumber, userId, message?)`:**
  - **Checks:** only the owner; the version must exist and must not be the newest one.
  - **File first:** the file of vK is read from storage and stored as the next version number. Only
    after that is the version row written, with:
    - the size and the MD5 hash of the bytes;
    - the type of vK;
    - the description "Restored from vK", or the user's own text.
  - **Failures:** if storage fails, nothing is written. If the row cannot be saved, the stored file is
    removed again.
  - **Document:** `UpdatedAt`, `FileType`, `FileSize` and `CurrentVersionId` follow the new version.
  - No AI request is made.
- **`CurrentVersionId` fix:** uploading a new version (both the upload and the content-update path) now
  also sets `CurrentVersionId`. Before, it kept pointing at the version that was current when the demo
  was seeded, so "delete version" could refuse the wrong version.
- **Details page:**
  - **Versions tab:**
    - a restore icon on each older version; disabled with the tip for anyone but the owner, and in the
      demo;
    - with three or more versions, a checkbox on each row and "Compare selected". JS names the pair
      ("Compare v1 ↔ v3"), and a third tick drops the oldest one. It is a GET form with `pick=1&pick=3`,
      so it also works without JS.
  - **"⋯" menu:** "Restore vK…" while an older version is previewed.
  - **Version comparisons block:** a "Compare any two versions…" link, which opens the Versions tab.
  - **Restore dialog:** "Restore v1 as v4?", an optional description, "Restore as v4". After the
    restore the page shows the Versions tab and the toast "Restored v1 as v4".
- **Compare page:**
  - **Header:** visible "Older" / "Newer" labels; the dropdowns mark the current version.
  - **Pair chips:** for 3 to 6 versions, a chip for every pair. The active pair is marked, and ✦ means a
    saved AI comparison.
  - **Toolbar:** it now shows for every pair and has "Restore vK" for the older side. In the demo, or for
    anyone but the owner, the button is disabled with a tip. It opens a dialog that posts to the Details
    handler.
  - `pick=` is accepted and redirected to `from` / `to`.
- **`DocumentComparisonView`:** new `IsOwner` and `SavedPairs` fields.
- **Demo seed:** an AI comparison for every pair of versions (3 per document with three versions).

## Differences from the plan

- **The AI summary is not copied** (S22-3). Summaries are stored per document, not per version, so
  there is nothing to copy. The summary stays as it was, and no AI request is made.
- **No ⇄ swap button** (S22-6). The older version is always on the left and the newer on the right, so
  swapping would show the same page.
- **"Compare any two versions…"** opens the Versions tab with the checkboxes, not the Compare page.
- **No unit test for the all-pairs demo seed:** the seeder needs the Identity user manager. The demo
  smoke test checks it instead: every chip has the AI mark.

## Tests

- **Unit (8 new, 249 total):**
  - a restore adds v3 with the v1 bytes, its hash, size, type, path and description, and moves
    `CurrentVersionId`;
  - the user's description is kept and trimmed;
  - someone else is refused;
  - the newest version and an unknown version are refused;
  - an unknown document is refused;
  - an unreadable file or failed storage write no version;
  - two restores in a row give v3 and v4.
- **E2E smoke (demo):**
  - **Pair chips:** they show; every chip has the AI mark; "v1 ↔ v3" opens that pair with its AI
    comparison and the diff; the labels read Older / Newer; Restore is disabled.
  - **Versions tab:** ticking two versions names the pair; a third tick drops the oldest; the button
    opens the pair; the restore icon is disabled.
  - **Phone (390px):** the chips wrap and nothing scrolls sideways.
- **E2E write spec (non-demo):**
  - upload v1 and v2, then restore v1 from the Versions tab ("Restore v1 as v3?"): the toast, three
    rows, the current row "Restored from v1", and the preview with the v1 text;
  - Compare v1 ↔ v3 says "No text differences"; v2 ↔ v3 shows "+1";
  - restore v2 from the Compare page with a custom description: v4.
  - The test deletes its document.
- **Screenshots:** `version screens` adds compare-pairs and versions-pick.

## Verification

- Release build: 0 errors. Unit tests: 249/249.
- **Demo mode, fresh database:**
  - smoke: 46 passed, 2 skipped (non-demo only);
  - screenshots: 18 passed, 2 skipped (on demand);
  - crawler: 1,112 pages (522 user + 590 admin), 0 errors;
  - no `fail` or `crit` log lines.
- **Demo mode off, after a fresh seed:** write specs 20/20. The two `fail` log lines are the text
  extractor rejecting the deliberately damaged PDF and DOCX; they are expected.
- Port 5199 was free after each run.
