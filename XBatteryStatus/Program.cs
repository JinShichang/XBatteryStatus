using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XBatteryStatus
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Localization.Initialize();

            // preview the battery alert popup without a connected controller
            if (args.Any(a => a.Equals("--test-popup", StringComparison.OrdinalIgnoreCase)))
            {
                var overlay = new BatteryAlertOverlay(15, "Xbox Wireless Controller", false);
                overlay.FormClosed += (s, e) => Application.Exit();
                overlay.Show();
                Application.Run();
                return;
            }

            // dump popup rendering parameters for DPI debugging
            if (args.Any(a => a.Equals("--diag-popup", StringComparison.OrdinalIgnoreCase)))
            {
                var overlay = new BatteryAlertOverlay(15, "Xbox Wireless Controller", false);
                try
                {
                    string text = overlay.WriteDiagnostics(System.IO.Path.Combine(AppContext.BaseDirectory, "popup_diag.txt"));
                    System.IO.File.WriteAllText(System.IO.Path.GetTempPath() + "popup_diag.txt", text);
                }
                catch (Exception ex)
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "popup_diag.txt"), "ERROR: " + ex);
                }
                overlay.Close();
                Application.Exit();
                return;
            }

            // screenshot-test the popup settings dialog
            if (args.Any(a => a.Equals("--test-dialog", StringComparison.OrdinalIgnoreCase)))
            {
                var form = new PopupSettingsForm();
                var closeTimer = new Timer { Interval = 5000 };
                closeTimer.Tick += (s, e) => { closeTimer.Stop(); form.Close(); };
                closeTimer.Start();
                form.ShowDialog();
                Application.Exit();
                return;
            }

            // screenshot-test the position preview overlay
            if (args.Any(a => a.Equals("--test-position", StringComparison.OrdinalIgnoreCase)))
            {
                BatteryAlertOverlay.ShowPositionPreview();
                var closeTimer = new Timer { Interval = 5000 };
                closeTimer.Tick += (s, e) => { closeTimer.Stop(); BatteryAlertOverlay.HidePositionPreview(); Application.Exit(); };
                closeTimer.Start();
                Application.Run();
                return;
            }

            // screenshot-test the devices window
            if (args.Any(a => a.Equals("--test-devices", StringComparison.OrdinalIgnoreCase)))
            {
                var ctx = new MyApplicationContext();
                var form = new DevicesForm(ctx);
                var closeTimer = new Timer { Interval = 40000 };
                closeTimer.Tick += (s, e) => { closeTimer.Stop(); form.Close(); Application.Exit(); };
                closeTimer.Start();
                form.ShowDialog();
                return;
            }

            var proc = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(proc.ProcessName);

            if (processes.Length > 1)
            {
                foreach (var process in processes)
                {
                    if (process.Id != proc.Id)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                    }
                }
            }

            Application.Run(new MyApplicationContext());
        }
    }
}


