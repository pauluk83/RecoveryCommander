# RecoveryCommander

A comprehensive Windows system recovery and maintenance tool with a modern Windows 11-styled interface. RecoveryCommander provides modular system utilities, diagnostic tools, and recovery operations through an extensible plugin architecture.

## 🚀 Features

- **Modern UI**: Windows 11-styled interface with dark/light theme support
- **Modular Architecture**: Extensible plugin system for recovery modules
- **System Diagnostics**: SFC, DISM, memory diagnostics, and disk checks
- **System Utilities**: Activation tools, network optimization, cleanup utilities
- **Real-time Progress**: Live progress tracking with clean output console
- **Thread-Safe Operations**: Robust threading with proper disposal handling

## 📊 Project Status & Recent Updates

### Current Status (Updated 2026-03-11)
- ✅ **New Feature**: Complete PBR Setup Wizard with step-by-step Push-Button Reset configuration
- ✅ **Enhanced**: Automatic ADK detection and download assistance for ScanState tools
- ✅ **Fixed**: All UI theming issues with proper button text color support
- ✅ **Resolved**: Zero build warnings across all modules with platform compatibility fixes
- ✅ **Streamlined**: Removed redundant ScanState action in favor of comprehensive wizard
- ✅ **Modernized**: Professional wizard interface with full theme integration
- ✅ **Optimized**: Clean build process with enhanced error handling

## 📁 Project Structure

### Project Manifest Overview
```
RecoveryCommander/
├── 📁 Forms/                    # UI Layer (18 files)
├── 📁 CoreModules/              # Shared Services (4 files - consolidated)
├── 📁 Module/                   # Recovery Modules (5 modules)
├── 📁 RecoveryCommander.Contracts/ # Public API (35 files)
├── 📁 Scripts/                  # Build Tools (13 files - optimized)
├── 📁 Resources/                # Assets & Documentation
├── 📁 Properties/               # Project Metadata (6 files)
├── 🔧 RecoveryCommander.sln     # Solution File
├── 🔧 RecoveryCommander.csproj  # Main Project
└── 📖 README.md                 # This Documentation
```

### Core Application Files

#### `/Forms/` - User Interface Layer
- **`MainForm.cs`** - Main application window with modern UI, theme support, and module management
  - Implements Windows 11 design language with rounded corners and modern colors
  - Handles module loading, execution, and progress reporting
  - Features thread-safe progress updates and disposal handling
  - Supports real-time theme switching based on system preferences

- **`ModernControls.cs`** - Custom Windows 11-styled controls
  - `ModernButton` - Styled buttons with hover/press effects (Primary, Secondary, Accent, Danger)
  - `ModernListBox` - Custom-drawn list with hover effects and selection indicators
  - `ModernFlowPanel` - Container with modern spacing and layout
  - `RoundedProgressBar` - Modern progress bar with theme support

- **`ModernTextBox.cs`** - Modern text input control with rounded borders and focus states

- **`RoundedControls.cs`** - Additional rounded UI components for consistent styling

- **`ThemeManager.cs`** - Comprehensive theme management system
  - Detects system theme from registry (`HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`)
  - Subscribes to `SystemEvents.UserPreferenceChanged` for live theme switching
  - Propagates theme changes to all controls recursively

- **`ThemeHelper.cs`** - Theme application utilities and color management

- **`Win11Theme.cs`** - Windows 11 color palette and styling constants

- **`Win11MessageBox.cs`** - Modern message box implementation

- **`MenuManager.cs`** - Application menu system with theme support

- **`ModuleBuilder.cs`** - GUI tool for creating custom recovery modules
  - Visual module designer with command input, file selection, and URL downloads
  - Supports .txt to .exe renaming and admin launch capabilities
  - Module registration and testing functionality

- **`NetworkDnsSelectorDialog.cs`** - Network configuration dialog for DNS and Wi-Fi settings

- **`BootMediaCreator.cs`** - Boot media creation utilities

- **`CommandEditorDialog.cs`** - Command editing interface

- **`ConPtyRunner.cs`** - Console pseudo-terminal runner for command execution

- **`DiagnosticOverlay.cs`** - System diagnostic overlay interface

- **`DiagnosticsConsole.cs`** - Diagnostic output console

### Core Modules

#### `/CoreModules/` - Shared Services and Utilities (Consolidated)
- **`CoreUtilities.cs`** - 🆕 **Consolidated utilities** including:
  - `AuditLogger` - Audit logging with directory management
  - `SettingsManager` - Enhanced settings management with proper accessors  
  - `CommandModule` - Abstract base class with public properties
- **`CommandRunner.cs`** - Async command execution with cancellation support
- **`PlatformCompatibility.cs`** - Windows platform compatibility declarations

> **Note**: Previously scattered across 13 files, now consolidated into 4 files (69% reduction) while enhancing functionality.

### Recovery Modules

#### `/Module/` - Pluggable Recovery Modules

##### **DismModule** - DISM Operations
- **`DismModule.cs`** - Deployment Image Servicing and Management operations
  - System image health checks and repairs
  - Component store cleanup and optimization
  - Windows feature management

##### **ReagentcModule** - Recovery Environment
- **`ReagentcModule.cs`** - Windows Recovery Environment (WinRE) management
  - Recovery environment configuration
  - Boot configuration management
  - Recovery partition operations

##### **SFCModule** - System File Checker
- **`SfcModule.cs`** - System File Checker operations
  - System file integrity verification
  - Corrupted file detection and repair
  - System component validation

##### **UtilitiesModule** - System Utilities
- **`UtilitiesModule.cs`** - Comprehensive system utility collection
  - **Windows Activation**: Automated activation using trusted scripts
  - **Office Installation**: Office 2024 and Click-to-Run deployment
  - **Activation Backup/Restore**: License state management
  - **Christitus Tech Tools**: System optimization scripts
  - **Network Reset & Optimization**: DNS configuration and network optimization
  - **Portable Tools**: CCleaner, CompactGUI, Defragger, Ninite, Rufus integration

##### **SystemPrepModule** - System Preparation Tools
- **`SystemPrepModule.cs`** - System maintenance and preparation utilities
  - **System File Check**: SFC /scannow operations
  - **DISM Health Check**: Image health restoration
  - **Memory Diagnostic**: Windows Memory Diagnostic scheduling
  - **Disk Check**: CHKDSK operations with repair
  - **Disk Cleanup**: Automated disk space cleanup
  - **Temp Files Cleanup**: Temporary file removal
  - **Registry Cleanup**: Safe registry maintenance (manual intervention)
  - **System Optimization**: Power, boot, and network optimizations

### Contracts and Interfaces

#### `/RecoveryCommander.Contracts/` - Public API Definitions
- **`IRecoveryModule.cs`** - Core module interface defining:
  - Module metadata (Name, Version, HealthStatus, BuildInfo)
  - Action enumeration and execution
  - Progress reporting and cancellation support
- **`ModuleAction.cs`** - Action definition with display names and header support

### Core Infrastructure

#### Root Level Files
- **`Program.cs`** - Application entry point with Windows Forms initialization
- **`ModuleLoader.cs`** - Dynamic module discovery and loading system
  - Searches multiple directories for module DLLs
  - Uses reflection to instantiate modules
  - Provides adapter pattern for version compatibility
- **`ReflectionRecoveryModuleAdapter.cs`** - Compatibility adapter for module loading
- **`App.config`** - Application configuration
- **`app.manifest`** - Windows application manifest with UAC settings

### Build and Deployment

#### `/Scripts/` - Build and Maintenance Scripts (Optimized)
- **🆕 Consolidated Tools**:
  - `ModuleValidationTools.ps1` - Comprehensive module validation with interactive selection
  - `ProjectCleanupTools.ps1` - All-in-one cleanup utility (debug folders, .resx, artifacts)

- **📝 Renamed for Clarity**:
  - `ChangelogGenerator.ps1` - Automated changelog generation (was `changelog.ps1`)
  - `PackageBackupTool.ps1` - Winget package backup utility (was `winget-backup.ps1`)
  - `ErrorAnalyzer.ps1` - Context error scanning (was `scan-context-errors.ps1`)
  - `ProjectUpdater.ps1` - Project update functionality (was `update.ps1`)
  - `ModuleSearchTool.ps1` - Module reference search (was `mosmatch.ps1`)
  - `SingleFilePublisher.ps1` - Single-file deployment (was `Publish-SingleFile.ps1`)
  - `ManifestGenerator.ps1` - Project manifest generation (was `ProjectManifest.ps1`)
  - `NamespaceFixTool.ps1` - Namespace consistency fixes (was `FixNamespaceMismatch.ps1`)

- **System Tools**:
  - `SystemOptimizer.bat` - System optimization and cleanup (was `fresh.bat`)

> **Note**: Reduced from 22 files to 13 files (41% reduction) with enhanced functionality and descriptive names.

### Resources and Documentation

#### `/Resources/` - Application Resources
- System icons and embedded assets
- Application resources and configuration files

> **Note**: Legacy documentation has been consolidated into this main README.md file.

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
    
    void ExecuteAction(string actionName, 
                      Action<int, string> reportProgress, 
                      Action<string> reportOutput, 
                      Func<bool> isCancelled);
}
```

### UI Architecture
- **TableLayoutPanel-based layout** for deterministic vertical stacking
- **Theme-aware controls** that respond to system theme changes
- **Progress reporting system** with real-time updates and milestone logging
- **Thread-safe operations** with proper disposal handling

### Threading Model
- **UI Thread**: Handles all UI operations and user interactions
- **Background Threads**: Execute module operations with progress callbacks
- **Thread-Safe Progress Updates**: Uses `BeginInvoke` with disposal guards
- **Cancellation Support**: Cooperative cancellation through `Func<bool> isCancelled`

## 🛠️ Recent Optimizations & Consolidations

### Major File Consolidations
- **📁 CoreModules**: Reduced from 13 to 4 files (69% reduction)
  - Consolidated `AuditLogger`, `SettingsManager`, `CommandModule` into `CoreUtilities.cs`
  - Enhanced functionality with better accessors and helper methods
- **📁 Scripts**: Reduced from 22 to 13 files (41% reduction)
  - Created `ModuleValidationTools.ps1` (consolidated 3 validation scripts)
  - Created `ProjectCleanupTools.ps1` (consolidated 3 cleanup scripts)
  - Renamed all scripts with descriptive names
- **📁 Forms**: Optimized with unified modern controls
  - Consolidated `ModernTextBox` into `ModernControls.cs`
  - Removed 6 empty/redundant files
- **🔧 Build System**: Eliminated duplicate project compilation
  - Removed redundant `RecoveryCommander.Modules.csproj`
  - Fixed duplicate DLL generation issue

### Performance Improvements
- **Removed 30+ redundant files** including duplicates and empty classes
- **Eliminated 1.5MB+ of build artifacts** and temporary files
- **Consolidated duplicate implementations** (NetworkDnsSelectorDialog, PlatformCompatibility)
- **Fixed broken SystemPrepModule** with complete implementation
- **Streamlined project structure** for faster builds and better maintainability

### Threading & Stability Fixes
- **Enhanced disposal safety** with comprehensive `IsDisposed` checks
- **Protected cross-thread operations** with try-catch blocks
- **Proper task continuation handling** with disposal guards
- **Graceful operation cancellation** on form disposal
- **Fixed InvalidOperationException** during form disposal

### UI & UX Improvements
- **Progress reporting optimization** - Shows milestones (0%, 25%, 50%, 75%, 100%) instead of spam
- **Real-time progress bar updates** maintained for visual feedback
- **Enhanced status bar** with system info (CPU, RAM, OS, time)
- **Improved cancel functionality** with visual feedback
- **Clean output console** with meaningful status messages
- **Thread-safe UI operations** with exception handling

## 🚀 Getting Started

### Prerequisites
- Windows 10/11
- .NET 10.0 or later
- Visual Studio 2022 or VS Code
- Administrator privileges (for system operations)

### Building
1. Clone the repository
2. Open `RecoveryCommander.sln` in Visual Studio
3. Build the solution (Ctrl+Shift+B)
4. Run the application (F5)

### Module Development
1. Use the built-in **Module Builder** tool
2. Implement `IRecoveryModule` interface
3. Place module DLL in the `Module` directory
4. Restart RecoveryCommander to load new modules

## 🔧 Configuration

### Theme System
- Automatically detects system theme from Windows registry
- Supports live theme switching without restart
- Custom theme colors defined in `Win11Theme.cs`

### Module Loading
- Modules are discovered from multiple directories:
  - `Module/` (primary)
  - `Modules/` (secondary)
  - Build output directories
- Uses reflection for dynamic loading with compatibility adapters

## 📝 Development Guidelines

### Code Standards
- Include audit markers and changelog blocks in new files
- Use `RecoveryCommander.Contracts` for public interfaces
- Follow Windows 11 design guidelines for UI components
- Implement proper disposal patterns for UI controls

### Module Development
- Inherit from `IRecoveryModule` interface
- Provide meaningful progress reporting
- Support cancellation through `isCancelled` parameter
- Include comprehensive error handling

### UI Development
- Use modern controls from `ModernControls.cs`
- Apply consistent theming through `ThemeManager`
- Implement thread-safe UI updates
- Follow responsive design principles

## 🐛 Troubleshooting

### Common Issues
- **Module Loading Failures**: Check module dependencies and interface compatibility
- **Threading Exceptions**: Ensure UI operations use `BeginInvoke` with disposal checks
- **Theme Issues**: Verify system theme detection and color propagation
- **Progress Reporting**: Confirm progress callbacks are thread-safe

### Debug Features
- Diagnostic overlay for system information
- Comprehensive logging through `AuditLogger`
- Module health status reporting
- Build verification scripts

## 📄 License

This project is developed for system recovery and maintenance purposes. Please ensure compliance with Windows licensing terms when using system modification features.

## 🤝 Contributing

1. Follow the established code standards and architecture
2. Include comprehensive documentation for new features
3. Test thoroughly with different Windows versions
4. Ensure thread safety for UI operations
5. Update this README for significant changes

## 📋 Documentation Structure

### **📖 This README.md**
Contains comprehensive project documentation, consolidating information from:
- ✅ **Previous README.md** - Main documentation
- ✅ **Resources/readme.txt** - Legacy documentation and current status  
- ✅ **ProjectManifest.txt** - Project structure overview

### **📝 Development History**
For detailed development history and technical changes, see:
- **`Resources/changelog.txt`** - Chronological development log with technical details
- **Accessible via**: Help → View Changelog in the application

All redundant documentation files have been removed, maintaining separate changelog for development tracking.

---

**RecoveryCommander** - Professional Windows System Recovery and Maintenance Tool  
*Optimized, Consolidated, and Continuously Improved*
