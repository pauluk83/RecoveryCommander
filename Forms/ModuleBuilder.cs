using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using RecoveryCommander.UI;

namespace RecoveryCommander.Forms
{
    public class ModuleBuilder : Form
    {
        private TextBox txtName;
        private TextBox txtDescription;
        private TextBox txtVersion;
        private ListBox listActions;
        private List<ActionMetadata> _actions = new List<ActionMetadata>();

        public ModuleBuilder()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Module Builder";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            Theme.ApplyFormStyle(this);
            Theme.ApplyTheme(this);
            Theme.ApplyMicaEffect(this);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(20),
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            // Left Panel - Metadata (7 logical rows: header + 3 label/textbox pairs)
            var leftPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7
            };

            leftPanel.Controls.Add(CreateHeader("Module Metadata"), 0, 0);
            leftPanel.Controls.Add(CreateLabel("Module Name:"), 0, 1);
            txtName = CreateTextBox("e.g. Custom Scanner");
            leftPanel.Controls.Add(txtName, 0, 2);

            leftPanel.Controls.Add(CreateLabel("Description:"), 0, 3);
            txtDescription = CreateTextBox("e.g. Custom system scanning tools.");
            leftPanel.Controls.Add(txtDescription, 0, 4);

            leftPanel.Controls.Add(CreateLabel("Version:"), 0, 5);
            txtVersion = CreateTextBox("1.0.0");
            leftPanel.Controls.Add(txtVersion, 0, 6);

            // Right Panel - Actions
            var rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };

            rightPanel.Controls.Add(CreateHeader("Module Actions"), 0, 0);
            
            listActions = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                Font = Theme.Typography.Body,
                ItemHeight = 24
            };
            rightPanel.Controls.Add(listActions, 0, 1);

            var actionBtnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            var btnAddAction = new ModernButton { Text = "Add Action", Width = 120, Height = 36 };
            var btnRemoveAction = new ModernButton { Text = "Remove Action", Width = 140, Height = 36 };
            Theme.ApplyButtonStyle(btnAddAction, Theme.ButtonStyle.Secondary, 8);
            Theme.ApplyButtonStyle(btnRemoveAction, Theme.ButtonStyle.Secondary, 8);

            btnAddAction.Click += BtnAddAction_Click;
            btnRemoveAction.Click += BtnRemoveAction_Click;

            actionBtnPanel.Controls.Add(btnAddAction);
            actionBtnPanel.Controls.Add(btnRemoveAction);
            rightPanel.Controls.Add(actionBtnPanel, 0, 2);

            mainLayout.Controls.Add(leftPanel, 0, 0);
            mainLayout.Controls.Add(rightPanel, 1, 0);

            // Footer Panel
            var footerPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var btnGenerate = new ModernButton { Text = "Generate Module Code", Width = 220, Height = 40 };
            var btnCancel = new ModernButton { Text = "Close", Width = 100, Height = 40 };
            
            Theme.ApplyButtonStyle(btnGenerate, Theme.ButtonStyle.Primary, 8);
            Theme.ApplyButtonStyle(btnCancel, Theme.ButtonStyle.Secondary, 8);

            btnGenerate.Click += BtnGenerate_Click;
            btnCancel.Click += (s, e) => this.Close();

            footerPanel.Controls.Add(btnGenerate);
            footerPanel.Controls.Add(btnCancel);

            mainLayout.SetColumnSpan(footerPanel, 2);
            mainLayout.Controls.Add(footerPanel, 0, 1);

            this.Controls.Add(mainLayout);
        }

        private Label CreateHeader(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Theme.Text,
                Font = Theme.Typography.Title,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Height = 40
            };
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Theme.Text,
                Font = Theme.Typography.Body,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Height = 30
            };
        }

        private TextBox CreateTextBox(string placeholder)
        {
            return new TextBox
            {
                Dock = DockStyle.Top,
                Font = Theme.Typography.Body,
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                PlaceholderText = placeholder,
                Margin = new Padding(0, 5, 0, 15)
            };
        }

        private void BtnAddAction_Click(object? sender, EventArgs e)
        {
            using var dialog = new ModuleActionDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _actions.Add(dialog.ActionData);
                UpdateActionList();
            }
        }

        private void BtnRemoveAction_Click(object? sender, EventArgs e)
        {
            if (listActions.SelectedIndex >= 0 && listActions.SelectedIndex < _actions.Count)
            {
                _actions.RemoveAt(listActions.SelectedIndex);
                UpdateActionList();
            }
        }

        private void UpdateActionList()
        {
            listActions.Items.Clear();
            foreach (var action in _actions)
            {
                listActions.Items.Add($"{action.Name} ({action.Command})");
            }
        }

        // Matches a valid C# identifier start (letter or underscore) followed by letters/digits/underscores.
        private static readonly Regex IdentifierStart = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        private void BtnGenerate_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(this, "Module Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var rawClassName = SanitizeIdentifier(txtName.Text);
            if (rawClassName.Length == 0)
            {
                MessageBox.Show(this, "Module Name must contain at least one letter or underscore so it can become a valid C# class name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!IdentifierStart.IsMatch(rawClassName))
            {
                MessageBox.Show(this, $"Module Name produced an invalid C# identifier: '{rawClassName}'.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            for (int i = 0; i < _actions.Count; i++)
            {
                var act = _actions[i];
                if (string.IsNullOrWhiteSpace(act.Name) || string.IsNullOrWhiteSpace(act.Command))
                {
                    MessageBox.Show(this, $"Action #{i + 1} is missing a Name or Command.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            string code = GenerateCode();

            using var sfd = new SaveFileDialog
            {
                Filter = "C# Files (*.cs)|*.cs|All Files (*.*)|*.*",
                Title = "Save Module Code",
                FileName = rawClassName + (rawClassName.EndsWith("Module", StringComparison.OrdinalIgnoreCase) ? string.Empty : "Module") + ".cs"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, code);
                    MessageBox.Show(this, "Module code generated and saved successfully!\n\nTo use this module, include the .cs file in the project or compile it into a DLL and place it in the Module folder.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to save file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string SanitizeIdentifier(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
            }
            var s = sb.ToString();
            if (s.Length > 0 && char.IsDigit(s[0])) s = "_" + s;
            return s;
        }

        // Escapes a string for embedding inside a regular C# double-quoted literal.
        // Rejects control characters that would break the literal.
        private static string EscapeStringLiteral(string value)
        {
            if (value is null) return string.Empty;
            var sb = new StringBuilder(value.Length + 8);
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\0': break; // drop nulls
                    default:
                        if (ch < 0x20) sb.Append('?'); else sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        // Escapes a string for embedding inside a C# verbatim @"..." literal.
        private static string EscapeVerbatimLiteral(string value)
        {
            return value is null ? string.Empty : value.Replace("\"", "\"\"");
        }

        private string GenerateCode()
        {
            var className = SanitizeIdentifier(txtName.Text);
            if (!className.EndsWith("Module", StringComparison.OrdinalIgnoreCase))
                className += "Module";

            var safeName = EscapeStringLiteral(txtName.Text);
            var safeDescription = EscapeStringLiteral(txtDescription.Text);
            var safeVersion = EscapeStringLiteral(string.IsNullOrWhiteSpace(txtVersion.Text) ? "1.0.0" : txtVersion.Text);

            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using RecoveryCommander.Contracts;");
            sb.AppendLine("using RecoveryCommander.Core;");
            sb.AppendLine();
            sb.AppendLine("namespace RecoveryCommander.Modules");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className} : IRecoveryModule");
            sb.AppendLine("    {");
            sb.AppendLine($"        public string Name => \"{safeName}\";");
            sb.AppendLine($"        public string Description => \"{safeDescription}\";");
            sb.AppendLine($"        public string Version => \"{safeVersion}\";");
            sb.AppendLine("        public string HealthStatus => \"Healthy\";");
            sb.AppendLine("        public string BuildInfo => $\"Built {DateTime.Now:yyyy-MM-dd}\";");
            sb.AppendLine("        public bool SupportsAsync => true;");
            sb.AppendLine();
            sb.AppendLine("        public IEnumerable<ModuleAction> Actions { get; }");
            sb.AppendLine();
            sb.AppendLine($"        public {className}()");
            sb.AppendLine("        {");
            sb.AppendLine("            Actions = new List<ModuleAction>");
            sb.AppendLine("            {");

            foreach (var action in _actions)
            {
                var aName = EscapeStringLiteral(action.Name);
                var aDesc = EscapeStringLiteral(string.IsNullOrWhiteSpace(action.Description) ? action.Name : action.Description);
                var aCmd = EscapeVerbatimLiteral(action.Command);
                var aArgs = EscapeVerbatimLiteral(action.Arguments ?? string.Empty);

                sb.AppendLine($"                new ModuleAction(\"{aName}\", \"{aDesc}\", async (progress, output, ct) =>");
                sb.AppendLine("                {");
                sb.AppendLine($"                    output(\"Starting: {aDesc}\");");
                sb.AppendLine("                    var psi = new System.Diagnostics.ProcessStartInfo");
                sb.AppendLine("                    {");
                sb.AppendLine($"                        FileName = @\"{aCmd}\",");
                sb.AppendLine($"                        Arguments = @\"{aArgs}\",");
                sb.AppendLine("                        UseShellExecute = false,");
                sb.AppendLine("                        RedirectStandardOutput = true,");
                sb.AppendLine("                        RedirectStandardError = true,");
                sb.AppendLine("                        CreateNoWindow = true");
                sb.AppendLine("                    };");
                if (action.RunAsAdmin)
                {
                    sb.AppendLine("                    // Action declared RunAsAdmin: re-launch elevated via ShellExecute.");
                    sb.AppendLine("                    psi.UseShellExecute = true;");
                    sb.AppendLine("                    psi.RedirectStandardOutput = false;");
                    sb.AppendLine("                    psi.RedirectStandardError = false;");
                    sb.AppendLine("                    psi.Verb = \"runas\";");
                }
                sb.AppendLine($"                    output(\"> {aCmd} {aArgs}\");");
                sb.AppendLine("                    await AsyncHelpers.RunProcessAsync(psi, output, null, ct);");
                sb.Append("                })");
                if (action.RunAsAdmin)
                {
                    sb.AppendLine(" { RequiresAdmin = true },");
                }
                else
                {
                    sb.AppendLine(",");
                }
            }

            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
