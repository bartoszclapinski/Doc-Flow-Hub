# Deploying the read-only demo to Azure (free tiers)

This is the step-by-step for putting DocFlowHub online as a **public, read-only demo**
using free Azure tiers. The app code is already demo-ready — it just needs the Azure
resources and a few config values.

What "demo mode" does (all controlled by `DemoMode:Enabled=true`):
- **Read-only** — every mutating action is blocked server-side (you can browse, search,
  and download; you can't create/edit/delete).
- **Registration disabled** — the sign-in page shows the two demo logins instead.
- **AI disabled** — no provider key needed in the cloud; documents show pre-seeded summaries.
- **Seeded data** — on first request the DB is migrated and filled with demo content
  (documents, teams, projects, folders, and ~90 days of usage data for the admin charts).

Demo logins (shown on the sign-in page):
- User: `demo@docflowhub.com` / `Demo123!`
- Admin: `demo-admin@docflowhub.com` / `Demo123!`

---

## 1. Create the Azure resources (free tiers)

You can do this in the Azure Portal or with the `az` CLI. Names must be globally unique
where noted.

1. **Resource group** — e.g. `docflowhub-demo-rg` (pick a region close to you).
2. **Azure SQL Database** — create a SQL *server* + a database on the **free/serverless**
   tier. Note the server admin login/password. Allow Azure services to access the server
   (firewall rule "Allow Azure services").
3. **Storage account** — Standard LRS. Create a **blob container** named `documents`.
   Copy the storage **connection string** (Access keys blade).
4. **App Service** — create a **Linux** Web App on a **Free (F1)** App Service Plan,
   runtime **.NET 9 (STS)**. This name is your `AZURE_WEBAPP_NAME`.

## 2. Configure the Web App (Settings → Environment variables / Application settings)

Add these (note the double-underscore `__` — that's how nested config maps to env vars):

| Name | Value |
|------|-------|
| `DemoMode__Enabled` | `true` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | *your Azure SQL connection string* |
| `ConnectionStrings__ApplicationDbContextConnection` | *same Azure SQL connection string* |
| `DocumentStorage__ConnectionString` | *your storage account connection string* |
| `DocumentStorage__ContainerName` | `documents` |

**Do NOT set any AI key** (`DocFlowHub:AnthropicApiKey`, `OpenAI:ApiKey`) — AI is disabled
in demo mode and none is needed.

> The SQL connection string should use SQL auth (the server admin login you created), e.g.
> `Server=tcp:YOURSERVER.database.windows.net,1433;Database=YOURDB;User ID=...;Password=...;Encrypt=True;TrustServerCertificate=False;`

## 3. Wire up GitHub Actions deploy

1. In the Azure Web App → **Get publish profile** (download the `.PublishSettings` file).
2. In GitHub → repo **Settings → Secrets and variables → Actions → New repository secret**:
   - Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Value: paste the entire contents of the downloaded publish-profile file.
3. Edit `.github/workflows/deploy-demo.yml` → set `AZURE_WEBAPP_NAME` to your App Service name.
4. GitHub → **Actions → "Deploy demo to Azure" → Run workflow**.

## 4. First run

On the first request after deploy, the app:
- runs EF Core migrations (creates the schema), then
- seeds the demo dataset (idempotent — subsequent restarts won't duplicate it).

The first request may take a few seconds (F1 has no Always-On). Then open the site,
sign in with a demo login above, and browse. Sign in as the **demo admin** to see the
admin dashboards/charts populated.

## Notes & limits

- **Downloads of seeded documents** won't return a real file — the seed creates document
  *metadata* (for lists, details, versions, and charts) with placeholder storage paths, not
  actual blobs. Everything else (browsing, search, details, admin analytics) works. To make
  downloads work, upload matching blobs to the `documents` container or extend the seeder.
- **F1 Free** sleeps when idle and has limited CPU/RAM — fine for a demo, not for load.
- To take the demo down, stop or delete the App Service; to reset data, drop the SQL
  database and let the next request re-migrate + re-seed.
