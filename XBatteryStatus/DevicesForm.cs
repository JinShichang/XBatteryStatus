using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace XBatteryStatus
{
    /// <summary>
    /// Lists all paired Bluetooth devices with a battery service (deduplicated by address)
    /// plus the Xbox Wireless Adapter slots. Bluetooth rows use percentage thresholds,
    /// adapter rows use level dropdowns (no alert / Medium / Low / Empty).
    /// </summary>
    public class DevicesForm : Form
    {
        private readonly MyApplicationContext context;
        private readonly TableLayoutPanel rowsPanel;
        private readonly NumericUpDown pollBox;
        private readonly CheckBox loggingBox;
        private readonly List<RowControls> rows = new List<RowControls>();

        private class RowControls
        {
            public IBatteryDevice Device;   // BLE / PnP battery row
            public int Slot = -1;      // XInput row
            public CheckBox Alert;
            public TextBox Name;       // BLE only
            public NumericUpDown Level1;   // BLE
            public NumericUpDown Level2;
            public NumericUpDown Level3;
            public ComboBox Cmb1;          // XInput
            public ComboBox Cmb2;
            public ComboBox Cmb3;
            public Label Battery;
        }

        private class LevelItem
        {
            public int Value { get; }
            private readonly string text;

            public LevelItem(string text, int value)
            {
                this.text = text;
                Value = value;
            }

            public override string ToString() => text;
        }

        public DevicesForm(MyApplicationContext context)
        {
            this.context = context;
            Text = Localization.Tr("Devices");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(780, 500);

            var scrollPanel = new Panel
            {
                Location = new Point(12, 12),
                Size = new Size(756, 380),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(scrollPanel);

            rowsPanel = new TableLayoutPanel
            {
                ColumnCount = 7,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Padding = new Padding(4)
            };
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270)); // Device
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));  // Battery
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // Alert
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // Custom name
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));  // Level 1
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));  // Level 2
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));  // Level 3
            scrollPanel.Controls.Add(rowsPanel);

            AddHeaderRow();

            foreach (var device in context.GetDevices())
            {
                AddBleRow(device);
            }

            for (int slot = 0; slot < XInputHelper.MaxSlots; slot++)
            {
                AddXInputRow(slot);
            }

            // poll interval
            var pollLabel = new Label
            {
                Text = Localization.Tr("PollInterval"),
                AutoSize = true,
                Location = new Point(12, 408)
            };
            Controls.Add(pollLabel);

            pollBox = new NumericUpDown
            {
                Minimum = 3,
                Maximum = 3600,
                Value = Math.Max(3, Math.Min(3600, AppConfig.PollInterval)),
                Location = new Point(240, 405),
                Size = new Size(70, 23)
            };
            Controls.Add(pollBox);

            var secondsLabel = new Label { Text = "s", AutoSize = true, Location = new Point(316, 409) };
            Controls.Add(secondsLabel);

            loggingBox = new CheckBox
            {
                Text = Localization.Tr("EnableLogging"),
                Checked = AppConfig.Logging,
                AutoSize = true,
                Location = new Point(400, 407)
            };
            Controls.Add(loggingBox);

            var okButton = new Button
            {
                Text = Localization.Tr("OK"),
                DialogResult = DialogResult.OK,
                Location = new Point(460, 456),
                Size = new Size(90, 28)
            };
            Controls.Add(okButton);
            AcceptButton = okButton;

            var cancelButton = new Button
            {
                Text = Localization.Tr("Cancel"),
                DialogResult = DialogResult.Cancel,
                Location = new Point(560, 456),
                Size = new Size(90, 28)
            };
            Controls.Add(cancelButton);
            CancelButton = cancelButton;

            Shown += DevicesForm_Shown;
        }

        /// <summary>Refreshes the device list when the dialog opens, so a scan that finished
        /// after the dialog was constructed is still picked up.</summary>
        private async void DevicesForm_Shown(object sender, EventArgs e)
        {
            context.DevicesChanged += OnDevicesChanged;
            try
            {
                await context.RefreshDevicesLightAsync();
                RebuildRows();
            }
            catch
            {
            }
        }

        private void OnDevicesChanged()
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (!IsDisposed)
                    {
                        RebuildRows();
                    }
                }));
            }
            catch
            {
            }
        }

        private void RebuildRows()
        {
            rows.Clear();
            SuspendLayout();
            rowsPanel.SuspendLayout();
            try
            {
                while (rowsPanel.Controls.Count > 7) // keep the header row
                {
                    Control c = rowsPanel.Controls[rowsPanel.Controls.Count - 1];
                    rowsPanel.Controls.RemoveAt(rowsPanel.Controls.Count - 1);
                    c.Dispose();
                }

                foreach (var device in context.GetDevices())
                {
                    AddBleRow(device);
                }
                for (int slot = 0; slot < XInputHelper.MaxSlots; slot++)
                {
                    AddXInputRow(slot);
                }
            }
            finally
            {
                rowsPanel.ResumeLayout(false);
                rowsPanel.PerformLayout();
                ResumeLayout(false);
                PerformLayout();
            }
        }

        private void AddHeaderRow()
        {
            string[] headers =
            {
                Localization.Tr("Device"),
                "Battery",
                Localization.Tr("EnableAlert"),
                Localization.Tr("CustomName"),
                Localization.Tr("AlertAt") + " 1",
                Localization.Tr("AlertAt") + " 2",
                Localization.Tr("AlertAt") + " 3"
            };
            foreach (var header in headers)
            {
                rowsPanel.Controls.Add(new Label { Text = header, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 4, 3, 4) });
            }
        }

        private void AddBleRow(IBatteryDevice device)
        {
            var controls = new RowControls { Device = device };

            var nameLabel = new Label
            {
                Text = device.DeviceName + " (" + Localization.Tr("Bluetooth") + ")",
                Font = new Font(this.Font, FontStyle.Bold),
                AutoEllipsis = true,
                Width = 260,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(3, 5, 3, 5)
            };
            rowsPanel.Controls.Add(nameLabel);

            controls.Battery = new Label
            {
                Text = device.IsConnected && device.LastBattery >= 0 ? device.LastBattery + "%" : "—",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 5, 3, 5)
            };
            rowsPanel.Controls.Add(controls.Battery);

            controls.Alert = new CheckBox
            {
                Checked = device.Config.Enabled,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 5, 3, 5)
            };
            rowsPanel.Controls.Add(controls.Alert);

            controls.Name = new TextBox
            {
                Text = device.Config.CustomName,
                Width = 140,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(3, 4, 3, 4)
            };
            rowsPanel.Controls.Add(controls.Name);

            controls.Level1 = MakePercentBox(device.Config.Levels[0]);
            rowsPanel.Controls.Add(controls.Level1);
            controls.Level2 = MakePercentBox(device.Config.Levels[1]);
            rowsPanel.Controls.Add(controls.Level2);
            controls.Level3 = MakePercentBox(device.Config.Levels[2]);
            rowsPanel.Controls.Add(controls.Level3);

            rows.Add(controls);
        }

        private void AddXInputRow(int slot)
        {
            var controls = new RowControls { Slot = slot };
            var config = context.GetXInputConfigPublic(slot);

            var nameLabel = new Label
            {
                Text = context.XInputDisplayName(slot),
                Font = new Font(this.Font, FontStyle.Bold),
                AutoEllipsis = true,
                Width = 260,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(3, 5, 3, 5)
            };
            rowsPanel.Controls.Add(nameLabel);

            controls.Battery = new Label
            {
                Text = context.IsXInputConnected(slot) ? MyApplicationContext.LevelName((XInputLevel)context.GetXInputLevel(slot)) : "—",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 5, 3, 5)
            };
            rowsPanel.Controls.Add(controls.Battery);

            controls.Alert = new CheckBox
            {
                Checked = config.Enabled,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 5, 3, 5)
            };
            rowsPanel.Controls.Add(controls.Alert);

            rowsPanel.Controls.Add(new Label
            {
                Text = "",
                Width = 140,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(3, 4, 3, 4)
            });

            controls.Cmb1 = MakeLevelBox(config.Levels[0]);
            rowsPanel.Controls.Add(controls.Cmb1);
            controls.Cmb2 = MakeLevelBox(config.Levels[1]);
            rowsPanel.Controls.Add(controls.Cmb2);
            controls.Cmb3 = MakeLevelBox(config.Levels[2]);
            rowsPanel.Controls.Add(controls.Cmb3);

            rows.Add(controls);
        }

        private static NumericUpDown MakePercentBox(int value)
        {
            return new NumericUpDown
            {
                Minimum = 1,
                Maximum = 100,
                Value = Math.Max(1, Math.Min(100, value)),
                Width = 58,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 4, 3, 4)
            };
        }

        private static ComboBox MakeLevelBox(int value)
        {
            var box = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 58,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 4, 3, 4)
            };
            box.Items.Add(new LevelItem(Localization.Tr("NoAlert"), -1));
            box.Items.Add(new LevelItem(Localization.Tr("BatteryMedium"), 2));
            box.Items.Add(new LevelItem(Localization.Tr("BatteryLow"), 1));
            box.Items.Add(new LevelItem(Localization.Tr("BatteryEmpty"), 0));

            int selected = 0;
            for (int i = 0; i < box.Items.Count; i++)
            {
                if (((LevelItem)box.Items[i]).Value == value)
                {
                    selected = i;
                    break;
                }
            }
            box.SelectedIndex = selected;
            return box;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            context.DevicesChanged -= OnDevicesChanged;
            if (DialogResult == DialogResult.OK)
            {
                AppConfig.PollInterval = (int)pollBox.Value;
                AppConfig.Logging = loggingBox.Checked;
                AppConfig.Save();

                foreach (var row in rows)
                {
                    if (row.Device != null)
                    {
                        row.Device.Config.Enabled = row.Alert.Checked;
                        row.Device.Config.CustomName = row.Name.Text.Trim();
                        row.Device.Config.Levels = new[]
                        {
                            (int)row.Level1.Value,
                            (int)row.Level2.Value,
                            (int)row.Level3.Value
                        };
                    }
                    else if (row.Slot >= 0)
                    {
                        var config = context.GetXInputConfigPublic(row.Slot);
                        config.Enabled = row.Alert.Checked;
                        config.Levels = new[]
                        {
                            ((LevelItem)row.Cmb1.SelectedItem).Value,
                            ((LevelItem)row.Cmb2.SelectedItem).Value,
                            ((LevelItem)row.Cmb3.SelectedItem).Value
                        };
                    }
                }

                context.ApplyDeviceConfig();
            }
            base.OnClosing(e);
        }
    }
}
