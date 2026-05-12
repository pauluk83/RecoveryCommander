using System;
using System.Drawing;
using System.Windows.Forms;
using RecoveryCommander.UI;

namespace RecoveryCommander.Forms
{
    public class ActionMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public bool RunAsAdmin { get; set; } = false;
    }

    public class ModuleActionDialog : Form
    {
        public ActionMetadata ActionData { get; private set; } = new ActionMetadata();

        private TextBox txtName;
        private TextBox txtDescription;
        private TextBox txtCommand;
        private TextBox txtArguments;
        private CheckBox chkAdmin;
        private ModernButton btnOk;
        private ModernButton btnCancel;

        public ModuleActionDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Add Module Action";
            this.Size = new Size(500, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Theme.ApplyFormStyle(this);
            Theme.ApplyTheme(this);
            Theme.ApplyMicaEffect(this);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(20),
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Name
            layout.Controls.Add(CreateLabel("Action Name:"), 0, 0);
            txtName = CreateTextBox("e.g. Run SFC Scan");
            layout.Controls.Add(txtName, 1, 0);

            // Description
            layout.Controls.Add(CreateLabel("Description:"), 0, 1);
            txtDescription = CreateTextBox("e.g. Runs the System File Checker");
            layout.Controls.Add(txtDescription, 1, 1);

            // Command
            layout.Controls.Add(CreateLabel("Command/File:"), 0, 2);
            txtCommand = CreateTextBox("e.g. sfc.exe");
            layout.Controls.Add(txtCommand, 1, 2);

            // Arguments
            layout.Controls.Add(CreateLabel("Arguments:"), 0, 3);
            txtArguments = CreateTextBox("e.g. /scannow");
            layout.Controls.Add(txtArguments, 1, 3);

            // Admin
            chkAdmin = new CheckBox 
            { 
                Text = "Requires Administrator Privileges", 
                ForeColor = Theme.Text,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 10)
            };
            layout.Controls.Add(chkAdmin, 1, 4);

            // Buttons
            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            btnCancel = new ModernButton { Text = "Cancel", Width = 100, Height = 36, DialogResult = DialogResult.Cancel };
            btnOk = new ModernButton { Text = "Add Action", Width = 120, Height = 36 };
            
            Theme.ApplyButtonStyle(btnCancel, Theme.ButtonStyle.Secondary, 8);
            Theme.ApplyButtonStyle(btnOk, Theme.ButtonStyle.Primary, 8);
            
            btnOk.Click += BtnOk_Click;

            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnOk);

            layout.Controls.Add(btnPanel, 1, 5);

            this.Controls.Add(layout);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Theme.Text,
                Font = Theme.Typography.Body,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private TextBox CreateTextBox(string placeholder)
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Font = Theme.Typography.Body,
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                PlaceholderText = placeholder,
                Margin = new Padding(0, 5, 0, 5)
            };
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtCommand.Text))
            {
                MessageBox.Show(this, "Action Name and Command are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ActionData.Name = txtName.Text.Trim();
            ActionData.Description = txtDescription.Text.Trim();
            ActionData.Command = txtCommand.Text.Trim();
            ActionData.Arguments = txtArguments.Text.Trim();
            ActionData.RunAsAdmin = chkAdmin.Checked;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
