# Daily SQLite backup for Baymont Internal Registry (Windows Task Scheduler)
# Needs sqlite3.exe (free "sqlite-tools" zip from sqlite.org). It uses the online backup API,
# so it is safe while the site is running (WAL mode).

param(
    [string]$Sqlite3 = "C:\sqlite\sqlite3.exe",
    [string]$DbFile = "C:\baymont\publish\App_Data\baymont.db",
    [string]$BackupRoot = "D:\BaymontBackups",
    [int]$RetainDays = 30
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BackupRoot)) {
    New-Item -ItemType Directory -Path $BackupRoot | Out-Null
}

$stamp = Get-Date -Format "yyyy-MM-dd_HHmm"
$outFile = Join-Path $BackupRoot "baymont_$stamp.db"

& $Sqlite3 $DbFile ".backup '$outFile'"
if ($LASTEXITCODE -ne 0) {
    Write-Error "sqlite3 backup failed with exit code $LASTEXITCODE"
}

Get-ChildItem -Path $BackupRoot -Filter "baymont_*.db" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$RetainDays) } |
    Remove-Item -Force

# Uploaded photos (contractor documents) are plain files: mirror them next to the DB backup.
$uploads = Join-Path (Split-Path $DbFile) "uploads"
if (Test-Path $uploads) {
    robocopy $uploads (Join-Path $BackupRoot "uploads") /MIR /NFL /NDL /NJH /NJS | Out-Null
}

Write-Host "Backup completed: $outFile"
