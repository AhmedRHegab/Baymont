# Deploying to Railway

The repo deploys from the root `Dockerfile` (see `railway.json`). Every push to `main` redeploys.

## One-time setup (Railway dashboard)

1. **New Project → Deploy from GitHub repo →** `AhmedRHegab/Baymont`.
2. **Add a Volume** to the service and mount it at `/app/App_Data`.
   It holds the SQLite database (`baymont.db`), uploaded photos (`uploads/`) and login keys (`keys/`).
   Without the volume, all data is lost on every deploy.
3. **Variables** (service → Variables):

   | Variable | Value |
   |----------|-------|
   | `ADMIN_INITIAL_PASSWORD` | **Required.** Strong password for the `admin` user (8+ chars, upper, lower, digit, symbol). Used only when the admin does not exist yet. |
   | `GATE_INITIAL_PASSWORD` | Optional. Creates the read-only `gate` user. |

   The app refuses to start on an empty database without `ADMIN_INITIAL_PASSWORD`.
4. **Settings → Networking → Generate Domain.** Open it and log in as `admin`.

## Things to know

- **Single replica only.** SQLite is a file on one volume; keep `numReplicas` at 1.
- **It is on the public internet.** Unlike the LAN setup, anyone with the URL can reach the login page. Use strong passwords, create users from the Users page, and remove unused accounts. Contractors' ID cards and criminal-record photos are stored here.
- **Backups.** Railway volumes have backup options in the volume settings; enable them. The `deploy/backup.ps1` script is for the Windows LAN setup.
- **Health check:** `GET /healthz`.
- **Local test of the image:**
  ```bash
  docker build -t baymont .
  docker run --rm -p 8080:8080 -e ADMIN_INITIAL_PASSWORD='Str0ng!Passw0rd' -v baymont_data:/app/App_Data baymont
  ```
