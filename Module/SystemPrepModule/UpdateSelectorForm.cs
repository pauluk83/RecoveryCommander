using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RecoveryCommander.Modules
{
    public class UpdateSelectorForm<T> : Form where T : class
    {
        private readonly DataGridView _grid;
        private readonly Button _okButton;
        private readonly Button _cancelButton;
        private readonly Button _selectAllButton;
        private readonly Button _clearButton;
        private readonly Label _totalSizeLabel;
        private readonly Func<T, string>? _getSizeString;

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
            Func<T, object[]> getRowData,
            Func<T, string>? getSizeString = null)
        {
            _getSizeString = getSizeString;
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            Size = new Size(1000, 600);
            MinimumSize = new Size(800, 400);

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(0, 10, 0, 0) };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(64, 64, 64),
                RowHeadersVisible = false
            };

            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            _grid.ColumnHeadersHeight = 40;
            _grid.DefaultCellStyle.BackColor = Color.FromArgb(32, 32, 32);
            _grid.DefaultCellStyle.ForeColor = Color.White;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);

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
                var rowData = new List<object> { true }; // Default to selected
                rowData.AddRange(getRowData(item));
                var rowIndex = _grid.Rows.Add(rowData.ToArray());
                _grid.Rows[rowIndex].Tag = item;
            }

            _grid.CellValueChanged += (s, e) => UpdateTotalSize();
            _grid.CurrentCellDirtyStateChanged += (s, e) => { if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };

            _totalSizeLabel = new Label
            {
                Text = "Total Size: calculating...",
                Dock = DockStyle.Left,
                Width = 350,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 189, 255)
            };

            _okButton = new Button
            {
                Text = "Install Selected",
                DialogResult = DialogResult.OK,
                Size = new Size(150, 40),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            var spacer = new Control { Dock = DockStyle.Right, Width = 10 };

            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(100, 40),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };

            var spacer2 = new Control { Dock = DockStyle.Right, Width = 20 };

            _clearButton = new Button
            {
                Text = "Clear",
                Size = new Size(100, 40),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };

            var spacer3 = new Control { Dock = DockStyle.Right, Width = 10 };

            _selectAllButton = new Button
            {
                Text = "Select All",
                Size = new Size(100, 40),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };

            buttonPanel.Controls.Add(_totalSizeLabel);
            buttonPanel.Controls.Add(_selectAllButton);
            buttonPanel.Controls.Add(spacer3);
            buttonPanel.Controls.Add(_clearButton);
            buttonPanel.Controls.Add(spacer2);
            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(spacer);
            buttonPanel.Controls.Add(_okButton);

            // CRITICAL: In WinForms docking, the order of adding to controls 
            // determines the layout. Add Bottom-docked first, then Fill-docked.
            mainPanel.Controls.Add(_grid);
            mainPanel.Controls.Add(buttonPanel);
            Controls.Add(mainPanel);

            this.BackColor = Color.FromArgb(32, 32, 32);
            UpdateTotalSize();
        }

        private void SetAllSelection(bool value)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                row.Cells[0].Value = value;
            }
            UpdateTotalSize();
        }

        private void UpdateTotalSize()
        {
            if (_getSizeString == null)
            {
                _totalSizeLabel.Text = $"Items Selected: {SelectedItems.Count()}";
                return;
            }

            double totalMB = 0;
            int unknownCount = 0;

            foreach (var item in SelectedItems)
            {
                string sizeStr = _getSizeString(item);
                if (sizeStr.Contains("MB"))
                {
                    if (double.TryParse(sizeStr.Replace("MB", "").Trim(), out double mb))
                        totalMB += mb;
                }
                else if (sizeStr.Contains("GB"))
                {
                    if (double.TryParse(sizeStr.Replace("GB", "").Trim(), out double gb))
                        totalMB += gb * 1024;
                }
                else if (sizeStr.Contains("KB"))
                {
                    if (double.TryParse(sizeStr.Replace("KB", "").Trim(), out double kb))
                        totalMB += kb / 1024;
            }
                else
                {
                    unknownCount++;
                }
            }

            string totalText = totalMB > 1024 ? $"{(totalMB / 1024):F2} GB" : $"{totalMB:F2} MB";
            _totalSizeLabel.Text = unknownCount > 0 
                ? $"Total Download: ~{totalText} (+{unknownCount} unknown)" 
                : $"Total Download: {totalText}";
        }
    }
}
