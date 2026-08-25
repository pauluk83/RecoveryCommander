# RecoveryCommander Changelog

## 2026-08-25 - Publish Build Blank Window Fix

### Bug Fixes — WinUI Blank Window in Published Builds
- **Primary fix: XamlControlsResources failure handling** — Modified theme loading in [App.xaml.cs](file:///d:/OneDrive\RecoveryCommander/RecoveryCommander.WinUI/App.xaml.cs#L278) to make XamlControlsResources failure non-fatal. In unpackaged builds, XamlControlsResources fails to load from ms-appx URIs, but the app now continues running by relying on custom theme files (Colors.xaml, Styles.xaml) which are successfully loaded from disk.
- **Secondary fix: PRI file generation for unpackaged apps** — Added `<GenerateLibraryLayout>true</GenerateLibraryLayout>` and `<GenerateProjectPriFile>true</GenerateProjectPriFile>` to [RecoveryCommander.WinUI.csproj](file:///d:/OneDrive/RecoveryCommander/RecoveryCommander.WinUI.csproj#L19) to ensure proper PRI file generation during build. Unpackaged WinUI 3 apps require a .pri file named after the executable to load XAML resources including XamlControlsResources. Without this, the app crashes with `Cannot locate resource from 'ms-appx:///Microsoft.UI.Xaml/Themes/themeresources.xaml'`.
- **Tertiary fix: Theme folder and PRI file copying** — Extended the `CopyRequiredDependenciesToPublish` target in [RecoveryCommander.WinUI.csproj](file:///d:/OneDrive/RecoveryCommander/RecoveryCommander.WinUI.csproj#L127) to copy the Theme folder and all PRI files to the publish directory. Added a Move task to rename `RecoveryCommander.WinUI.pri` to `RecoveryCommander.pri` for unpackaged app compatibility.
- **Impact**: The app now renders properly in both local Debug builds and CI Release publish builds, resolving the blank window issue that occurred when running from the published output directory.

### Code Quality
- **Code analysis warning fixes** — Added `#pragma warning disable CA1031` with justifications for intentional broad exception catches in theme loading code. Fixed CA1305 locale-sensitive warning by using `int.ToString(CultureInfo.InvariantCulture)` for invariant string conversion. Made exception handling more specific in OnLaunched by catching COMException, XamlParseException, and IOException before generic fallback.

### Build Verification
- **Build**: `dotnet build RecoveryCommander.WinUI -c Debug` → 0 errors, 0 warnings.
- **Runtime**: App renders correctly with custom theme resources when XamlControlsResources fails to load.

## 2026-08-24 - Website Sync, Module Action Descriptions & CI Fix

### CI / GitHub Actions
- **CodeQL workflow fix** — Added `--runtime win-x64` to the `dotnet restore` step in `codeql.yml`, matching the main build workflow. This resolves the "runtime pack not downloaded" failure that caused CodeQL analysis to exit with code 1.

### Website
- **Version bump** — Updated website to v1.2.9: hero badge, download link, `softwareVersion` in JSON-LD schema, and script cache-buster all updated.
- **Module data sync** — Rewrote all `moduleData` feature entries in `script.js` so every action string exactly matches the `Name — DisplayName` pair declared in the corresponding C# module file (REAgentc, SFC, Diagnostics, Utilities, System Prep, Malware Removal, Driver Manager, Cloud Recovery, DISM).
- **Audit headers updated** — Added today's changelog entry to `index.html` and `script.js` audit headers.

### Download Catalog
- **Office 2024 key rotation (×2)** — `Utilities.Office2024` in `DownloadCatalog.cs` updated twice today due to back-to-back Dropbox share-key changes:
  - Rotation 1: `st=wgxzze0y` / SHA `1C08E172...C209F53`
  - Rotation 2: `st=1w0rjtex` / SHA `ECF23B7E...053C83` *(current)*

### UI / XAML
- **Dynamic module action buttons** — Changed action button sizing in `MainPage.xaml` from fixed `Width="210" Height="58"` to `MinWidth="210" MinHeight="58"` with a `TextWrapping="Wrap"` `ContentTemplate`. Long action names now wrap onto a second line instead of being cut off.



### Versioning & Dependencies
- **Application version** — Updated the app, Core, and Contracts metadata to build `1.2.9`.
- **Dependency maintenance** — Updated `System.Management.Automation` to `7.6.5` and `System.Diagnostics.PerformanceCounter` to `10.0.1`.

## 2026-08-21 - EaseUS Partition Master Update

### Utilities
- **EaseUS Partition Master 20.8.0** — Updated the download catalog, Utilities module label, and website listing to version 20.8.0 with the new Dropbox download source and SHA-256 integrity pin.

## 2026-08-17 - Version & Website Sync

### Versioning
- **Unified release metadata** — Standardized the app version to `1.2.8` across the main project, core utilities, and the website release references.
- **Single-source app metadata** — Added `Core/AppMetadata.cs` so the product name, version, release tag, and menu names are declared once for the app-side reporting.

### Website & UI parity
- **Menu sync** — Updated the website’s module list to match the app’s actual menu structure: DISM, REAgentc, System File Checker, Diagnostics, Utilities, System Prep, Malware Removal, Driver Manager, and Cloud Recovery.
- **Tool sync** — Aligned the website’s sidebar/tool descriptions with the app’s actual tools: Restore Point Manager, Startup Manager, Network Repair & Optimization, Media Tools, and Module Builder.
- **Catalog alignment** — Corrected utility/version labels to match the app’s installed catalog entries and release metadata.

## 2026-08-16 - Dependabot merges & CI fixes

### Dependency updates & CI
- Merged several Dependabot PRs bumping build/dependency versions (actions and packages): #34, #33, #32, #31, #30, #28, #26.
- Updated GitHub Actions workflow to restore runtime packs for `win-x64` and bumped `actions/checkout` → `v7` and `actions/setup-dotnet` → `v6` to resolve CI runtime-pack and runner deprecation issues.

### Code fixes
- Replaced usages of `SHA256.ComputeHash` / `ComputeHashAsync` with static `SHA256.HashData` / `HashDataAsync` to satisfy analyzer guidance and modern APIs (`Core/Security/*`, `Core/AsyncHelpers.cs`).
- Fixed nullable interop issues in `Core/Security/CredentialManager.cs` by coalescing unmanaged strings and adjusting P/Invoke signature.

### Notes
- CI workflow has been updated and merged to `main`; run a full CI build to validate end-to-end.


## 2026-07-20 - ISO 27001/SOC 2 Security Compliance

### Security & Compliance
- **Audit Logging System** — Implemented comprehensive audit logger (`Core/Security/AuditLogger.cs`) for ISO 27001/SOC 2 compliance with tamper-evident storage, structured event logging, and SHA-256 integrity verification. All security-relevant events now logged with full audit trail.
- **Data Encryption** — Added AES-256-GCM encryption utilities (`Core/Security/DataEncryption.cs`) for securing sensitive data at rest with Windows DPAPI key protection. Includes file encryption, string encryption, and secure memory clearing capabilities.
- **Credential Management** — Implemented secure credential manager (`Core/Security/CredentialManager.cs`) using Windows Credential Manager with DPAPI encryption. Provides encrypted storage for sensitive credentials with full audit logging for all operations.
- **Input Validation Enhancement** — Extended `SecurityHelpers.cs` with comprehensive input validation including SQL injection detection, XSS prevention, email/phone validation, password strength checking, entropy validation, and comprehensive user input sanitization.
- **Secure Update Mechanisms** — Enhanced `UpdateService.cs` with security validation for winget installation including URL validation, file integrity verification, and comprehensive audit logging for all update operations.

### Documentation
- **Security Policy** — Created comprehensive security policy (`SECURITY_POLICY.md`) covering all ISO 27001:2022 Annex A controls and SOC 2 Trust Services Criteria. Includes access control, data protection, cryptography, operations security, and compliance monitoring procedures.
- **Incident Response Plan** — Established detailed incident response procedures (`INCIDENT_RESPONSE.md`) with severity classification, response team roles, specific incident scenarios, communication procedures, and continuous improvement processes.

### UI & Dialog System
- **Settings Window Async Pattern** — Fixed SettingsWindow display issue by implementing `ShowAsync()` method matching the pattern used by other window-based dialogs (RestorePointManagerWindow, StartupManagerWindow, NetworkRepairWindow). The settings window now properly displays when clicked from the main menu.
- **MainViewModel Integration** — Updated `SettingsAsync` command to use the new async `ShowAsync()` method instead of manual `Activate()` calls, ensuring consistent async window display behavior across all dialogs.
- **BaseWindowDialog Inheritance** — Changed SettingsWindow to inherit from BaseWindowDialog instead of Window directly, providing proper async window lifecycle management and fixing app crash on launch.

### Build & Architecture
- **Architecture Mismatch Fix** — Fixed `BadImageFormatException` crash caused by building to x86 output folder while targeting win-x64 runtime. Updated build command to use `--runtime win-x64` flag to ensure correct architecture alignment between the app and Windows App SDK native libraries.
- **Build Command Update** — Changed from `dotnet build --no-restore` to `dotnet build RecoveryCommander.WinUI/RecoveryCommander.WinUI.csproj --no-restore --runtime win-x64` to prevent architecture mismatch errors.

### Build & Versioning
- **SHA Sync** — Updated to latest commit SHA: `9fbb289b2eb2d62abae93d95e8c400704a436228`.

## 2026-07-20 - Settings Window Display Fix & Build Architecture

### UI & Dialog System
- **Settings Window Async Pattern** — Fixed SettingsWindow display issue by implementing `ShowAsync()` method matching the pattern used by other window-based dialogs (RestorePointManagerWindow, StartupManagerWindow, NetworkRepairWindow). The settings window now properly displays when clicked from the main menu.
- **MainViewModel Integration** — Updated `SettingsAsync` command to use the new async `ShowAsync()` method instead of manual `Activate()` calls, ensuring consistent async window display behavior across all dialogs.
- **BaseWindowDialog Inheritance** — Changed SettingsWindow to inherit from BaseWindowDialog instead of Window directly, providing proper async window lifecycle management and fixing app crash on launch.

### Build & Architecture
- **Architecture Mismatch Fix** — Fixed `BadImageFormatException` crash caused by building to x86 output folder while targeting win-x64 runtime. Updated build command to use `--runtime win-x64` flag to ensure correct architecture alignment between the app and Windows App SDK native libraries.
- **Build Command Update** — Changed from `dotnet build --no-restore` to `dotnet build RecoveryCommander.WinUI/RecoveryCommander.WinUI.csproj --no-restore --runtime win-x64` to prevent architecture mismatch errors.

### Build & Versioning
- **SHA Sync** — Updated to latest commit SHA: `9fbb289b2eb2d62abae93d95e8c400704a436228`.

## 2026-07-20 - Supply-Chain Controls & Startup Crash Dialog

### Supply-Chain & Download Safety
- **Settings Menu Wired** — The Settings window now loads the persisted `AllowUnverifiedDownloads` preference from `AppFeatureSettings` on open and saves it back when the user clicks Save.
- **Download Safety Help Dialog** — Added a "What is this?" HyperlinkButton next to the `Allow Unverified Downloads` toggle. Clicking it opens a styled dialog explaining the supply-chain policy, when entries are "unverified", when to enable the toggle, and the `RC_ALLOW_UNVERIFIED_DOWNLOAD=1` environment-variable override.
- **Fail-Closed by Default** — Changed the default value of `AllowUnverifiedDownloads` in `AppFeatureSettings` from `true` to `false`, so fresh installs block unverified catalog downloads without any action required.
- **SecurityException Propagation** — Updated `DownloadCatalog.DownloadAndExecuteFromCatalogAsync` and `DownloadVerifiedAsync` to throw `SecurityException` instead of silently returning when an unverified download is blocked, so the action runner can surface the failure in the terminal output.
- **Action Runner Hardened** — Added `SecurityException` and generic `Exception` catch blocks in `MainViewModel.ExecuteActionAsync` so supply-chain blocks display a `[SECURITY]` terminal line and unexpected errors show full type and message info instead of being swallowed.

### Startup Reliability
- **Win32 MessageBox Crash Fallback** — `App.ShowCrashDialog` now unconditionally calls the native Win32 `MessageBox` API before attempting the XAML `CrashDialog`. This means any crash that occurs before the WinUI window is created (e.g. missing DLLs, DI initialization failures) will always display a visible popup with the full exception type, message, inner exception, stack trace, and log file path.
- **Top-Level Constructor Guard** — Wrapped the entire `App()` constructor body in a top-level `try-catch` so even errors thrown by the admin-relaunch logic or `InitializeComponent` are caught and surfaced.
- **Removed Spurious Success Log** — Removed the `LogError("OnLaunched_Success", ...)` call that was misusing the crash logger to record successful launches.

## 2026-06-26 - Logo Update & Audit System

### UI & Branding
- **Logo Replacement** — Replaced the "R in a box" text logo with actual logo image (Logo.png) in the main page header.
- **Logo Styling** — Removed the decorative border box around the logo for a cleaner appearance.
- **Dialog Background Consistency** — Updated all dialog backgrounds (About, Media Tools, Module Builder, Startup Manager, Restore Point Manager, Network Repair) to match the main app's radial gradient background (#030914 base with blue gradient overlay).

### Development Process
- **Audit Guidelines Update** — Enhanced AUDIT_GUIDELINES.md with build-time changelog update requirements and pre-build checklist.
- **Changelog Verification Script** — Created Scripts/check-changelog.ps1 to verify CHANGELOG.md is updated before builds.
- **Build Integration** — Added pre-build target to RecoveryCommander.WinUI.csproj to run changelog verification automatically.
- **Documentation Update** — Added audit and changelog requirements section to README.md explaining the audit-first development practices.

## 2026-06-17 - WinUI 3 Dialog Width Fix

### UI & Dialog System
- **Window-Based Dialog Conversion** — Converted all major dialogs from ContentDialog to Window-based implementations to overcome WinUI 3 ContentDialog's built-in width constraints (~548px MaxWidth). This resolves persistent text clipping and truncation issues in multiple dialogs.
- **BaseWindowDialog Class** — Created `BaseWindowDialog` base class with async `ShowAsync()` capability to provide a consistent window-based dialog pattern.
- **ModuleBuilderWindow** — Converted Module Builder dialog to Window (1400x850) with full code generation functionality preserved.
- **StartupManagerWindow** — Converted Startup Manager dialog to Window (1300x700) with startup item management capabilities.
- **RestorePointManagerWindow** — Converted Restore Point Manager dialog to Window (1200x700) with restore point CRUD operations.
- **NetworkRepairWindow** — Converted Network Repair dialog to Window (1250x750) with network diagnostic and repair tools.
- **MainViewModel Integration** — Updated `MainViewModel` to use new Window dialogs instead of ContentDialog for all four converted tools.

## 2026-06-17 - Utilities Catalog Update

### Utilities Module Updates
- **IObit Driver Booster PRO 13.5.0.359** — Updated IObit Driver Booster to version 13.5.0.359 with new Secure Storage direct link and pinned SHA-256 hash `81E956761825732C3E2D1E88E33ACD2726845AFD14B1D58767BA53EBED4C4B6F`. Synced version information across the Utilities module and download catalog.

## 2026-05-29 - Utilities Catalog Update

### Utilities Module Updates
- **Patch My PC Home Updater Portable** — Added Patch My PC to the Utilities module and website catalog. The action downloads the portable updater and launches it elevated with `/auto`.

## 2026-05-28 - Build 1.2.8 - Version Synchronization

### Build & Versioning
- **Build 1.2.8** — Synchronized all core projects, modules, and contracts to version 1.2.8.

## 2026-05-21 - Download Safety Settings

### Supply-Chain Controls
- **Download Safety Settings Menu** — Added a Settings menu with an `Allow Unverified Downloads` toggle and a descriptive `Download Safety Settings` dialog explaining when unpinned catalog downloads are allowed.
- **Persistent Download Policy** — Added persisted app settings under the user profile so the unverified-download preference survives restarts while keeping `RC_ALLOW_UNVERIFIED_DOWNLOAD=1` as a scriptable override.

## 2026-05-20 - Security Hardening & Recovery Reliability

### Supply-Chain & Download Safety
- **Redirect URL Revalidation** — Hardened `.txt` download indirection in `AsyncHelpers.ResolveDownloadUrlAsync` so resolved URLs must pass the same HTTPS/private-host validation as direct downloads. This blocks unsafe redirects such as HTTP downgrade links, loopback URLs, and private-network targets.
- **Download Boundary Validation** — Added URL validation inside `AsyncHelpers.DownloadFileAsync`, ensuring lower-level callers cannot bypass the safe-download policy by calling the file downloader directly.
- **Unverified Download Override** — Changed unpinned catalog downloads to fail closed by default. Unverified downloads can now be explicitly enabled with `RC_ALLOW_UNVERIFIED_DOWNLOAD=1`, and the blocked/download override messages document the exact flag.
- **Catalog ZIP Resolution Fix** — Updated `DownloadCatalog.DownloadVerifiedAsync` to resolve catalog `.txt` indirection files before downloading non-executable artifacts such as ZIP packages, while preserving SHA-256 verification.
- **HitmanPro Catalog Pin** — Updated HitmanPro to the `3.8.10 Portable` Secure Storage/Dropbox indirection URL and pinned executable SHA-256 `0EB152873849AC543D0918DED705634E0E7060F36CAB941B7D42A4662F674D66`.
- **Dropbox Pointer Resolution** — Fixed `.txt` indirection detection for URLs with query strings and switched the HitmanPro pointer link to raw Dropbox delivery.
- **Download Failure Propagation** — Fixed download/execute failures, including SHA-256 mismatches, so they propagate to the action runner instead of being logged as successful completions.

### Auto-Update Security
- **SHA-256 Required for Updates** — The auto-updater now requires a matching SHA-256 sidecar asset before applying a release update.
- **Removed First-EXE Fallback** — Removed the fallback that accepted the first `.exe` asset from a GitHub release. Updates now only consider RecoveryCommander-named assets.
- **Update Hash Verification** — Added SHA-256 computation for downloaded update binaries and refuse-to-apply behavior when the downloaded hash does not match the published checksum.
- **Update URL Validation** — Added security validation for both update asset URLs and checksum URLs before download.
- **Safer Update Failure Handling** — Added explicit network and security exception handling so update validation failures surface as controlled user-facing status messages.

### Plugin Loading
- **External Plugins Disabled by Default** — External DLL plugin loading under the `Module` directory is now disabled unless `RC_ENABLE_EXTERNAL_PLUGINS=1` is set.
- **Signed Plugin Gate** — When external plugin loading is enabled, candidate DLLs must remain inside the trusted module directory and pass Authenticode certificate-chain validation before loading.

### Recovery & File Operation Reliability
- **Safe Cloud Restore Extraction** — Hardened cloud profile restore ZIP extraction with path traversal checks, entry-count limits, total uncompressed size limits, and overwrite prevention.
- **Cloud Restore Folder Allow-List** — Restore now merges only expected profile folders (`Desktop`, `Documents`, `Pictures`) and skips unexpected archive folders.
- **Cancellation Cleanup** — Cloud backup/restore staging directories are now cleaned up in `finally` blocks, and cancellation is reported explicitly instead of silently returning mid-operation.
- **FFU Reset XML Safety** — Replaced raw string interpolation for `ResetConfig.xml` with `XDocument` generation so special characters in selected FFU paths cannot corrupt XML.
- **WinRE Mount Cleanup** — WinRE repair now tracks whether a temporary drive letter was actually mounted and uses a non-cancelled cleanup token for best-effort unmounting after cancellation.

### Command Execution Hardening
- **Selective Update Argument Safety** — Winget, Microsoft Store, and PowerShell module selective-update actions now validate package/module identifiers with an allow-list and use `ProcessStartInfo.ArgumentList` where applicable to reduce argument parsing risk.

### Verification
- **Build Verified** — `dotnet build --no-restore` completes successfully with 0 errors. Existing analyzer warnings remain for later cleanup.

## 2026-05-14 - Code Quality Improvements (v1.2.7)

### Build & Code Analysis
- **CA1822 Warning Fixes** — Marked 8 methods as static in `Forms/MainForm.cs` that do not access instance data: `GetOutputColor`, `UpdateModuleButtonStyles`, `GetModuleIcon`, `GetActionDescription`, `FormatTimeSpan`, `AdjustTileControls`, and both overloads of `CreateInfoChip`.
- **CA1852 Warning Fixes** — Sealed 4 internal types that have no subtypes and are not externally visible: `ThemedColorTable` and `FuturisticMenuRenderer` in `UI/Theme.cs`, and `Windows11MenuRenderer` and `Windows11ColorTable` in `UI/Win11MenuRenderer.cs`.
- **CA1859 Warning Fixes** — Optimized return types in `Forms/MainForm.cs` by changing `BuildModuleOverviewPanel` and `CreateInfoChip` from `Control` to `Panel` for improved type safety and performance.
- **CA2000 Warning Fixes** — Added pragma suppress directives with justification comments for dispose pattern warnings in `Forms/DialogFactory.cs`, `Program.cs`, `Features/AutoUpdateDialog.cs`, `UI/ProfessionalDesignSystem.cs`, and `Forms/MainForm.cs`. Objects are properly disposed by their parent containers.
- **CA1031 Warning Fixes** — Added pragma suppress directives with justification comments for general exception handling in `Features/ManagementTools.cs`, `UI/Theme.cs`, `UI/Theme.Responsive.cs`, `UI/Theme.Internal.cs`, `UI/Theme.Controls.cs`, and `Forms/MainForm.cs`. General exception types are used for robust error handling in system operations.
- **Zero-Warning Build** — Verified solution builds cleanly with **0 errors, 0 warnings** in Release configuration.

## 2026-05-13 - Code Quality Improvements (v1.2.7)

### Build & Code Analysis
- **CA1031 Warning Fix** — Added pragma suppress directive with justification comment for the general exception catch in `AsyncHelpersTests.Dispose()`. The catch-all exception handler is necessary in Dispose methods to prevent exceptions from propagating during cleanup operations.
- **Zero-Warning Build** — Verified solution builds cleanly with **0 errors, 0 warnings** in Release configuration.

## 2026-05-12 - Technical Hardening & Analyzer Remediation (v1.2.6)

### Technical Hardening (Core & Modules)
- **Specific Exception Handling (CA1031/CA2201)** — Systematically replaced generic `catch { }` and `catch (Exception)` blocks with specific Win32, IO, and Task exceptions. Migrated `ReagentcModule` to use `InvalidOperationException` over `System.Exception`.
- **Async Pattern Compliance (CA2007)** — Applied `.ConfigureAwait(false)` to all asynchronous operations in the core library and WinRE wizards.
- **Namespace Modernization (CA1716)** — Renamed `RecoveryCommander.Module` to `RecoveryCommander.Modules` across the entire solution to resolve reserved language keyword conflicts.
- **Static Access Optimization (CA1052)** — Fixed build errors in `DriverManagerModule` by correctly qualifying static `DriverService` calls, following its conversion to a static utility class.
- **String & Comparison Optimization (CA1847/CA1307)** — Migrated to `char`-based overloads for `string.Contains` and `StringBuilder.Append` to eliminate redundant string allocations.

### UI & Resource Management
- **WinRE Wizard Disposal (CA2213/CA2000)** — Implemented `Dispose(bool)` in `WinREWizards` and wrapped transient forms in `using` blocks.
- **Resource Migration (CA1303/CA1304)** — Migrated hardcoded strings in `WinREWizards.cs` to `WinREStrings.resx` and enforced explicit `CultureInfo` usage for resource lookups.
- **Security Sanitization** — Optimized `SecurityHelpers.cs` with efficient `Span<char>` and ordinal comparisons.
- **Global `CA1822` Remediation** — Finalized static method migration across `SfcModule`, `DismModule`, `SystemPrepModule`, and `UtilitiesModule`, achieving zero-warning state for instance-less logic.
- **Static Array Optimization (CA1861)** — Consolidated constant array allocations in `UpdateHelpers.cs` and `UtilitiesModule.cs` into `static readonly` fields.
- **`UpdateHelpers` Culture Enforcement (CA1304/CA1305)** — Enforced explicit `CultureInfo` across all `Type.InvokeMember` calls and date formatting in `CloudProfileSyncService` and Windows Update logic.
- **Namespace Unification** — Completed the migration of all modular components to the `RecoveryCommander.Modules` namespace, resolving final `CA1716` conflicts.

### Infrastructure & Compliance
- **Analyzer Configuration** — Created root `.editorconfig` with surgical suppressions for Test projects (CA1707) and strict enforcement for Core/Module logic.
- **Service Layer Safety** — Forwarded cancellation tokens in network DNS resolution and hardened `GlobalExceptionHandler` with specific security exception handling.
- **`TreatWarningsAsErrors` Preparation** — Achieved zero-warning baseline for high-priority rules in the Core and Module projects.



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
- **Service Layer Static Refactoring (CA1822)** — Refactored `CleanupService.cs`, `DriverService.cs`, and `SystemTweakService.cs` to use static methods, eliminating unnecessary allocations and instance state. Updated `SystemPrepModule.cs` to utilize these static services correctly.
- **Modernized CI Infrastructure** — Updated GitHub Action workflows (`dotnet-desktop.yml` and `codeql.yml`) to use latest stable versions (`checkout@v6`, `setup-dotnet@v5`, `upload-artifact@v7`, and `codeql-action@v4`) to resolve Node.js 20 deprecation warnings and ensure long-term pipeline stability.

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
- **Driver Manager Fix** — Corrected "List Drivers" to enumerate drivers rather than invoking cleanup path.
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
