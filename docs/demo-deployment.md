# Deploying the read-only demo (Render, free)

The public demo is **one self-contained container** on Render's free plan. There is no
database server and no cloud storage: on every start the app creates a fresh SQLite
database inside the container, seeds the demo data and writes the demo files to the
container's disk. Nothing needs to persist, because the demo is read-only.

What "demo mode" does (`DemoMode:Enabled=true`):
- **Read-only**: every mutating action is blocked server-side (you can browse, search
  and download; you can't create, edit or delete).
- **Registration disabled**: the sign-in page shows the two demo logins instead.
- **AI disabled**: no provider key is needed; documents show pre-seeded summaries.
- **Seeded data**: documents with real downloadable files (PDF, DOCX, TXT, MD), teams,
  projects, folders and ~90 days of usage data for the admin charts, dated relative to
  the moment the container started.
- **No default admin**: the `admin@docflowhub.com` account (its password is in this repo)
  is not created in demo mode.
- **`/robots.txt` disallows crawling.**

Demo logins (shown on the sign-in page):
- User: `demo@docflowhub.com` / `Demo123!`
- Admin: `demo-admin@docflowhub.com` / `Demo123!`

---

## How it is wired

| File | Role |
|------|------|
| `Dockerfile` | Builds the image (.NET 10, `libman restore` for the client libraries) and sets the demo configuration as environment variables. |
| `render.yaml` | Render Blueprint: one free Docker web service, deploys `master` after the CI checks pass, health check `/healthz`. |
| `.github/workflows/ci.yml` | Builds, runs the tests and builds the Docker image on every push and PR. |

Demo configuration (set in the `Dockerfile`):

| Setting | Value |
|---------|-------|
| `DemoMode__Enabled` | `true` |
| `Database__Provider` | `Sqlite` (default is SQL Server) |
| `ConnectionStrings__DefaultConnection` | `Data Source=/tmp/docflowhub/demo.db` |
| `DocumentStorage__Provider` | `Local` (default is Azure Blob Storage) |
| `DocumentStorage__LocalPath` | `/tmp/docflowhub/files` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` (Render terminates HTTPS in front of the app) |
| `PORT` | `10000` (Render's default; the app listens on `PORT`) |

No secrets are needed. **Do not add any AI key.**

## One-time setup on Render

1. Sign up at render.com with your GitHub account and allow Render to access this repo.
2. **New → Blueprint**, pick this repository and branch `master`. Render reads `render.yaml`
   and proposes the `docflowhub-demo` web service on the **Free** plan. Click **Apply**.
3. Wait for the first build and deploy (a few minutes), then open the `*.onrender.com` URL.

From then on every merge to `master` deploys automatically once CI is green. A deploy
whose health check fails is rolled back and the previous version stays live.

## What to expect

- **Sleep**: a free service sleeps after 15 minutes without traffic. The next visitor sees
  Render's loading screen for about a minute while the container starts and seeds.
- **Fresh data on every start**: anything a visitor could change (nothing, in read-only
  mode) and the login sessions are reset when the container restarts.
- **Limits**: 750 free instance hours per month per workspace (enough for one service
  running all month). If the hours or the included bandwidth run out, Render suspends free
  services until the next month.
- **Logs**: Render dashboard → the service → **Logs**. A healthy start shows
  `Demo seed complete: … documents` and `Uploaded … demo files to storage`.

## Run the demo container locally

```
docker build -t docflowhub-demo .
docker run --rm -p 8080:10000 docflowhub-demo
```

Then open http://localhost:8080. Add `--cpus 0.1 --memory 512m` to get close to Render's
free instance.

## Take the demo down

Render dashboard → the service → **Settings → Suspend** (or **Delete**).
