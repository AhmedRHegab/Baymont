# Deploying to Railway

The repo deploys from the root `Dockerfile` (see `railway.json`). Every push to `main` redeploys.

## One-time setup (Railway dashboard)

1. **New Project → Deploy from GitHub repo →** `AhmedRHegab/Baymont`.
2. **Add a Volume** to the service and mount it at `/app/App_Data`.
   It holds the SQLite database (`baymont.db`), uploaded photos (`uploads/`) and login keys (`keys/`).
   Without the volume, all data is lost on every deploy.
3. **Passwords.** The first-start passwords for `admin` and `gate` are read from the `Seed` section of
   `backend/Baymont.Api/appsettings.json`. To use different ones on Railway without editing the repo, add the
   variables `Seed__AdminPassword` and `Seed__GatePassword` (8+ chars, upper, lower, digit, symbol).
   They are used only when the user does not exist yet; later changes go through the Users page.
4. **Settings → Networking → Generate Domain.** Open it and log in as `admin`.

## Things to know

- **Single replica only.** SQLite is a file on one volume; keep `numReplicas` at 1.
- **The default passwords are in the repository.** Anyone who can read the repo can read them, so set `Seed__AdminPassword` and `Seed__GatePassword` on Railway (or change them from the Users page right after the first login).
- **It is on the public internet.** Unlike the LAN setup, anyone with the URL can reach the login page. Use strong passwords, create users from the Users page, and remove unused accounts. Contractors' ID cards and criminal-record photos are stored here.
- **Backups.** Railway volumes have backup options in the volume settings; enable them. The `deploy/backup.ps1` script is for the Windows LAN setup.
- **Health check:** `GET /healthz`.
- **Local test of the image:**
  ```bash
  docker build -t baymont .
  docker run --rm -p 8080:8080 -v baymont_data:/app/App_Data baymont
  ```
