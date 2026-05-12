# RecoveryCommander Changelog

## 2026-05-12 - Static Audit Remediation (Website & CI)

### Website SEO & Structured Data
- **JSON-LD Structured Data [Finding #1]** — Added a `<script type="application/ld+json">` block in `<head>` with a `SoftwareApplication` schema (name, OS, category, version, author, free pricing, license URL). Enables rich results in Google and AI-search crawlers.
- **Canonical Link [Finding #3]** — Added `<link rel="canonical" href="https://pauluk83.github.io/RecoveryCommander/">` to prevent duplicate-content penalties.
- **Favicon Link [Finding #8]** — Added `<link rel="icon" href="favicon.ico" type="image/x-icon">` to `<head>`. A matching `favicon.ico` file must be placed in the `Website/` directory.

### Accessibility (WCAG 2.4.1)
- **Skip-to-Content Link [Finding #5]** — Added `<a href="#main-content" class="skip-link">Skip to main content</a>` as the first child of `<body>`, with `id="main-content"` on `<main>`. Styled in `styles.css` to be off-screen by default, sliding into view on keyboard `:focus`.

### AI-Search & GEO
- **`llms.txt` [Finding #4]** — Created `Website/llms.txt` per the llmstxt.org specification. Contains a Markdown summary of the project for AI crawlers (Perplexity, ChatGPT search, Gemini).

### CI/CD Security
- **HTTPS Timestamp URL [Finding #2]** — Changed the Authenticode signing timestamp server in `.github/workflows/dotnet-desktop.yml` from `http://timestamp.digicert.com` to `https://timestamp.digicert.com`, eliminating the unencrypted supply-chain vector during code signing.

### CI/CD & Code Quality
- **Test Integrity Restoration** — Fixed 6 failing unit tests in `SecurityHelpersTests.cs` that were incorrectly failing after the architectural shift to network-layer SSRF protection.
- **Defense-in-Depth SSRF Validation** — Restored static loopback and private IP validation in `SecurityHelpers.IsValidDownloadUrl` as a fast-fail mechanism complementing the dynamic `SocketsHttpHandler` checks.
- **Code Analysis Remediation (CA1805)** — Eliminated redundant explicit initializations of boolean members to `false` in `WinREWizards.cs` and `SimpleContracts.cs`.
- **API Cross-Language Compatibility (CA1716)** — Renamed the `error` parameter in `IProgressReporter.ReportError` to avoid reserved keyword conflicts, ensuring library compatibility with VB.NET and other consumers.
- **Performance & Modernization (CA1835, CA1848, CA1872)** — Implemented high-performance span-based async I/O in `AsyncHelpers.cs`, optimized logging with `LoggerMessage` delegates in `CoreUtilities.cs` and `GlobalExceptionHandler.cs`, and migrated to `Convert.ToHexStringLower` for efficient hash formatting.
- **Code Quality & Readability (CA2249, CA1822, CA1861)** — Refactored `UpdateService.cs` to use static methods where applicable, migrated `DiskUtility.cs` to `string.Contains` for improved readability, and utilized `static readonly` fields for constant arrays to reduce heap allocations.
- **Globalization & Culture Safety (CA1305, CA1310, CA1311)** — Hardened string operations in `SecurityHelpers.cs`, `DismHelper.cs`, and `CoreUtilities.cs` by explicitly specifying `StringComparison.OrdinalIgnoreCase` and `CultureInfo.InvariantCulture`, preventing locale-dependent behavior.

### Security & Hardening
- **Content-Security-Policy (CSP) [Finding #1]** — Implemented a strict CSP meta tag in `index.html` to mitigate XSS risks. The policy restricts scripts to `'self'`, styles to `'self'` and Google Fonts, and images to `'self'` and trusted GitHub sources, while disabling dangerous features like `object-src`.

## 2026-05-12 - Build Verification & Module Catalog Migration

### Build & Versioning
- **Build 1.2.6 Verified** — Full solution builds cleanly with **0 errors, 0 warnings** across all configurations. All 47 modified and 20 newly added files are verified stable.
- **README Version Sync** — Updated the README header to `v1.2.6` and refreshed the Project Status section with current supply-chain, reliability, quality, accessibility, and maintainability milestones.

### Diagnostics Module Refactoring
- **Record-Based Command Definitions** — Replaced the inline `ModuleAction` list with a strongly-typed `DiagnosticCommand` record array. Diagnostic commands are now defined as compact data (`Name`, `DisplayName`, `Description`, `FileName`, `Arguments`, `StatusMessage`) and dynamically projected into `ModuleAction` instances via `yield return`, eliminating boilerplate.
- **Cancellation Support** — Added explicit `OperationCanceledException` handling to `RunDiagnosticCommand`, ensuring cancelled diagnostics report gracefully instead of surfacing raw exception text.

### Malware Removal Module Updates
- **Full Catalog Migration** — Migrated all 14 malware removal tools from inline `DownloadUrls` constants to the centralized `DownloadCatalog` with SHA-256 hash verification. Tools now route through `DownloadCatalog.DownloadAndExecuteFromCatalogAsync` for consistent supply-chain validation.
- **Emsisoft Emergency Kit** — Added Emsisoft Emergency Kit as a new scanner option in the Malware Removal module, catalogued with pinned SHA-256 hash.
- **Comodo Verified Download** — Upgraded Comodo Cleaning Essentials extraction to use `DownloadCatalog.DownloadVerifiedAsync` for hash-verified ZIP download before extraction.

### Utilities Module Updates
- **Full Catalog Migration** — Migrated all utility tools to `DownloadCatalog.DownloadAndExecuteFromCatalogAsync`. Tools with dynamic release discovery (Rufus, Visual C++ AIO) now emit `[supply-chain] WARN` notices documenting their unpinned status.
- **Ordinal String Comparisons** — Updated `.EndsWith()` and `.StartsWith()` calls in Rufus and VC++ Redist asset discovery to use `StringComparison.OrdinalIgnoreCase`, preventing culture-sensitive matching bugs.
- **IDisposable Compliance** — Added `using` statements to `HttpResponseMessage`, `HttpRequestMessage`, and `JsonDocument` instances across Rufus/VC++ download flows to prevent socket and memory leaks.

### ModuleLoader & Startup
- **Diagnostic Logging** — `Assembly.Load` failures in `ModuleLoader` now emit structured `Debug.WriteLine` messages with the exception type and message instead of silently swallowing errors, aiding single-file deployment debugging.
- **Structured Startup Log** — `Program.cs` startup log now includes the assembly version and rolling log directory path for faster triage.

### Documentation
- **Project Manifest Retired** — Replaced the `Resources/PROJECT_MANIFEST.md` reference in the Help menu with `Resources/ARCHITECTURAL_NOTES.md`, consolidating architectural documentation into a single living document.

### UI/UX Audit Resolutions
The following items were identified in the formal UI/UX Architecture & Design Audit and have been resolved:
- **[UX-01] Dialog Visual Consistency** — Applied `Theme.ApplyMicaEffect` to every secondary dialog (`ModuleActionDialog`, `ModuleBuilder`, `NetworkOptimizer`, `RestorePointManager`, `StartupManager`, and all `DialogFactory` forms). All windows now render with the immersive DWM dark-mode title bar, eliminating the legacy white-border flash.
- **[UX-02] Fluid Operations Grid** — Replaced the fixed-width `FlowLayoutPanel` tiles with a dynamic `Resize` handler that recalculates tile width based on available client area, eliminating jagged right-margins at any resolution (1080p through 4K).
- **[UX-03] Categorical Sidebar Navigation** — Restructured the module list from alphabetical order into logical workflow categories (Core Repair, Optimization, Security, Custom), reducing cognitive load when selecting a module.
- **[UX-04] Micro-Interaction Click Feedback** — Action tiles now immediately morph their icon to a warning-coloured spinner (`⌛`) on click, providing sub-100ms visual confirmation before the background task scheduler engages.
- **[A11y-01] Keyboard Focus Indicators** — `ModernButton` now renders a 2px `Colors.Primary` focus ring when keyboard-focused (via `ShowFocusCues`), using `SystemColors.Highlight` under high-contrast themes for guaranteed visibility.
- **[A11y-02] Screen Reader Accessibility** — Assigned `AccessibleRole` and `AccessibleName` to all custom GDI+ controls: `ModernButton` → `PushButton`, `RoundedPanel` → `Pane`, `RoundedTextBox` → `Text`, `ToastNotification` → `Alert` with descriptive accessible names.
- **[A11y-03] DPI-Scaled Toast Notifications** — Toast dimensions now scale by `DeviceDpi / 96f`, and positioning uses the scaled size to correctly anchor to the top-right corner of the working area on high-DPI displays. Added `Esc` key to dismiss.
- **[A11y-04] High Contrast Theme Support** — All `Theme.Colors` properties now snap to `SystemColors` equivalents (e.g., `SystemColors.Window`, `SystemColors.Highlight`, `SystemColors.WindowText`) when `SystemInformation.HighContrast` is active, maintaining WCAG AA contrast without a separate theme.

## 2026-05-09 - Security Audit, Architecture Hardening & Utilities Updates

### Security & Compliance
- **SSRF Mitigation [S-01]** — Replaced the static blacklist in URL validation with a network-layer `ConnectCallback` via `SocketsHttpHandler`. This effectively mitigates DNS rebinding and IP obfuscation attacks by blocking connections to private and loopback IPs *after* DNS resolution.
- **ZipSlip Protection [S-02]** — Verified and documented the existing extraction boundary checks in `UtilitiesModule.cs`. Extraction paths are strictly validated using `Path.GetFullPath` to prevent malicious archive entries from overwriting system files.
- **Command Injection Prevention [S-04]** — Confirmed that `SanitizeCommandArguments` properly strips single (`'`) and double (`"`) quotes, closing potential string-boundary breakout vectors when arguments are passed to shell contexts.

### Architectural Refactoring
- **God Object Elimination [A-01]** — Refactored the monolithic 2,500+ line `Theme.cs` into a clean, partial-class architecture spanning `Theme.Controls.cs`, `Theme.Responsive.cs`, `Theme.Internal.cs`, and `Theme.Animation.cs`. This significantly improves maintainability and developer cognitive load.
- **Service Provider Rebuild [A-02]** — Completely removed the unused `RegisterModules` method in `ServiceContainer.cs` that incorrectly rebuilt the service provider. Modules are safely loaded on-demand by `MainForm.cs` via `ModuleLoader`.

### UI/UX Enhancements
- **Fluid Grid Layout** — Re-engineered the Operations Grid in `MainForm` to use a fully responsive, fluid tile layout that dynamically calculates width based on the window's dimensions, eliminating jagged margins.
- **Micro-Interaction Feedback** — Implemented an immediate visual state change (spinner) on Action Tiles upon click, providing sub-100ms user feedback before background task scheduling completes.
- **Categorical Navigation Hierarchy** — Restructured the sidebar module loader to group modules into logical categories (Core Repair, Optimization, Security, Custom) instead of an alphabetical list, reducing cognitive load.
- **Global Dialog Visual Consistency** — Enforced DWM Immersive Dark Mode (Mica/Acrylic) title bars across all secondary application dialogs (`ModuleActionDialog`, `ModuleBuilder`, `NetworkOptimizer`, and all dynamic `DialogFactory` forms) matching the `MainForm` aesthetic.

### Performance & Stability
- **UI Thread Contention [P-01]** — Replaced high-frequency synchronous `Refresh()` calls with asynchronous `Invalidate()` calls during progress updates in `MainForm.cs`, preventing UI stuttering during high-intensity disk or network operations.
- **Event Memory Leaks [B-01]** — Implemented the `IDisposable` pattern in `MainForm` to explicitly detach static `Theme.OnThemeChanged` and `Theme.OnThemePreferencesChanged` event handlers upon closure, eliminating long-term memory leak vectors.

### Diagnostics Module Updates
- **UI Simplification** — Removed the "WHAT THIS TOOLKIT CHECKS" info pane from the Diagnostics module. The actions grid now utilizes the full width of the interface for better visibility and information density.

### Utilities Module Updates
- **Secure Storage Catalog Hardening** — Cryptographically pinned SHA-256 hashes for all Secure Storage-hosted utilities (CCleaner, Macrium, PC Repair Suite, etc.) in the `DownloadCatalog.cs`. This transition enforces verified-integrity execution for the entire external utility stack.
- **EaseUS Partition Master 20.3.0** — Updated EaseUS Partition Master to version 20.3.0 (Build 202604081519). Updated the download catalog with the new Secure Storage direct link and pinned the SHA-256 hash for secure verification. Synced the version information across the Utilities module and website.

## 2026-05-06 - Utilities Expansion

### Utilities Module Updates
- **UniGetUI 2026.1.9** — Added UniGetUI (Package Manager UI) to the Utilities module. This tool provides a graphical interface for managing packages via Winget, Scoop, and other managers. The implementation handles ZIP download, extraction to a unique temp directory, and execution as administrator.
- **Enhanced UI Density** — Optimized the action button layout for the Utilities module to display 4 buttons across instead of 3. Buttons were slightly shrunken (285x60) to maintain high information density while preserving readability on standard displays.

## 2026-05-04 - UI Overhaul & Code Quality Pass

### UX & Navigation
- **Menu Cleanup** — Removed the "View" section from the application menu. This section previously contained theme backdrop (Material) selection options, which have been streamlined to simplify the top-level navigation.

### UI Architecture
- **Win11MenuRenderer Extraction** — Extracted `Windows11MenuRenderer`, `Windows11ColorTable`, and `DirectUIProgress<T>` from `MainForm.cs` into a dedicated `UI/Win11MenuRenderer.cs` file. Reduces `MainForm` size and keeps the Win11-styled menu renderer co-located with other UI primitives.
- **Theme Dead Code Removal** — Removed the redundant `Theme.SystemUtilities.ErrorHandler` nested class from `Theme.cs`; it was an exact duplicate of the live `SystemUtilities.ErrorHandler` in `UI/SystemUtilities.cs`. Eliminates the ambiguity and ~40 lines of stale code.

### Reliability & Async Correctness
- **Async Tile Interaction** — Action tile click handlers upgraded from fire-and-forget `_ = ExecuteActionSafelyAsync()` to properly awaited `async Task` lambdas with a defensive `catch` block, preventing unobserved task exceptions from crashing the application on action faults.
- **Progress Bar Rendering** — Removed the `progressBar.Update()` synchronous paint flush that was stalling the UI thread on slow machines. `Invalidate()` alone is sufficient; the 250 ms refresh loop delivers updates without blocking.
- **Debug Noise Removed** — Module button creation logs (`[DEBUG] Building module buttons…`, `[DEBUG] Added button…`) moved from the user-visible `ShowOutput` feed to `System.Diagnostics.Debug.WriteLine`. End users no longer see internal loader diagnostics in the command output window.
- **Dead Sync Wrapper Removed** — Deleted the no-op `ExecuteActionSafely(module, action)` method that only called `_ = ExecuteActionSafelyAsync(...)`. All call sites now use the async path directly.

### Module Builder
- **Action Dialog** — Added `Forms/ModuleActionDialog.cs` as the companion input dialog to `ModuleBuilder`. Provides a themed form for defining action name, description, command, arguments, and the `RunAsAdmin` flag — replacing the previous inline prompt approach.
- **Input Sanitization** — `ModuleBuilder.GenerateCode()` now uses separate `EscapeStringLiteral` and `EscapeVerbatimLiteral` helpers to safely embed user-supplied strings into generated C# code, preventing malformed output when names contain backslashes or quotes.

### SFC Module
- **Exit Code Handling** — Rewrote `SfcHelper.cs` to drive `sfc.exe` directly and read integer exit codes. Previously, non-zero exit codes (like 1, 2, or 3) were being treated as fatal errors by the async runner; they are now correctly mapped to user-facing results (e.g., "Violations repaired", "Reboot required").
- **Output Sanitization** — Added null-byte stripping and space-collapse regex to the SFC output processor to handle corrupted wide-char redirection buffers emitted by `sfc.exe`.

### Build
- Verified solution stability: **0 errors, 0 warnings**.

## 2026-05-02 - Build 1.2.6 - Reliability, Security, Tests, Accessibility

### Core Platform
- **Build 1.2.6** — Updated application version to `1.2.6`.
- **Repo Build Policy** — Added `Directory.Build.props` to centralize analyzer and warning policy defaults.
- **Rolling File Logs** — Added daily rolling logs under `%LOCALAPPDATA%\RecoveryCommander\logs` with retention cleanup.

### Security & Supply Chain
- **Download Catalog** — Added `Core/DownloadCatalog.cs` to centralize third-party download endpoints and metadata.
- **Module Download Migration** — Utilities, Malware Removal, and Driver Manager now resolve downloads through the central catalog.
- **CI Security** — Added CodeQL workflow and Dependabot configuration.
- **Release Hardening** — Updated desktop workflow with optional Authenticode signing, release SHA256SUMS generation, and tighter NuGet push scope.

### Defects & Reliability
- **Dialog Threading Fixes** — Removed cross-thread MessageBox usage in restore-point flows and made help-file loading asynchronous.
- **Driver Manager Fix** — Corrected “List Drivers” to enumerate drivers rather than invoking cleanup path.
- **Cancellation Improvements** — Wired cancellation through Media tools, Cloud recovery flow checks, PS module update scan, and registry tweak operations.
- **Dispose Stability** — MainForm now cancels and briefly awaits in-flight tasks during disposal.

### UI & Accessibility
- **Progress UI** — Replaced broken `EnhancedProgressDialog.ShowAsync` stub with a real async-work modal pattern.
- **A11y Focus & Roles** — Added keyboard focus cues/focus ring + accessibility metadata to custom controls.
- **High Contrast** — Theme color properties now snap to `SystemColors` when Windows High Contrast is enabled.
- **Toast UX** — Scaled toast dimensions for DPI and added `Esc` to dismiss.

### Tests & Documentation
- **Test Project** — Added `Tests/RecoveryCommander.Tests` with xUnit + FluentAssertions + Moq.
- **Coverage Surface** — Added tests for security helpers, download catalog invariants, module action behavior, app path conventions, and async download guardrails.
- **CI Tests** — Desktop workflow now runs `dotnet test` (Release matrix) and uploads test artifacts.
- **Docs Refresh** — Updated README status/version and architectural notes with the v1.2.6 download supply-chain policy.

## 2026-05-02 - Module Builder Reintegration

### Core Utilities
- **Module Builder** — Successfully reintegrated the `ModuleBuilder` tool. This utility allows users to easily create their own recovery modules (`IRecoveryModule`) with custom actions and commands, automatically generating the C# code necessary to compile into a plugin. It is now accessible from the application's "Tools" menu.
- **Async Execution Pipeline** — Modules generated by the Builder are natively hooked into the core `AsyncHelpers.RunProcessAsync` pipeline, ensuring proper process management, UI threading safety, and background task execution.

## 2026-04-30 - Utilities Module Expansion

### Utilities Module Updates
- **EaseUS Partition Master 18** — Added EaseUS Partition Master 18.0.0 to the Utilities module and website catalog.

## 2026-04-29 - Utilities & Build Versioning Update

### Utilities Module Updates
- **Dell OS Recovery Tool** — Updated the download URL to a high-speed Secure Storage direct link. This improves download reliability and speed for the Dell recovery utility.
- **Enhanced Version Visibility** — Integrated build numbers into the display names and filenames for all Secure Storage-hosted utilities (Office 2024, Backup & Restore, Macrium, etc.). This provides clear version tracking during both selection and execution.

### Website Updates
- **Catalog Synchronization** — Updated the website's feature list and download catalog to include explicit version numbers for all utilities, ensuring parity with the application's utilities module.

## 2026-04-28 - Build 1.2.6 - Project-Wide Version Synchronization

### Build & Versioning
- **Build 1.2.6** — Synchronized all core projects, modules, and contracts to version 1.2.6.
- **Website Update** — Updated the public website with latest release information and download links pointing to the v1.2.6 release assets.
- **Documentation Audit** — Updated architectural notes and internal documentation to reflect the new build number.


## 2026-04-28 - Secure Storage Migration & Interactive UI Enhancements

### Hosting & Infrastructure
- **Migration to Secure Storage** — Finalized transition of all file hosting from InfinityFree to Secure Storage direct-download links. This resolves the "Access Denied" and "Corrupted File" errors caused by InfinityFree's anti-bot security challenges.
- **Resilient Downloader (AsyncHelpers)** — Implemented `ResolveDownloadUrlAsync` to support following URLs hosted inside `.txt` files. Added explicit security challenge detection to prevent the application from downloading "JS-Challenge" HTML as binary installers.

### UI & UX Enhancements
- **Interactive Command Feed** — Refactored the output box from a custom-drawn control to a native `RichTextBox` wrapper. This enables full text selection, character-level copying, and native keyboard shortcuts (`Ctrl+C`, `Ctrl+A`).
- **Rich Context Menu** — Integrated a professional right-click menu in the output box with "Copy", "Select All", "Copy All", and "Clear" functionality.
- **Privacy Masking** — Implemented automatic URL sanitization in the output logs. Sensitive infrastructure and cloud storage IDs are now masked as `[Secure Storage]` to protect infrastructure privacy.
- **Compiler Cleanup** — Resolved all inheritance warnings in the UI theme engine by removing redundant member overrides.

### Website Updates
- **Synced Web Downloads** — Updated `Website/script.js` with new Secure Storage direct-links.
- **Expanded Tool Catalog** — Added **Office 2024** and **Backup & Restore Activation** to the public website download grid.

### Build
- Verified stability: **0 errors, 0 warnings**.

## 2026-04-26 - Utilities Updates

### Utilities Module Updates
- **CCleaner 6.40.115.62** — Updated the download URL and name for CCleaner in the Utilities module to version 6.40.115.62.

## 2026-04-25 - Utilities and Driver Management Updates

### Utilities Module Updates
- **IObit Driver Booster PRO 13.4.0.234** — Updated the download URL and name for IObit Driver Booster in the Utilities module to version 13.4.0.234.

### Driver Manager Module Updates
- **IObit Driver Booster PRO 13.4.0.234** — Updated the download URL and name for IObit Driver Booster in the Driver Manager module to version 13.4.0.234.

## 2026-04-20 - Performance Optimization & Code Surgical Refinement

### UI & UX Responsiveness
- **Throttled Window Resizing** — Implemented a dual-stage resize handler that defers heavy layout recalculations until the user finishes dragging the window (`ResizeEnd`), significantly reducing layout thrashing and UI stutter.
- **Progress Bar Performance** — Added font caching to `RoundedProgressBar` to eliminate GDI object leakage and allocation churn during progress updates.
- **Output Feed Optimization** — Removed redundant `SuspendLayout`/`ResumeLayout` calls and pre-compiled regex patterns for progress detection, moving computational cost from runtime to static initialization.
- **Redundant State Management** — Eliminated unnecessary progress bar value self-assignments and throttled UI heartbeats.

### Runtime & Memory Efficiency
- **Output History Capping** — Implemented a 5,000-entry limit for the command feed history to prevent uncontrolled memory growth during long sessions.
- **Optimized Module Loading** — Refactored `ModuleLoader` to use `HashSet<string>` for O(1) duplicate detection and implemented early-exit logic for plugin discovery, improving startup time by ~15%.
- **Enhanced Download Buffering** — Increased the internal buffer size from 80KB to 256KB in `CopyToAsyncWithProgress` for improved sequential throughput during large resource downloads.

### Code Quality & Surgical Refinement
- **Dead Code Purge** — Removed ~400 lines of identified dead code, including simulation logic (`SimulateActionExecution`), redundant action executors (`RunActionWithUiAsync`), and no-op methods from `MainForm.cs`.
- **Async Logic Simplification** — Deleted pointless async wrappers in `AsyncHelpers.cs` for trivial file operations to reduce Task scheduling overhead.
- **Build Configuration Cleanup** — Removed no-op MSBuild properties from the project file to clean up the deployment definition.

### Build
- Verified solution stability: **0 errors, 0 warnings**.

## 2026-04-18 - Security Audit & Bug Remediation

### Critical Security Fixes
- **ZipSlip Vulnerability (MalwareRemovalModule)** — `ZipFile.ExtractToDirectory` in Comodo extraction accepted crafted zip entries with `..` path traversal. Added post-extraction validation ensuring all resolved paths stay within the extraction directory.
- **Security Validation Bypass (UtilitiesModule)** — `RunBackupActivation` downloaded files directly via `DownloadFileAsync` to user profile, bypassing URL validation, filename sanitization, and extension whitelist. Rewired to use `DownloadAndExecuteAsync` which enforces all security checks.
- **Shell Injection via cmd.exe (NetworkOptimizer)** — All network commands were passed through `cmd.exe /c` with string interpolation. Replaced with direct process execution (`netsh.exe`, `ipconfig.exe`, etc.) avoiding the shell entirely.
- **Batch Script Injection (AutoUpdateService)** — Paths with `%` characters were expanded as environment variables by cmd.exe; malicious GitHub release tags could inject batch commands. Added path escaping (`%` → `%%`) and version string sanitization.
- **Predictable Temp Directories** — Comodo extraction and winget installer used fixed temp paths vulnerable to pre-population attacks. Changed to GUID-based unique directories.

### Bug Fixes
- **IServiceProvider Memory Leak (ServiceContainer)** — `RegisterModules` rebuilt the service provider without disposing the old one. Added proper disposal before reassignment.
- **CS1998 Async Warning (CloudRecoveryModule)** — `ExecuteCloudResetAsync` was `async` with no `await` on all paths. Changed to synchronous `Task`-returning method.
- **CancellationTokenSource Leak (WinREWizards)** — CTS was only cancelled in `OnFormClosing` but never disposed. Added `cts.Dispose()`.
- **Silent Failure on Drive Letter Exhaustion (DiskUtility)** — `FindAvailableDriveLetter` returned `'R'` even when all D-Z letters were in use. Now throws `InvalidOperationException`.

### Code Quality
- **Dead Code Removal (SecurityHelpers)** — Removed redundant regex patterns for `&&`, `||`, `;` that could never match because those characters were already stripped.
- **Missing Import (CloudRecoveryModule)** — Added explicit `using System.Linq` for `.Any()` and `.First()` calls.
- **Typo Fix (CloudRecoveryModule)** — Fixed "Downalod" → "Download" in action description.
- **URL Constant (UtilitiesModule)** — Moved inline Chris Titus URL to `DownloadUrls.ChrisTitusUtility` constant.

### Build
- All changes verified with clean build: **0 errors, 0 warnings**.

## 2026-04-17 - Utilities Module Expansion

### Utilities Module Updates
- **CleanMyPc v1.12.2.2178** — Added a new button for CleanMyPc v1.12.2.2178 to the Utilities module. Automatically downloads the tool, renames it to .exe, and executes with administrator privileges.

### Malware Removal Module Updates
- **ClamWin Portable URL Fix** — Updated the ClamWin Portable download URL in the Malware Removal module to the latest version (Build 1.4.3) to resolve reported download failures.

## 2026-04-15 - Utilities Module Expansion

### Utilities Module Updates
- **Dell OS Recovery Tool** — Added a dedicated button for the Dell OS Recovery Tool (v2.3.4.3569) to the Utilities module. Automatically handles downloading and execution.

## 2026-04-12 - Code Optimization & Dead Code Removal

### Core Cleanup & Optimization
- **Removed Duplicate ErrorHandler Class** — Eliminated 35-line duplicate `ErrorHandler` class from `Theme.cs` that was identical to `SystemUtilities.ErrorHandler`.
- **Consolidated Global Exception Handling** — Removed duplicate `SetupGlobalExceptionHandling()` method from `GlobalExceptionHandler.cs` (22 lines); now delegates to `CoreUtilities.SetupGlobalExceptionHandling()` for single source of truth.
- **Removed Redundant Async Wrappers** — Deleted `RunOnBackgroundThread<T>()` and `RunOnBackgroundThread()` methods from `AsyncHelpers.cs` (14 lines); callers now use `Task.Run()` directly.

### Program.cs Simplification
- **Simplified Error Handling** — Reduced nested try-catch spaghetti from ~45 lines to ~20 lines of clean, direct error reporting.
- **Removed Commented Code** — Eliminated stale commented-out animation code (`// Theme.Animations.AnimateForm...`).
- **Streamlined Startup** — Removed redundant try-catch nesting around `mainForm.Show()` and error display logic.

### Code Quality Improvements
- **Reduced Excessive Whitespace** — Cleaned up 4-5 consecutive blank lines to standard 1-2 throughout `Theme.cs`.
- **Build Verification** — All changes verified with successful build (0 errors, 0 warnings).

### Impact
- **~120+ lines removed** of duplicate/redundant code
- **Maintainability improved** by consolidating duplicate logic into single sources of truth
- **No functional changes** — all existing behavior preserved

## 2026-04-11 - Versioning, URLs, and System Prep Modernization

### Versioning System
- **Dynamic Versioning** — Synchronized all project and module versions to v1.2.5. Refactored the codebase to dynamically retrieve versions from assembly metadata instead of relying on hardcoded strings. The "About" dialog now consistently reflects the active project version.

### Module Updates
- **Macrium URL Update** — Updated the Macrium Reflect download URL in the `UtilitiesModule` to point to the newly hosted resource location.
- **System Prep Updates** — Modernized the System Prep module by replacing automated "all-or-nothing" updates with an interactive, user-controlled selection interface. Includes UI choices for Winget, Microsoft Store, and PowerShell updates with accurate download sizes and progress reporting.

## 2026-04-10 - Cloud Profile Sync Implementation

### Cloud Features
- **Cloud Profile Backup & Restore** — Implemented fully functional profile backup and restore capabilities in the `CloudRecoveryModule`, supporting OneDrive and Google Drive.
- **Secure Compression** — Added a secure compression mechanism for user profiles prior to cloud upload.
- **Sync Reliability** — Resolved bugs that prevented the Cloud Profile Sync feature from properly uploading data, ensuring complete profile transfers.

## 2026-04-09 - Production Security & Stability Audit

### Security & Organization
- **Enforced HTTPS** — Audited and enforced HTTPS on all external download routines across the application to ensure secure transit.
- **Browser Cache Cleanup** — Implemented a more robust and comprehensive browser cache cleanup mechanism.
- **Diagnostic Command Refactor** — Refactored diagnostic command mappings to improve maintainability and expandability.
- **General Cleanups** — Executed another successful pass at removing dead or redundant code.

## 2026-04-05 - .NET 11 Modernization & Build Refinements

### Build Stability
- **.NET 11 Transition** — Finalized the transition of the application to .NET 11, eliminating all build errors to achieve a clean compilation state.
- **Warning Elimination** — Eliminated 29 build warnings (CS8602, CS8604, CS8622) across the `CloudRecoveryModule` and `RepairModule` by implementing proper null-conditional operators and null-forgiving delegate parameters.
- **Signature Fixes** — Resolved 27 build errors by correctly standardizing method signatures for `DownloadAndExecuteAsync` across the `ToolsModule`.
- **Module Architecture** — Verified build stability of the newly consolidated Core modular architecture (Repair, Tools, SystemPrep, CloudRecovery).

## 2026-03-10 - Dialog Enhancements & Word Wrap Support

### UI & UX Improvements
- **Sizable Dialogs** — Major application dialogs (About, Content, Restore Point Manager, Startup Manager, Media Tools, Network Optimizer) are now sizable and include Maximize/Minimize buttons.
- **Enhanced `RoundedRichTextBox`** — Refactored the custom text control to support internal word wrapping and improved scrollability. 
- **Dynamic Re-wrapping** — Text automatically re-wraps when dialogs are resized, ensuring optimal readability at any window size.
- **Improved Scrolling** — Integrated better scrollbar handling and smooth scrolling for text-heavy content like changelogs and tool outputs.

## 2026-03-09 - Modern Recovery Customization (ScanState & OEM Restore)

### REAgentc Module Enhancements
- **Modern Recovery Support** — Added the "modern Windows 11 way" to achieve `recimg`-like results using Provisioning Packages and OEM extensibility.
- **Capture Customization Package** — Integrated `ScanState.exe` (USMT) to capture all installed desktop applications and settings into `.ppkg` files. These are automatically applied by Windows during a "Keep my files" reset.
- **Register OEM Restore Image** — Added automation for creating `ResetConfig.xml` to support custom WIM-based factory resets.
- **Modern Recovery Guide** — Embedded a comprehensive guide for Audit Mode, Sysprep, and DISM capture workflows within the module documentation.

## 2026-03-08 - SFC & REAgentc Modernization, Themed UI

### SFC Module Modernization
- **Refactored SFC Logic** — Ported `SFCModule` to modern C# syntax and abstracted process logic into `SfcHelper.cs`.
- **Phase Detection** — Implemented distinct phase tracking for Verification and Repair stages with localized progress parsing.
- **Exit Code Interpretation** — Added mapping for `sfc.exe` exit codes (0-4) to provide specific user-friendly summaries (e.g., "Violations Repaired", "Reboot Pending").
- **Offline Scan Helper** — Improved folder selection and validation for offline system file checking.

### REAgentc Module Modernization
- **Modernized Recovery Actions** — Removed legacy/redundant custom WIM creation actions to align with Windows 11's image-less recovery model.
- **WinRE Link Repair** — Added a new "Reset Recovery" action that performs a Disable/Enable toggle to fix common recovery environment issues.
- **ReagentcHelper Integration** — Abstracted `reagentc.exe` interaction into a dedicated helper class for accurate status parsing and partition identification.
- **Themed Information Popup** — The output of the "Check Status" action is now displayed in a premium, dark-themed popup instead of a standard Windows message box.

### UI & Core Architecture
- **Themed Content Dialogs** — Added `ShowContentDialog` to `DialogFactory` for consistent, rounded, dark-mode information windows.
- **`IDialogService` Interface** — Introduced a new contract to allow modules to trigger themed UI popups without direct dependencies on the UI project.
- **Safety Popup Removal** — Removed the redundant "Safety First" popup from the `MainForm` execution path to streamline the user experience.
- **Build & Contracts** — Updated `IRecoveryModule` to support extended execution delegates for UI service injection.

## 2026-03-07 - System Prep Modernization & Build Automation

### Build & Release Automation
- **GitHub Actions Workflow** — Implemented a full CI/CD pipeline (`dotnet-desktop.yml`) for automated building, testing, and publishing.
- **Automated GitHub Releases** — Configured automatic Release creation and executable asset delivery on version tag pushes.
- **NuGet Package Publishing** — Enabled automated publishing of core library packages to GitHub Packages on successful main branch builds.
- **Build Warning Suppression** — Resolved critical warnings related to nullability and single-file publish compatibility.

### Management & Updates
- **Native Windows Update Engine** — Replaced the legacy `usoclient.exe` (undocumented/unreliable) with a native C# implementation using the **Microsoft.Update.Session COM API**.
- **Granular Update Selection** — Introduced a new selection UI for Windows Updates that displays **Title**, **Size (MB)**, and **KB Article**. Users can now selectively download and install updates instead of trigger-and-hope.
- **Microsoft Store Update Overhaul** — Replaced the global "re-register all apps" command with a selective **Winget-based Store Update** engine. Users can now choose which specific Store apps to update.
- **Improved Winget Integration** — Modernized the Winget upgrade flow with better parsing and explicit selection UI for all software upgrades.
- **"No Updates" Feedback** — Added mandatory user feedback (MessageBox and Toast notifications) when scanning for Windows, Store, or Winget updates if no updates are available, ending silent non-actions.

### Network & Performance
- **Modern Network Reset** — Enhanced the network repair stack by adding physical **Network Adapter Restarts** via PowerShell (`Restart-NetAdapter`). This provides a true "hardware-level" reset beyond just clearing DNS/IP caches.
- **Compact OS Performance Guard** — Updated the "Compact OS" action description with a performance warning. It is now correctly recommended only for small SSDs (<128GB) to prevent unnecessary CPU overhead on high-end NVMe systems.

### Auditing & Redundancy
- **Enhanced Software Export** — Upgraded the software auditor to perform a **Live AppX Manifest Query**. It now captures Microsoft Store apps and Winget packages alongside traditional desktop applications for a truly complete system audit.
- **Action Redundancy Cleanup** — Stripped duplicate DISM component cleanup logic from "Quick Disk Cleanup", consolidating it into the dedicated "Deep Clean WinSxS" action to ensure surgical efficiency.
- **Refined Temp File Logic** — Unified User and Windows temp folder cleanup into a single, high-reliability atomic pass.

### Build & Reliability
- **Logic Collision Fixes** — Resolved several method naming collisions and duplicate logic blocks in `SystemPrepModule` that were causing build warnings and non-deterministic behavior.
- **Thread-Safe COM Operations** — Implemented STA-threaded execution for Windows Update COM calls to ensure stability on modern Windows kernels.

## 2026-03-04 - UI Fixes & Utilities URL Updates

### Bug Fixes
- **Progress Bar & Output Panel Restored** — Fixed a critical regression where the progress bar and output feed were invisible when running module actions. The root cause was that `progressPanel.Visible` and `outputPanel.Visible` were never being set to `true` in the async action execution path (`RunModuleActionAsync`) — only their child controls were made visible, while the parent panels remained hidden. Both panels are now explicitly shown when an action starts, and the bottom panel height is expanded to 300px to accommodate both.
- **Cancel Button Restored** — The Cancel button was hidden along with the progress panel due to the same parent visibility issue. It now correctly appears during any running operation and hides again on completion.

### UI Cleanup
- **"Command Feed" Label Removed** — The "Command Feed" heading above the output box has been removed for a cleaner, more minimal look. The output area now shows only the log feed and shell input field without a title banner.
- **Output Toolbar Removed** — Auto Scroll, Copy, Save, Clear, and Filter toolbar buttons have been removed from the output panel header as requested, simplifying the output area.

### Utilities Module — Download URL Updates
- **PC Repair Suite** — Updated download URL to new hosted location.
- **CCleaner Portable** — Updated download URL to new hosted location.
- **Macrium Reflect Portable** — Updated download URL to new hosted location.

### Build
- Solution builds successfully with 0 errors.

### Security Enhancements
- **DLL Hijacking Mitigation** — Removed insecure recursive fallback directory searching for plugins in `ModuleLoader.cs`. The module loader now strictly enforces loading modules only from the local `AppContext.BaseDirectory` + `"Module"`, effectively preventing potential arbitrary DLL execution if a malicious DLL was dropped in a globally writable parent directory.

### Auditing & Tooling
- **Autonomous AI Security Agent** — Added `Scripts/AIAuditAgent.py`, a new standalone autonomous AI agent that leverages OpenAI's API to scan the entire C# codebase. It checks for security vulnerabilities (e.g., command injection, path traversal), concurrency bugs (e.g., deadlocks, race conditions), and logical errors. It also automatically runs build tests and outputs a structured Markdown report (`Security_Audit_Report.md`).

## 2026-02-22 - Module Cleanup, Architecture Refactor & Typed Delegates

### Core Architecture & Cleanup
- **Deduplicated Utilities** — Removed redundant `#region Async Helpers` block from `CoreUtilities.cs` to enforce a single source of truth via `AsyncHelpers.cs`.
- **Project Structure Optimization** — Cleared orphaned empty `<ItemGroup>` tags and commented-out broken project references from `RecoveryCommander.csproj` for a leaner build definition.
- **Synchronous Execution Deprecated** — Wiped `void ExecuteAction(...)` entirely from the `IRecoveryModule` interface and all 6 implementing modules. Pushed the application strictly towards full `async/await` flows to guarantee a non-blocking UI.

### Dependency Injection & Networking
- **`IHttpClientFactory` Integration** — Replaced singleton `HttpClient` implementations with `Microsoft.Extensions.Http`'s standard injected factory inside `ServiceContainer.cs` to prevent socket exhaustion and DNS caching bugs.
- **Dependency Aligning** — Downgraded `Microsoft.Extensions.Http` to `10.0.0` inside `RecoveryCommander.csproj` to match the target runtime and prevent namespace collisions.
- **Form Registration** — Extended `ServiceContainer.Initialize` with an optional `configureServices` action to allow `Program.cs` to safely register forms (like `MainForm`) heavily streamlining form management scaling.

### Performance Upgrades
- **UI Timer Extermination** — Completely eliminated the archaic `System.Windows.Forms.Timer` (`uiRefreshTimer`) inside `MainForm.cs`, which was needlessly soaking up UI thread time. It was cleanly replaced with a dedicated `StartUIRefreshLoop()` running `await Task.Delay` to slash CPU usage dramatically.

### Typed Delegate Plugin System (Major Refactor)
- **Massive String-Matching Purge** — Entirely wiped out every fragile string-based `switch-case` and `if/else` block inside execution handling across all core modules!
- **`SimpleContracts.cs` Evolution** — Built a new `Func<IProgress<ProgressReport>, Action<string>, CancellationToken, Task>` delegate property onto `ModuleAction`.
- **Default Interface Implementation** — Modernized `IRecoveryModule` with a default `ExecuteActionAsync` routing body that automatically processes bound delegates, meaning modules no longer manually construct the mapping layer.
- **Action Inlining** — Rewrote `SFCModule`, `DismModule`, `ReagentcModule`, `MalwareRemovalModule`, `UtilitiesModule`, and `SystemPrepModule` to strongly bind direct method delegates alongside the GUI button initializers. If an Action's display name changes in the future, the action trigger will no longer silently break.

### Chocolatey Module Removed
- **Full Module Deletion** — Removed the entire `ChocolateyModule` project, including source code (`ChocolateyModule.cs`, `ChocolateyModule.csproj`), build artifacts, and compiled output.
- **Solution Cleanup** — Removed the project declaration, all build configuration entries, and solution folder nesting from `RecoveryCommander.sln`.
- **Clean Build Verified** — Solution builds successfully with zero errors after removal.

### Utilities Module: PC Repair Suite
- **New "PC Repair Suite" Button** — Added a new action to the Utilities module that downloads and launches PC Repair Suite.
- **Download & Execute** — Uses the same proven `DownloadAndExecuteAsync` pattern as CCleaner Portable — downloads the tool as a `.txt` file, renames it to `PCRepairSuite.exe`, and launches it.
- **URL Constant** — Added `PCRepairSuite` download URL to the centralized `DownloadUrls` class for maintainability.

## 2026-02-15 - Malware Removal, Security Expansion & Multi-threading

### New "Malware Removal" Module
- **Extracted Security Tools** — Created a dedicated "Malware Removal" module to clean up the Utilities panel and provide a focused security toolkit.
- **Auto-Discovery** — Fully integrated into the sidebar with a specialized action panel and unified execution logic.

### Comprehensive Security Toolkit (14 Tools Total)
- **Added Industry Standard Scanners:**
- **ESET Online Scanner** — Comprehensive on-demand cloud scanner.
- **Norton Power Eraser** — Aggressive tool for deep-seated rootkits and persistent threats.
- **HitmanPro** — High-performance cloud-based second-opinion scanner.
- **Microsoft MSRT** — Official Microsoft Malicious Software Removal Tool.
- **Dr.Web CureIt!** — Powerful portable antivirus scanner.
- **SuperAntiSpyware Portable** — Specialized remover for spyware, trojans, and adware.
- **Added Specialized Removal & Prep Tools:**
- **AdwCleaner** & **KVRT** — Migrated from Utilities for better organization.
- **Comodo Cleaning Essentials** — Added with automated ZIP extraction logic for zero-install execution.
- **ClamWin Portable** — Open-source standalone antivirus.
- **RKill** (Coming soon) — Prep tool to terminate malware processes before scanning.
- **Added Cloud-Based Scanners:**
- **F-Secure Online Scanner**
- **Trend Micro HouseCall**
- **Sophos Scan && Clean** — Fixed literal ampersand matching bug in UI execution.

### Execution & Downloader Reliability Overhaul
- **Browser Emulation** — Updated downloader `User-Agent` to a standard Chrome string to bypass "Access Denied" blocks on security vendor servers.
- **IO Lock Protection** — Implemented a 500ms post-download delay to allow real-time AV/SmartScreen scanning to release file locks before execution, resolving "File unreadable" errors.
- **Automated Pre-Cleanup** — Added logic to delete stale or corrupt binaries in the temp folder before starting a new download.
- **Centralized Extraction Logic** — Implemented `System.IO.Compression` handling for tools like Comodo CE that require ZIP extraction to run portably.

### Module Refactoring & UI Cleanup
- **Utilities Module Optimization** — Reduced buttons from 20 to 13, focusing on system maintenance (Office, Rufus, CCleaner, etc.).
- **Zemana Removal** — Added and subsequently removed Zemana AntiMalware based on user preference to keep the toolkit lean.

### Multi-threading & Responsive UI
- **Concurrent Job Execution** — Users can now navigate freely and start operations in different modules simultaneously.
- **Background Persistence** — Progress, status, and logs are preserved and restored when switching back to a running module.
- **Active Job Indicators** — Modules with running background tasks are highlighted with a glowing border and lightning bolt icon.
- **Smart Output Routing** — "ShowOutput" captures logs for background jobs without corrupting the active view.

## 2026-02-12 - Progress Bar & ETA Accuracy Overhaul

### Root Cause: Spaced-Out / Null-Character Output
- **Identified encoding mismatch** — SFC and DISM output captured via redirected `StandardOutput` arrives with null bytes or literal spaces between every character (e.g., `V e r i f i c a t i o n   3 4 %   c o m p l e t e .`). This is caused by a Unicode-vs-ANSI encoding mismatch when Windows tools write to a redirected stream.
- **All percentage-extraction regexes were failing** — The existing patterns `(\d+)\s*%` could not match `3 4 %` because the digits themselves were separated by spaces/nulls. The progress bar therefore never advanced past 2%.

### Space-Agnostic & Null-Resilient Parsing (MainForm.cs, SFCModule.cs, DismModule.cs)
- **Strip-then-match strategy** — Before applying any percentage regex, the raw text is now cleaned with `.Replace("\0", "").Replace(" ", "")`. This collapses `V e r i f i c a t i o n 3 4 %` into `Verification34%`, allowing the existing `(\d+)%` pattern to match reliably.
- **Applied to three independent parsing layers:**

  1. **MainForm fail-safe** (`ShowOutput`) — The global last-resort scanner that checks every line of output for a percentage.

  2. **SFCModule mid-line buffer** (`ReadStreamAsync`) — Character-by-character reader that fires when a `%` is appended to the buffer mid-line.

  3. **SFCModule line processor** (`ProcessSfcOutputLine`) — Full-line parser invoked on newline/carriage-return boundaries.

  4. **DismModule parser** (`ParseDismProgress`) — Output callback that extracts DISM percentages and maps them to progress reports.

### Forward-Only Progress Cache Protection (MainForm.cs)
- **Prevented stale report reversion** — The `DirectUIProgress<ProgressReport>` callback that feeds the UI timer's `lastProgressReport` cache now rejects any incoming report whose `PercentComplete` is lower than the currently cached value. This stops thousands of low-percentage status updates (e.g., "SFC process started" at 2%) from overwriting a higher value (e.g., 34%) that the fail-safe already detected.
- **Fail-safe cache synchronization** — When the fail-safe in `ShowOutput` detects a new high percentage, it now explicitly writes that report into `lastProgressReport` in addition to calling `UpdateProgressUI`. This ensures the 250ms UI refresh timer always works with the most recent data.

### Forced UI Heartbeat (MainForm.cs)
- **Explicit control redraw** — `UpdateProgressUI` now calls `progressBar.Invalidate()` and `progressBar.Update()` after setting the value, forcing an immediate WM_PAINT bypass of the Windows message queue.
- **Progress readout label sync** — The external `progressReadoutLabel` is now updated alongside the progress bar and immediately `.Refresh()`-ed for zero-lag text updates.
- **Busy overlay coordination** — When the progress bar advances past 5%, `SetBusyState` is called with the current status text to keep the overlay label in sync with the actual progress.

### SFC Module Optimizations (SFCModule.cs)
- **Eliminated redundant zero-percent flooding** — The SFC line processor now only calls `progress.Report()` when the extracted percentage has actually increased, preventing the UI thread from being saturated with thousands of identical 0% or low-percentage reports for every individual file scanned.

### DISM Module Optimizations (DismModule.cs)
- **Space-resilient DISM parsing** — Applied the same `.Replace("\0", "").Replace(" ", "")` cleanup to `ParseDismProgress`, ensuring DISM percentage extraction works even with wide-character or null-padded output.
- --

## 2026-02-12 - Asynchronous Refinement and Security Hardening

### Core Execution Improvements
- **Asynchronous Overhaul** - Refactored `AsyncHelpers.RunProcessAsync` to use reliable `TaskCompletionSource` patterns, ensuring processes and their entire trees are killed immediately upon cancellation.
- **Command Persistence** - Improved `CoreUtilities.ExecuteCommandAsync` to accurately distinguish between timeouts and user cancellations, providing better diagnostic feedback.
- **Resource Management** - Standardized disposal patterns for processes and cancellation tokens to prevent memory leaks and handle "zombie" processes.

### Security Hardening
- **Path Traversal Protection** - Enhanced `SecurityHelpers.IsValidFilePath` with mandatory full-path resolution to block advanced directory climbing and traversal attacks.
- **SSRF Mitigation** - Updated `IsValidDownloadUrl` to block a comprehensive list of private IP ranges, including IPv6 and link-local addresses, preventing Server-Side Request Forgery.
- **Reserved Filename Blocking** - Added explicit checks in `IsValidFileName` to block reserved Windows system names (CON, PRN, AUX, etc.) and sanitize illegal characters.
- **Injection Prevention** - Strengthened argument sanitization to remove command chaining operators (`&&`, `||`, `;`) and other dangerous symbols.

### Module Stability
- **Execution Bridging** - Refactored `ExecuteAction` in all modules (`SystemPrep`, `SFC`, `DISM`, `REAgentc`) to properly wait for asynchronous operations. This prevents the UI from prematurely resetting the "busy" state while background processes are still running.
- **Consistent Progress** - Unified progress reporting patterns across modules to ensure the status bar and progress indicators accurately reflect operation life-cycles.

### Bug Fixes
- **Code Consolidation** - Removed redundant and duplicate validation logic in `SecurityHelpers.cs`.
- **Exception Handling** - Improved error reporting in `ExecuteCommandAsync` to provide more specific messages when commands fail, time out, or are cancelled.

## 2026-01-24 - Driver Backup Feature and Build Optimization

### SystemPrep Module: Backup Drivers
- **New "Backup Drivers" Action** - Added a dedicated button to the Maintenance group in the System Prep module
- **DISM Integration** - Implemented full driver export using `dism /online /export-driver /destination:"D:\Drivers"`
- **Robust Validation** - Added smart drive detection to ensure the D: drive is present and ready before starting
- **Async Progress Reporting** - Properly routed execution through the asynchronous path to ensure real-time UI updates and progress bars
- **Enhanced Diagnostics** - Added detailed logging for drive checks, directory creation, and DISM exit codes to improve reliability

### Build System & Distribution
- **Framework-Dependent Builds** - Switched from Self-Contained to Framework-Dependent deployment to reduce build size and clutter
- **Cleaned Output Directory** - Removed `RuntimeIdentifiers` and platform-specific subfolders (`win-x64`), resulting in a cleaner `bin/Debug/net10.0-windows/` directory
- **Reduced Footprint** - Build no longer bundles the entire .NET runtime, making standard builds much faster and smaller

### Bug Fixes & Refactoring
- **Fixed Execution Routing** - Resolved an issue where newly added actions would "Complete" instantly without running by adding missing async handlers
- **Resolved Code Conflicts** - Cleaned up duplicate method definitions and unused logic in SystemPrepModule
- **Consolidated Process Execution** - Optimized how system commands are called via `cmd.exe /c` for better reliability in elevated contexts

## 2025-01-06 - Major System Cleanup and UI Enhancements

### Core System Cleanup
- **Removed MemoryManager dead code** - Eliminated commented-out MemoryManager property and class references from CoreUtilities.cs since the class doesn't exist
- **Removed unused Constants** - Deleted Chrome and Edge cache directory arrays from CoreUtilities.cs that were never used anywhere in the codebase
- **Deleted SystemSafety.cs** - Removed entire file as it duplicated functionality already present in ManagementTools.RestorePointManager
- **Fixed GlobalExceptionHandler** - Simplified initialization, removed broken service resolution, and fixed CS0120 error by making SetupGlobalExceptionHandling static
- **Removed Performance Profiling** - Deleted InitializePerformanceProfiler() and LogPerformanceMetric() methods that were never called
- **Fixed Missing Class References** - Removed references to non-existent MemoryManager, PerformanceProfiler, and CodingStandards classes

### SFC and DISM Progress Fixes
- **Fixed SFC Percentage Display** - Corrected progress calculation in SFCModule.cs to show actual tool percentages instead of incorrectly mapped values
- Changed from `30 + (percent * 60 / 100)` to `Math.Max(30, Math.Min(90, percent))`
- Enhanced regex patterns to better extract percentages from SFC output
- Improved time-based progress calculation (3% per minute instead of 5%)
- **Fixed DISM Percentage Display** - Added comprehensive progress parsing to DismModule.cs
- Implemented `ParseDismProgress` method with multiple regex patterns for DISM output
- Changed from mapped percentages to actual DISM percentages using `Math.Max(10, Math.Min(95, percent))`
- Added progress states for different DISM operations (scanning, restoring, cleaning, etc.)

### PowerShell Shell Integration
- **Removed ModernTerminal Control** - Deleted UI/ModernTerminal.cs file completely
- **Integrated Shell into Output Box** - Added PowerShell input field directly to MainForm output panel
- Added shell toggle functionality with "shell" command to enable/disable
- Implemented proper shell process management with cleanup on form closing
- Enhanced user experience with unified command interface in main output feed

### Build System Improvements
- **Fixed Compilation Errors** - Resolved 4 build errors related to missing classes and static member access
- **Cleaned ServiceContainer** - Removed comments about dead classes and updated HttpClient registration
- **Updated SystemPrepModule** - Replaced references to deleted constants with local cache directory definitions

### Code Quality Metrics
- **Removed ~200 lines** of dead/unused code across Core project
- **Eliminated redundancy** - Consolidated duplicate functionality between SystemSafety and ManagementTools
- **Fixed 4 compilation errors** - CS0120, missing constants, and SystemSafety reference
- **Maintained functionality** - All existing features preserved while removing dead code

### Technical Details
- **MemoryManager**: Property was commented out in CoreUtilities.cs (line 426)
- **Constants**: CHROME_CACHE_DIRS and EDGE_CACHE_DIRS arrays removed from CoreUtilities.cs (lines 26-47)
- **SystemSafety.cs**: Entire file deleted as it duplicated RestorePointManager functionality
- **GlobalExceptionHandler**: Fixed static member access and simplified initialization logic
- **SystemPrepModule**: Updated to use local cache definitions instead of removed CoreUtilities constants

### Impact
- **Build Status**: Changed from "failed with 4 error(s)" to "succeeded with 0 errors and 0 warnings"
- **File Reduction**: Core project now more maintainable with less dead code
- **Performance**: Improved compilation time and reduced code complexity
- **User Experience**: Progress bars now accurately display actual SFC/DISM percentages instead of incorrect mapped values

## 2025-12-18 - UI/UX Overhaul, Fluent Design, and Plugin System

### UI/UX & Theming Deep Overhaul
- **High-Performance Animation System** - Developed a 60FPS fluid `Animator` class in `Theme.cs` supporting eased transitions for opacity, position, and hover states.
- **Fluent & Mica Design** - Implemented native Windows 11 Mica effect and Windows 10 Acrylic support using P/Invoke `DwmSetWindowAttribute`.
- **Typography Modernization** - Overhauled the typography system to use **Inter** as the primary typeface with standardized design tokens for Display, Title, and Body text.
- **Fluid UI Transitions** - Added module-level fade-in transitions and "lift-on-hover" micro-interactions for module action tiles.
- **Advanced Progress Reporting** - Enhanced `RoundedProgressBar` with indeterminate marquee animations, high-quality rendering, and integrated status/ETA text.
- **Refined Control Styling** - Replaced standard WinForms controls in critical dialogs with custom `ModernButton` and `DarkFlowLayoutPanel` components for a premium look.
- **Consolidated Theming System** - Fully unified the theming engine into `Theme.cs`, eliminating redundant files and resolving event inconsistencies.

### Plugin Architecture & Extensibility
- **Drop-in Plugin System** - Rewrote `ModuleLoader` to recursively scan the `Module/` directory for any DLL implementing `IRecoveryModule`.
- **Dynamic Discovery** - Implemented robust assembly loading with dynamic type discovery, allowing for true "drop-in" extensibility without manual registration.
- **Enhanced Error Resiliency** - Added detailed logging and robust skip-logic for non-module assemblies during the discovery phase.

### Component Refinement & Stability
- **Modernized Startup Manager** - Fully themed the Startup Manager dialog with dark backgrounds, white text, and themed scrollbars.
- **Refined Restore Manager** - Cleaned up the Restore Point Manager UI, removing legacy actions and standardizing button alignment.
- **Build Quality** - Resolved dozens of compiler warnings (CS8602, CS0117, CS0649, CS8618) to ensure a clean, warning-free build.

## 2025-12-18 - Core Architecture Refactoring and Process Modernization

### Core System Architecture
- **Created RecoveryCommander.Core Library** - Converted the `Core` source folder into a proper `RecoveryCommander.Core.csproj` class library project
- **Updated Project References** - Refactored all 5 modules (`DismModule`, `ReagentcModule`, `SFCModule`, `SystemPrepModule`, `UtilitiesModule`) and the main application to reference the Core library project instead of compiling source files directly
- **Eliminated Duplicate Compilation** - Resolved build inefficiencies where Core files were being compiled multiple times across different projects

### Process Execution Consolidation
- **Consolidated Process Logic** - Centralized all process execution into `CoreUtilities.RunProcessAsync` and `CoreUtilities.CreateProcessInfo`
- **Removed Zombie Code** - Deleted `Core/SystemCore.cs` which contained duplicate and buggy legacy process execution logic
- **Unified Module Implementation** - Refactored `DismModule`, `ReagentcModule`, and `SystemPrepModule` to use the unified `CoreUtilities` methods, removing custom `RunProcessAndReport` implementations
- **Fixed Critical Bugs** - Resolved timeout issues and race conditions in process execution across the entire application by using a single, robust implementation

### Resource Management
- **Optimized HttpClient Usage** - Consolidated `HttpClient` instantiation to `ServiceContainer.GetHttpClient()` in `MediaCreator` and all modules
- **Prevented Socket Exhaustion** - Replaced per-request `HttpClient` creation with a shared singleton pattern

### Cleanup and Maintenance
- **Removed Redundant Files** - Deleted `CONSOLIDATION_OPPORTUNITIES.md` after addressing all high and medium priority items
- **Cleaned Up Attributes** - Removed duplicate `RecoveryModuleAttribute` from the Core folder
- **Standardized Codebase** - Ensured consistent patterns for process execution and resource access throughout the solution

## 2025-12-13 - Major Code Consolidation, UI Updates, and System Improvements

### Core System Consolidation
- **Merged AsyncHelpers.cs into CoreUtilities.cs** - Consolidated async utility methods into single core file for better organization
- **Merged SystemCore.cs into CoreUtilities.cs** - Combined system core operations and CommandResult class into centralized utilities
- **Deleted consolidated files** - Removed AsyncHelpers.cs and SystemCore.cs to eliminate redundancy
- **Updated all references** - Modified all modules (.cs files and .csproj files) to reference CoreUtilities instead of separate files
- **Fixed ProcessHelpers references** - Updated all ProcessHelpers.CreateProcessInfo calls to CoreUtilities.CreateProcessInfo across SystemPrepModule, ReagentcModule, and DismModule
- **Enhanced CoreUtilities.cs** - Now contains comprehensive
- .NET 10 compatibility fixes
- Async helper methods (RunProcessAsync, DownloadAndExecuteAsync, etc.)
- System core operations (ExecuteCommandAsync, CreateProcessInfo, CommandResult)
- ProcessHelpers functionality
- Memory management and performance profiling
- Global exception handling
- File operations utilities

### UI Framework Consolidation  
- **Merged UIExtensions.cs into Theme.cs** - Added UI utility methods as UIUtils class and extension methods as ControlExtensions
- **Merged UIAnimations.cs into Theme.cs** - Consolidated animation and transition effects as nested Animations class
- **Merged ThemePreferences.cs into Theme.cs** - Theme preferences already existed in Theme.cs (verified no duplication)
- **Merged RoundedProgressBar.cs into Theme.cs** - Custom control already existed in Theme.cs (removed duplicate)
- **Deleted consolidated UI files** - Removed UIExtensions.cs, ThemePreferences.cs, RoundedProgressBar.cs, UIAnimations.cs
- **Fixed extension method issues** - Moved ControlExtensions to top-level class outside Theme namespace to comply with C# requirements
- **Updated MainForm references** - Changed RoundedProgressBar references to Theme.RoundedProgressBar
- **Enhanced Theme.cs structure** - Now contains comprehensive theming system with:
- Main theme classes and color definitions
- UI utility methods (UIUtils)
- Extension methods (ControlExtensions) 
- Animation effects (Animations)
- Custom controls (RoundedProgressBar, etc.)
- Professional design system integration

### Tools Menu and Feature Updates
- **Restored Tools menu items** - Added back Network Repair & Optimization and Media Creation Tools to provide full feature set
- **Updated Media Tools functionality** - Enhanced media creation and management tools with improved UI
- **Improved Help menu system** - Added comprehensive help documentation and README access
- **Enhanced dialog functionality** - Improved DialogFactory methods for better user experience
- **Fixed Create Restore Point dialog theming** - Changed button styles from FuturisticPrimary/FuturisticGhost to Primary/Secondary for standard dark theme colors

### Dead Code Removal and Cleanup
- **Removed AsyncHelpers.cs** - Found dead file that should have been deleted during Core consolidation
- **Verified code health** - Build succeeds with no warnings, indicating no significant dead code remains
- **Confirmed active references** - All major classes and files have active usage throughout codebase
- **Validated consolidation** - All consolidated functionality properly integrated and accessible
- **Cleaned up unused references** - Removed stale project references and unused imports

### Build System and Reference Updates
- **Updated all .csproj files** - Modified module project files to reference consolidated files correctly
- **Fixed namespace references** - Updated all code references to use new consolidated class structure
- **Resolved ProcessHelpers dependencies** - Fixed all ProcessHelpers.CreateProcessInfo references to use CoreUtilities
- **Verified build success** - Application builds successfully with no errors after consolidation
- **Tested functionality** - All features remain functional with new consolidated structure
- **Cleaned build artifacts** - Removed stale build files and improved build performance

### Technical Improvements and Performance
- **Reduced file count** - Core folder reduced from multiple files to CoreUtilities.cs as main consolidation target
- **UI folder optimization** - Reduced from 7 files to 3 files (Theme.cs, UIControls.cs, ProfessionalDesignSystem.cs)
- **Improved maintainability** - Centralized related functionality into cohesive, well-organized files
- **Enhanced code organization** - Better logical grouping of related methods and classes
- **Simplified dependency management** - Fewer files to reference and manage across projects
- **Maintained functionality** - All original functionality preserved and accessible through consolidated structure
- **Enhanced debugging capabilities** - Improved error reporting and debugging information

### User Experience Enhancements
- **Improved dialog theming** - Standardized button styles and colors across all dialogs
- **Enhanced media tools interface** - Better UI for media creation and management features
- **Optimized help system** - More accessible documentation and user guides
- **Fixed UI inconsistencies** - Resolved theme application issues in various dialogs
- **Improved responsiveness** - Better performance in UI interactions and module loading

## 2025-12-09 - Tools Menu Optimization, Theme Consolidation, and Module Cleanup

### Tools Menu Restructuring
- **Streamlined Tools menu** - Limited menu items to only Boot Media Tool, Restore Point Manager, Startup Manager, and Boot Media Creator for cleaner interface
- **Removed Network Repair & Optimization** - Eliminated from Tools menu to reduce complexity and focus on core recovery tools
- **Removed Media Creation Tools** - Consolidated functionality into Boot Media Creator to eliminate redundancy
- **Updated MenuManager.cs** - Cleaned up menu item definitions to reflect new streamlined tool set
- **Verified dialog functionality** - Ensured all remaining Tools menu items work correctly with updated DialogFactory methods

### Theme System Consolidation
- **Consolidated scroll bar components** - Merged DarkScrollBar and CustomScrollBar into Theme.cs as nested types
- **Deleted redundant UI files** - Removed UI/DarkScrollBar.cs and UI/CustomScrollBar.cs to reduce code duplication
- **Updated all theme references** - Modified Forms/MenuManager.cs, Module/SFCModule/SfcProgressDialog.cs, UI/ModernControls.cs, and UI/EnhancedProgressSystem.cs to use consolidated theme components
- **Enhanced theme application** - Improved Theme.ApplyTheme to recursively apply DarkScrollBar to all ScrollableControl, TextBoxBase, ListBox, TreeView, and ListView controls
- **Fixed late-initialized theming** - Updated ApplyPanelStyle and ApplyRichTextBoxStyle to hook HandleCreated event for controls without handles, ensuring proper theming when shown

### Module System Improvements
- **SFC Module consolidation** - Condensed SFCModule into single file by merging Platform.cs assembly attributes and deleting unused files (IModule.cs, PlatformCompatibility.cs, SfcProgressDialog.cs)
- **SystemPrep Module optimization** - Consolidated FileOperations.cs (SafeDeleteFile, SafeDeleteDirectory) into SystemPrepModule.cs and removed PackageSelector.cs
- **Updated project references** - Modified SystemPrepModule.csproj to remove references to deleted files
- **Enhanced module loading** - Improved ModuleLoader scanning to avoid stale artifacts and prefer runtime Module/* folders

### UI/UX Enhancements
- **Fixed button text truncation** - Enabled AutoSize=true on buttons and FlowLayoutPanel containers in Restore Point Manager and Startup Manager to prevent text cutoff
- **Standardized button themes** - Updated StartupManager and RestorePointManager buttons to use Primary (Blue) theme instead of manual Warning colors for consistent appearance
- **Improved English localization** - Ensured all button text is properly displayed in English regardless of system locale
- **Enhanced dark scrollbars** - Implemented custom dark scrollbars for System Prep list and Log Box using DarkFlowLayoutPanel wrapper and DarkRichTextBox

### Script and Resource Consolidation
- **Merged wifi.ps1 into Tweaks.ps1** - Extracted advanced Wi-Fi optimization logic and consolidated into single Resources/Tweaks.ps1 file
- **Deleted redundant scripts** - Removed wifi.ps1 to reduce file count and centralize optimization logic
- **Enhanced Tweaks.ps1** - Updated to be non-interactive for silent execution and combined both system and network optimizations
- **Consolidated Apply-Windows11RecoveryTweaks.ps1** - Merged more complete script including DNS selection into Resources/Tweaks.ps1

### Technical Improvements
- **Async execution enhancement** - Replaced BackgroundWorker in MainForm with async flow using RunActionWithUiAsync for non-blocking operations
- **Removed checkboxes from System Prep** - Updated MainForm.DisplayModule to remove CheckBox creation for System Prep tiles, maintaining multi-select capability with cleaner UI
- **Enhanced error handling** - Improved exception handling and user-friendly message generation throughout the application
- **Build system cleanup** - Eliminated duplicate project compilation and cleaned up redundant build artifacts

### Code Quality and Performance
- **Reduced file count** - Consolidated multiple modules and scripts, reducing overall project complexity
- **Improved maintainability** - Centralized theme management and reduced code duplication across multiple areas
- **Enhanced debugging** - Added comprehensive logging for UI creation and action processing
- **Memory optimization** - Removed unused controls and streamlined resource usage

## 2025-12-04 - UI Fixes, Network Optimizer Improvements, and Build Cleanup

### Tools Menu and Dialog Fixes
- **Restored Tools menu items** - Reverted Network Repair & Optimization and Media Creation Tools to original state after user feedback about removal
- **Fixed Create Restore Point dialog theming** - Changed button styles from FuturisticPrimary/FuturisticGhost to Primary/Secondary for consistent dark theme appearance
- **Resolved dialog text visibility issues** - Fixed InputDialogForm text positioning and sizing problems that caused cut-off text
- **Enhanced Restore Point error handling** - Added comprehensive error checking for System Restore enabled status, administrator privileges, and detailed error messages

### Network Repair & Optimization Overhaul
- **Complete layout redesign** - Restructured from 60/40 to 35/65 top/bottom split for better space utilization
- **Eliminated wasted space** - Reduced window size to 900x750 with more efficient layout proportions
- **Implemented tabbed interface** - Combined ping results and operation log into tabbed interface to maximize available space
- **Fixed text sizing consistency** - Updated all fonts and colors to match Theme standards
- **Optimized button styling** - Changed from futuristic gradient buttons to standard theme buttons (Primary/Secondary)
- **Resolved cutoff issues** - Bottom section now gets 65% of window height, preventing content cutoff

### Build System and Code Quality
- **Fixed all nullable warnings (CS8618)** - Resolved 7 warnings in UIAnimations.cs by properly handling nullable fields
- **Added null suppression operators** - Used `!` operators for fields initialized in constructor to satisfy C# nullable requirements
- **Clean build configurations** - Both Debug and Release builds now compile with 0 warnings and 0 errors
- **Enhanced error messages** - Added specific error codes and user-friendly messages for System Restore operations

### User Experience Improvements
- **Better error feedback** - Restore Point creation now provides clear guidance for common issues (admin rights, System Restore disabled, etc.)
- **Consistent theming** - All dialogs and windows now use unified Theme colors and fonts
- **Improved space efficiency** - Network Optimizer layout maximizes usable space while maintaining functionality
- **Professional appearance** - Removed flashy gradients in favor of clean, consistent dark theme styling

### Technical Enhancements
- **System Restore validation** - Added helper methods to check System Restore status and administrator privileges
- **Enhanced exception handling** - Improved error categorization and user-friendly message generation
- **Layout optimization** - Simplified complex TableLayoutPanel structures for better performance and maintainability
- **Code cleanup** - Removed hardcoded colors and inconsistent styling patterns

## 2025-11-25 - Command Feed UX & Toast Integration

### Command Feed Enhancements
- **Interactive command feed header** – added a themed toolbar with Copy, Save, and Clear controls plus severity filtering and auto-scroll toggles so operators can manage log output without leaving the app.
- **Filtered output history** – command feed now stores timestamped entries with level metadata, enabling on-demand filtering and accurate “visible vs total” counts in the header.
- **Instant theming** – the command feed header and toolbar react to theme and preference changes through new Theme event hooks, keeping typography and colors in sync.

### Toast Notifications & Feedback
- **Centralized toast helper** – MainForm now routes success, warning, and error states through `EnhancedProgressSystem` to surface non-blocking notifications across batch runs and single actions.
- **Progress toasts for operations** – batch executions, module loads, and individual action runs emit contextual toasts (start/complete/cancel/fail) so users get feedback even when the command feed is collapsed.
- **Resilient fallback messaging** – when the enhanced progress system is unavailable, toast requests gracefully fall back to the command feed with level-aware tagging.

### Stability & Maintenance
- **Theme listener lifecycle** – theme change and preference handlers are registered once and disposed with the form to prevent leaks during shutdown.
- **Changelog documentation** – recorded the latest UI/notification improvements and supporting infrastructure changes.

## 2025-11-20 - Core & SystemPrep Maintenance Enhancements

### Module and Core cleanup
- **Removed obsolete Module system stubs** – deleted `Modules/ModuleBase.cs` and `Modules/ModuleSystem.cs` plus the legacy `Modules` folder that was no longer referenced anywhere in the app.
- **Consolidated core utilities** – moved `RecoveryCommander.Core.Utilities.Constants` and `FileOperations` into `Core/SystemCore.cs` under the `RecoveryCommander.Core.Utilities` namespace, removing the old `Core/Utilities` files and updating `RecoveryCommander.Core.csproj` accordingly.
- **Safer file operations** – tightened `SHFILEOPSTRUCT` interop and nullability around shell file operations to prevent warnings and improve robustness when deleting files/directories.

### UI and contracts hygiene
- **Theme migration cleanup** – removed unused `UI/ThemeMigration.cs` and related dead code.
- **Single source of truth for progress reporting** – deleted a duplicate `IProgressReporter` interface from `UI/CommonInterfaces.cs` so modules and UI now consistently use the contracts defined in `RecoveryCommander.Core`.

### SystemPrep maintenance actions
- **Winget upgrade discovery** – added `UpdateHelpers.GetWingetUpgradesAsync` to parse `winget upgrade` output into structured items (name, ID, installed version, available version, source).
- **Selectable winget updates dialog** – introduced `WingetUpdateSelectorForm` so users can review available winget package updates in a grid and choose exactly which ones to apply.
- **Per-package winget execution** – `SystemPrepModule` now runs `winget upgrade --id <packageId>` per selected package with progress updates instead of a single blind `--all` call.
- **No-updates UX** – when no winget updates are available or no packages are selected, System Prep now logs the outcome and shows a non-blocking toast notification via `EnhancedProgressSystem` instead of relying on modal message boxes.
- **Improved winget resolution** – refined `FindWingetExecutable` and GitHub/App Installer fallback logic so machines without a readily discoverable `winget.exe` are handled more gracefully.

### Build and nullability
- **Warning-free SystemPrepModule** – annotated and refactored async helpers and shared process runners so the entire solution builds cleanly with `/warnaserror`, eliminating previous CS860x nullable warnings.

## 2025-11-20 - Futuristic UI Refinement and Utilities Reliability

### Main Experience Refresh
- **Hero/Operations layout parity** – hero, action tiles, and command feed now share a consistent 28px gutter, eliminating the offset between sections.
- **Operations grid polish** – module content shell uses Neon grid backdrops so futuristic tiles keep uniform widths across every module.
- **Command feed alignment** – progress bar + output log stack matches hero width with balanced padding, smaller progress height, and neon readout text that mirrors job status.
- **Status chips stability** – reverted to proven gradient badges (Modules / Active Mode) for crisp typography without overlap while still matching the neon palette.

### New Futuristic Controls
- **Holographic panel & neon divider** – hero surface now renders animated scanlines with a pulsing divider for added depth.
- **Neon grid & glowing tiles** – action tiles gained hover/selection pulses driven by timers, and operations wrap inside a shimmering grid panel to emphasize modularity.
- **Progress readout label** – live "% · status" indicator keeps users informed even when the progress bar is minimized.

### Dialog & Menu Consistency
- **Theme extensions** – FuturisticPanel/FuturisticActionTile refinements plus new helper controls ensure all future dialogs can adopt the same holographic language.
- **Alignment fixes** – module content auto-scroll + hero padding adjustments keep modules, operations, and hero titles perfectly stacked, regardless of selected module.

### Utilities Module Enhancements
- **CCleaner URL reliability** – both synchronous and async execution paths now download from the new hosted manifest, preventing legacy Google Drive failures even after clean builds.
- **Clean/rebuild verification** – project scripts were run to ensure modules pick up the new endpoints without stale DLLs.

### Quality & Build Health
- **Timer ambiguity resolved** – all animation helpers alias `System.Windows.Forms.Timer`, eliminating CS0104 collisions during builds.
- **Changelog revived** – documentation now reflects all futuristic theming work for quick reference.

## 2025-10-15 - Workflow System Cleanup and Comprehensive Theming Overhaul

### Workflow System Removal
- **Removed orphaned workflow files** - Eliminated unused workflow infrastructure (WorkflowTypes.cs, WorkflowSerializer.cs, WorkflowRunner.cs)
- **Updated WorkflowDesignerForm** - Converted misleading "Visual Workflow Designer" to accurate "Module Browser" functionality
- **Code cleanup** - Removed ~800+ lines of unused workflow code and dependencies
- **Menu accuracy** - Form now correctly represents its actual functionality as a module browser

### Comprehensive Theming System Implementation

### ThemeManager Integration
- **Centralized theme management** - All forms now use ThemeManager for consistent light/dark mode support
- **Automatic theme detection** - System automatically detects Windows light/dark mode preference
- **Dynamic theme switching** - Real-time theme updates when Windows theme changes
- **Consistent color palette** - Unified color scheme across all UI components

### Forms Updated with Modern Theming
- **WorkflowDesignerForm.cs** - Complete theming overhaul with ThemeManager integration
- **MainForm.cs** - Updated acrylic effects and menu renderers to use theme colors
- **TestRunnerForm.cs** - Full theming integration with proper initialization
- **OptimizationDashboard.cs** - Updated Fluent Design components for theme consistency
- **NetworkDiagnosticsSuite.cs** - Comprehensive theming for all ListView and TabControl elements
- **RegistryHealthScanner.cs** - Consistent theming throughout interface
- **RestorePointManager.cs** - Complete theming integration with proper error handling
- **CommandEditorDialog.cs** - Modern theming with ThemeManager support

### UI Components Enhanced
- **ModernControls.cs** - Comprehensive update of all custom controls to use ThemeManager colors
- **EnhancedProgressBar.cs** - Added ThemeManager support with dynamic color calculation
- **All hardcoded colors replaced** - Eliminated Color.FromArgb() calls in favor of ThemeManager properties

### Theme System Features

### Light Mode Support
- Light gray backgrounds (248, 248, 248)
- Dark text for readability (30, 30, 30)
- Light selection colors (200, 220, 240)
- Subtle borders (200, 200, 200)

### Dark Mode Support
- Dark gray backgrounds (32, 32, 32)
- Light text for contrast (220, 220, 220)
- Dark selection colors (60, 80, 100)
- Dark borders (64, 64, 64)

### Consistent Accent Colors
- Unified blue accent (0, 120, 212) across both themes
- Proper contrast ratios for accessibility
- Dynamic color calculation for gradients and effects

### Technical Improvements

### Code Quality
- **Proper using statements** - Added `using RecoveryCommander.UI;` where needed
- **Theme initialization** - Consistent theme setup in all form constructors
- **Error handling** - Improved exception handling in theme-related code
- **Performance optimization** - Efficient theme application without UI flicker

### Architecture Benefits
- **Single source of truth** - All theming controlled through ThemeManager
- **Maintainable code** - Easy to update colors across entire application
- **Future-proof design** - Easy to add new themes or modify existing ones
- **Windows integration** - Seamless integration with Windows theme preferences

### User Experience Enhancements
- **Automatic adaptation** - Application theme matches Windows system preference
- **Consistent interface** - Unified look and feel across all dialogs and forms
- **Modern appearance** - Contemporary Windows 11-style theming
- **Accessibility compliance** - Proper contrast ratios in both light and dark modes
- **Professional polish** - Eliminated visual inconsistencies and hardcoded colors

## 2025-10-14 - Module Loading Fixes, SFC consolidation, and Store Updates GUI

### Module Discovery and Build Output
- Adjusted `Module/SystemPrepModule/SystemPrepModule.csproj` output to `bin/Debug/…/Module/SystemPrepModule/` and restored reference to `RecoveryCommander.csproj` to resolve `RecoveryCommander.Forms` types.
- Fixed `ModuleLoader` scanning to avoid stale `obj/ref/refint` artifacts and prefer runtime `Module/*` folders.
- Further refined `ModuleLoader` to recursively scan `Module/` and de-duplicate by processed file path, ensuring actual module DLLs load and preventing type load errors from partial artifacts.
- Microsoft Store Updates (Check & Install Microsoft Store Updates) - winget store source
- Winget Updates (Check & Install Winget App Updates) - comprehensive app updates
- Implemented multi-selection capability with "Run Selected" button for batch execution
- Added visual selection system - click buttons to toggle selection (blue <-> orange)
- Auto-selection of recommended actions (AutoTick) for user convenience
- Uniform button styling with consistent blue coloring and minimal 2px spacing

### Technical Architecture Improvements
- Built-in module system - reliable SystemPrepModule implementation independent of external DLLs
- Module replacement logic - automatically replaces problematic external modules with enhanced built-in versions
- Real command execution - actual system commands (sfc, dism, powercfg, winget) instead of simulations
- Enhanced error handling with comprehensive exception handling and fallback mechanisms
- Progress tracking with real-time progress bars during command execution

### System Maintenance Actions Enhanced

### System Maintenance:
- System File Check (SFC /scannow)
- DISM Health Check (checkhealth + restorehealth)
- Memory Diagnostic (mdsched scheduling)
- Disk Check (chkdsk with repair options)

### System Cleanup:
- Disk Cleanup (cleanmgr utility)
- Temp Files Cleanup (PowerShell-based cleanup)
- Registry Cleanup (integration with Registry Health Scanner)
- Power Settings (high performance + hibernation management)

### Bug Fixes and Stability
- Fixed duplicate SystemPrepModule loading - prevents conflicts between external and built-in modules
- Resolved action mapping errors - buttons now execute correct corresponding actions
- Fixed module version inconsistencies with proper version tracking and display
- Eliminated intrusive startup popup notifications
- Fixed missing action buttons - System Prep actions now display reliably
- Resolved layout conflicts using simplified, proven layout system
- Enhanced progress bar visibility and feedback systems

### Code Quality and Performance
- Improved module loading with optimized external module filtering
- Better responsiveness with improved UI thread management
- Streamlined execution with direct command execution without unnecessary overhead
- Enhanced debugging with detailed logging for module loading and action execution
- Comprehensive error isolation - failed operations don't affect other actions

## 2025-10-04 - Major Project Consolidation and Optimization

### Project Structure Optimization

### CoreModules: Reduced from 13 to 4 files (69% reduction)
- Consolidated AuditLogger, SettingsManager, CommandModule into CoreUtilities.cs
- Enhanced functionality with better accessors and helper methods
- Removed 7 empty/unused files (ChangelogManager, VersionManager, SendEngager, etc.)

### Scripts: Reduced from 22 to 13 files (41% reduction)
- Created ModuleValidationTools.ps1 (consolidated 3 validation scripts)
- Created ProjectCleanupTools.ps1 (consolidated 3 cleanup scripts)
- Renamed all scripts with descriptive names for better clarity
- Removed broken windows install builder module

### Forms: Optimized with unified modern controls
- Consolidated ModernTextBox into ModernControls.cs
- Removed 6 empty/redundant files (IThemedForm, ThemeHelper, etc.)

### Build System Improvements
- Eliminated duplicate project compilation (removed RecoveryCommander.Modules.csproj)
- Fixed duplicate DLL generation issue
- Cleaned up redundant build artifacts and temporary files
- Removed 30+ redundant files including duplicates and empty classes

### UI/UX Enhancements
- Enhanced status bar with real-time system information (CPU, RAM, OS, time)
- Improved progress feedback with cancel functionality and visual feedback
- Enhanced cancel button with proper state management and user feedback
- Progress bar now shows elapsed time for both completed and cancelled operations
- Status bar updates show current operation progress and system status

### Documentation Consolidation
- Consolidated README.md with Resources/readme.txt and ProjectManifest.txt content
- Enhanced in-app README viewer with Markdown formatting support
- Updated MenuManager to display consolidated documentation
- Improved help window with larger size, resizable interface, and better formatting
- Added basic Markdown-to-text conversion for better readability

### Threading and Stability
- Fixed Timer ambiguity issues (explicitly using System.Windows.Forms.Timer)
- Enhanced disposal safety with comprehensive IsDisposed checks
- Improved cross-thread operation protection with proper exception handling
- Fixed InvalidOperationException during form disposal

### Performance Improvements
- Removed 1.5MB+ of build artifacts and temporary files
- Streamlined project structure for faster builds and better maintainability
- Eliminated code duplication across multiple areas
- Optimized file organization and reduced project complexity

## 2025-10-04 - System Prep UI Modernization and Bug Fixes

### System Prep Module UI Refactor
- Completely redesigned System Prep module UI with modern card-based layout
- Replaced single-pass grouping with proper header-based action organization
- Added grouped checkboxes in themed cards with clear visual hierarchy
- Implemented Select All/Clear All/Run Selected buttons with proper theming
- Enhanced action grouping by headers with fallback logic for non-header actions
- Added comprehensive debug logging for UI creation and action processing

### UI/UX Improvements
- Fixed compilation errors in MainForm.cs related to missing variable declarations
- Added debug visual indicators (background colors) to troubleshoot UI visibility issues
- Improved theme support for System Prep controls with proper dark/light mode colors
- Enhanced control hierarchy with FlowLayoutPanel and Panel-based card system
- Added size guarantees and minimum dimensions for reliable UI rendering

### Code Quality & Debugging
- Added extensive logging throughout System Prep UI creation process
- Implemented proper error handling and fallback logic for action grouping
- Fixed method structure issues and unreachable code warnings
- Enhanced debug output with control counts, visibility status, and hierarchy information

### Technical Improvements
- Refactored ModuleListBox_SelectedIndexChanged to properly handle System Prep special case
- Improved control creation with AutoSize and AutoSizeMode.GrowAndShrink
- Added theme-aware color selection for all UI elements
- Implemented robust action processing with null-safe operations

## 2025-09-11 - Additional fixes, features and refactors

### Modules

### ReagentcModule
- Reordered actions so Info is shown first
- Added Enable/Disable actions for REAgentc and implemented handlers to run reagentc /enable and /disable
- Added CreateCustomRecoveryImage action (SaveFileDialog) to capture a WIM using DISM
- Added CreateAndSetCustomRecoveryImage action to capture a WIM and set it as the recovery image via reagentc (includes file picker)
- Removed the header entry from the actions list and annotated module as Windows-only ([SupportedOSPlatform("windows")]) to address platform compatibility warnings

### DismModule
- Fixed unreachable code and ensured CheckHealth action is present and handled
- Added CreateCustomImage action earlier (file picker) and ensured DISM invocation is consistent

### SystemPrepModule & SfcModule
- Improved UI integration and fixed theming for System Prep checklist display

### UI / Theming / UX

### MainForm
- Centralized dark theme color (DarkBackground) and improved theme application so modules and controls update when theme changes
- Ensured module containers, header buttons, action buttons and System Prep CheckedListBox respect light/dark themes
- Reduced menu and bottom area footprint so module scroll area is not obscured; MenuStrip docks to Top
- Added a custom ThemedProgressBar control that renders a green progress fill (in-progress and complete) and matches app theme
- Fixed multiple visual inconsistencies and ensured ApplyThemeRecursive does not overwrite module-specific BackColor for container controls

### MenuManager
- Replaced Assembly.Location usage with AppContext.BaseDirectory and safer assembly attribute lookup to work correctly for single-file publishing
- Improved the 'Check for updates' flow: compares semantic versions, offers to open releases page, or download the first suitable release asset and run it

### Build / Packaging / Scripts

### RecoveryCommander.csproj
- Disabled PublishReadyToRun to avoid crossgen2 duplicate-input errors during publish
- Added targets to copy module binaries into the output Module folder after build

### Scripts
- Added Scripts/Publish-SingleFile.ps1 to publish a single-file self-contained bundle (includes option to disable R2R)
- Added Scripts/Clean-DebugFolders.ps1 to remove Debug folders under bin/obj before builds

### Reliability / Code Quality

### ModuleLoader
- Robust type loading with ReflectionTypeLoadException handling; null-safe filtering of types and safer instance creation/logging

### Nullability & warnings
- Addressed several CS8600/CS8602 nullable warnings in MenuManager and ModuleLoader with explicit null checks and safer code paths
- Annotated Windows-only modules and APIs to suppress CA1416 warnings where appropriate

### Misc / Notes
- Fixed numerous minor bugs, syntax issues and improved logging across modules and loader
- Build verified locally (single-file publish script provided and R2R disabled to avoid crossgen2 issues)

## 2025-09-10 - Multiple fixes and features

### UI & Layout
- Reworked main form layout to a deterministic TableLayoutPanel stacking: menu, modules, bottom area (progress + output), status
- Adjusted module panel and output area sizes; modules now occupy larger portion of the window by default
- Menu bar z-order fixed so it remains above module controls
- Restored form title and app icon loading from Resources/system_restore.ico
- Added system theme detection (HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme) and live updates via SystemEvents.UserPreferenceChanged

### Theming
- Added SetTheme to MainForm and ApplyThemeRecursive helper to apply Light/Dark themes across controls
- MenuManager theme menu integrated with MainForm theme switching
- Implemented a simple MenuColorTable for menu coloring

### App Backup & Restore
- Improved manifest parsing for both YAML and JSON winget export formats 
- Detects winget export JSON schema (Sources[].Packages[].PackageIdentifier). Parses PackageIdentifier, PackageId and friendly DisplayName when available
- Enhanced YAML line scanning for Id/Name keys
- Added a dedicated Restore dialog using a ListView (Package, Id, Status) instead of a plain CheckedListBox
- Implemented availability checks using `winget show --id <id>` to mark packages as Available / Missing / Unknown
- Restore flow only installs packages marked as Available; prompts user if Missing items are selected
- Export (backup) flow now runs `winget export` and saves both yaml and a `winget-list.json` when possible

### UX & Controls
- Replaced native ProgressBar with a small ThemedProgressBar control so progress matches app theme colors
- Fixed several crashes and added defensive null-checks across MainForm to improve robustness during module discovery

### Misc
- Various small fixes for parsing, error handling, and logging during module loading and winget interactions

### Notes
- The restore availability check relies on the `winget` binary being present and accessible on PATH. If winget is not installed the dialog will mark statuses as Unknown
- Some installed programs are not available via winget or require license acceptance; these are shown as Missing or requiring user agreement during install
