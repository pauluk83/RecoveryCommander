# RecoveryCommander — v1.2.6

A comprehensive Windows system recovery and maintenance tool with a modern Windows 11-styled interface. RecoveryCommander provides modular system utilities, diagnostic tools, and recovery operations through an extensible plugin architecture.

---

## 🚀 Features

- **Modern UI**: Windows 11-styled interface with dark/light theme support
- **Advanced Boot Media Creator**: Driver injection and WinRE integration for recovery drives
- **Driver Manager**: Dedicated module for third-party driver backup, restoration, and cleanup
- **Cloud Recovery**: One-click access to Windows Cloud Reset and profile backup/sync
- **Modular Architecture**: Extensible plugin system for recovery modules
- **System Diagnostics**: SFC, DISM, memory diagnostics, and disk checks
- **System Utilities**: Activation tools, network optimization, cleanup utilities
- **Real-time Progress**: Live progress tracking with clean output console
- **Thread-Safe Operations**: Robust threading with proper disposal handling

---

## 🔗 Links & Resources

- **GitHub Repository**: [pauluk83/RecoveryCommander](https://github.com/pauluk83/RecoveryCommander)
- **Official Website**: [RecoveryCommander Portal](https://pauluk83.github.io/RecoveryCommander/)

---

## 📊 Project Status & Recent Updates

*Current Status (Updated 2026-05-12) - Build 1.2.6*

- ✅ **Security**: Centralized `DownloadCatalog` with explicit supply-chain policy and SHA-256 pinning hooks.
- ✅ **Reliability**: Rolling file logs under `%LOCALAPPDATA%\RecoveryCommander\logs` + safer global exception handling.
- ✅ **Quality**: Added xUnit test project (`Tests/RecoveryCommander.Tests`) and CI `dotnet test` coverage upload.
- ✅ **UX/A11y**: Focus indicators on custom controls, high-contrast palette snapping, scalable toasts + Esc dismiss.
- ✅ **Maintainability**: Diagnostics command metadata unified; key refactors extracted from `MainForm` monolith.

---

## 📁 Project Structure

Project Manifest Overview

```text
RecoveryCommander/
├── 📁 Forms/                    # UI Layer (Modern Windows 11 Dialogs)
├── 📁 UI/                       # Custom Controls & Theme Engine
├── 📁 Core/                     # Shared Services & Core Utilities
├── 📁 Module/                   # Pluggable Recovery Modules
├── 📁 Features/                 # Advanced Feature Implementations
├── 📁 RecoveryCommander.Contracts/ # Public API & Interface Definitions
├── 📁 Scripts/                  # Build Tools & Maintenance Scripts
├── 📁 Resources/                # Assets, Icons & Documentation
├── 🔧 RecoveryCommander.sln     # Visual Studio Solution
└── 🔧 RecoveryCommander.csproj  # Main Project Definition
```

---

## 🛠️ Recovery Modules

RecoveryCommander ships with 9 specialized modules for comprehensive system care:

- **Cloud Recovery**: Initiate Windows Cloud Resets and manage profile sync.
- **Diagnostics**: Run deep system health checks and generate reports.
- **DISM Operations**: Deployment Image Servicing and Management repairs.
- **Driver Manager**: Backup, restore, and optimize third-party drivers.
- **Malware Removal**: Access scanners like Emsisoft, KVRT, and AdwCleaner.
- **REAgentC**: Manage the Windows Recovery Environment (WinRE).
- **System File Checker (SFC)**: Verify and repair system file integrity.
- **System Prep**: Automated system maintenance and optimization tasks.
- **Utilities**: Bulk installers, activation tools, and performance utilities.

---

## 🏗️ Architecture Overview

### Module System
RecoveryCommander uses a plugin-based architecture where modules implement the `IRecoveryModule` interface:

```csharp
public interface IRecoveryModule
{
    string Name { get; }
    string Version { get; }
    string HealthStatus { get; }
    string BuildInfo { get; }
    IEnumerable<ModuleAction> Actions { get; }
    
    Task ExecuteActionAsync(string actionName, 
                          IProgress<ProgressReport> progress, 
                          Action<string> reportOutput, 
                          IDialogService dialogService,
                          CancellationToken cancellationToken);
}
```

### UI Architecture
- **Fluent Design**: Native Windows 11 Mica and Windows 10 Acrylic support.
- **Deterministic Layout**: TableLayoutPanel-based stacking for consistent scaling.
- **Theme Engine**: Real-time system theme detection and propagation.
- **Progress System**: Milestone-based reporting (0%, 25%, 50%, 75%, 100%) for cleaner logs.

### Threading Model
- **UI Thread**: Dedicated to responsive user interactions.
- **Background Workers**: Operations execute on separate threads with safe progress callbacks.
- **Disposal Safety**: Comprehensive `IsDisposed` guards prevent crashes during window closing.

---

## 🚀 Getting Started

### Prerequisites
- **OS**: Windows 10 or 11
- **Runtime**: .NET 9.0 or later
- **IDE**: Visual Studio 2022 (recommended)
- **Privileges**: Administrator rights required for most recovery operations.

### Building from Source
1. Clone the repository: `git clone https://github.com/pauluk83/RecoveryCommander.git`
2. Open `RecoveryCommander.sln` in Visual Studio.
3. Restore NuGet packages.
4. Build the solution (**Ctrl+Shift+B**).
5. Run the application (**F5**).

---

## 🔧 Module Development

1. Use the built-in **Module Builder** tool to design your action flow.
2. Implement the `IRecoveryModule` interface in your C# project.
3. Reference `RecoveryCommander.Contracts` for standardized communication.
4. Drop your compiled DLL into the `Module/` directory.
5. Restart the application to auto-discover the new module.

---

## ⚖️ Third-Party Disclaimer

This project incorporates third-party applications, which remain the property of their respective owners. All trademarks and copyrights belong to their owners. We express our sincere gratitude to these developers for making their software available for the community.

---

## 📄 License

This project is developed for system recovery and maintenance purposes. Please ensure compliance with Windows licensing terms when using system modification features.

---

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:
- Adhere to the established Windows 11 design language.
- Ensure all new features are properly documented.
- Maintain thread safety and proper disposal patterns.
- Test changes across multiple Windows versions.

---

**RecoveryCommander** — *Professional Windows System Recovery and Maintenance Tool*  
Optimized, Consolidated, and Continuously Improved.
