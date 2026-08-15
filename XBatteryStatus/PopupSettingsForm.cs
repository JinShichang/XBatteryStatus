using System;
using System.Drawing;
using System.Windows.Forms;

namespace XBatteryStatus
{
    /// <summary>
    /// Popup customization dialog: title/subtitle text (with example placeholders),
    /// per line fonts, overall size and interactive position setup.
    /// </summary>
    public class PopupSettingsForm : Form
    {
        private readonly TextBox titleBox;
        private readonly TextBox subtitleBox;
        private readonly Label titleFontLabel;
        private readonly Label subtitleFontLabel;
        private readonly TrackBar scaleBar;
        private readonly Label scaleLabel;
        private readonly Label positionHint;
        private readonly Button setPositionButton;
        private readonly Button resetPositionButton;
        private readonly Button testButton;

        private Font titleFont;
        private Font subtitleFont;

        private bool positioning;
        private bool movedDuringPositioning;

        private readonly string originalTitle;
        private readonly string originalSubtitle;
        private readonly string originalTitleFontFamily;
        private readonly float originalTitleFontSize;
        private readonly bool originalTitleFontBold;
        private readonly string originalSubFontFamily;
        private readonly float originalSubFontSize;
        private readonly bool originalSubFontBold;
        private readonly float originalScale;

        public PopupSettingsForm()
        {
            originalTitle = AppConfig.PopupTitle;
            originalSubtitle = AppConfig.PopupSubtitle;
            originalTitleFontFamily = AppConfig.PopupTitleFontFamily;
            originalTitleFontSize = AppConfig.PopupTitleFontSize;
            originalTitleFontBold = AppConfig.PopupTitleFontBold;
            originalSubFontFamily = AppConfig.PopupSubFontFamily;
            originalSubFontSize = AppConfig.PopupSubFontSize;
            originalSubFontBold = AppConfig.PopupSubFontBold;
            originalScale = AppConfig.PopupScale;

            Text = Localization.Tr("PopupSettings");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(380, 350);

            int y = 12;

            // title
            Controls.Add(MakeLabel(Localization.Tr("PopupTitle"), 12, y));
            y += 20;
            titleBox = new PlaceholderTextBox
            {
                Location = new Point(12, y),
                Width = 356,
                Text = AppConfig.PopupTitle,
                Watermark = EffectiveTitle()
            };
            Controls.Add(titleBox);
            y += 30;

            // subtitle
            Controls.Add(MakeLabel(Localization.Tr("PopupSubtitle"), 12, y));
            y += 20;
            subtitleBox = new PlaceholderTextBox
            {
                Location = new Point(12, y),
                Width = 356,
                Text = AppConfig.PopupSubtitle,
                Watermark = EffectiveSubtitleExample()
            };
            Controls.Add(subtitleBox);
            y += 22;
            Controls.Add(MakeLabel(Localization.Tr("Placeholders"), 12, y, SystemColors.GrayText));
            y += 26;

            // title font
            var titleFontButton = new Button { Text = Localization.Tr("TitleFont"), Location = new Point(12, y), Size = new Size(110, 28) };
            titleFontButton.Click += (s, e) => PickFont(true);
            Controls.Add(titleFontButton);
            titleFontLabel = MakeLabel("", 130, y + 6);
            titleFontLabel.AutoEllipsis = true;
            titleFontLabel.Width = 238;
            Controls.Add(titleFontLabel);
            y += 34;

            // subtitle font
            var subtitleFontButton = new Button { Text = Localization.Tr("SubtitleFont"), Location = new Point(12, y), Size = new Size(110, 28) };
            subtitleFontButton.Click += (s, e) => PickFont(false);
            Controls.Add(subtitleFontButton);
            subtitleFontLabel = MakeLabel("", 130, y + 6);
            subtitleFontLabel.AutoEllipsis = true;
            subtitleFontLabel.Width = 238;
            Controls.Add(subtitleFontLabel);
            y += 36;

            // scale (TrackBar is 45 px tall, leave enough room)
            Controls.Add(MakeLabel(Localization.Tr("Scale"), 12, y + 4));
            scaleBar = new TrackBar
            {
                Location = new Point(70, y),
                Width = 190,
                Minimum = 100,
                Maximum = 200,
                TickStyle = TickStyle.None,
                Value = 100
            };
            scaleBar.Scroll += (s, e) => UpdateScaleLabel();
            Controls.Add(scaleBar);
            scaleLabel = MakeLabel("100%", 270, y + 4);
            scaleLabel.Width = 60;
            Controls.Add(scaleLabel);
            y += 50;

            // position
            Controls.Add(MakeLabel(Localization.Tr("Position"), 12, y + 4));
            setPositionButton = new Button { Text = Localization.Tr("SetPosition"), Location = new Point(70, y), Size = new Size(110, 28) };
            setPositionButton.Click += (s, e) => StartPositioning();
            Controls.Add(setPositionButton);
            resetPositionButton = new Button { Text = Localization.Tr("ResetPosition"), Location = new Point(188, y), Size = new Size(120, 28) };
            resetPositionButton.Click += (s, e) => ResetPosition();
            Controls.Add(resetPositionButton);
            y += 34;

            positionHint = MakeLabel("", 12, y, SystemColors.GrayText);
            positionHint.Width = 356;
            Controls.Add(positionHint);
            y += 26;

            // buttons
            testButton = new Button { Text = Localization.Tr("Test"), Location = new Point(12, y), Size = new Size(75, 28) };
            testButton.Click += (s, e) => TestPreview();
            Controls.Add(testButton);

            var delayedTestButton = new Button { Text = Localization.Tr("TestDelayed"), Location = new Point(96, y), Size = new Size(90, 28) };
            delayedTestButton.Click += (s, e) => DelayedTestPreview(delayedTestButton);
            Controls.Add(delayedTestButton);

            var okButton = new Button { Text = Localization.Tr("OK"), DialogResult = DialogResult.OK, Location = new Point(196, y), Size = new Size(80, 28) };
            Controls.Add(okButton);
            AcceptButton = okButton;

            var cancelButton = new Button { Text = Localization.Tr("Cancel"), DialogResult = DialogResult.Cancel, Location = new Point(284, y), Size = new Size(80, 28) };
            Controls.Add(cancelButton);
            CancelButton = cancelButton;

            LoadCurrentFonts();
            scaleBar.Value = (int)Math.Round(AppConfig.PopupScale * 100);
            UpdateScaleLabel();
        }

        private string EffectiveTitle()
        {
            return string.IsNullOrWhiteSpace(AppConfig.PopupTitle) ? Localization.Tr("LowBattery") : AppConfig.PopupTitle;
        }

        private string EffectiveSubtitleExample()
        {
            string format = string.IsNullOrWhiteSpace(AppConfig.PopupSubtitle) ? Localization.Tr("SubtitleDefault") : AppConfig.PopupSubtitle;
            return format
                .Replace("{battery}", "35")
                .Replace("{device}", "Xbox Wireless Controller")
                .Replace("{controller}", "Xbox Wireless Controller");
        }

        private static Label MakeLabel(string text, int x, int y, Color? color = null)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Location = new Point(x, y),
                ForeColor = color ?? SystemColors.ControlText
            };
        }

        private void LoadCurrentFonts()
        {
            titleFont = FontFromSettings(AppConfig.PopupTitleFontFamily, AppConfig.PopupTitleFontSize, AppConfig.PopupTitleFontBold);
            subtitleFont = FontFromSettings(AppConfig.PopupSubFontFamily, AppConfig.PopupSubFontSize, AppConfig.PopupSubFontBold);
            UpdateFontLabels();
        }

        private static Font FontFromSettings(string family, float size, bool bold)
        {
            string[] candidates = { family, "Segoe UI", "Segoe UI Variable Text", "Microsoft Sans Serif" };
            foreach (var name in candidates)
            {
                try
                {
                    return new Font(name, size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
                }
                catch
                {
                }
            }
            return new Font(FontFamily.GenericSansSerif, size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
        }

        private void UpdateFontLabels()
        {
            titleFontLabel.Text = $"{titleFont.FontFamily.Name}, {titleFont.SizeInPoints:F1}pt, {(titleFont.Bold ? "B" : "")}{(titleFont.Bold && titleFont.Italic ? "I" : "")}";
            subtitleFontLabel.Text = $"{subtitleFont.FontFamily.Name}, {subtitleFont.SizeInPoints:F1}pt, {(subtitleFont.Bold ? "B" : "")}{(subtitleFont.Bold && subtitleFont.Italic ? "I" : "")}";
        }

        private void PickFont(bool title)
        {
            using (var dialog = new FontDialog())
            {
                dialog.Font = title ? titleFont : subtitleFont;
                dialog.ShowEffects = true;
                dialog.AllowVerticalFonts = false;
                dialog.MaxSize = 48;
                dialog.MinSize = 6;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (title) titleFont = dialog.Font;
                    else subtitleFont = dialog.Font;
                    UpdateFontLabels();
                }
            }
        }

        private void UpdateScaleLabel()
        {
            scaleLabel.Text = scaleBar.Value + "%";
        }

        private void ApplyToSettings()
        {
            AppConfig.PopupTitle = titleBox.Text.Trim();
            AppConfig.PopupSubtitle = subtitleBox.Text.Trim();
            AppConfig.PopupTitleFontFamily = titleFont.FontFamily.Name;
            AppConfig.PopupTitleFontSize = titleFont.SizeInPoints;
            AppConfig.PopupTitleFontBold = titleFont.Bold;
            AppConfig.PopupSubFontFamily = subtitleFont.FontFamily.Name;
            AppConfig.PopupSubFontSize = subtitleFont.SizeInPoints;
            AppConfig.PopupSubFontBold = subtitleFont.Bold;
            AppConfig.PopupScale = scaleBar.Value / 100f;
        }

        private void TestPreview()
        {
            ApplyToSettings();
            BatteryAlertOverlay.ShowAlert(15, "Xbox Wireless Controller");
        }

        /// <summary>Shows the alert 5 seconds later, so it can be tested while a game has focus.</summary>
        private void DelayedTestPreview(Button button)
        {
            ApplyToSettings();
            button.Enabled = false;
            var timer = new Timer { Interval = 5000 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                button.Enabled = true;
                BatteryAlertOverlay.ShowAlert(15, "Xbox Wireless Controller");
            };
            timer.Start();
        }

        private void StartPositioning()
        {
            if (positioning) return;
            ApplyToSettings();
            BatteryAlertOverlay.ShowPositionPreview();
            positioning = true;
            movedDuringPositioning = false;
            positionHint.Text = Localization.Tr("PositioningHint");
            setPositionButton.Enabled = false;
            // make sure the dialog is active so arrow keys reach ProcessCmdKey
            Activate();
            ActiveControl = null;
        }

        private void ResetPosition()
        {
            AppConfig.PopupPosSet = false;
            AppConfig.Save();
        }

        /// <summary>
        /// Handles positioning keys (arrow / Enter / Esc are command keys, so they arrive here
        /// before KeyDown). Single press moves 1 px, holding (WM_KEYDOWN repeat flag) moves 10 px.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (positioning)
            {
                bool isRepeat = (((int)msg.WParam) >> 30 & 1) == 1;
                int step = isRepeat ? 10 : 1;

                switch (keyData & Keys.KeyCode)
                {
                    case Keys.Left:
                        BatteryAlertOverlay.MovePositionPreview(-step, 0);
                        movedDuringPositioning = true;
                        return true;
                    case Keys.Right:
                        BatteryAlertOverlay.MovePositionPreview(step, 0);
                        movedDuringPositioning = true;
                        return true;
                    case Keys.Up:
                        BatteryAlertOverlay.MovePositionPreview(0, -step);
                        movedDuringPositioning = true;
                        return true;
                    case Keys.Down:
                        BatteryAlertOverlay.MovePositionPreview(0, step);
                        movedDuringPositioning = true;
                        return true;
                    case Keys.Enter:
                        ConfirmPosition();
                        return true;
                    case Keys.Escape:
                        CancelPositioning();
                        return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ConfirmPosition()
        {
            if (movedDuringPositioning)
            {
                var center = BatteryAlertOverlay.GetPositionPreviewCenter();
                AppConfig.PopupPosSet = true;
                AppConfig.PopupPosX = center.X;
                AppConfig.PopupPosY = center.Y;
            }
            else
            {
                AppConfig.PopupPosSet = false;
            }
            AppConfig.Save();
            CancelPositioning();
        }

        private void CancelPositioning()
        {
            BatteryAlertOverlay.HidePositionPreview();
            positioning = false;
            movedDuringPositioning = false;
            positionHint.Text = "";
            setPositionButton.Enabled = true;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            BatteryAlertOverlay.HidePositionPreview();
            if (DialogResult == DialogResult.OK)
            {
                ApplyToSettings();
                AppConfig.Save();
            }
            else
            {
                // restore in-memory values mutated by Test previews so cancelled changes don't leak
                AppConfig.PopupTitle = originalTitle;
                AppConfig.PopupSubtitle = originalSubtitle;
                AppConfig.PopupTitleFontFamily = originalTitleFontFamily;
                AppConfig.PopupTitleFontSize = originalTitleFontSize;
                AppConfig.PopupTitleFontBold = originalTitleFontBold;
                AppConfig.PopupSubFontFamily = originalSubFontFamily;
                AppConfig.PopupSubFontSize = originalSubFontSize;
                AppConfig.PopupSubFontBold = originalSubFontBold;
                AppConfig.PopupScale = originalScale;
            }
            base.OnClosing(e);
        }

        /// <summary>
        /// TextBox with a painted watermark (gray example text shown while empty and unfocused).
        /// The watermark is drawn in WM_PAINT, so it never intercepts mouse clicks or focus.
        /// </summary>
        private class PlaceholderTextBox : TextBox
        {
            private const int WM_PAINT = 0x000F;

            public string Watermark { get; set; } = "";

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if (m.Msg == WM_PAINT && Text.Length == 0 && !Focused && Watermark.Length > 0)
                {
                    using (var g = CreateGraphics())
                    {
                        TextRenderer.DrawText(
                            g,
                            Watermark,
                            Font,
                            ClientRectangle,
                            SystemColors.GrayText,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    }
                }
            }
        }
    }
}
