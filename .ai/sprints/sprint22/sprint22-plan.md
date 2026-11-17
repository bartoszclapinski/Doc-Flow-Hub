# Sprint 22: Restore a version, and compare any two versions

Two gaps in working with versions:

- an older version cannot be restored;
- comparing anything but the two newest versions is hard to find. With three versions, v1 ↔ v3 and
  v1 ↔ v2 can be reached, but the demo has no AI comparison for them, so they look empty.

Status: **done** (2026-11-17, see [`sprint22-log.md`](sprint22-log.md)) · Branch: `feat/versions-upload` → PR into `master`.
Merging into `master` redeploys the public demo, so it needs the user's separate approval.

## State before the sprint

- **Compare page:** `GetComparisonAsync` accepts any pair, and the pickers on the Compare page offer
  every version. But the pickers look like small chips ("v1⌄"), not like a choice.
- **Entry points:** they open v(n-1) ↔ vn. In the Versions tab, each older row compares only with the
  current version.
- **Demo seed:** it saves AI comparisons only for neighbouring versions (v1 → v2, v2 → v3). For v1 ↔ v3
  the inspector says "No AI comparison yet", and the demo cannot generate one.
- **Line diff (Sprint 21):** it already works for every pair.
- **Restore:** there is none. `IDocumentService.RestoreDocumentAsync` restores a deleted document, not a
  version.

## Decisions

| # | Topic | Decision |
|---|---|---|
| S22-1 | What restore does | Restoring vK **adds a new version** (v4 when the newest is v3) with the file of vK. Nothing is overwritten or deleted. Its description is "Restored from vK", or the user's own text. Title, description, categories, folder and sharing stay as they are. |
| S22-2 | Who | Same rule as uploading a new version: the owner, or someone with a manage right. In the demo, the button is disabled with the read-only tip, like the other writes. |
| S22-3 | AI | No AI request. If vK has an AI summary, the new version gets a copy, marked "copied from vK". The line diff of v4 and vK is then empty ("No text differences"), which shows the restore worked. |
| S22-4 | Where | 1. On the Compare page, in the toolbar (as in the 3i mock): "Restore vK" for the older side. 2. In the Versions tab: a restore icon on each older version. 3. In the Details "⋯" menu, when an older version is previewed. Each one opens a confirmation dialog ("Restore v1 as v4?", an optional description, Restore / Cancel). After the restore, the page goes to Details on the new version with a toast. |
| S22-5 | Storage | The file is copied with the same storage service as an upload (blob or local folder). Size and hash come from the copy. If the copy fails, no version row is written. |
| S22-6 | Comparing any pair | **Compare page:** the version header becomes "Compare [v1 ⌄] with [v3 ⌄]", where both pickers look like real dropdowns, with a ⇄ swap button. Under the header, for documents with up to 6 versions, a row of pair chips (v1↔v2 · v2↔v3 · v1↔v3 · …); the chips with a saved AI comparison get a ✦. |
| S22-7 | Picking in Versions | Each row gets a checkbox. With two rows checked, "Compare v1 ↔ v3" appears above the list. The per-row "compare with current" icon stays. |
| S22-8 | Details | The "Version comparisons" block lists the saved AI comparisons as now, plus "Compare any two versions…", which opens Compare. |
| S22-9 | Demo | The seed saves an AI comparison for **every pair** of versions, not only neighbours, so v1 ↔ v3 has one too. The texts come from the same templates. |

## Tasks

1. **Core / Infrastructure:**
   - `IDocumentService.RestoreVersionAsync(documentId, versionNumber, userId, message?)`: permission
     check, file copy, new version row, copy of the summary;
   - the demo seed with all pairs.
2. **Details:**
   - the restore dialog, the restore icon in the Versions tab and the "⋯" entry;
   - the version checkboxes and "Compare selected";
   - the "Compare any two" link.
3. **Compare:**
   - the "Compare [v1 ⌄] with [v3 ⌄]" header and the swap;
   - the pair chips;
   - "Restore vK".
4. **Unit tests:**
   - restore adds a version with the same bytes and hash, and the description "Restored from vK";
   - a user without the right is refused;
   - an unknown version is refused;
   - restoring the newest version is refused (nothing to restore);
   - the summary is copied;
   - the storage fails → no version row;
   - the demo seed has n(n-1)/2 comparisons per document.
5. **E2E:**
   - **demo:**
     - the pair chips open v1 ↔ v3, which has an AI comparison;
     - the swap swaps;
     - two checkboxes in Versions open their pair;
     - Restore is disabled with the tip;
     - phones (390px): no sideways scroll.
   - **non-demo (write spec):**
     - upload v1, then v2;
     - restore v1 → v3 exists;
     - its preview has the v1 text;
     - the diff of v1 ↔ v3 says "No text differences";
     - the test deletes its document.
   - **screenshots:**
     - the Compare page with the chips;
     - the restore dialog;
     - the Versions tab with two rows checked.
6. **Docs:**
   - README: the feature list and "Not there yet" (drop "cannot be restored");
   - the sprint log;
   - the `redesign-ide-plan.md` note on 3i.

## Out of scope

Restoring a deleted version, restoring the old title or categories along with the file, comparing
more than two versions at once.

## Verification

Release build, unit tests, smoke + screenshots + crawler (demo), write specs (non-demo), CI on the PR.
After the master merge (separate approval): live smoke against the demo.
