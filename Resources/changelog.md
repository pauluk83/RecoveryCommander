# RecoveryCommander Changelog

## 2026-03-07 - System Prep Modernization & Update Engine Overhaul

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

---

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
#### ThemeManager Integration
- **Centralized theme management** - All forms now use ThemeManager for consistent light/dark mode support
- **Automatic theme detection** - System automatically detects Windows light/dark mode preference
- **Dynamic theme switching** - Real-time theme updates when Windows theme changes
- **Consistent color palette** - Unified color scheme across all UI components

#### Forms Updated with Modern Theming
- **WorkflowDesignerForm.cs** - Complete theming overhaul with ThemeManager integration
- **MainForm.cs** - Updated acrylic effects and menu renderers to use theme colors
- **TestRunnerForm.cs** - Full theming integration with proper initialization
- **OptimizationDashboard.cs** - Updated Fluent Design components for theme consistency
- **NetworkDiagnosticsSuite.cs** - Comprehensive theming for all ListView and TabControl elements
- **RegistryHealthScanner.cs** - Consistent theming throughout interface
- **RestorePointManager.cs** - Complete theming integration with proper error handling
- **CommandEditorDialog.cs** - Modern theming with ThemeManager support

#### UI Components Enhanced
- **ModernControls.cs** - Comprehensive update of all custom controls to use ThemeManager colors
- **EnhancedProgressBar.cs** - Added ThemeManager support with dynamic color calculation
- **All hardcoded colors replaced** - Eliminated Color.FromArgb() calls in favor of ThemeManager properties

### Theme System Features
#### Light Mode Support
- Light gray backgrounds (248, 248, 248)
- Dark text for readability (30, 30, 30)
- Light selection colors (200, 220, 240)
- Subtle borders (200, 200, 200)

#### Dark Mode Support
- Dark gray backgrounds (32, 32, 32)
- Light text for contrast (220, 220, 220)
- Dark selection colors (60, 80, 100)
- Dark borders (64, 64, 64)

#### Consistent Accent Colors
- Unified blue accent (0, 120, 212) across both themes
- Proper contrast ratios for accessibility
- Dynamic color calculation for gradients and effects

### Technical Improvements
#### Code Quality
- **Proper using statements** - Added `using RecoveryCommander.UI;` where needed
- **Theme initialization** - Consistent theme setup in all form constructors
- **Error handling** - Improved exception handling in theme-related code
- **Performance optimization** - Efficient theme application without UI flicker

#### Architecture Benefits
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
#### System Maintenance:
- System File Check (SFC /scannow)
- DISM Health Check (checkhealth + restorehealth)
- Memory Diagnostic (mdsched scheduling)
- Disk Check (chkdsk with repair options)

#### System Cleanup:
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
#### CoreModules: Reduced from 13 to 4 files (69% reduction)
- Consolidated AuditLogger, SettingsManager, CommandModule into CoreUtilities.cs
- Enhanced functionality with better accessors and helper methods
- Removed 7 empty/unused files (ChangelogManager, VersionManager, SendEngager, etc.)

#### Scripts: Reduced from 22 to 13 files (41% reduction)
- Created ModuleValidationTools.ps1 (consolidated 3 validation scripts)
- Created ProjectCleanupTools.ps1 (consolidated 3 cleanup scripts)
- Renamed all scripts with descriptive names for better clarity
- Removed broken windows install builder module

#### Forms: Optimized with unified modern controls
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
#### ReagentcModule
- Reordered actions so Info is shown first
- Added Enable/Disable actions for REAgentc and implemented handlers to run reagentc /enable and /disable
- Added CreateCustomRecoveryImage action (SaveFileDialog) to capture a WIM using DISM
- Added CreateAndSetCustomRecoveryImage action to capture a WIM and set it as the recovery image via reagentc (includes file picker)
- Removed the header entry from the actions list and annotated module as Windows-only ([SupportedOSPlatform("windows")]) to address platform compatibility warnings

#### DismModule
- Fixed unreachable code and ensured CheckHealth action is present and handled
- Added CreateCustomImage action earlier (file picker) and ensured DISM invocation is consistent

#### SystemPrepModule & SfcModule
- Improved UI integration and fixed theming for System Prep checklist display

### UI / Theming / UX
#### MainForm
- Centralized dark theme color (DarkBackground) and improved theme application so modules and controls update when theme changes
- Ensured module containers, header buttons, action buttons and System Prep CheckedListBox respect light/dark themes
- Reduced menu and bottom area footprint so module scroll area is not obscured; MenuStrip docks to Top
- Added a custom ThemedProgressBar control that renders a green progress fill (in-progress and complete) and matches app theme
- Fixed multiple visual inconsistencies and ensured ApplyThemeRecursive does not overwrite module-specific BackColor for container controls

#### MenuManager
- Replaced Assembly.Location usage with AppContext.BaseDirectory and safer assembly attribute lookup to work correctly for single-file publishing
- Improved the 'Check for updates' flow: compares semantic versions, offers to open releases page, or download the first suitable release asset and run it

### Build / Packaging / Scripts
#### RecoveryCommander.csproj
- Disabled PublishReadyToRun to avoid crossgen2 duplicate-input errors during publish
- Added targets to copy module binaries into the output Module folder after build

#### Scripts
- Added Scripts/Publish-SingleFile.ps1 to publish a single-file self-contained bundle (includes option to disable R2R)
- Added Scripts/Clean-DebugFolders.ps1 to remove Debug folders under bin/obj before builds

### Reliability / Code Quality
#### ModuleLoader
- Robust type loading with ReflectionTypeLoadException handling; null-safe filtering of types and safer instance creation/logging

#### Nullability & warnings
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

#### Notes
- The restore availability check relies on the `winget` binary being present and accessible on PATH. If winget is not installed the dialog will mark statuses as Unknown
- Some installed programs are not available via winget or require license acceptance; these are shown as Missing or requiring user agreement during install
