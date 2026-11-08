# Deploying the read-only demo to Azure (free tiers)

This is the step-by-step for putting DocFlowHub online as a **public, read-only demo**
on free Azure tiers. The app code is demo-ready; it needs three Azure resources, a few
app settings and one GitHub Actions run.

What "demo mode" does (all controlled by `DemoMode:Enabled=true`):
- **Read-only**: every mutating action is blocked server-side (you can browse, search
  and download; you can't create, edit or delete).
- **Registration disabled**: the sign-in page shows the two demo logins instead.
- **AI disabled**: no provider key is needed; documents show pre-seeded summaries.
- **Seeded data**: on start the DB is migrated and filled with demo content (documents
  with real downloadable files, teams, projects, folders and ~90 days of usage data for
  the admin charts).
- **No default admin**: the `admin@docflowhub.com` account (its password is in this repo)
  is not created in demo mode.
- **`/robots.txt` disallows crawling**: every visit can wake the free-tier database.

Demo logins (shown on the sign-in page):
- User: `demo@docflowhub.com` / `Demo123!`
- Admin: `demo-admin@docflowhub.com` / `Demo123!`

---

## 1. Create the Azure resources (free tiers)

Use one resource group and one region for everything (e.g. West Europe or North Europe).

1. **Azure SQL Database, free tier**: go to `aka.ms/azuresqlhub` → **Start free**, or use
   the CLI. Free-tier databases auto-pause after 15 idle minutes and wake on the next
   connection (up to ~1 minute). Keep the behaviour at the free limit on **Auto-pause
   until next month**; the other option bills you and can't be undone.
   ```
   az sql db create -g <rg> -s <server> -n docflowhub --edition GeneralPurpose --family Gen5 \
     --capacity 2 --compute-model Serverless --use-free-limit true \
     --free-limit-exhaustion-behavior AutoPause --backup-storage-redundancy Local
   ```
   Under the server's **Networking**, allow Azure services and resources to access it.
2. **Storage account**: Standard, LRS, Hot. Create a blob container named `documents`.
   Copy the **connection string** (Security + networking → Access keys). Storage is
   required: most pages fail without it.
3. **App Service**: a **Linux** Web App on the **Free (F1)** plan, runtime **.NET 10 (LTS)**.
   Turn on **SCM Basic Auth Publishing Credentials** (needed for the publish profile).

## 2. Configure the Web App (Settings → Environment variables → App settings)

`__` (double underscore) maps to nested config keys.

| Name | Value |
|------|-------|
| `DemoMode__Enabled` | `true` |
| `ConnectionStrings__DefaultConnection` | your Azure SQL connection string (see below) |
| `DocumentStorage__ConnectionString` | your storage account connection string |
| `DocumentStorage__ContainerName` | `documents` |
| `Logging__LogLevel__Microsoft` | `Warning` (otherwise every SQL command of the seed is logged) |
| `WEBSITES_CONTAINER_START_TIME_LIMIT` | `600` (the first start migrates and seeds) |
| `WEBSITE_WEBDEPLOY_USE_SCM` | `true` (required by the publish-profile deploy on Linux) |

`ASPNETCORE_ENVIRONMENT` defaults to `Production` on App Service; leave it unset.
**Do not set any AI key** (`DocFlowHub:AnthropicApiKey`, `OpenAI:ApiKey`).

SQL connection string (SQL authentication, the server admin login):
```
Server=tcp:<server>.database.windows.net,1433;Initial Catalog=docflowhub;User ID=<login>;Password=<password>;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True;Connection Timeout=60;
```

## 3. Wire up the GitHub Actions deploy

1. Azure Web App → **Overview → Download publish profile**.
2. GitHub → repo **Settings → Secrets and variables → Actions**:
   - **Secrets** tab → `AZURE_WEBAPP_PUBLISH_PROFILE` = the whole publish-profile file.
   - **Variables** tab → `AZURE_WEBAPP_NAME` = your App Service name.
3. GitHub → **Actions → "Deploy demo to Azure" → Run workflow**.

The workflow restores the client libraries (`libman restore`; `wwwroot/lib` is not in git),
builds, runs the tests, publishes and deploys. Never commit the publish profile or any
connection string.

## 4. First run

On every start the app runs EF Core migrations and then the demo seed. The seed is
idempotent: a restart doesn't duplicate data, it only uploads the demo files again if they
are missing from storage. The first start takes a few minutes. Open the site, sign in with
a demo login and browse; sign in as the **demo admin** to see the admin dashboards.

Useful log lines (App Service → Log stream):
- `Demo seed complete: … documents` and `Uploaded … demo files to storage`: first start OK.
- `Demo data already present — skipping demo seed.`: a normal restart.
- `Demo files missing from storage — uploading them.`: files are being re-uploaded.
- `Demo file step failed` / `Document storage is not configured`: check `DocumentStorage__*`.
- `Database initialization failed`: the database was unreachable; restart the app later.

## Notes & limits

- **Sleeping database**: transient SQL errors (e.g. 40613 while the database resumes) are
  retried for about two minutes, for queries at startup and per request. If the database
  is still unreachable at startup (e.g. the free monthly allowance is used up), the app
  starts anyway and shows an error page that explains the demo is waking up or out of quota.
  Restart the app once the database is back.
- **F1 Free** unloads after ~20 idle minutes and has 60 CPU minutes per day; fine for a
  demo, not for load.
- **Take the demo down**: stop or delete the App Service.
- **Reset the data**: stop the App Service, delete and recreate the database (free tier,
  same name), optionally delete and recreate the `documents` container, then start the app
  again so it re-migrates and re-seeds (files with the same IDs are overwritten).
