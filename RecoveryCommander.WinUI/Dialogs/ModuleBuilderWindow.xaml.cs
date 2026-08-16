using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace RecoveryCommanderWinUI.Dialogs;

/// <summary>
/// Module Builder window - generates C# IRecoveryModule scaffold code from a template.
/// Window-based implementation to overcome ContentDialog width limitations.
/// </summary>
public sealed partial class ModuleBuilderWindow : BaseWindowDialog
{
    private string _lastGeneratedCode = string.Empty;

    public ModuleBuilderWindow()
    {
        InitializeComponent();
    }

    // ─── Live input change → auto-generate ────────────────────────────────────

    private void InputChanged(object sender, object e)
    {
        // Auto-generate only if the module name is filled in
        if (!string.IsNullOrWhiteSpace(ModuleNameBox.Text))
            GenerateCode();
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
        => GenerateCode();

    // ─── Code generation ──────────────────────────────────────────────────────

    private void GenerateCode()
    {
        var moduleName = SanitizeIdentifier(ModuleNameBox.Text.Trim());
        var description = DescriptionBox.Text.Trim();
        var version = VersionBox.Text.Trim();
        var actionName = ActionNameBox.Text.Trim();
        var template = (TemplateCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "Basic Action";

        if (string.IsNullOrWhiteSpace(moduleName))
        {
            SetStatus("⚠ Enter a module name to generate code.");
            return;
        }

        if (string.IsNullOrWhiteSpace(description)) description = $"{moduleName} module for RecoveryCommander.";
        if (string.IsNullOrWhiteSpace(version)) version = "1.0.0";
        if (string.IsNullOrWhiteSpace(actionName)) actionName = "Run";

        _lastGeneratedCode = template switch
        {
            "PowerShell Runner" => BuildPowerShellTemplate(moduleName, description, version, actionName),
            "Download Tool"     => BuildDownloadTemplate(moduleName, description, version, actionName),
            "Multi-Step"        => BuildMultiStepTemplate(moduleName, description, version, actionName),
            _                   => BuildBasicTemplate(moduleName, description, version, actionName),
        };

        CodePreviewBox.Text = _lastGeneratedCode;
        SetStatus($"✔ Generated {template} scaffold for \"{moduleName}\" — {_lastGeneratedCode.Length:N0} characters");
    }

    // ─── Templates ────────────────────────────────────────────────────────────

    private static string BuildBasicTemplate(string name, string desc, string ver, string actionName) => $@"/*
 * Module: {name}
 * Description: {desc}
 * Version: {ver}
 * Created: {DateTime.Now:yyyy-MM-dd}
 * Author: [Your Name]
 */

using RecoveryCommander.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RecoveryCommander.Modules;

[RecoveryModule(""{name}"")]
public class {name} : IRecoveryModule
{{
    public string Name        => ""{name}"";
    public string Description => ""{desc}"";
    public string Version     => ""{ver}"";
    public string HealthStatus => ""Healthy"";
    public string BuildInfo    => ""{name} v{ver}"";
    public bool   SupportsAsync => true;

    public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
    {{
        new(""{actionName}"", ""{actionName}"")
        {{
            ExecuteAction = {actionName}Async
        }}
    }};

    private static async Task {actionName}Async(
        IProgress<ProgressReport> progress,
        Action<string> reportOutput,
        CancellationToken cancellationToken)
    {{
        progress.Report(new ProgressReport(0, ""Starting {actionName}...""));
        reportOutput(""[{name}] {actionName} started."");

        // TODO: implement your logic here
        await Task.Delay(500, cancellationToken);

        progress.Report(new ProgressReport(100, ""Done.""));
        reportOutput(""[{name}] {actionName} complete."");
    }}
}}
";

    private static string BuildPowerShellTemplate(string name, string desc, string ver, string actionName) => $@"/*
 * Module: {name}  (PowerShell Runner template)
 * Description: {desc}
 * Version: {ver}
 * Created: {DateTime.Now:yyyy-MM-dd}
 */

using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RecoveryCommander.Modules;

[RecoveryModule(""{name}"")]
public class {name} : IRecoveryModule
{{
    public string Name        => ""{name}"";
    public string Description => ""{desc}"";
    public string Version     => ""{ver}"";
    public string HealthStatus => ""Healthy"";
    public string BuildInfo    => ""{name} v{ver}"";
    public bool   SupportsAsync => true;

    public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
    {{
        new(""{actionName}"", ""{actionName}"")
        {{
            ExecuteAction = {actionName}Async
        }}
    }};

    private static async Task {actionName}Async(
        IProgress<ProgressReport> progress,
        Action<string> reportOutput,
        CancellationToken cancellationToken)
    {{
        progress.Report(new ProgressReport(0, ""Running PowerShell script...""));

        // TODO: replace this script with your logic
        const string script = @""
            Write-Output 'Hello from {name}!'
            # Add your PowerShell commands here
        "";

        await AsyncHelpers.ExecutePowerShellCommandAsync(
            script,
            line => reportOutput(line),
            cancellationToken);

        progress.Report(new ProgressReport(100, ""Done.""));
    }}
}}
";

    private static string BuildDownloadTemplate(string name, string desc, string ver, string actionName) => $@"/*
 * Module: {name}  (Download Tool template)
 * Description: {desc}
 * Version: {ver}
 * Created: {DateTime.Now:yyyy-MM-dd}
 */

using RecoveryCommander.Contracts;
using RecoveryCommander.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RecoveryCommander.Modules;

[RecoveryModule(""{name}"")]
public class {name} : IRecoveryModule
{{
    public string Name        => ""{name}"";
    public string Description => ""{desc}"";
    public string Version     => ""{ver}"";
    public string HealthStatus => ""Healthy"";
    public string BuildInfo    => ""{name} v{ver}"";
    public bool   SupportsAsync => true;

    public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
    {{
        new(""{actionName}"", ""{actionName}"")
        {{
            ExecuteAction = (p, o, c) =>
                DownloadCatalog.DownloadAndExecuteFromCatalogAsync(""YourCatalog.KeyHere"", p, o, c)
        }}
    }};

    // NOTE: Register your download entry in Core/DownloadCatalog.cs:
    //
    //   {{ ""YourCatalog.KeyHere"",
    //       new DownloadEntry(""https://example.com/tool.exe"",
    //           ""tool.exe"", ""1.0"", ""<sha256-hash-here>"") }},
}}
";

    private static string BuildMultiStepTemplate(string name, string desc, string ver, string actionName) => $@"/*
 * Module: {name}  (Multi-Step template)
 * Description: {desc}
 * Version: {ver}
 * Created: {DateTime.Now:yyyy-MM-dd}
 */

using RecoveryCommander.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RecoveryCommander.Modules;

[RecoveryModule(""{name}"")]
public class {name} : IRecoveryModule
{{
    public string Name        => ""{name}"";
    public string Description => ""{desc}"";
    public string Version     => ""{ver}"";
    public string HealthStatus => ""Healthy"";
    public string BuildInfo    => ""{name} v{ver}"";
    public bool   SupportsAsync => true;

    public IEnumerable<ModuleAction> Actions => new List<ModuleAction>
    {{
        new(""{actionName}"", ""{actionName}"") {{ ExecuteAction = {actionName}Async }},
        new(""Step 2"",        ""Step 2"")        {{ ExecuteAction = Step2Async }},
        new(""Step 3"",        ""Step 3"")        {{ ExecuteAction = Step3Async }},
    }};

    private static async Task {actionName}Async(
        IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken ct)
    {{
        progress.Report(new ProgressReport(0,  ""Step 1: Starting...""));
        // TODO: Step 1 logic
        await Task.Delay(300, ct);
        progress.Report(new ProgressReport(33, ""Step 1 done.""));
    }}

    private static async Task Step2Async(
        IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken ct)
    {{
        progress.Report(new ProgressReport(33, ""Step 2: Processing...""));
        // TODO: Step 2 logic
        await Task.Delay(300, ct);
        progress.Report(new ProgressReport(66, ""Step 2 done.""));
    }}

    private static async Task Step3Async(
        IProgress<ProgressReport> progress, Action<string> reportOutput, CancellationToken ct)
    {{
        progress.Report(new ProgressReport(66, ""Step 3: Finalising...""));
        // TODO: Step 3 logic
        await Task.Delay(300, ct);
        progress.Report(new ProgressReport(100, ""All done.""));
    }}
}}
";

    // ─── Actions ─────────────────────────────────────────────────────────────

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_lastGeneratedCode))
        {
            SetStatus("⚠ Nothing to copy yet — generate code first.");
            return;
        }

        var package = new DataPackage();
        package.SetText(_lastGeneratedCode);
        Clipboard.SetContent(package);
        SetStatus("✔ Code copied to clipboard.");
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_lastGeneratedCode))
        {
            SetStatus("⚠ Generate code first.");
            return;
        }

        var moduleName = SanitizeIdentifier(ModuleNameBox.Text.Trim());
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            SetStatus("⚠ Enter a module name.");
            return;
        }

        try
        {
            // Resolve the Module/ directory relative to the app binary
            var appDir = AppContext.BaseDirectory;
            // Walk up from bin/[Config]/[TFM]/[RID]/ to the project root
            var root = appDir;
            for (int i = 0; i < 6; i++)
            {
                var candidate = Path.Combine(root, "Module");
                if (Directory.Exists(candidate)) { root = candidate; break; }
                root = Path.GetDirectoryName(root) ?? root;
            }

            if (!root.EndsWith("Module", StringComparison.OrdinalIgnoreCase))
            {
                // Fallback: write next to the exe
                root = Path.Combine(appDir, "Modules");
            }

            var moduleDir = Path.Combine(root, $"{moduleName}Module");
            Directory.CreateDirectory(moduleDir);

            var csFile = Path.Combine(moduleDir, $"{moduleName}Module.cs");
            await File.WriteAllTextAsync(csFile, _lastGeneratedCode);

            SetStatus($"✔ Saved to: {csFile}");

            // Offer to open the folder
            var confirm = new ContentDialog
            {
                Title = "Saved",
                Content = $"Module scaffold saved to:\n{csFile}\n\nOpen the folder now?",
                PrimaryButtonText = "Open Folder",
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.MainWindow?.Content?.XamlRoot
            };
            if (await confirm.ShowAsync() == ContentDialogResult.Primary)
                OpenFolder(moduleDir);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
        {
            SetStatus($"✘ Save failed: {ex.Message}");
        }
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var appDir = AppContext.BaseDirectory;
        var root = appDir;
        for (int i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(root, "Module");
            if (Directory.Exists(candidate)) { OpenFolder(candidate); return; }
            root = Path.GetDirectoryName(root) ?? root;
        }
        // Fallback to app dir
        OpenFolder(appDir);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private static void OpenFolder(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = false
            });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Ignore failures opening the folder, this is a best-effort helper.
        }
        catch (InvalidOperationException)
        {
            // Ignore if the process cannot be started.
        }
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private static string SanitizeIdentifier(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var sb = new StringBuilder();
        foreach (var ch in input)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
                sb.Append(ch);
        }
        var result = sb.ToString().TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
        return string.IsNullOrEmpty(result) ? "MyModule" : result;
    }
}
