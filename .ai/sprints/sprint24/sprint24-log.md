# Sprint 24 log: A richer demo

Plan: [`sprint24-plan.md`](sprint24-plan.md) · Branch `feat/richer-demo` → PR into `master`.

## What was built

- **`DemoCatalog`** (`Infrastructure/Data/Demo/`): the fixed demo workspace of a small software company.
  - **People:** 10 colleagues with job titles (stored as the profile bio), e.g. Maya Chen, Product
    Manager; Tom Becker, Backend Engineer.
  - **Teams:** 6 with real descriptions and 3–5 members, Admin or Member. The demo user is in
    Engineering, Product, Marketing and Customer Success; the demo admin in Engineering, Finance and
    Legal & Compliance.
  - **Projects:** 10, each with its own colour, icon, description and folder tree (2 levels, e.g.
    Website Redesign: Research, Wireframes, Wireframes / Mobile, Content, Launch). 8 belong to the demo
    user, one of them archived (Office Move 2025); 2 belong to the demo admin.
  - **Documents:** 129, written for the projects:
    - mostly single documents (requirements, contracts, budgets, postmortems, runbooks, policies, …);
    - plus series: customer check-ins, release notes, monthly KPI reports, Northwind invoices, weekly
      product syncs, engineering standups.

    Their titles are the key for rebuilding files, so the series are numbered, not dated.
- **`DemoContent`:** text templates for 15 kinds of document, filled with facts derived from the title
  through a stable FNV-1a hash.
  - **Structure:** headings, lists, tables, bold labels, italic quotes and code blocks.
  - **Versions:** each version changes specific lines, named by its change description ("Updated the
    monthly fee after the vendor call", "Added clause 7: data retention"). The line diff and the saved
    AI comparisons therefore agree.
  - **Layouts that fit the title:** reports have five (KPI, security findings, error budget, hiring,
    survey, plus an agenda); guides are runbooks with commands or business checklists; only the travel
    policy has a meal allowance.
  - **PDF:** KPI reports get a monthly-detail page and an appendix, so a PDF has three pages.
- **`DemoImageFactory`:** 960×600 PNG pictures drawn in code — bar chart, line chart, page wireframe,
  box-and-arrow diagram, donut. It uses a small RGB canvas and a PNG writer (zlib from the BCL, CRC-32
  in code), so no package is needed. Each version changes the picture: new cards in the wireframe, a
  new service in the diagram, moved figures in the charts.
- **`DemoFileFactory`:**
  - **Markdown:** "## " headings, lists, pipe tables with a header separator, fenced code.
  - **TXT:** underlined headings, tables with lined-up columns, indented code.
  - **PDF:** table cells side by side, indented code, no bold or italic marks.
  - **DOCX:** code in Consolas.
  - **PNG:** a supported demo type.
- **`DemoDataSeeder`:**
  - **People:** colleagues are created with a random password that is never stored, so nobody can
    sign in as them. Their accounts are dated before the demo accounts.
  - **Content:** teams, projects, folders, documents and versions come from the catalogue. Version
    dates spread from the first version to almost today; version authors come from the document's team.
  - **AI texts:**
    - summaries name the document's own facts and cover about 75% of documents;
    - comparisons for every pair of versions list exactly the changes between them, with a type and
      significance that follow the number of changes;
    - 640 usage logs spread over 14 months and split between all people.
  - **Files:** `BuildFile(doc, version)` builds every file from stored metadata: the template, the
    picture, or the old generic text for rows from an earlier seed. The upload and the backfill both
    use it.

## Differences from the plan

- **DOCX files are not byte-identical** when rebuilt: a DOCX is a zip with timestamps, as in the earlier
  seed. The test compares what the document shows instead; the other types are compared byte by byte.
- **Folder trees are 2 levels deep**, not 2–3.
- **No `docs/` note:** the catalogue and the templates are documented in code, and the README says what
  the demo contains.

## Tests

- **Unit (139 new including one theory row per document, 398 total):**
  - **Catalogue:**
    - the agreed size;
    - everything it refers to exists (people, teams, projects, folders and their parents, categories);
    - unique titles and emails;
    - colleagues' documents reach a demo account through a team, and everything in the demo user's
      projects is visible to them.
  - **Every document:** every version builds a valid upload (type and content check), rebuilds the same,
    and differs from the version before.
  - **Text documents:** the line diff finds a change between every pair of consecutive versions.
  - **AI texts:** they name the document's facts, and the comparison lists the right changes.
  - **Pictures:** every one is a PNG and changes between versions.
  - **Old rows:** a row from an older seed still gets a file.
  - **Files:** Markdown and TXT output for headings, lists, tables and code.
- **E2E smoke (demo):**
  - **Two new tests:**
    - the user's projects (7 active and the archived Office Move), project folders, a colleague's
      document, an image document, and the 7 versions of the requirements document;
    - the admin's 12 users, and the 7-version Northwind contract with clause 7.
  - **Updated tests:** the tests that relied on the old random data now open documents by title:
    - the Word preview: the requirements document;
    - the PDF pages: the three-page revenue forecast;
    - the palette: "revenue fore";
    - the details tabs.
- **E2E write specs (non-demo):** the move test picks the "Interview kits" folder (there are no "Drafts"
  folders any more).

## Verification

- Release build: 0 errors. Unit tests: 398/398.
- **Demo mode, fresh database:**
  - seed: 129 documents, 333 files, 640 usage logs;
  - smoke: 51 passed, 2 skipped (non-demo only);
  - screenshots: 19 passed, 2 skipped (on demand);
  - crawler: 900 pages (342 user + 558 admin), 0 errors;
  - no `fail` or `crit` log lines.
- **Demo mode off, after a fresh seed:** write specs 22/22. The two `fail` log lines are the text
  extractor rejecting the deliberately damaged PDF and DOCX; they are expected.
- Port 5199 was free after each run.
