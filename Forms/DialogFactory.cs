using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using RecoveryCommander.UI;
using RecoveryCommander.Core;
using RecoveryCommander.Features;

namespace RecoveryCommander.Forms
{
    public static class DialogFactory
    {
        private static string PromptForDescription(string title, string prompt, string defaultValue)
        {
            using var form = new Form();
            form.Text = title;
            form.Size = new Size(400, 180);
            form.StartPosition = FormStartPosition.CenterParent;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;

            var label = new Label { Text = prompt, Left = 20, Top = 20, Width = 340 };
            var textBox = new TextBox { Text = defaultValue, Left = 20, Top = 50, Width = 340 };
            var okButton = new RecoveryCommander.UI.ModernButton { Text = "OK", Left = 220, Top = 100, Width = 120, Height = 36, DialogResult = DialogResult.OK, ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 10, TextAlign = ContentAlignment.MiddleLeft };
            var cancelButton = new RecoveryCommander.UI.ModernButton { Text = "Cancel", Left = 350, Top = 100, Width = 120, Height = 36, DialogResult = DialogResult.Cancel, ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 10, TextAlign = ContentAlignment.MiddleLeft };

            form.Controls.AddRange([label, textBox, okButton, cancelButton]);
            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;

            return form.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
        }
        public static void ShowAboutDialog(Form? parent)
        {
            string buildDate = CoreUtilities.GetBuildDate();
            string version = "0.0.0";
            try
            {
                var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var infoAttr = asm.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>();
                version = infoAttr?.InformationalVersion ?? asm.GetName().Version?.ToString() ?? version;
            }
            catch { }

            string info = $"RecoveryCommander\nVersion: {version}\nBuild Date: {buildDate}\nAuthor: Zane Stanton\n" +
                          "© 2025 RecoveryCommander™\nAll rights reserved.\n\n" +
                          "Licensed for use under the RecoveryCommander License Agreement.";

            // Create themed About dialog instead of MessageBox
            using var form = new Form()
            {
                Text = "About RecoveryCommander",
                Size = new Size(450, 350),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = true
            };

            Theme.ApplyFormStyle(form);
            Theme.ApplyTheme(form);

            var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30) };
            var contentLabel = new Label
            {
                Text = info,
                Font = Theme.Typography.Body,
                ForeColor = Theme.Text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            contentPanel.Controls.Add(contentLabel);

            form.Controls.Add(contentPanel);
            form.ShowDialog(parent);
        }

        public static void ShowHelpWindow(Form? host, string filePath, string title)
        {
            string content = "";
            try
            {
                var fullPath = Path.Combine(AppContext.BaseDirectory, filePath);
                if (File.Exists(fullPath))
                {
                    content = File.ReadAllText(fullPath);
                }
                else if (File.Exists(filePath))
                {
                    content = File.ReadAllText(filePath);
                }
                else
                {
                    content = $"{title} not found: {filePath}";
                }
            }
            catch (Exception ex)
            {
                content = $"Error loading {title}: {ex.Message}";
            }

            ShowContentDialog(host, content, title);
        }

        public static void ShowContentDialog(Form? host, string content, string title)
        {
            using var form = new Form()
            {
                Text = title,
                Size = new System.Drawing.Size(700, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = true
            };

            Theme.ApplyFormStyle(form);
            Theme.ApplyTheme(form);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(20)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };
            var heading = new Label
            {
                Text = title,
                Font = Theme.Typography.Title,
                ForeColor = Theme.Text,
                Dock = DockStyle.Top,
                Height = 36
            };

            var rtb = new Theme.RoundedRichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Text = content,
                Font = Theme.Typography.Body,
                BackColor = Theme.Surface,
                ForeColor = Theme.Text
            };
            
            rtb.BackColor = Theme.Surface;
            rtb.ForeColor = Theme.Text;

            contentPanel.Controls.Add(rtb);
            contentPanel.Controls.Add(heading);

            var footerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };
            var footerFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
            var closeBtn = new RecoveryCommander.UI.ModernButton { Text = "Close", Width = 150, Height = 36, Margin = new Padding(0, 0, 12, 0), ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 10, TextAlign = ContentAlignment.MiddleCenter };
            closeBtn.Click += (s, e) => form.Close();
            Theme.ApplyButtonStyle(closeBtn, Theme.ButtonStyle.Secondary, 8);
            footerFlow.Controls.Add(closeBtn);
            footerPanel.Controls.Add(footerFlow);

            layout.Controls.Add(contentPanel, 0, 0);
            layout.Controls.Add(footerPanel, 0, 1);

            form.Controls.Add(layout);
            form.ShowDialog(host);
        }

        public static void ShowRestorePointManager(Form? host)
        {
            try
            {
            using var form = new Form()
            {
                Text = "Restore Point Manager",
                Size = new System.Drawing.Size(820, 520),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = true
            };

            var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, BorderStyle = BorderStyle.None };
            list.Columns.Add("Seq", 60);
            list.Columns.Add("Description", 420);
            list.Columns.Add("Created", 240);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(20)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };
            var titleLabel = new Label
            {
                Text = "Restore Points",
                Font = Theme.Typography.Title,
                ForeColor = Theme.Text,
                Dock = DockStyle.Top,
                Height = 34
            };
            list.Margin = new Padding(0, 12, 0, 0);
            listPanel.Controls.Add(list);
            listPanel.Controls.Add(titleLabel);

            var footerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };
            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            var btnCreate = new RecoveryCommander.UI.ModernButton { Text = "Create New Point", AutoSize = false, Width = 200, Height = 40, ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 10, TextAlign = ContentAlignment.MiddleCenter };
            var btnDelete = new RecoveryCommander.UI.ModernButton { Text = "Delete Restore Point", AutoSize = false, Width = 200, Height = 40, ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 10, TextAlign = ContentAlignment.MiddleCenter };
            var btnRestore = new RecoveryCommander.UI.ModernButton { Text = "Restore System", AutoSize = false, Width = 200, Height = 40, ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 10, TextAlign = ContentAlignment.MiddleCenter };
            btnPanel.Controls.AddRange(new Control[] { btnCreate, btnDelete, btnRestore });
            footerPanel.Controls.Add(btnPanel);

            layout.Controls.Add(listPanel, 0, 0);
            layout.Controls.Add(footerPanel, 0, 1);

            form.Controls.Add(layout);

            Theme.ApplyFormStyle(form);
            Theme.ApplyTheme(form);

            Theme.ApplyButtonStyle(btnCreate, Theme.ButtonStyle.Primary, 8);
            Theme.ApplyButtonStyle(btnDelete, Theme.ButtonStyle.Primary, 8);
            Theme.ApplyButtonStyle(btnRestore, Theme.ButtonStyle.Primary, 8);

            list.BackColor = Theme.Colors.Background;
            list.ForeColor = Theme.Colors.Text;
            Theme.DarkScrollBar.ApplyDarkTheme(list);
            layout.BackColor = Color.Transparent;

            async Task LoadPointsAsync()
            {
                list.Items.Clear();
                try
                {
                    var points = await RestorePointManager.GetRestorePointsAsync();
                    foreach (var p in points)
                    {
                        var item = new ListViewItem(p.Id.ToString());
                        item.SubItems.Add(p.Description);
                        item.SubItems.Add(p.CreationTime.ToString());
                        item.Tag = p;
                        list.Items.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(form, $"Failed to load restore points: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            btnCreate.Click += async (s, e) =>
            {
                var desc = PromptForDescription("Create Restore Point", "Description:", "Recovery Point");
                if (string.IsNullOrWhiteSpace(desc)) return;
                var result = await RestorePointManager.CreateRestorePointAsync(desc);
                MessageBox.Show(form, result.Message, result.Success ? "Success" : "Error", MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                await LoadPointsAsync();
            };

            btnDelete.Click += async (s, e) =>
            {
                if (list.SelectedItems.Count == 0) return;
                var sel = list.SelectedItems[0].Tag as RestorePoint;
                if (sel == null) return;

                await Task.Run(() =>
                {
                    MessageBox.Show(form, "Delete restore point functionality is not currently available.", "Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            };

            btnRestore.Click += async (s, e) =>
            {
                if (list.SelectedItems.Count == 0) return;
                if (list.SelectedItems[0].Tag is RestorePoint sel && sel != null)
                {
                    var confirm = MessageBox.Show(form, $"Restore to '{sel.Description}'? This will reboot the system.", "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes) return;
                    var res = await RestorePointManager.RestoreToPointAsync(sel.Id);
                    MessageBox.Show(form, res.Message, res.Success ? "Initiated" : "Error", MessageBoxButtons.OK, res.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                }
            };

            _ = LoadPointsAsync();
            form.ShowDialog(host);
            }
            catch (Exception ex)
            {
                MessageBox.Show(host, $"Failed to open Restore Point Manager: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ShowStartupManager(Form? host)
        {
            try
            {
                using var form = new Form()
                {
                    Text = "Startup Manager",
                    Size = new System.Drawing.Size(920, 640),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.Sizable,
                    MaximizeBox = true,
                    MinimizeBox = true
                };

            var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, BorderStyle = BorderStyle.None };
            list.Columns.Add("Name", 260);
            list.Columns.Add("Command", 450);
            list.Columns.Add("Location", 210);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(20)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };
            var titleLabel = new Label
            {
                Text = "Startup Entries",
                Font = Theme.Typography.Title,
                ForeColor = Theme.Text,
                Dock = DockStyle.Top,
                Height = 34
            };
            list.Margin = new Padding(0, 12, 0, 0);
            listPanel.Controls.Add(list);
            listPanel.Controls.Add(titleLabel);

            var footerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };
            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            var btnRefresh = new RecoveryCommander.UI.ModernButton { Text = "Refresh List", AutoSize = false, Width = 180, Height = 40, ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 10, TextAlign = ContentAlignment.MiddleCenter };
            var btnDisable = new RecoveryCommander.UI.ModernButton { Text = "Disable Item", AutoSize = false, Width = 180, Height = 40, ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 10, TextAlign = ContentAlignment.MiddleCenter };
            var btnEnable = new RecoveryCommander.UI.ModernButton { Text = "Enable Item", AutoSize = false, Width = 180, Height = 40, ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 10, TextAlign = ContentAlignment.MiddleCenter };
            var btnDelete = new RecoveryCommander.UI.ModernButton { Text = "Delete Permanently", AutoSize = false, Width = 180, Height = 40, ButtonStyle = Theme.ButtonStyle.Secondary, CornerRadius = 10, TextAlign = ContentAlignment.MiddleCenter };
            btnPanel.Controls.AddRange(new Control[] { btnRefresh, btnDisable, btnEnable, btnDelete });
            footerPanel.Controls.Add(btnPanel);

            layout.Controls.Add(listPanel, 0, 0);
            layout.Controls.Add(footerPanel, 0, 1);

            form.Controls.Add(layout);

            Theme.ApplyFormStyle(form);
            Theme.ApplyTheme(form);

            Theme.ApplyButtonStyle(btnRefresh, Theme.ButtonStyle.Secondary, 8);
            Theme.ApplyButtonStyle(btnDisable, Theme.ButtonStyle.Primary, 8);
            Theme.ApplyButtonStyle(btnEnable, Theme.ButtonStyle.Primary, 8);
            Theme.ApplyButtonStyle(btnDelete, Theme.ButtonStyle.Primary, 8);

            list.BackColor = Theme.Colors.Background;
            list.ForeColor = Theme.Colors.Text;
            Theme.DarkScrollBar.ApplyDarkTheme(list);

                async Task LoadItemsAsync()
                {
                    list.Items.Clear();
                    try
                    {
                        var items = await StartupManager.GetStartupItemsAsync();
                        foreach (var it in items)
                        {
                            var item = new ListViewItem(it.Name);
                            item.SubItems.Add(it.Command);
                            item.SubItems.Add(it.Location);
                            item.Tag = it;
                            list.Items.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(form, $"Failed to load startup items: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

            btnRefresh.Click += async (s, e) => await LoadItemsAsync();
            btnDisable.Click += async (s, e) =>
            {
                if (list.SelectedItems.Count == 0) return;
                if (list.SelectedItems[0].Tag is StartupItem sel && sel != null)
                {
                    var ok = await StartupManager.DisableStartupItemAsync(sel);
                    MessageBox.Show(form, ok ? "Disabled" : "Failed to disable", ok ? "Success" : "Error", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                    await LoadItemsAsync();
                }
            };
            btnEnable.Click += async (s, e) =>
            {
                if (list.SelectedItems.Count == 0) return;
                if (list.SelectedItems[0].Tag is StartupItem sel && sel != null)
                {
                    var ok = await StartupManager.EnableStartupItemAsync(sel);
                    MessageBox.Show(form, ok ? "Enabled" : "Failed to enable", ok ? "Success" : "Error", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                    await LoadItemsAsync();
                }
            };

            btnDelete.Click += async (s, e) =>
            {
                if (list.SelectedItems.Count == 0) return;
                if (list.SelectedItems[0].Tag is StartupItem sel && sel != null)
                {
                    var confirmResult = MessageBox.Show(
                        form, 
                        $"Are you sure you want to permanently delete '{sel.Name}' from the startup list?\n\nThis action cannot be undone.", 
                        "Confirm Permanent Deletion", 
                        MessageBoxButtons.YesNo, 
                        MessageBoxIcon.Warning
                    );
                    
                    if (confirmResult == DialogResult.Yes)
                    {
                        var ok = await StartupManager.DeleteStartupItemAsync(sel);
                        MessageBox.Show(form, ok ? "Deleted permanently" : "Failed to delete", ok ? "Success" : "Error", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                        await LoadItemsAsync();
                    }
                }
            };

                _ = LoadItemsAsync();
                form.ShowDialog(host);
            }
            catch (Exception ex)
            {
                MessageBox.Show(host, $"Failed to open Startup Manager: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ShowMediaTools(Form? host)
        {
            try
            {
                RecoveryCommander.Forms.MediaTools.ShowMediaToolsDialog(host);
            }
            catch (Exception ex)
            {
                MessageBox.Show(host, $"Failed to open Media Tools: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ShowNetworkOptimizer(Form? host)
        {
            try
            {
                using var form = new NetworkOptimizer();
                form.ShowDialog(host);
            }
            catch (Exception ex)
            {
                MessageBox.Show(host, $"Failed to open Network Optimizer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
