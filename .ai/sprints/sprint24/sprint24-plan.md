# Sprint 24: A richer demo

The demo shows every screen, but the data behind it is thin. In the current IDE layout, which puts the
tree, the lists and the inspector side by side, that makes the app look empty. This sprint rewrites the
demo seed so the data looks like a real small company's workspace.

Status: **done** (2026-11-17, see [`sprint24-log.md`](sprint24-log.md)) · Branch: `feat/richer-demo` → PR into `master`.
Merging into `master` redeploys the public demo (the demo database is built fresh on each deploy).

## The seed today (`DemoDataSeeder`)

- **People:** two accounts only, the demo user and the demo admin. Every document, version and team
  member is one of the two.
- **Teams:** 4 teams with the same two members, described as "Engineering team workspace".
- **Projects:** 4, each with the same folders (Drafts, Final / Approved, Archive), described as
  "… project workspace".
- **Documents:**
  - 52 documents with random titles ("Annual Report 2025-03", "Invoice 2024-11"), placed in a random
    project and folder;
  - PDF, DOCX, TXT and MD only (no images);
  - 1–3 versions, named "Revision 2";
  - the same three body sentences in every file;
  - the same four AI summary texts.

## Decisions

| # | Topic | Decision |
|---|---|---|
| S24-1 | People | **10 colleagues** besides the two demo accounts, with names, job titles and avatars' initials (e.g. Maya Chen, Product Manager; Tom Becker, Backend Engineer; Priya Nair, Finance Lead). They own documents, write versions and sit in teams. They **cannot sign in**: they get a random password nobody knows. The admin user list gets real rows too. |
| S24-2 | Teams | **6 teams** with real descriptions: Engineering, Product, Marketing, Finance, Legal & Compliance, Customer Success. Each has 3–6 members with Admin / Member roles. The demo user is in 4 of them, so shared documents from colleagues show up for them. |
| S24-3 | Projects | **10 projects** with their own colours, icons, descriptions and folder trees that fit the project, 2–3 levels deep. For example, "Website Redesign": Research / Wireframes / Content / Launch checklist; "FY2026 Budget": Departments / Forecasts / Board pack. One project is archived, so the Archived tab is not empty. The project tree shows only the signed-in user's projects, so 8 belong to the demo user and 2 to the demo admin. |
| S24-4 | Documents | **About 130 documents** from a catalogue written for each project: meeting notes, contracts, specifications, release notes, budgets, policies, postmortems, guides. The type fits the content: contracts and policies in DOCX / PDF, notes and release notes in MD, logs and exports in TXT, diagrams and charts as images. Some live outside projects ("Unfiled"). About 40% are shared with a team, and about 25% belong to colleagues and reach the demo user through a team. |
| S24-5 | Content | Each kind of document has its own **text templates** with real structure, filled from the document's metadata. <ul><li>**Meeting notes:** attendees, decisions, action items with owners and dates.</li><li>**Contracts:** parties, numbered clauses and a table of fees.</li><li>**Specifications:** sections and an endpoint table.</li><li>**Release notes:** version headings, lists and a code block.</li><li>**Budgets:** a table per department.</li></ul> The DOCX view, the Markdown view and the PDF pages then show headings, lists and tables. |
| S24-6 | Pictures | **PNG pictures generated in code:** bar charts, pie-like blocks, wireframe boxes and a team photo placeholder, drawn with a tiny PNG writer (no new package). About 10 documents are images, so the image preview and the Image type filter have content. |
| S24-7 | Versions | **1 to 7 versions** per document: most have 1–3, and about 15 documents have 5–7. Each version changes **specific lines** of the text (a figure, a date, an added clause, a removed item, a new section), so the line diff shows real edits. The authors are different people, and the change descriptions say what changed ("Updated the hosting fee after the vendor call", "Added clause 7: data retention"). |
| S24-8 | AI texts | Summaries, key points and version comparisons come from templates for each kind of document and name the document's own facts (the vendor, the amount, the release number). About 75% of documents have a summary. Every pair of versions has a comparison (as since Sprint 22). |
| S24-9 | Time | Dates spread over 14 months, more of them recent, so the dashboard, "last 7 days", the activity lists and the admin charts look alive. AI usage logs follow the same spread and are split between people. |
| S24-10 | Categories | 10 categories instead of 7: Reports, Contracts, Invoices, Specifications, Manuals, Presentations, Policies, plus Meeting notes, Design and Research. Each document gets 1–3 categories that fit it. |
| S24-11 | Rebuildable files | The demo files are built from stored metadata (the title points into the catalogue, plus the version number). The existing backfill can therefore still rebuild a missing file without the random generator. Unit tests check that a rebuilt file has the same bytes as the seeded one. |

## Tasks

1. **Catalogue:**
   - `DemoCatalog`: people, teams, projects with folder trees, and documents (title, kind, type,
     project / folder, owner, team, categories, number of versions);
   - `DemoContent`: text templates for each kind, and the edits each version makes;
   - `DemoAiTexts`: the summaries and comparisons.
2. **Files:**
   - `DemoFileFactory`: tables and code blocks for DOCX / MD / PDF where it lacks them;
   - a PNG writer for the generated pictures.
3. **Seeder:**
   - people (UserManager, random passwords), teams, projects, folders, documents, versions, summaries,
     comparisons and usage logs from the catalogue;
   - kept: the deterministic seed, idempotency, and the backfill.
4. **Unit tests:**
   - the catalogue is consistent: every folder exists in its project, every owner and team exists, and
     no title appears twice;
   - every demo file passes the upload checks (type and content);
   - the versions of a document differ, and in the expected lines;
   - a rebuilt file matches the seeded one;
   - colleagues cannot sign in.
5. **E2E:**
   - the existing smoke tests (they pick documents by type and version count);
   - new checks:
     - the demo user sees at least 8 projects, a document from a colleague and an image document;
     - a document has 5 or more versions;
     - the admin user list has more than 10 users.
   - screenshots of the dashboard, the documents list, a project, a team and the admin users page.
6. **Docs:** README (what the demo contains), the sprint log, a short `docs/` note on how the demo seed
   is built.

## Questions for the user

1. **Scale:** is it right? About 130 documents, 10 projects, 6 teams and 10 colleagues.
2. **Language:** English content, like the app?
3. **Pictures:** generated charts and wireframes as image documents, OK?

## Out of scope

Comments, notifications and activity feeds that the app does not have; real sample files from outside.

## Verification

Release build, unit tests, smoke + screenshots + crawler (demo), write specs (non-demo), CI on the PR.
After the master merge (separate approval): live smoke against the demo.
