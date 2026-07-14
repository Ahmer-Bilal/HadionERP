# How to Run HadionERP

The application has three parts: a **database** (PostgreSQL, holds the real data), a **backend** (the
server that does the actual work), and a **frontend** (the website you look at in your browser). The
backend and frontend each need their own terminal window, left running.

## 0. Database (one-time setup)

Requires PostgreSQL installed and running locally (already set up on this machine). The connection string
is stored via .NET User Secrets, never in a committed file:

```
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=erp_platform_dev;Username=postgres;Password=YOUR_PASSWORD" --project src/Gateway/Gateway.Api
```

You only need to do this once per machine. If you ever need to reapply database migrations (after pulling
new schema changes), run from the repo root:

```
dotnet tool install --global dotnet-ef   # first time only
$env:ERP_MASTERDATA_CONNECTION = "Host=localhost;Port=5432;Database=erp_platform_dev;Username=postgres;Password=YOUR_PASSWORD"
dotnet ef database update --project src/Modules/Modules.MasterData/Infrastructure/Modules.MasterData.Infrastructure.csproj
```

## 1. Start the backend

Open a terminal (PowerShell) in the project folder and run:

```
dotnet run --project src/Gateway/Gateway.Api
```

Leave this window open. It's ready when you see a line like `Now listening on: http://localhost:5210`.

You can check it directly by opening **http://localhost:5210/swagger** in a browser — that's the
backend's own built-in documentation/testing page.

## 2. Start the frontend

Open a **second** terminal window and run:

```
cd src/Apps/Apps.Shell
npm run dev
```

The first time only, run `npm install` in that same folder before `npm run dev`.

Leave this window open too. It's ready when you see a line like `Local: http://localhost:5173/`.

## 3. Open it in your browser

Go to **http://localhost:5173** — that's the actual application. You'll land on a "System Status" page
showing the backend is alive; use the left-hand navigation to go to **Master Data → All Business
Partners** to see the first real business screen (create/view customers and vendors). The language
switcher (English / العربية) in the top-right flips the whole layout right-to-left for Arabic, everywhere
in the app, not just the status page.

## What you'll actually see right now

Phase 0 (platform foundation) and the first slice of Phase 1 (Business Partners) are done — see
`PROGRESS.md`. There's no Chart of Accounts, Procurement, or Construction yet. Business Partner data you
create is real and persisted (PostgreSQL) — it survives closing and reopening the app, unlike the System
Status page's numbers which reset per session.

## Stopping

Press `Ctrl+C` in each terminal window to stop that process.
