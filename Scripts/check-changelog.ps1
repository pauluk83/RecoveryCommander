# Pre-build Changelog Verification Script
# This script checks if CHANGELOG.md has been updated since the last build
# and warns if there are uncommitted changes without changelog entries.

param(
    [string]$ProjectRoot = $PSScriptRoot + "\.."
)

$ErrorActionPreference = "Stop"

Write-Host "=== Changelog Verification ===" -ForegroundColor Cyan

$changelogPath = Join-Path $ProjectRoot "CHANGELOG.md"

if (-not (Test-Path $changelogPath)) {
    Write-Host "ERROR: CHANGELOG.md not found at $changelogPath" -ForegroundColor Red
    exit 1
}

# Check if there are uncommitted changes
$gitStatus = git -C $ProjectRoot status --porcelain 2>$null

if ($gitStatus) {
    Write-Host "INFO: Uncommitted changes detected; changelog verification will continue." -ForegroundColor Cyan
    
    # Check if CHANGELOG.md is among the modified files
    $changelogModified = $gitStatus | Select-String "CHANGELOG.md"
    
    if (-not $changelogModified) {
        Write-Host "ERROR: CHANGELOG.md has not been updated with recent changes." -ForegroundColor Red
        Write-Host "Please update CHANGELOG.md with all ongoing changes before building." -ForegroundColor Red
        Write-Host ""
        Write-Host "Modified files:" -ForegroundColor Yellow
        Write-Host $gitStatus
        exit 1
    }
    
    # Check if CHANGELOG.md has been modified today
    $changelogLastModified = (Get-Item $changelogPath).LastWriteTime
    $today = Get-Date
    $daysSinceUpdate = ($today - $changelogLastModified).Days
    
    if ($daysSinceUpdate -gt 1) {
        Write-Host "WARNING: CHANGELOG.md was last updated $daysSinceUpdate days ago." -ForegroundColor Yellow
        Write-Host "Please ensure it reflects the most recent changes." -ForegroundColor Yellow
    }
} else {
    Write-Host "No uncommitted changes detected." -ForegroundColor Green
}

# Verify CHANGELOG.md has today's date or recent entries
$changelogContent = Get-Content $changelogPath -Raw
$todayPattern = Get-Date -Format "yyyy-MM-dd"
$hasTodayEntry = $changelogContent -match $todayPattern

if (-not $hasTodayEntry) {
    Write-Host "WARNING: CHANGELOG.md does not have an entry for today ($todayPattern)." -ForegroundColor Yellow
    Write-Host "If you made changes today, please add a changelog entry." -ForegroundColor Yellow
}

Write-Host "=== Changelog Verification Complete ===" -ForegroundColor Green
exit 0
