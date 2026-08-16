# RecoveryCommander — Architectural & Design Notes

Professional Windows System Recovery and Maintenance Tool.

---

## 🧱 Core Principles & Workflow

RecoveryCommander follows a strict philosophy of explicit control and modularity:

- **Designer-Free UI**: Avoids auto-generated `.Designer.cs` files. All layouts, controls, and event bindings are written directly in C# for maximum transparency and precise control.
- **Plugin Architecture**: Each module implements the `IRecoveryModule` contract. The application dynamically discovers and loads these modules via reflection at runtime.
- **Audit-First Development**: Every source file carries a permanent audit header and changelog block. Changes are mirrored in the project-wide `CHANGELOG.md` to maintain a perfect traceability trail.
- **Clean Code & Archive Management**: Redundant or deprecated code is aggressively purged or moved to the `\Archive` folder (ignored during builds) to ensure zero-waste deployments.
- **High-Performance Backgrounding**: All recovery operations run on background threads with safe progress callbacks, ensuring the UI remains responsive even during heavy system repairs.

---

## 🔐 Download Supply-Chain Policy (v1.2.6)

All third-party tool downloads are governed by a central catalog:

- **Single source of truth**: `Core/DownloadCatalog.cs` contains canonical IDs, URLs, output filenames, vendor/version metadata, and optional SHA-256 values.
- **HTTPS required**: URLs must be HTTPS; insecure or local/private targets are rejected by `SecurityHelpers`.
- **Hash verification path**: `DownloadCatalog.DownloadVerifiedAsync(...)` verifies SHA-256 whenever a catalog entry has `Sha256`.
- **No silent trust gaps**: if an entry has no hash yet, runtime emits a clear `[supply-chain] WARN` message before download/execute.
- **Update rule**: when bumping a pinned binary, update `Version` and `Sha256` together in the catalog (never change URL only).

This policy lets the app remain operational today while steadily moving every binary to cryptographic pinning.

---

## 🎨 UI & UX Design Language

The application is built to feel native to **Windows 11**:

- **Aesthetics**: Rounded corners, fluent spacing, and consistent dark/light mode theming based on the **Mica** design system.
- **Typography**: Uses **Inter** and **Outfit** typefaces for high readability.
- **Main Interface**:
    - **Sidebar**: Lists available recovery modules with metadata (Build Date/Version).
    - **Action Pane**: Dynamically populates with buttons or checkboxes based on the selected module.
    - **Output Log**: A native, interactive `RichTextBox` for real-time command feedback with full text selection and clipboard support.
- **Standard Menus**:
    - **File** → Exit
    - **Help** → **About**: Displays version info, author (**Zane Stanton**), and licensing.
    - **Help** → **Changelog**: Opens the interactive Markdown changelog viewer.
    - **Help** → **README**: Opens the comprehensive project documentation.

---

## 🖼️ Layout Blueprint (ASCII)

```text
+------------------------------------------------------------------+
| [ Menu: File  Help ]                                 [ _ ] [ □ ] [ X ] |
+------------------------------------------------------------------+
|  MODULES (Sidebar)         |  ACTIONS (Dynamic Pane)             |
|  ------------------------  |  ---------------------------------  |
|  [ Cloud Recovery ]        |  (Standard Module Actions)          |
|  [ Diagnostics    ]        |  [ > Run Action A ]  [ > Run Action B ] |
|  [ DISM Operations]        |  [ > Run Action C ]  [ > Run Action D ] |
|  [ Driver Manager ]        |                                     |
|  [ Malware Removal]        |  (Special: System Prep Layout)      |
|  [ REAgentC       ]        |  Maintenance       Optimizations    |
|  [ SFC Verification]       |  [✓] Task 1        [✓] Task 2       |
|  [ System Prep    ]        |  [✓] Task 3        [✓] Task 4       |
|  [ Utilities      ]        |  [ Untick All ]    [ Run Selected ] |
|                            |                                     |
+------------------------------------------------------------------+
|  LIVE OUTPUT CONSOLE (Interactive RichText Feed)                 |
|  > Processing system health check...                             |
|  > Done. [Secure Content Delivery] masked.                       |
|                                                                  |
+------------------------------------------------------------------+
|  [ Progress Bar: 75% ----------------------------]  [ Cancel ]   |
+------------------------------------------------------------------+
|  STATUS: Connected | CPU: 12% | RAM: 4.2GB | Ver: 1.2.8          |
+------------------------------------------------------------------+
```

---

## 🛠️ Technical Specifications

- **Runtime**: .NET 9.0 (Windows Forms)
- **Host**: Secure Storage Direct-Download Infrastructure (for secure, high-speed resource delivery)
- **Theming Engine**: Dynamic Registry-based theme detection with live-switching support.
- **Downloader**: Resilient, async-based system with security challenge detection and path sanitization.
- **Security**: 
    - **ZipSlip Protection**: Post-extraction path validation.
    - **Shell Injection Hardening**: Direct process execution without shell expansion.
    - **Privacy Masking**: Real-time scrubbing of sensitive infrastructure IDs from logs.

---

## 🪟 WinUI 3 Dialog Architecture (v1.2.8)

### Window-Based Dialog System
To overcome WinUI 3 ContentDialog's built-in width constraints (~548px MaxWidth), all major dialogs have been converted to Window-based implementations:

- **BaseWindowDialog**: Base class providing async `ShowAsync()` capability and window centering logic.
- **ModuleBuilderWindow**: 1400x850 window for generating C# module scaffold code.
- **StartupManagerWindow**: 1300x700 window for managing Windows startup programs.
- **RestorePointManagerWindow**: 1200x700 window for system restore point management.
- **NetworkRepairWindow**: 1250x750 window for network diagnostic and repair tools.

### Dialog System Benefits
- **No Width Limitations**: Window-based dialogs bypass ContentDialog's hardcoded MaxWidth.
- **Async Show Pattern**: Consistent `ShowAsync()` method using `TaskCompletionSource`.
- **Centering**: Automatic centering on the main window for consistent UX.
- **Lifecycle Events**: Proper handling of Loaded/Activated events for async initialization.

---

## 🔒 Development Philosophy

- **No Regression**: Updates are cumulative; features are optimized, never removed without a replacement.
- **Autonomous Recovery**: Designed to run as a standalone toolkit with minimal external dependencies.
- **Transparency**: Open Source architecture allowing for deep auditing of all recovery mechanisms.
- **Rapid Prototyping**: Modular system allows for adding new recovery modules in minutes via the `ModuleBuilder` tool.

---

**RecoveryCommander** — *The Ultimate Windows System Repair Infrastructure*
