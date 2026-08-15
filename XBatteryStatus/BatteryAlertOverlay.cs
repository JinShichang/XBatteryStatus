using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace XBatteryStatus
{
    /// <summary>
    /// Xbox achievement style overlay popup (bottom center, pop + expand animation).
    /// Uses a per-pixel alpha layered window so it never steals focus and is click-through.
    /// Title and subtitle fonts, text and size are customizable. The position can be set
    /// interactively (positioning preview mode stays visible until confirmed).
    /// </summary>
    public class BatteryAlertOverlay : Form
    {
        // layout constants (logical pixels @96dpi, multiplied by popup scale and system DPI)
        private const float Pad = 48f;
        private const float PillH = 64f;
        private const float TextGapX = 14f;
        private const float RightPad = 38f;
        private const float BottomMargin = 28f;
        private const float MinPillW = 250f;
        private const float MaxPillW = 600f;

        // animation timeline (milliseconds)
        private const int PopMs = 320;
        private const int ExpandStart = 180;
        private const int ExpandMs = 380;
        private const int TextStart = 380;
        private const int TextMs = 280;
        private const int ShineStart = 620;
        private const int ShineMs = 530;
        private const int GlowMs = 700;
        private const int RingMs = 600;
        private const int TextOutStart = 5000;
        private const int TextOutMs = 180;
        private const int ShrinkStart = 5150;
        private const int ShrinkMs = 330;
        private const int PopOutStart = 5450;
        private const int PopOutMs = 300;
        private const int TotalMs = 5850;

        private static BatteryAlertOverlay current;
        private static BatteryAlertOverlay positionPreview;

        private readonly bool previewMode;

        private string titleText;
        private string subtitleText;
        private int batteryPercent;

        private float effScale = 1f;      // popupScale * dpi / 96
        private float fullPillW;
        private int winW, winH;

        private Bitmap bmp;
        private Timer timer;
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        private Font titleFont;
        private Font subFont;

        private static readonly Color CardColor = Color.FromArgb(245, 30, 30, 30);
        private static readonly Color CardBorderColor = Color.FromArgb(28, 255, 255, 255);
        private static readonly Color SubColor = Color.FromArgb(176, 176, 176);

        internal BatteryAlertOverlay(int batteryPercent, string deviceName, bool previewMode)
        {
            this.batteryPercent = batteryPercent;
            this.previewMode = previewMode;

            BuildTexts(deviceName);
            BuildFonts();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            TopMost = true;

            SetupLayout();
            var handle = Handle; // force handle creation for UpdateLayeredWindow

            if (previewMode)
            {
                RenderFrame(3000); // fully expanded static frame (TotalMs would render the collapsed end state)
                PushToWindow();
            }
            else
            {
                timer = new Timer { Interval = 15 };
                timer.Tick += OnTick;
                sw.Restart();
                timer.Start();
            }
        }

        // ---------- public API ----------

        /// <summary>Shows the battery alert overlay, restarting the currently visible one if any.</summary>
        public static void ShowAlert(int batteryPercent, string deviceName)
        {
            MyApplicationContext.PlayAlertSound();
            if (current != null && !current.IsDisposed)
            {
                current.Restart(batteryPercent, deviceName);
                return;
            }
            current = new BatteryAlertOverlay(batteryPercent, deviceName, false);
            current.Show();
        }

        /// <summary>Shows the positioning preview (persistent, full state).</summary>
        public static void ShowPositionPreview()
        {
            HidePositionPreview();
            positionPreview = new BatteryAlertOverlay(50, "XBatteryStatus", true);
            positionPreview.Show();
            ClampToWorkArea(positionPreview);
        }

        /// <summary>Moves the positioning preview window.</summary>
        public static void MovePositionPreview(int dx, int dy)
        {
            if (positionPreview == null || positionPreview.IsDisposed) return;
            positionPreview.SetBounds(positionPreview.Left + dx, positionPreview.Top + dy, positionPreview.Width, positionPreview.Height);
            ClampToWorkArea(positionPreview);
        }

        /// <summary>Center of the positioning preview window (window center == pill center).</summary>
        public static Point GetPositionPreviewCenter()
        {
            if (positionPreview != null && !positionPreview.IsDisposed)
            {
                return new Point(positionPreview.Left + positionPreview.Width / 2, positionPreview.Top + positionPreview.Height / 2);
            }
            return GetPopupCenter();
        }

        public static void HidePositionPreview()
        {
            if (positionPreview != null && !positionPreview.IsDisposed)
            {
                positionPreview.Close();
            }
            positionPreview = null;
        }

        /// <summary>Current popup center position, respecting the stored position.</summary>
        public static Point GetPopupCenter()
        {
            if (AppConfig.PopupPosSet)
            {
                return new Point(AppConfig.PopupPosX, AppConfig.PopupPosY);
            }
            return DefaultPopupCenter();
        }

        private static Point DefaultPopupCenter()
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            float dpi = GetDpiForSystem() / 96f;
            float effScale = AppConfig.PopupScale * dpi;
            int cx = wa.Left + wa.Width / 2;
            int cy = wa.Bottom - (int)Math.Ceiling(BottomMargin * dpi + PillH / 2f * effScale);
            return new Point(cx, cy);
        }

        private static void ClampToWorkArea(BatteryAlertOverlay overlay)
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int x = Math.Max(wa.Left, Math.Min(overlay.Left, wa.Right - overlay.Width));
            int y = Math.Max(wa.Top, Math.Min(overlay.Top, wa.Bottom - overlay.Height));
            overlay.SetBounds(x, y, overlay.Width, overlay.Height);
            overlay.PushToWindow();
        }

        private void Restart(int batteryPercent, string deviceName)
        {
            this.batteryPercent = batteryPercent;
            BuildTexts(deviceName);
            BuildFonts();
            SetupLayout();
            sw.Restart();
            if (!timer.Enabled) timer.Start();
        }

        // ---------- content ----------

        private void BuildTexts(string deviceName)
        {
            string settingsTitle = AppConfig.PopupTitle;
            string settingsSubtitle = AppConfig.PopupSubtitle;

            titleText = string.IsNullOrWhiteSpace(settingsTitle) ? Localization.Tr("LowBattery") : settingsTitle;

            string subFormat = string.IsNullOrWhiteSpace(settingsSubtitle) ? Localization.Tr("SubtitleDefault") : settingsSubtitle;
            subtitleText = subFormat
                .Replace("{battery}", batteryPercent.ToString())
                .Replace("{device}", deviceName)
                .Replace("{controller}", deviceName);
        }

        private void BuildFonts()
        {
            titleFont?.Dispose();
            subFont?.Dispose();
            float scale = AppConfig.PopupScale;
            titleFont = CreateFont(AppConfig.PopupTitleFontFamily, AppConfig.PopupTitleFontSize * scale, AppConfig.PopupTitleFontBold ? FontStyle.Bold : FontStyle.Regular);
            subFont = CreateFont(AppConfig.PopupSubFontFamily, AppConfig.PopupSubFontSize * scale, AppConfig.PopupSubFontBold ? FontStyle.Bold : FontStyle.Regular);
        }

        private static Font CreateFont(string familyName, float size, FontStyle style)
        {
            string[] families = { familyName, "Segoe UI Variable Text", "Segoe UI", "Microsoft Sans Serif" };
            foreach (var family in families)
            {
                try
                {
                    return new Font(family, size, style, GraphicsUnit.Point);
                }
                catch
                {
                }
            }
            return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Point);
        }

        private void SetupLayout()
        {
            float dpiRatio = GetDpiForSystem() / 96f;
            effScale = AppConfig.PopupScale * dpiRatio;

            float titleW, subW, titleH, subH;
            using (var measureBmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(measureBmp))
            {
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                titleW = g.MeasureString(titleText, titleFont, int.MaxValue, StringFormat.GenericTypographic).Width;
                subW = g.MeasureString(subtitleText, subFont, int.MaxValue, StringFormat.GenericTypographic).Width;
                titleH = titleFont.GetHeight(96f);
                subH = subFont.GetHeight(96f);
            }

            // convert the 96 dpi measurements to physical pixels
            titleW *= dpiRatio;
            subW *= dpiRatio;
            titleH *= dpiRatio;
            subH *= dpiRatio;
            this.titleH = titleH;
            textGap = 4f * effScale;
            textBlockH = titleH + textGap + subH;

            fullPillW = Math.Max(MinPillW, Math.Min(MaxPillW, PillH + TextGapX + Math.Max(titleW, subW) + RightPad)) * effScale;

            winW = (int)Math.Ceiling(fullPillW + Pad * 2f * effScale);
            winH = (int)Math.Ceiling((PillH + Pad * 2f) * effScale);

            Point center = GetPopupCenter();
            Bounds = new Rectangle(center.X - winW / 2, center.Y - winH / 2, winW, winH);

            bmp?.Dispose();
            bmp = new Bitmap(winW, winH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }

        private float textBlockH;
        private float textGap;
        private float titleH;

        // ---------- animation ----------

        private void OnTick(object sender, EventArgs e)
        {
            long ms = sw.ElapsedMilliseconds;
            if (ms >= TotalMs)
            {
                timer.Stop();
                Close();
                return;
            }

            try
            {
                RenderFrame(ms);
                PushToWindow();
            }
            catch
            {
                timer.Stop();
                Close();
            }
        }

        private void RenderFrame(long ms)
        {
            float t = ms;

            double popT = Clamp01(t / PopMs);
            double circleScale = popT < 1 ? EaseOutBack(popT) : 1;

            double expandT = Clamp01((t - ExpandStart) / ExpandMs);
            double pillW = Lerp(PillH * effScale, fullPillW, CubicBezierY(expandT, 0, 0.5, 1, 1));

            double textT = Clamp01((t - TextStart) / TextMs);
            double textAlpha = textT;
            float textRise = (float)(1 - EaseOutCubic(textT)) * 10f * effScale;

            double shineT = Clamp01((t - ShineStart) / ShineMs);

            double glowT = Clamp01(t / GlowMs);
            double ringT = Clamp01(t / RingMs);

            textAlpha *= 1 - Clamp01((t - TextOutStart) / TextOutMs);

            double shrinkT = Clamp01((t - ShrinkStart) / ShrinkMs);
            if (shrinkT > 0) pillW = Lerp(fullPillW, PillH * effScale, CubicBezierY(shrinkT, 0.75, 0, 1, 1));

            double popOutT = Clamp01((t - PopOutStart) / PopOutMs);
            if (popOutT > 0) circleScale = 1 - EaseInCubic(popOutT);

            if (circleScale <= 0.01) return;

            float pad = Pad * effScale;
            float pillH = PillH * effScale;
            float pw = (float)pillW; // already physical pixels
            float px = (winW - pw) / 2f;
            float py = pad;
            float ccx = px + pillH / 2f;
            float ccy = py + pillH / 2f;
            float r = pillH / 2f;

            bool critical = batteryPercent <= 10;
            Color circleTop = critical ? Color.FromArgb(228, 70, 70) : Color.FromArgb(22, 168, 22);
            Color circleBottom = critical ? Color.FromArgb(163, 22, 33) : Color.FromArgb(13, 100, 13);
            Color glowColor = critical ? Color.FromArgb(230, 80, 80) : Color.FromArgb(60, 220, 60);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.Clear(Color.Transparent);

                using (var pillPath = Capsule(px, py, pw, pillH))
                {
                    // drop shadow
                    float shadowAlpha = (float)Math.Min(1, circleScale);
                    for (int i = 12; i >= 1; i--)
                    {
                        float grow = i * 1.6f * effScale;
                        using (var shadowPath = Capsule(px - grow / 2f, py + 5f * effScale + grow * 0.3f - grow / 2f, pw + grow, pillH + grow))
                        using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(7 * shadowAlpha), 0, 0, 0)))
                        {
                            g.FillPath(shadowBrush, shadowPath);
                        }
                    }

                    // radial glow around the circle
                    if (glowT > 0 && glowT < 1)
                    {
                        int glowA = (int)(Math.Sin(glowT * Math.PI) * 115);
                        float gr = 85f * effScale;
                        using (var glowPath = new GraphicsPath())
                        {
                            glowPath.AddEllipse(ccx - gr, ccy - gr, gr * 2, gr * 2);
                            using (var glowBrush = new PathGradientBrush(glowPath))
                            {
                                glowBrush.CenterColor = Color.FromArgb(glowA, glowColor);
                                glowBrush.SurroundColors = new[] { Color.FromArgb(0, glowColor) };
                                g.FillEllipse(glowBrush, ccx - gr, ccy - gr, gr * 2, gr * 2);
                            }
                        }
                    }

                    // expanding ring around the circle
                    if (ringT > 0 && ringT < 1)
                    {
                        int ringA = (int)((1 - ringT) * 140);
                        float rr = r + (float)ringT * 30f * effScale;
                        using (var ringPen = new Pen(Color.FromArgb(ringA, glowColor), 2.5f * effScale))
                        {
                            g.DrawEllipse(ringPen, ccx - rr, ccy - rr, rr * 2, rr * 2);
                        }
                    }

                    // card background
                    using (var cardBrush = new SolidBrush(CardColor))
                    {
                        g.FillPath(cardBrush, pillPath);
                    }
                    using (var borderPen = new Pen(CardBorderColor, Math.Max(1f, effScale)))
                    {
                        g.DrawPath(borderPen, pillPath);
                    }

                    // shine sweep (clipped to the card)
                    if (shineT > 0 && shineT < 1)
                    {
                        float bandW = 56f * effScale;
                        float shineX = px - bandW + (float)shineT * (pw + bandW * 2);
                        int shineA = (int)(Math.Sin(shineT * Math.PI) * 56);
                        var oldClip = g.Clip;
                        var oldTransform = g.Transform;
                        g.SetClip(pillPath);
                        var shineMatrix = new Matrix();
                        shineMatrix.Translate(shineX, 0);
                        shineMatrix.Shear(-0.577f, 0); // ~ -30 degrees
                        g.Transform = shineMatrix;
                        var shineRect = new RectangleF(0, py - 10, bandW, pillH + 20);
                        using (var shineBrush = new LinearGradientBrush(shineRect, Color.Transparent, Color.White, LinearGradientMode.Horizontal))
                        {
                            var blend = new ColorBlend(3);
                            blend.Colors = new[] { Color.FromArgb(0, 255, 255, 255), Color.FromArgb(shineA, 255, 255, 255), Color.FromArgb(0, 255, 255, 255) };
                            blend.Positions = new[] { 0f, 0.5f, 1f };
                            shineBrush.InterpolationColors = blend;
                            g.FillRectangle(shineBrush, shineRect);
                        }
                        g.Transform = oldTransform;
                        g.Clip = oldClip;
                    }

                    // circle with battery icon (scaled pop)
                    var state = g.Save();
                    g.TranslateTransform(ccx, ccy);
                    float cs = (float)circleScale;
                    g.ScaleTransform(cs, cs);

                    using (var circleBrush = new LinearGradientBrush(new RectangleF(-r, -r, r * 2, r * 2), circleTop, circleBottom, LinearGradientMode.Vertical))
                    {
                        g.FillEllipse(circleBrush, -r, -r, r * 2, r * 2);
                    }

                    DrawBatteryIcon(g, r);

                    g.Restore(state);

                    // texts (vertically centered, clipped to the card, right of the circle)
                    if (textAlpha > 0.004)
                    {
                        int alpha = (int)(textAlpha * 255);
                        float tx = px + pillH + TextGapX * effScale;
                        float blockTop = ccy - textBlockH / 2f;

                        var oldClip2 = g.Clip;
                        g.SetClip(pillPath);
                        g.SetClip(new RectangleF(px + pillH * 0.75f, py, pw, pillH), CombineMode.Intersect);

                        using (var titleBrush = new SolidBrush(Color.FromArgb(alpha, Color.White)))
                        {
                            g.DrawString(titleText, titleFont, titleBrush, tx, blockTop + textRise, StringFormat.GenericTypographic);
                        }
                        using (var subBrush = new SolidBrush(Color.FromArgb(alpha, SubColor)))
                        {
                            g.DrawString(subtitleText, subFont, subBrush, tx, blockTop + titleH + textGap + textRise, StringFormat.GenericTypographic);
                        }

                        g.Clip = oldClip2;
                    }
                }
            }
        }

        private void DrawBatteryIcon(Graphics g, float circleRadius)
        {
            float s = effScale;
            float bodyW = 30f * s, bodyH = 16f * s;
            float bx = -bodyW / 2f, by = -bodyH / 2f;

            using (var pen = new Pen(Color.White, 2.2f * s))
            using (var bodyPath = RoundedRect(bx, by, bodyW, bodyH, 4f * s))
            {
                g.DrawPath(pen, bodyPath);
            }

            using (var whiteBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(whiteBrush, bx + bodyW + 1.2f * s, -4f * s, 3.6f * s, 8f * s);

                float inset = 3.5f * s;
                float fillW = (bodyW - inset * 2) * Math.Max(0, Math.Min(100, batteryPercent)) / 100f;
                if (fillW > 0.5f)
                {
                    using (var fillPath = RoundedRect(bx + inset, by + inset, fillW, bodyH - inset * 2, 2f * s))
                    {
                        g.FillPath(whiteBrush, fillPath);
                    }
                }
            }
        }

        private static GraphicsPath Capsule(float x, float y, float w, float h)
        {
            var path = new GraphicsPath();
            path.AddArc(x, y, h, h, 90, 180);
            path.AddArc(x + w - h, y, h, h, 270, 180);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath RoundedRect(float x, float y, float w, float h, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        internal void PushToWindow()
        {
            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(screenDc);
            IntPtr hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
            IntPtr oldBitmap = SelectObject(memDc, hBitmap);
            try
            {
                SIZE size = new SIZE(bmp.Width, bmp.Height);
                POINT src = new POINT(0, 0);
                POINT dst = new POINT(Left, Top);
                BLENDFUNCTION blend = new BLENDFUNCTION
                {
                    BlendOp = 0,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = 1
                };
                UpdateLayeredWindow(Handle, screenDc, ref dst, ref size, memDc, ref src, 0, ref blend, 2 /* ULW_ALPHA */);
            }
            finally
            {
                SelectObject(memDc, oldBitmap);
                DeleteObject(hBitmap);
                DeleteDC(memDc);
                ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        // ---------- animation helpers ----------

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        private static double EaseOutBack(double t)
        {
            const double c1 = 1.70158;
            const double c3 = c1 + 1;
            double u = t - 1;
            return 1 + c3 * u * u * u + c1 * u * u;
        }

        private static double EaseOutCubic(double t)
        {
            double u = 1 - t;
            return 1 - u * u * u;
        }

        private static double EaseInCubic(double t) => t * t * t;

        private static double CubicBezierY(double x, double x1, double y1, double x2, double y2)
        {
            double t = x;
            for (int i = 0; i < 8; i++)
            {
                double cx = Bezier(t, 0, x1, x2, 1) - x;
                if (Math.Abs(cx) < 1e-6) break;
                double dx = BezierDerivative(t, 0, x1, x2, 1);
                if (Math.Abs(dx) < 1e-6) break;
                t -= cx / dx;
                t = Clamp01(t);
            }
            return Bezier(t, 0, y1, y2, 1);
        }

        private static double Bezier(double t, double p0, double p1, double p2, double p3)
        {
            double u = 1 - t;
            return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
        }

        private static double BezierDerivative(double t, double p0, double p1, double p2, double p3)
        {
            double u = 1 - t;
            return 3 * u * u * (p1 - p0) + 6 * u * t * (p2 - p1) + 3 * t * t * (p3 - p2);
        }

        // ---------- window setup ----------

        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation => true;

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            timer?.Stop();
            timer?.Dispose();
            bmp?.Dispose();
            titleFont?.Dispose();
            subFont?.Dispose();
            if (current == this) current = null;
            if (positionPreview == this) positionPreview = null;
            base.OnFormClosed(e);
        }

        // ---------- P/Invoke ----------

        [DllImport("user32.dll")]
        private static extern int GetDpiForSystem();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, uint dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y) { X = x; Y = y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
            public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }
    }
}
