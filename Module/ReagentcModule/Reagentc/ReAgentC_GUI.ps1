# ReAgentC Manager - Native Windows PowerShell GUI
# Zero external dependencies. Runs on any Windows 10/11 installation.

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$reagentcExe = "$env:SystemRoot\System32\reagentc.exe"
if (-not (Test-Path $reagentcExe)) { $reagentcExe = "reagentc.exe" }

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

# Color palette
$bgDark     = [System.Drawing.Color]::FromArgb(15, 23, 42)
$bgCard     = [System.Drawing.Color]::FromArgb(30, 41, 59)
$bgInput    = [System.Drawing.Color]::FromArgb(15, 23, 42)
$textLight  = [System.Drawing.Color]::FromArgb(241, 245, 249)
$textMuted  = [System.Drawing.Color]::FromArgb(148, 163, 184)
$accent     = [System.Drawing.Color]::FromArgb(99, 102, 241)
$success    = [System.Drawing.Color]::FromArgb(16, 185, 129)
$warning    = [System.Drawing.Color]::FromArgb(245, 158, 11)
$danger     = [System.Drawing.Color]::FromArgb(244, 63, 94)
$cyan       = [System.Drawing.Color]::FromArgb(56, 189, 248)

$form = New-Object System.Windows.Forms.Form
$form.Text = "ReAgentC Manager — Windows RE Control Suite"
$form.Size = New-Object System.Drawing.Size(1100, 740)
$form.MinimumSize = New-Object System.Drawing.Size(900, 650)
$form.StartPosition = "CenterScreen"
$form.BackColor = $bgDark
$form.ForeColor = $textLight
$form.Font = New-Object System.Drawing.Font("Segoe UI", 9.5)
$form.KeyPreview = $true

# --- Header ---
$header = New-Object System.Windows.Forms.Panel
$header.Dock = "Top"
$header.Height = 82
$header.BackColor = $bgCard
$header.Padding = New-Object System.Windows.Forms.Padding(16, 12, 16, 12)

$lblTitle = New-Object System.Windows.Forms.Label
$lblTitle.Text = "ReAgentC Control Center"
$lblTitle.Font = New-Object System.Drawing.Font("Segoe UI", 15, [System.Drawing.FontStyle]::Bold)
$lblTitle.ForeColor = $textLight
$lblTitle.AutoSize = $true
$lblTitle.Location = New-Object System.Drawing.Point(16, 12)

$lblSubtitle = New-Object System.Windows.Forms.Label
$lblSubtitle.Text = "Simple controls for WinRE status, recovery setup, and boot options"
$lblSubtitle.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$lblSubtitle.ForeColor = $textMuted
$lblSubtitle.AutoSize = $true
$lblSubtitle.Location = New-Object System.Drawing.Point(17, 40)

$badgeAdmin = New-Object System.Windows.Forms.Label
$badgeAdmin.Size = New-Object System.Drawing.Size(260, 34)
$badgeAdmin.TextAlign = "MiddleCenter"
$badgeAdmin.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$badgeAdmin.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Right
$badgeAdmin.Location = New-Object System.Drawing.Point(780, 18)

if ($isAdmin) {
    $badgeAdmin.Text = "✓ Administrator Access"
    $badgeAdmin.BackColor = $success
    $badgeAdmin.ForeColor = [System.Drawing.Color]::White
} else {
    $badgeAdmin.Text = "⚠ Administrator Required"
    $badgeAdmin.BackColor = $warning
    $badgeAdmin.ForeColor = $bgDark
}

$btnElevate = New-Object System.Windows.Forms.Button
$btnElevate.Text = "Run as Admin"
$btnElevate.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$btnElevate.Size = New-Object System.Drawing.Size(122, 34)
$btnElevate.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Right
$btnElevate.Location = New-Object System.Drawing.Point(1048, 18)
$btnElevate.BackColor = $accent
$btnElevate.ForeColor = [System.Drawing.Color]::White
$btnElevate.FlatStyle = "Flat"
$btnElevate.FlatAppearance.BorderSize = 0
$btnElevate.Visible = (-not $isAdmin)

$header.Controls.AddRange(@($lblTitle, $lblSubtitle, $badgeAdmin, $btnElevate))

# --- Dashboard ---
$grpDash = New-Object System.Windows.Forms.Panel
$grpDash.Location = New-Object System.Drawing.Point(16, 96)
$grpDash.Size = New-Object System.Drawing.Size(1050, 116)
$grpDash.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Right
$grpDash.BackColor = [System.Drawing.Color]::FromArgb(24, 33, 54)
$grpDash.BorderStyle = "FixedSingle"
$grpDash.Padding = New-Object System.Windows.Forms.Padding(16)

$lblDashTitle = New-Object System.Windows.Forms.Label
$lblDashTitle.Text = "System Overview"
$lblDashTitle.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$lblDashTitle.ForeColor = $accent
$lblDashTitle.AutoSize = $true
$lblDashTitle.Location = New-Object System.Drawing.Point(16, 12)

$lblDashHint = New-Object System.Windows.Forms.Label
$lblDashHint.Text = "Check the current WinRE status and the recovery image details at a glance."
$lblDashHint.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)
$lblDashHint.ForeColor = $textMuted
$lblDashHint.AutoSize = $true
$lblDashHint.Location = New-Object System.Drawing.Point(16, 34)

$lblState = New-Object System.Windows.Forms.Label
$lblState.Text = "STATE: Loading..."
$lblState.Location = New-Object System.Drawing.Point(16, 62)
$lblState.AutoSize = $true
$lblState.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)

$lblLoc = New-Object System.Windows.Forms.Label
$lblLoc.Text = "LOCATION: --"
$lblLoc.Location = New-Object System.Drawing.Point(240, 62)
$lblLoc.AutoSize = $true
$lblLoc.ForeColor = [System.Drawing.Color]::FromArgb(203, 213, 225)

$lblBcd = New-Object System.Windows.Forms.Label
$lblBcd.Text = "BCD ID: --"
$lblBcd.Location = New-Object System.Drawing.Point(560, 62)
$lblBcd.AutoSize = $true
$lblBcd.ForeColor = [System.Drawing.Color]::FromArgb(203, 213, 225)

$lblCustom = New-Object System.Windows.Forms.Label
$lblCustom.Text = "CUSTOM IMAGE: --"
$lblCustom.Location = New-Object System.Drawing.Point(16, 86)
$lblCustom.AutoSize = $true
$lblCustom.ForeColor = [System.Drawing.Color]::FromArgb(203, 213, 225)

$btnRefresh = New-Object System.Windows.Forms.Button
$btnRefresh.Text = "Refresh"
$btnRefresh.Size = New-Object System.Drawing.Size(92, 32)
$btnRefresh.Location = New-Object System.Drawing.Point(940, 58)
$btnRefresh.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Right
$btnRefresh.BackColor = [System.Drawing.Color]::FromArgb(51, 65, 85)
$btnRefresh.ForeColor = $textLight
$btnRefresh.FlatStyle = "Flat"
$btnRefresh.FlatAppearance.BorderSize = 0

$grpDash.Controls.AddRange(@($lblDashTitle, $lblDashHint, $lblState, $lblLoc, $lblBcd, $lblCustom, $btnRefresh))

# --- Quick Actions ---
$quickPanel = New-Object System.Windows.Forms.Panel
$quickPanel.Location = New-Object System.Drawing.Point(16, 224)
$quickPanel.Size = New-Object System.Drawing.Size(1050, 56)
$quickPanel.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Right
$quickPanel.BackColor = [System.Drawing.Color]::Transparent

$lblQuick = New-Object System.Windows.Forms.Label
$lblQuick.Text = "Quick start"
$lblQuick.Font = New-Object System.Drawing.Font("Segoe UI", 8.5, [System.Drawing.FontStyle]::Bold)
$lblQuick.ForeColor = [System.Drawing.Color]::FromArgb(100, 116, 139)
$lblQuick.AutoSize = $true
$lblQuick.Location = New-Object System.Drawing.Point(0, 18)

$quickPanel.Controls.Add($lblQuick)

$quickActions = @(
    @{ Text = "Inspect"; Tab = 0; Color = [System.Drawing.Color]::FromArgb(71, 85, 105) },
    @{ Text = "Enable"; Tab = 1; Color = $accent },
    @{ Text = "Disable"; Tab = 2; Color = [System.Drawing.Color]::FromArgb(190, 18, 60) },
    @{ Text = "Boot to RE"; Tab = 3; Color = [System.Drawing.Color]::FromArgb(6, 182, 212) }
)

$quickButtons = @()
for ($i = 0; $i -lt $quickActions.Count; $i++) {
    $qb = New-Object System.Windows.Forms.Button
    $qb.Text = $quickActions[$i].Text
    $qb.Size = New-Object System.Drawing.Size(132, 36)
    $xPos = 110 + ([int]$i * 138)
    $qb.Location = New-Object System.Drawing.Point($xPos, 10)
    $qb.BackColor = $quickActions[$i].Color
    $qb.ForeColor = [System.Drawing.Color]::White
    $qb.FlatStyle = "Flat"
    $qb.FlatAppearance.BorderSize = 0
    $qb.Font = New-Object System.Drawing.Font("Segoe UI", 8.8, [System.Drawing.FontStyle]::Bold)
    $qb.Tag = $quickActions[$i].Tab
    $quickPanel.Controls.Add($qb)
    $quickButtons += $qb
}

# --- Split layout ---
$split = New-Object System.Windows.Forms.SplitContainer
$split.Location = New-Object System.Drawing.Point(16, 286)
$split.Size = New-Object System.Drawing.Size(1050, 418)
$split.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Bottom -bor [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Right
$split.SplitterDistance = 560
$split.BackColor = $bgDark

# Left: Tabs + Form
$tabs = New-Object System.Windows.Forms.TabControl
$tabs.Dock = "Top"
$tabs.Height = 28
$tabs.Font = New-Object System.Drawing.Font("Segoe UI", 8.5)

$tabNames = @("Info", "Enable", "Disable", "Boot to RE", "Set RE Image", "Set OS Image", "Boot Rank", "Migrate", "Custom")
foreach ($t in $tabNames) {
    $tabs.TabPages.Add((New-Object System.Windows.Forms.TabPage($t)))
}

$lblTabDesc = New-Object System.Windows.Forms.Label
$lblTabDesc.Dock = "Top"
$lblTabDesc.Height = 40
$lblTabDesc.Padding = New-Object System.Windows.Forms.Padding(12, 10, 12, 0)
$lblTabDesc.ForeColor = $textMuted
$lblTabDesc.BackColor = $bgCard
$lblTabDesc.Text = "Choose a task and review the command preview before you run it."

$tabDescriptions = @(
    "Displays Windows RE status and configuration parameters.",
    "Enables Windows Recovery Environment and updates boot configuration.",
    "Disables Windows Recovery Environment and unmounts recovery image.",
    "Configures system to boot into WinRE on next restart.",
    "Points WinRE to a custom Winre.wim boot image.",
    "Configures OS recovery image for push-button reset.",
    "Sets boot priority rank for Windows RE.",
    "Migrates WinRE configuration to a new target folder.",
    "Execute arbitrary reagentc switches directly."
)

$formPanel = New-Object System.Windows.Forms.Panel
$formPanel.Dock = "Fill"
$formPanel.BackColor = $bgCard
$formPanel.Padding = New-Object System.Windows.Forms.Padding(16)
$formPanel.AutoScroll = $true

$lblGuide = New-Object System.Windows.Forms.Label
$lblGuide.Text = "Pick a task, fill any optional values, and run the command when you are ready."
$lblGuide.Location = New-Object System.Drawing.Point(12, 12)
$lblGuide.AutoSize = $true
$lblGuide.ForeColor = [System.Drawing.Color]::FromArgb(203, 213, 225)
$lblGuide.Font = New-Object System.Drawing.Font("Segoe UI", 9)

$lblTarget = New-Object System.Windows.Forms.Label
$lblTarget.Text = "Target OS Path (/target):"
$lblTarget.Location = New-Object System.Drawing.Point(12, 50)
$lblTarget.AutoSize = $true
$lblTarget.ForeColor = $textMuted

$txtTarget = New-Object System.Windows.Forms.TextBox
$txtTarget.Location = New-Object System.Drawing.Point(12, 72)
$txtTarget.Width = 460
$txtTarget.BackColor = $bgInput
$txtTarget.ForeColor = $textLight
$txtTarget.BorderStyle = "FixedSingle"
$txtTarget.Font = New-Object System.Drawing.Font("Segoe UI", 9)

$lblPath = New-Object System.Windows.Forms.Label
$lblPath.Text = "Image Path (/path):"
$lblPath.Location = New-Object System.Drawing.Point(12, 112)
$lblPath.AutoSize = $true
$lblPath.ForeColor = $textMuted
$lblPath.Visible = $false

$txtPath = New-Object System.Windows.Forms.TextBox
$txtPath.Location = New-Object System.Drawing.Point(12, 134)
$txtPath.Width = 460
$txtPath.BackColor = $bgInput
$txtPath.ForeColor = $textLight
$txtPath.BorderStyle = "FixedSingle"
$txtPath.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$txtPath.Visible = $false

$lblCustomArgs = New-Object System.Windows.Forms.Label
$lblCustomArgs.Text = "Raw Arguments:"
$lblCustomArgs.Location = New-Object System.Drawing.Point(12, 50)
$lblCustomArgs.AutoSize = $true
$lblCustomArgs.ForeColor = $textMuted
$lblCustomArgs.Visible = $false

$txtCustomArgs = New-Object System.Windows.Forms.TextBox
$txtCustomArgs.Location = New-Object System.Drawing.Point(12, 72)
$txtCustomArgs.Width = 460
$txtCustomArgs.BackColor = $bgInput
$txtCustomArgs.ForeColor = $textLight
$txtCustomArgs.BorderStyle = "FixedSingle"
$txtCustomArgs.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$txtCustomArgs.Visible = $false

$formPanel.Controls.AddRange(@($lblGuide, $lblTarget, $txtTarget, $lblPath, $txtPath, $lblCustomArgs, $txtCustomArgs))

$previewPanel = New-Object System.Windows.Forms.Panel
$previewPanel.Dock = "Bottom"
$previewPanel.Height = 108
$previewPanel.BackColor = [System.Drawing.Color]::FromArgb(12, 18, 31)
$previewPanel.Padding = New-Object System.Windows.Forms.Padding(12)

$lblPreviewTitle = New-Object System.Windows.Forms.Label
$lblPreviewTitle.Text = "Command preview"
$lblPreviewTitle.Font = New-Object System.Drawing.Font("Segoe UI", 8.5, [System.Drawing.FontStyle]::Bold)
$lblPreviewTitle.ForeColor = $textMuted
$lblPreviewTitle.Location = New-Object System.Drawing.Point(12, 10)
$lblPreviewTitle.AutoSize = $true

$lblPreview = New-Object System.Windows.Forms.Label
$lblPreview.Text = "reagentc.exe /info"
$lblPreview.Font = New-Object System.Drawing.Font("Consolas", 10, [System.Drawing.FontStyle]::Bold)
$lblPreview.ForeColor = $cyan
$lblPreview.Location = New-Object System.Drawing.Point(12, 34)
$lblPreview.AutoSize = $true

$btnCopyCmd = New-Object System.Windows.Forms.Button
$btnCopyCmd.Text = "Copy"
$btnCopyCmd.Size = New-Object System.Drawing.Size(60, 30)
$btnCopyCmd.Location = New-Object System.Drawing.Point(460, 26)
$btnCopyCmd.BackColor = [System.Drawing.Color]::FromArgb(51, 65, 85)
$btnCopyCmd.ForeColor = $textLight
$btnCopyCmd.FlatStyle = "Flat"
$btnCopyCmd.FlatAppearance.BorderSize = 0

$btnRun = New-Object System.Windows.Forms.Button
$btnRun.Text = "Run Command"
$btnRun.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$btnRun.Size = New-Object System.Drawing.Size(220, 38)
$btnRun.Location = New-Object System.Drawing.Point(12, 60)
$btnRun.BackColor = $accent
$btnRun.ForeColor = [System.Drawing.Color]::White
$btnRun.FlatStyle = "Flat"
$btnRun.FlatAppearance.BorderSize = 0

$previewPanel.Controls.AddRange(@($lblPreviewTitle, $lblPreview, $btnCopyCmd, $btnRun))

$split.Panel1.Controls.AddRange(@($formPanel, $previewPanel, $lblTabDesc, $tabs))

# Right: Console
$consoleHeader = New-Object System.Windows.Forms.Panel
$consoleHeader.Dock = "Top"
$consoleHeader.Height = 34
$consoleHeader.BackColor = [System.Drawing.Color]::FromArgb(17, 23, 38)

$lblConsoleTitle = New-Object System.Windows.Forms.Label
$lblConsoleTitle.Text = "CONSOLE OUTPUT"
$lblConsoleTitle.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$lblConsoleTitle.ForeColor = $textMuted
$lblConsoleTitle.Location = New-Object System.Drawing.Point(10, 8)
$lblConsoleTitle.AutoSize = $true

$lblExitCode = New-Object System.Windows.Forms.Label
$lblExitCode.Text = "Exit Code: --"
$lblExitCode.Font = New-Object System.Drawing.Font("Segoe UI", 8.5, [System.Drawing.FontStyle]::Bold)
$lblExitCode.ForeColor = [System.Drawing.Color]::FromArgb(203, 213, 225)
$lblExitCode.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Right
$lblExitCode.Location = New-Object System.Drawing.Point(320, 8)
$lblExitCode.AutoSize = $true

$btnCopyLog = New-Object System.Windows.Forms.Button
$btnCopyLog.Text = "Copy"
$btnCopyLog.Size = New-Object System.Drawing.Size(50, 24)
$btnCopyLog.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Right
$btnCopyLog.Location = New-Object System.Drawing.Point(430, 4)
$btnCopyLog.BackColor = [System.Drawing.Color]::FromArgb(51, 65, 85)
$btnCopyLog.ForeColor = $textLight
$btnCopyLog.FlatStyle = "Flat"
$btnCopyLog.FlatAppearance.BorderSize = 0

$btnClearLog = New-Object System.Windows.Forms.Button
$btnClearLog.Text = "Clear"
$btnClearLog.Size = New-Object System.Drawing.Size(50, 24)
$btnClearLog.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Right
$btnClearLog.Location = New-Object System.Drawing.Point(485, 4)
$btnClearLog.BackColor = [System.Drawing.Color]::FromArgb(51, 65, 85)
$btnClearLog.ForeColor = $textLight
$btnClearLog.FlatStyle = "Flat"
$btnClearLog.FlatAppearance.BorderSize = 0

$consoleHeader.Controls.AddRange(@($lblConsoleTitle, $lblExitCode, $btnCopyLog, $btnClearLog))

$txtConsole = New-Object System.Windows.Forms.RichTextBox
$txtConsole.Dock = "Fill"
$txtConsole.BackColor = [System.Drawing.Color]::FromArgb(9, 13, 22)
$txtConsole.ForeColor = [System.Drawing.Color]::FromArgb(203, 213, 225)
$txtConsole.Font = New-Object System.Drawing.Font("Consolas", 9.5)
$txtConsole.ReadOnly = $true
$txtConsole.BorderStyle = "None"

$split.Panel2.Controls.AddRange(@($txtConsole, $consoleHeader))

# --- Helpers ---
function Log-Console($text, $color) {
    $txtConsole.SelectionStart = $txtConsole.TextLength
    $txtConsole.SelectionLength = 0
    $txtConsole.SelectionColor = $color
    $txtConsole.AppendText("$text`n")
    $txtConsole.ScrollToCaret()
}

function Update-Preview {
    $idx = $tabs.SelectedIndex
    $target = $txtTarget.Text.Trim()
    $targetArg = if ($target) { " /target `"$target`"" } else { "" }

    $cmd = switch ($idx) {
        0 { "/info$targetArg" }
        1 { "/enable$targetArg" }
        2 { "/disable" }
        3 { "/boottore" }
        4 {
            $p = if ($txtPath.Text.Trim()) { " /path `"$($txtPath.Text.Trim())`"" } else { "" }
            "/setreimage$p /index 1$targetArg"
        }
        5 {
            $p = if ($txtPath.Text.Trim()) { " /path `"$($txtPath.Text.Trim())`"" } else { "" }
            "/setosimage$p /index 1$targetArg"
        }
        6 { "/setbootrank /bootrank 1$targetArg" }
        7 {
            $p = if ($txtPath.Text.Trim()) { " /path `"$($txtPath.Text.Trim())`"" } else { "" }
            "/migrateto$p$targetArg"
        }
        8 { if ($txtCustomArgs.Text.Trim()) { $txtCustomArgs.Text.Trim() } else { "/info" } }
        default { "/info" }
    }

    $lblPreview.Text = "reagentc.exe $cmd"
}

function Update-TabFields {
    $idx = $tabs.SelectedIndex
    if ($idx -ge 0 -and $idx -lt $tabDescriptions.Count) {
        $lblTabDesc.Text = $tabDescriptions[$idx]
    }

    $lblTarget.Visible = ($idx -ne 8)
    $txtTarget.Visible = ($idx -ne 8)
    $lblPath.Visible = ($idx -in 4, 5, 7)
    $txtPath.Visible = ($idx -in 4, 5, 7)
    $lblCustomArgs.Visible = ($idx -eq 8)
    $txtCustomArgs.Visible = ($idx -eq 8)

    Update-Preview
}

function Refresh-Status {
    if (-not $isAdmin) {
        $lblState.Text = "STATE: Requires Administrator"
        $lblState.ForeColor = $warning
        Log-Console "NOTICE: Run as Administrator to query WinRE status." ([System.Drawing.Color]::Yellow)
        return
    }

    try {
        $res = & $reagentcExe /info 2>&1
        $resStr = ($res | Out-String).Trim()

        if ($resStr -match "Windows RE status:\s*(.+)") {
            $st = $matches[1].Trim()
            $lblState.Text = "STATE: $st"
            $lblState.ForeColor = if ($st -eq "Enabled") { $success } else { $danger }
        }
        if ($resStr -match "Windows RE location:\s*(.+)") {
            $lblLoc.Text = "LOCATION: " + $matches[1].Trim()
        }
        if ($resStr -match "Boot Configuration Data \(BCD\) identifier:\s*(.+)") {
            $lblBcd.Text = "BCD ID: " + $matches[1].Trim()
        }
        if ($resStr -match "Custom image location:\s*(.+)") {
            $lblCustom.Text = "CUSTOM IMAGE: " + $matches[1].Trim()
        }
    } catch {
        Log-Console "Error fetching status: $_" ([System.Drawing.Color]::Red)
    }
}

function Invoke-ReagentcCommand {
    if (-not $isAdmin) {
        $choice = [System.Windows.Forms.MessageBox]::Show(
            "ReAgentC commands require Administrator privileges.`n`nRelaunch as Administrator now?",
            "Elevation Required",
            [System.Windows.Forms.MessageBoxButtons]::YesNo,
            [System.Windows.Forms.MessageBoxIcon]::Warning
        )
        if ($choice -eq [System.Windows.Forms.DialogResult]::Yes) {
            Start-Process pwsh -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
            $form.Close()
        }
        return
    }

    $idx = $tabs.SelectedIndex
    if ($idx -in 2, 3) {
        $confirm = [System.Windows.Forms.MessageBox]::Show(
            "You are about to run a system recovery command:`n`n$($lblPreview.Text)`n`nProceed?",
            "Confirm Operation",
            [System.Windows.Forms.MessageBoxButtons]::YesNo,
            [System.Windows.Forms.MessageBoxIcon]::Warning
        )
        if ($confirm -ne [System.Windows.Forms.DialogResult]::Yes) { return }
    }

    Update-Preview
    $args = ($lblPreview.Text -replace "^reagentc\.exe ", "").Trim()
    Log-Console "`n> $reagentcExe $args" $cyan

    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = $reagentcExe
    $pinfo.Arguments = $args
    $pinfo.RedirectStandardOutput = $true
    $pinfo.RedirectStandardError = $true
    $pinfo.UseShellExecute = $false
    $pinfo.CreateNoWindow = $true

    $p = [System.Diagnostics.Process]::Start($pinfo)
    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
    $p.WaitForExit()

    $lblExitCode.Text = "Exit Code: $($p.ExitCode)"
    $lblExitCode.ForeColor = if ($p.ExitCode -eq 0) { $success } else { $danger }

    if ($stdout) { Log-Console $stdout ([System.Drawing.Color]::White) }
    if ($stderr) { Log-Console $stderr ([System.Drawing.Color]::FromArgb(248, 113, 113)) }

    if ($p.ExitCode -eq 0) {
        Log-Console "✓ Command completed successfully." ([System.Drawing.Color]::FromArgb(74, 222, 128))
        Refresh-Status
    } else {
        Log-Console "✖ Exit code $($p.ExitCode)" ([System.Drawing.Color]::FromArgb(251, 113, 133))
    }
}

# --- Event Handlers ---
$tabs.Add_SelectedIndexChanged({ Update-TabFields })
$txtTarget.Add_TextChanged({ Update-Preview })
$txtPath.Add_TextChanged({ Update-Preview })
$txtCustomArgs.Add_TextChanged({ Update-Preview })
$btnRun.Add_Click({ Invoke-ReagentcCommand })
$btnRefresh.Add_Click({ Refresh-Status })
$btnCopyCmd.Add_Click({ [System.Windows.Forms.Clipboard]::SetText($lblPreview.Text); Log-Console "Command copied." $textMuted })
$btnCopyLog.Add_Click({ if ($txtConsole.Text) { [System.Windows.Forms.Clipboard]::SetText($txtConsole.Text); Log-Console "Log copied." $textMuted } })
$btnClearLog.Add_Click({ $txtConsole.Clear(); $lblExitCode.Text = "Exit Code: --" })
$btnElevate.Add_Click({
    Start-Process pwsh -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    $form.Close()
})

foreach ($qb in $quickButtons) {
    $qb.Add_Click({
        $tabs.SelectedIndex = $this.Tag
        Invoke-ReagentcCommand
    })
}

$form.Add_KeyDown({
    if ($_.Control -and $_.KeyCode -eq [System.Windows.Forms.Keys]::Enter) {
        Invoke-ReagentcCommand
    }
})

$form.Controls.AddRange(@($header, $grpDash, $quickPanel, $split))

Update-TabFields
Refresh-Status
Log-Console "ReAgentC Manager ready. Use Quick Actions or select a command tab." $textMuted

[void]$form.ShowDialog()
