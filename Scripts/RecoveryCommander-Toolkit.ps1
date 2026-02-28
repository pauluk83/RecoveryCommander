# Recovery Commander Toolkit - Master Consolidation Script
# Combines all maintenance, development, and system tools into one comprehensive suite
# Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

param(
    [string]$Category = "menu",
    [string]$Action = "",
    [switch]$WhatIf,
    [switch]$Force
)

# Version tracking
$ScriptVersion = "1.0.0"
$ToolkitName = "Recovery Commander Toolkit"

# Utility functions
function Write-ToolkitHeader {
    param([string]$Title)
    Clear-Host
    Write-Host "=== $ToolkitName ===" -ForegroundColor Cyan
    Write-Host "Version: $ScriptVersion" -ForegroundColor Gray
    Write-Host "=== $Title ===" -ForegroundColor Yellow
    Write-Host ""
}

function Write-Section {
    param([string]$Text)
    Write-Host "`n--- $Text ---" -ForegroundColor Green
}

function Test-AdminPrivileges {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# PROJECT MAINTENANCE FUNCTIONS
function Project-Cleanup {
    param([string]$Root = ".", [bool]$whatIf = $false)
    
    Write-Section "Project Cleanup"
    
    if ($whatIf) {
        Write-Host "WHAT IF: Would clean debug folders, resx files, and build artifacts" -ForegroundColor Yellow
        return
    }
    
    Write-Host "Cleaning Debug folders..."
    $debugDirs = Get-ChildItem -Path $Root -Directory -Recurse -Force | Where-Object {
        $_.Name -ieq "Debug" -and ($_.FullName -match "\\bin\\" -or $_.FullName -match "\\obj\\")
    }
    
    foreach ($dir in $debugDirs) {
        Write-Host "Removing: $($dir.FullName)" -ForegroundColor Red
        Remove-Item $dir.FullName -Recurse -Force
    }
    
    Write-Host "Cleaning RESX files..."
    $resxFiles = Get-ChildItem -Path $Root -Filter "*.resx" -Recurse | Where-Object {
        $_.Name -match "\.designer\.resx$"
    }
    
    foreach ($file in $resxFiles) {
        Write-Host "Removing: $($file.FullName)" -ForegroundColor Red
        Remove-Item $file.FullName -Force
    }
    
    Write-Host "Project cleanup complete!" -ForegroundColor Green
}

function Generate-Manifest {
    param([string]$Root = ".", [bool]$whatIf = $false)
    
    Write-Section "Generate Project Manifest"
    
    if ($whatIf) {
        Write-Host "WHAT IF: Would generate project manifest" -ForegroundColor Yellow
        return
    }
    
    $manifestPath = Join-Path $Root "PROJECT_MANIFEST.md"
    $projects = @()
    
    Write-Host "Scanning for project files..."
    $csprojFiles = Get-ChildItem -Path $Root -Filter "*.csproj" -Recurse
    
    foreach ($csproj in $csprojFiles) {
        $projectInfo = @{
            Name = $csproj.BaseName
            Path = $csproj.FullName.Replace($Root, ".")
            Type = "CSharp"
            LastModified = $csproj.LastWriteTime
            Size = $csproj.Length
        }
        $projects += $projectInfo
    }
    
    $manifest = @"
# Recovery Commander Project Manifest
Generated: $(Get-Date)
Total Projects: $($projects.Count)

## Project Structure
$($projects | ForEach-Object { "- **$($_.Name)**: $($_.Path)" })
"@
    
    $manifest | Out-File -FilePath $manifestPath
    Write-Host "Manifest saved to: $manifestPath" -ForegroundColor Green
}

function Update-Changelog {
    param([string]$Root = ".", [bool]$whatIf = $false)
    
    Write-Section "Update Changelog"
    
    if ($whatIf) {
        Write-Host "WHAT IF: Would update project changelog" -ForegroundColor Yellow
        return
    }
    
    $changelogPath = Join-Path $Root "Resources\changelog.md"
    $backupPath = Join-Path $Root "Resources\changelog_backup_$(Get-Date -Format yyyyMMdd_HHmmss).md"
    
    if (Test-Path $changelogPath) {
        Copy-Item $changelogPath $backupPath
        Write-Host "Backup created: $backupPath" -ForegroundColor Gray
    }
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $changelogContent = @"
# Recovery Commander Changelog
Generated: $timestamp

## Recent Changes
- Automated changelog update via toolkit
- Project maintenance operations performed
"@
    
    $changelogContent | Out-File -FilePath $changelogPath
    Write-Host "Changelog updated: $changelogPath" -ForegroundColor Green
}

# SYSTEM MAINTENANCE FUNCTIONS
function System-Optimization {
    Write-Section "System Optimization"
    
    if (-not (Test-AdminPrivileges)) {
        Write-Host "Warning: Admin privileges required for full optimization" -ForegroundColor Yellow
        Write-Host "Some operations may fail. Continue anyway? (y/N): " -NoNewline
        $response = Read-Host
        if ($response -ne "y") { return }
    }
    
    if ($WhatIf) {
        Write-Host "WHAT IF: Would perform system optimization" -ForegroundColor Yellow
        return
    }
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logpath = "$env:USERPROFILE\Desktop\SystemOptimization_Log.txt"
    $rollbackpath = "$env:USERPROFILE\Desktop\SystemOptimization_Rollback.bat"
    
    Write-Host "Creating optimization log..." -ForegroundColor Gray
    "=== System Optimization Log ===" | Out-File $logpath
    "Timestamp: $timestamp" | Out-File $logpath -Append
    "System: $env:COMPUTERNAME" | Out-File $logpath -Append
    "User: $env:USERNAME" | Out-File $logpath -Append
    "Actions:" | Out-File $logpath -Append
    
    Write-Host "Rebuilding icon and font caches..."
    try {
        taskkill /f /im explorer.exe 2>$null
        Start-Sleep -Seconds 2
        Remove-Item "$env:LOCALAPPDATA\IconCache.db" -Force 2>$null
        Remove-Item "$env:LOCALAPPDATA\Microsoft\Windows\FontCache\*" -Force -Recurse 2>$null
        Start-Process explorer.exe
        Write-Host "✔ Icon and font caches rebuilt" -ForegroundColor Green
        "✔ Icon and font caches rebuilt" | Out-File $logpath -Append
    } catch {
        Write-Host "⚠ Cache rebuild failed: $($_.Exception.Message)" -ForegroundColor Yellow
        "⚠ Cache rebuild failed: $($_.Exception.Message)" | Out-File $logpath -Append
    }
    
    Write-Host "Cleaning Component Store..."
    try {
        dism /online /cleanup-image /startcomponentcleanup /resetbase
        Write-Host "✔ Component Store cleaned" -ForegroundColor Green
        "✔ Component Store cleaned" | Out-File $logpath -Append
    } catch {
        Write-Host "⚠ Component Store cleanup failed: $($_.Exception.Message)" -ForegroundColor Yellow
        "⚠ Component Store cleanup failed: $($_.Exception.Message)" | Out-File $logpath -Append
    }
    
    Write-Host "Applying registry performance tweaks..."
    try {
        reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v NtfsDisableLastAccessUpdate /t REG_DWORD /d 1 /f
        reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v Win32PrioritySeparation /t REG_DWORD /d 26 /f
        Write-Host "✔ Registry tweaks applied" -ForegroundColor Green
        "✔ Registry tweaks applied" | Out-File $logpath -Append
    } catch {
        Write-Host "⚠ Registry tweaks failed: $($_.Exception.Message)" -ForegroundColor Yellow
        "⚠ Registry tweaks failed: $($_.Exception.Message)" | Out-File $logpath -Append
    }
    
    Write-Host "Generating rollback script..."
    $rollbackContent = @"
@echo off
:: === System Optimization Rollback Script ===
:: Generated: $timestamp
reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v NtfsDisableLastAccessUpdate /t REG_DWORD /d 0 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v Win32PrioritySeparation /t REG_DWORD /d 18 /f
echo Rollback complete. Please restart your computer.
"@
    
    $rollbackContent | Out-File $rollbackPath
    Write-Host "✔ Rollback script saved to: $rollbackpath" -ForegroundColor Green
    "✔ Rollback script saved to: $rollbackpath" | Out-File $logpath -Append
    
    Write-Host "System optimization complete. Log saved to: $logpath" -ForegroundColor Green
}

function Winget-Backup {
    param([string]$OutputDir = "")
    
    Write-Section "Winget Backup/Restore"
    
    if ($WhatIf) {
        Write-Host "WHAT IF: Would backup winget packages" -ForegroundColor Yellow
        return
    }
    
    function Find-Winget {
        $cmd = Get-Command winget -ErrorAction SilentlyContinue
        if ($cmd) { return $cmd.Source }
        $localApp = [Environment]::GetFolderPath(LocalApplicationData)
        $candidate = Join-Path $localApp Microsoft\WindowsApps\winget.exe
        if (Test-Path $candidate) { return $candidate }
        return $null
    }
    
    $winget = Find-Winget
    if (-not $winget) {
        Write-Error "winget not found. Please install App Installer / winget before using this script."
        return
    }
    
    if (-not $OutputDir) {
        $OutputDir = Join-Path $env:USERPROFILE "Desktop\WingetBackup_$(Get-Date -Format yyyyMMdd_HHmmss)"
    }
    
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "Backup directory: $OutputDir" -ForegroundColor Gray
    
    $manifestPath = Join-Path $OutputDir "winget-manifest.json"
    $listPath = Join-Path $OutputDir "winget-list.txt"
    $restoreScript = Join-Path $OutputDir "restore-packages.ps1"
    
    Write-Host "Exporting installed packages..."
    & $winget export --id $manifestPath
    
    Write-Host "Creating readable package list..."
    & $winget list > $listPath
    
    Write-Host "Creating restore script..."
    $restoreContent = @"
# Winget Restore Script
# Generated: $(Get-Date)
# Usage: .\restore-packages.ps1

Write-Host "Restoring packages from manifest..."
winget import --id winget-manifest.json

Write-Host "Package restore complete!"
"@
    
    $restoreContent | Out-File -FilePath $restoreScript -Encoding UTF8
    Write-Host "Winget backup complete!" -ForegroundColor Green
    Write-Host "Files created in: $OutputDir" -ForegroundColor Gray
}

# DEVELOPMENT TOOLS FUNCTIONS
function Validate-Modules {
    Write-Section "Module Validation"
    
    $rootPath = "."
    $modulePath = Join-Path $rootPath "Module"
    
    if (-not (Test-Path $modulePath)) {
        Write-Host "Module directory not found: $modulePath" -ForegroundColor Red
        return
    }
    
    Write-Host "Validating module structure..."
    $modules = Get-ChildItem -Path $modulePath -Directory
    
    foreach ($module in $modules) {
        Write-Host "Checking module: $($module.Name)" -ForegroundColor Gray
        
        $csprojFile = Join-Path $module.FullName "$($module.Name).csproj"
        $csFile = Join-Path $module.FullName "$($module.Name).cs"
        
        $issues = @()
        
        if (-not (Test-Path $csprojFile)) {
            $issues += "Missing .csproj file"
        }
        
        if (-not (Test-Path $csFile)) {
            $issues += "Missing main .cs file"
        }
        
        if ($issues.Count -gt 0) {
            Write-Host "  ❌ Issues found:" -ForegroundColor Red
            $issues | ForEach-Object { Write-Host "    - $_" -ForegroundColor Red }
        } else {
            Write-Host "  ✅ Module structure valid" -ForegroundColor Green
        }
    }
}

function Fix-NamespaceMismatch {
    Write-Section "Namespace Mismatch Fix"
    
    if ($WhatIf) {
        Write-Host "WHAT IF: Would fix namespace mismatches" -ForegroundColor Yellow
        return
    }
    
    $rootPath = "."
    $csFiles = Get-ChildItem -Path $rootPath -Filter "*.cs" -Recurse
    
    Write-Host "Scanning for namespace mismatches..."
    
    foreach ($file in $csFiles) {
        $content = Get-Content $file.FullName -Raw
        if ($content -match 'namespace\s+([^;{]+)') {
            $namespace = $matches[1].Trim()
            $expectedNamespace = "RecoveryCommander"
            
            if ($namespace -ne $expectedNamespace -and $namespace -notlike "RecoveryCommander.*") {
                Write-Host "Fixing namespace in: $($file.FullName)" -ForegroundColor Yellow
                $newContent = $content -replace "namespace\s+([^;{]+)", "namespace $expectedNamespace"
                $newContent | Out-File -FilePath $file.FullName -Encoding UTF8
                Write-Host "  Updated to: $expectedNamespace" -ForegroundColor Green
            }
        }
    }
    
    Write-Host "Namespace fix complete!" -ForegroundColor Green
}

function Clean-DebugFolders {
    param([string]$Root = ".")
    
    Write-Section "Clean Debug Folders"
    
    if ($WhatIf) {
        Write-Host "WHAT IF: Would clean all debug and build folders" -ForegroundColor Yellow
        return
    }
    
    $foldersToClean = @("bin", "obj", "Debug", "Release")
    $totalCleaned = 0
    
    foreach ($folder in $foldersToClean) {
        $folders = Get-ChildItem -Path $Root -Directory -Recurse -Force | Where-Object { $_.Name -ieq $folder }
        
        foreach ($dir in $folders) {
            Write-Host "Removing: $($dir.FullName)" -ForegroundColor Red
            try {
                Remove-Item $dir.FullName -Recurse -Force
                $totalCleaned++
            } catch {
                Write-Host "  Failed to remove: $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
    }
    
    Write-Host "Cleaned $totalCleaned folders" -ForegroundColor Green
}

# MENU SYSTEMS
function Show-MainMenu {
    Write-ToolkitHeader "Main Menu"
    Write-Host "1. Project Maintenance"
    Write-Host "2. System Maintenance"
    Write-Host "3. Development Tools"
    Write-Host "4. Quick Actions"
    Write-Host "5. Exit"
    Write-Host ""
    Write-Host "Select a category: " -NoNewline
}

function Show-ProjectMenu {
    Write-ToolkitHeader "Project Maintenance"
    Write-Host "1. Project Cleanup (remove bin/obj, resx)"
    Write-Host "2. Generate Project Manifest"
    Write-Host "3. Update Changelog"
    Write-Host "4. Back to Main Menu"
    Write-Host ""
    Write-Host "Select an option: " -NoNewline
}

function Show-SystemMenu {
    Write-ToolkitHeader "System Maintenance"
    Write-Host "1. System Optimization (cache, registry, DISM)"
    Write-Host "2. Winget Backup/Restore"
    Write-Host "3. Back to Main Menu"
    Write-Host ""
    Write-Host "Select an option: " -NoNewline
}

function Show-DevToolsMenu {
    Write-ToolkitHeader "Development Tools"
    Write-Host "1. Validate Module Structure"
    Write-Host "2. Fix Namespace Mismatches"
    Write-Host "3. Clean Debug Folders"
    Write-Host "4. Back to Main Menu"
    Write-Host ""
    Write-Host "Select an option: " -NoNewline
}

function Show-QuickActionsMenu {
    Write-ToolkitHeader "Quick Actions"
    Write-Host "1. Full Project Cleanup + Manifest"
    Write-Host "2. System Optimization + Backup"
    Write-Host "3. Complete Development Reset"
    Write-Host "4. Back to Main Menu"
    Write-Host ""
    Write-Host "Select an option: " -NoNewline
}

# MAIN EXECUTION LOGIC
function Invoke-QuickAction {
    param([string]$Action)
    
    switch ($Action) {
        "1" {
            Write-Host "Executing: Full Project Cleanup + Manifest" -ForegroundColor Cyan
            Project-Cleanup -Root "." -whatIf:$WhatIf
            Generate-Manifest -Root "." -whatIf:$WhatIf
        }
        "2" {
            Write-Host "Executing: System Optimization + Backup" -ForegroundColor Cyan
            System-Optimization
            Winget-Backup
        }
        "3" {
            Write-Host "Executing: Complete Development Reset" -ForegroundColor Cyan
            Clean-DebugFolders -Root "." -whatIf:$WhatIf
            Fix-NamespaceMismatch -whatIf:$WhatIf
            Validate-Modules
            Project-Cleanup -Root "." -whatIf:$WhatIf
            Generate-Manifest -Root "." -whatIf:$WhatIf
        }
    }
}

# Main program loop
if ($Category -eq "menu") {
    do {
        Show-MainMenu
        $category = Read-Host
        
        switch ($category) {
            "1" {
                do {
                    Show-ProjectMenu
                    $choice = Read-Host
                    
                    switch ($choice) {
                        "1" { Project-Cleanup -Root "." -whatIf:$WhatIf }
                        "2" { Generate-Manifest -Root "." -whatIf:$WhatIf }
                        "3" { Update-Changelog -Root "." -whatIf:$WhatIf }
                        "4" { break }
                        default { Write-Host "Invalid option. Please try again." -ForegroundColor Red }
                    }
                    
                    if ($choice -ne "4") {
                        Write-Host "`nPress Enter to continue..." -NoNewline
                        Read-Host
                    }
                } while ($choice -ne "4")
            }
            "2" {
                do {
                    Show-SystemMenu
                    $choice = Read-Host
                    
                    switch ($choice) {
                        "1" { System-Optimization }
                        "2" { Winget-Backup }
                        "3" { break }
                        default { Write-Host "Invalid option. Please try again." -ForegroundColor Red }
                    }
                    
                    if ($choice -ne "3") {
                        Write-Host "`nPress Enter to continue..." -NoNewline
                        Read-Host
                    }
                } while ($choice -ne "3")
            }
            "3" {
                do {
                    Show-DevToolsMenu
                    $choice = Read-Host
                    
                    switch ($choice) {
                        "1" { Validate-Modules }
                        "2" { Fix-NamespaceMismatch }
                        "3" { Clean-DebugFolders -Root "." }
                        "4" { break }
                        default { Write-Host "Invalid option. Please try again." -ForegroundColor Red }
                    }
                    
                    if ($choice -ne "4") {
                        Write-Host "`nPress Enter to continue..." -NoNewline
                        Read-Host
                    }
                } while ($choice -ne "4")
            }
            "4" {
                do {
                    Show-QuickActionsMenu
                    $choice = Read-Host
                    
                    switch ($choice) {
                        "1" { Invoke-QuickAction -Action "1" }
                        "2" { Invoke-QuickAction -Action "2" }
                        "3" { Invoke-QuickAction -Action "3" }
                        "4" { break }
                        default { Write-Host "Invalid option. Please try again." -ForegroundColor Red }
                    }
                    
                    if ($choice -ne "4") {
                        Write-Host "`nPress Enter to continue..." -NoNewline
                        Read-Host
                    }
                } while ($choice -ne "4")
            }
            "5" { break }
            default { Write-Host "Invalid option. Please try again." -ForegroundColor Red }
        }
    } while ($category -ne "5")
    
    Write-Host "`n$ToolkitName closed. Thank you!" -ForegroundColor Green
} else {
    # Direct action execution
    switch ($Category.ToLower()) {
        "project" {
            switch ($Action.ToLower()) {
                "cleanup" { Project-Cleanup -Root "." -whatIf:$WhatIf }
                "manifest" { Generate-Manifest -Root "." -whatIf:$WhatIf }
                "changelog" { Update-Changelog -Root "." -whatIf:$WhatIf }
                default { Write-Host "Invalid project action. Use: cleanup, manifest, changelog" -ForegroundColor Red }
            }
        }
        "system" {
            switch ($Action.ToLower()) {
                "optimize" { System-Optimization }
                "backup" { Winget-Backup }
                default { Write-Host "Invalid system action. Use: optimize, backup" -ForegroundColor Red }
            }
        }
        "dev" {
            switch ($Action.ToLower()) {
                "validate" { Validate-Modules }
                "namespace" { Fix-NamespaceMismatch }
                "clean" { Clean-DebugFolders -Root "." }
                default { Write-Host "Invalid dev action. Use: validate, namespace, clean" -ForegroundColor Red }
            }
        }
        default {
            Write-Host "Usage: .\RecoveryCommander-Toolkit.ps1 [Category] [Action] [-WhatIf]" -ForegroundColor Yellow
            Write-Host "Categories: project, system, dev" -ForegroundColor Gray
            Write-Host "Run without parameters for interactive menu." -ForegroundColor Gray
        }
    }
}
