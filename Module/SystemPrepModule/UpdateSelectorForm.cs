using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RecoveryCommander.Module
{
    public class UpdateSelectorForm<T> : Form where T : class
    {
        private readonly DataGridView _grid;
        private readonly Button _okButton;
        private readonly Button _cancelButton;
        private readonly Button _selectAllButton;
        private readonly Button _clearButton;

        public IEnumerable<T> SelectedItems
        {
            get
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is T item)
                    {
                        var cellValue = row.Cells[0].Value;
                        bool isSelected = cellValue is bool b && b;
                        if (isSelected)
                        {
                            yield return item;
                        }
                    }
                }
            }
        }

        public UpdateSelectorForm(
            string title,
            List<T> items,
            List<DataGridViewColumn> columns,
            Func<T, object[]> getRowData)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(900, 500);

            _grid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 390,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Add select column
            var selectColumn = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Select",
                Width = 60,
                FillWeight = 10,
                ReadOnly = false
            };
            _grid.Columns.Add(selectColumn);

            // Add custom columns
            foreach (var column in columns)
            {
                _grid.Columns.Add(column);
            }

            // Add rows
            foreach (var item in items)
            {
                var rowData = new List<object> { true }; // First column is the checkbox
                rowData.AddRange(getRowData(item));
                var rowIndex = _grid.Rows.Add(rowData.ToArray());
                _grid.Rows[rowIndex].Tag = item;
            }

            // Create buttons
            _okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new Size(90, 30),
                Location = new Point(Width - 120, 410)
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new Size(90, 30),
                Location = new Point(Width - 220, 410)
            };

            _selectAllButton = new Button
            {
                Text = "Select All",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Size = new Size(90, 30),
                Location = new Point(20, 410)
            };
            _selectAllButton.Click += (s, e) => SetAllSelection(true);

            _clearButton = new Button
            {
                Text = "Clear",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Size = new Size(90, 30),
                Location = new Point(120, 410)
            };
            _clearButton.Click += (s, e) => SetAllSelection(false);

            // Add controls
            Controls.Add(_grid);
            Controls.Add(_okButton);
            Controls.Add(_cancelButton);
            Controls.Add(_selectAllButton);
            Controls.Add(_clearButton);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;

            // Apply basic styling
            this.BackColor = SystemColors.Window;
            this.ForeColor = SystemColors.WindowText;
            _grid.BackgroundColor = SystemColors.Window;
            _grid.DefaultCellStyle.BackColor = SystemColors.Window;
            _grid.DefaultCellStyle.ForeColor = SystemColors.WindowText;
        }

        private void SetAllSelection(bool value)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                row.Cells[0].Value = value;
            }
        }
    }
}
