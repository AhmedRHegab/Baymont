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

## Permit emails (Gmail reader)

Visit permits arrive by email as an `.xlsx` attachment. The app reads the mailbox every 5 minutes and puts the permits in the
review queue (page **الحجوزات**). It stays off until these variables are set on the service:

| Variable | Value |
|----------|-------|
| `Mail__User` | The Gmail address that receives the permits |
| `Mail__Password` | A Google **App Password** for that account (not the normal password) |
| `Mail__AllowedSenders` | Leadership addresses, comma separated. A whole domain works as `@company.com`. Mail from anyone else is never opened. |

Optional: `Mail__PollSeconds` (default 300, minimum 60), `Mail__RequireAuthentication` (default `true`).

**Create the App Password**
1. In the Google account: **Security → 2-Step Verification**, turn it on.
2. Open <https://myaccount.google.com/apppasswords>, name it "Baymont", and copy the 16-character password (no spaces).
3. Put it in `Mail__Password` on Railway. Never put it in the repository.
4. If your Google Workspace admin blocks App Passwords or IMAP, ask them to allow it.

**Safety**
- Only the allowed senders are read, and with `RequireAuthentication=true` the email must also pass Gmail's DMARC check, so a forged "From" is refused.
  If the leadership domain has no DMARC and real permits show as "فشل التحقق من هوية المرسل" on the **البريد** page, set it to `false`; the review queue is the second safety net.
- Every permit waits for a staff member to approve it. Nothing reaches the gate automatically.
- Check **البريد** (Admin/Editor) to see what was read, ignored and why, and use "افحص البريد الآن" to check immediately.
- Without email, an admin can still add a permit by hand from **الحجوزات → إدخال يدوي (للطوارئ)**.

## Sample data

A fresh volume starts with invented sample data and a yellow "بيانات تجريبية" banner, so the system can be shown to users.
Before real use, an admin presses **حذف البيانات التجريبية** (it is permanent and does not touch real records), or set
`Seed__DemoData=false` before the very first start to skip it.

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
