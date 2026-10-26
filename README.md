# Baymont Internal Registry

Owners / units / vehicles / contractors / deliveries registry for Baymont Al-Sokhna security desks.

## Stack

- ASP.NET Core 10 + EF Core + Identity + SQLite (single local file, no DB server)
- React (Vite + TypeScript) Arabic RTL UI
- Cookie auth, roles: Admin / Editor / SearchOnly / Overhead (handover engineer: records deliveries only)
- Optimistic concurrency + audit log

## Quick start (dev)

```bash
# No database server needed: the DB file is created at
# backend/Baymont.Api/App_Data/baymont.db on first run.

cd backend
dotnet run --project Baymont.Api

# other terminal
cd frontend
npm install
npm run dev
```

Open http://localhost:5173 — API proxied to http://localhost:5080

Users created on first start (passwords come from the `Seed` section of `appsettings.json`; override on a server with `Seed__AdminPassword` / `Seed__GatePassword`):

- `admin` (Admin)
- `gate` (SearchOnly)

## Deploy

- **Railway** (Docker, auto-deploy on push to `main`): see [deploy/railway.md](deploy/railway.md).
- **Windows LAN / IIS**: see below.

## Windows production publish

See [deploy/iis-notes.md](deploy/iis-notes.md) and [docs/ops-runbook.md](docs/ops-runbook.md).
