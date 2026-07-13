# How to Run This Application

The application has two parts that both need to be running at the same time: a **backend** (the server
that does the actual work) and a **frontend** (the website you look at in your browser). Each needs its
own terminal window, left running.

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

Go to **http://localhost:5173** — that's the actual application. You should see a "System Status" page
showing the backend is alive, with a language switcher (English / العربية) in the top-right that also
flips the whole layout right-to-left for Arabic.

## What you'll actually see right now

Only the technical foundation exists so far (Phase 0 — see `PROGRESS.md`) — there are no Finance,
Procurement, or Construction screens yet. What you're checking at this stage is that the underlying
plumbing works: backend running, frontend running, and both languages displaying correctly. Real business
screens get added module by module in the phases that follow, inside this same running application.

## Stopping

Press `Ctrl+C` in each terminal window to stop that process.
