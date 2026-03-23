# RecoveryCommander – Architectural & Design Notes

## 🧱 Code Structure & Workflow
Recovery Commander (now **Win Recovery**) avoids auto-generated designers and keeps everything explicit:
- **Designer-Free UI** – No `.Designer.cs` or `InitializeComponent()`. Layouts and controls are all written directly in code.
- **Modular Execution** – Each module implements the built-in `IModule` interface and can be injected into the UI with `InjectModuleUI(Control)`.
- **Audit & Change Tracking** – Every source file carries a permanent audit header and changelog block, with updates also mirrored into the project-wide `changelog.md`.
- **Whole File Responses** – All deliverables are full, error-checked files that can be pasted and run immediately. No partial snippets, no inline commentary.
- **Embedded Shared Types** – Interfaces and enums like `IModule` and `Theme` live inside `MainForm.cs`, avoiding extra dependency files.
- **Archive Handling** – All code must be checked, optimized, and cleaned of redundancy. Deprecated or unused code is moved into the `\Archive` folder. This folder is ignored during builds and debugging sessions.
- **Module Data & UI Control** – Each module manages only its own data (commands, metadata, build info). The main app owns and renders the central layout and UI elements. Once the UI structure is in place, modules simply define which commands run when their associated buttons are clicked.
- **Default Modules** – Present commands as Windows 11–styled buttons in the Module Action Pane.
- **System Prep Module (Special Case)** – Displays commands as tickboxes (auto-selected by default) in two columns:
    - Maintenance (header, no tickboxes)
    - Optimizations (header, no tickboxes)
    - Includes two control buttons: **Untick All** and **Run Selected**

---

## 🎨 UI & UX Design
The app should feel like it belongs in Windows 11:
- **Look & Feel** – Rounded corners, fluent spacing, Segoe UI font, Mica background. High DPI and accessibility are non-negotiable.
- **Main Window** – Launches maximized, with Minimize/Maximize/Close controls, and auto-elevates to admin rights.
- **Menus**
    - **File** → Exit
    - **Help** → About – shows build date, author (**Zane Stanton**), and license message.
    - **Help** → ChangeLog – opens a window showing `changelog.md` from `\Resources`.
    - **Help** → Readme – opens a window showing `README.md` from `\Resources`.
- **Theming** – Native Windows 11 dark mode applied consistently across all controls.
- **App Icon** – Stored as `system_restore.ico` in `\Resources`.
- **Blueprint Alignment** – The UI layout must follow the provided screenshots as a blueprint.

---

## 🖼️ Layout Blueprint (ASCII)
```text
+------------------------------------------------------------------+
|                        [ Title Bar ]                             |
|  [ Menu Bar ]                                                    |
+------------------------------------------------------------------+
|  Module Pane (Vertical)    |  Module Action Pane (Vertical)      |
|  ------------------------  |  --------------------------------   |
|  [ Module A ]  build: 01/01|  (Default Module Commands)          |
|  [ Module B ]  build: 02/01|  [ Button 1: Run Command A ]        |
|  [ Module C ]  build: 03/01|  [ Button 2: Run Command B ]        |
|  [ System Prep ] build: 04/01|  [ Button 3: Run Command C ]      |
|  [ Module D ]  build: 05/01|  (System Prep Module – Special)     |
|  [ Module E ]  build: 06/01|  Maintenance       Optimizations     |
|  [ Module F ]  build: 07/01|  [✓] Option 1      [✓] Option 2         |
|  [ Module G ]  build: 08/01|  [✓] Option 3      [✓] Option 4         |
|  [ Module H ]  build: 09/01|  [✓] Option 5      [✓] Option 6         |
|  [ Module I ]  build: 10/01|  [ Untick All ]    [ Run Selected ]     |
|  [ Module J ]  build: 11/01|                                     |
+------------------------------------------------------------------+
|       [ Live Output Log (auto-scrolling multi-line textbox) ]    |
+------------------------------------------------------------------+
|   [ Progress Bar ----------------------------------] [ Cancel ]  |
+------------------------------------------------------------------+
|                   [ Status Bar – real-time info ]                |
+------------------------------------------------------------------+
```

---

## 🔑 Notes
- **Module Pane (Vertical)** – Lists modules with Name + Build Date. Selecting a module populates the Module Action Pane.
- **Module Action Pane (Vertical)**
    - **Default Modules**: Buttons mapped to commands in runtime.dll.
    - **System Prep Module**: Tickboxes under two columns (Maintenance and Optimizations), headers never have tickboxes. Two control buttons: Untick All and Run Selected.
- **Live Output Log**: Streams real-time command output; auto-scrolls.
- **Progress Bar + Cancel Button**: Progress bar extends up to the Cancel button for continuous visual feedback.
- **Status Bar**: Displays real-time module and execution info.

---

## ⚙️ Runtime Behavior
- **Command Execution** – Outputs stream live into the log box; progress bar shows execution state. Cancel safely aborts tasks.
- **Centralized Logging** – All logs are timestamped and funneled through `Log(string message)`.
- **Error Handling** – Every public method uses try-catch. Failures are logged, never ignored.
- **Modules** – Auto-discovered from DLLs in the specific module folders.
- **Archive Safety** – Code in `\Archive` is ignored for builds and debugging; reference only.

---

## 🔒 Development Philosophy
- **No Regression** – Updates build cumulatively; nothing removed or downgraded.
- **Extensibility** – Patterns support long-term maintainability.
- **Rapid Iteration** – Modular injection enables fast prototyping.
- **Accessibility & Auditability** – Controls and modules are traceable and compliant.
- **Autonomous Operation** – Runs without repeated prompting.
- **Persistent Memory** – All requests, changes, and project decisions are remembered:
    - Every file carries audit/change markers.
    - `changelog.md` is auto-updated.
    - Local logging supports continuity and prevents repeated instructions.


