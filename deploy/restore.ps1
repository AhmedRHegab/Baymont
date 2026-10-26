# Restore a Baymont SQLite backup. Stop the site (IIS app pool / Kestrel service) first.
# Test drill: restore to a scratch path, e.g. -TargetFile C:\baymont\restore-test\baymont.db

param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile,
    [string]$TargetFile = "C:\baymont\publish\App_Data\baymont.db",
    [string]$Sqlite3 = "C:\sqlite\sqlite3.exe"
)

$ErrorActionPreference = "Stop"

$dir = Split-Path $TargetFile
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }

# Remove stale WAL/SHM files so they are not replayed onto the restored file.
Remove-Item "$TargetFile", "$TargetFile-wal", "$TargetFile-shm" -Force -ErrorAction SilentlyContinue
Copy-Item $BackupFile $TargetFile

Write-Host "Restore completed: $TargetFile"
& $Sqlite3 $TargetFile "SELECT 'owners', COUNT(*) FROM owners UNION ALL SELECT 'units', COUNT(*) FROM units UNION ALL SELECT 'vehicles', COUNT(*) FROM vehicles;"
