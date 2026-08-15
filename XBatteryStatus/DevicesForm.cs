using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace XBatteryStatus
{
    /// <summary>
    /// Lists all paired Bluetooth devices with a battery service (deduplicated by address).
    /// Per device: alert enabled, 3 custom alert levels and a custom display name.
    /// Also configures the battery polling interval.
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
            public BleDevice Device;
            public CheckBox Alert;
            public TextBox Name;
            public NumericUpDown Level1;
            public NumericUpDown Level2;
            public NumericUpDown Level3;
            public Label Battery;
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
            ClientSize = new Size(780, 480);

            var scrollPanel = new Panel
            {
                Location = new Point(12, 12),
                Size = new Size(756, 360),
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
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260)); // Device
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));  // Battery
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // Alert
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); // Custom name
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));  // Level 1
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));  // Level 2
            rowsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));  // Level 3
            scrollPanel.Controls.Add(rowsPanel);

            AddHeaderRow();

            foreach (var device in context.GetDevices())
            {
                AddDeviceRow(device);
            }

            // poll interval
            var pollLabel = new Label
            {
                Text = Localization.Tr("PollInterval"),
                AutoSize = true,
                Location = new Point(12, 388)
            };
            Controls.Add(pollLabel);

            pollBox = new NumericUpDown
            {
                Minimum = 3,
                Maximum = 3600,
                Value = Math.Max(3, Math.Min(3600, AppConfig.PollInterval)),
                Location = new Point(240, 385),
                Size = new Size(70, 23)
            };
            Controls.Add(pollBox);

            var secondsLabel = new Label { Text = "s", AutoSize = true, Location = new Point(316, 389) };
            Controls.Add(secondsLabel);

            loggingBox = new CheckBox
            {
                Text = Localization.Tr("EnableLogging"),
                Checked = AppConfig.Logging,
                AutoSize = true,
                Location = new Point(400, 387)
            };
            Controls.Add(loggingBox);

            var okButton = new Button
            {
                Text = Localization.Tr("OK"),
                DialogResult = DialogResult.OK,
                Location = new Point(460, 436),
                Size = new Size(90, 28)
            };
            Controls.Add(okButton);
            AcceptButton = okButton;

            var cancelButton = new Button
            {
                Text = Localization.Tr("Cancel"),
                DialogResult = DialogResult.Cancel,
                Location = new Point(560, 436),
                Size = new Size(90, 28)
            };
            Controls.Add(cancelButton);
            CancelButton = cancelButton;
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

        private void AddDeviceRow(BleDevice device)
        {
            var controls = new RowControls { Device = device };

            var nameLabel = new Label
            {
                Text = device.DeviceName,
                Font = new Font(this.Font, FontStyle.Bold),
                AutoEllipsis = true,
                Width = 250,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(3, 5, 3, 5)
            };
            rowsPanel.Controls.Add(nameLabel);

            controls.Battery = new Label
            {
                Text = device.LastBattery >= 0 ? device.LastBattery + "%" : "—",
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
                Width = 150,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(3, 4, 3, 4)
            };
            rowsPanel.Controls.Add(controls.Name);

            controls.Level1 = MakeLevelBox(device.Config.Levels[0]);
            rowsPanel.Controls.Add(controls.Level1);
            controls.Level2 = MakeLevelBox(device.Config.Levels[1]);
            rowsPanel.Controls.Add(controls.Level2);
            controls.Level3 = MakeLevelBox(device.Config.Levels[2]);
            rowsPanel.Controls.Add(controls.Level3);

            rows.Add(controls);
        }

        private static NumericUpDown MakeLevelBox(int value)
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

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                AppConfig.PollInterval = (int)pollBox.Value;
                AppConfig.Logging = loggingBox.Checked;
                AppConfig.Save();

                foreach (var row in rows)
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

                context.ApplyDeviceConfig();
            }
            base.OnClosing(e);
        }
    }
}
