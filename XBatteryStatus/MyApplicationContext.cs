using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Storage.Streams;

namespace XBatteryStatus
{
    public class MyApplicationContext : ApplicationContext
    {
        private static readonly Guid BatteryServiceGuid = new Guid("0000180f-0000-1000-8000-00805f9b34fb");
        private static readonly Guid BatteryLevelGuid = new Guid("00002a19-0000-1000-8000-00805f9b34fb");

        NotifyIcon notifyIcon = new NotifyIcon();
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem themeButton;
        private ToolStripMenuItem languageButton;
        private ToolStripMenuItem devicesButton;
        private ToolStripMenuItem popupButton;
        private ToolStripMenuItem startupButton;
        private ToolStripMenuItem soundButton;
        private ToolStripMenuItem exitButton;

        private Timer UpdateTimer;
        private Timer DiscoverTimer;

        public List<BleDevice> pairedDevices = new List<BleDevice>();
        public Radio bluetoothRadio;

        private Dictionary<string, DeviceSettings> deviceConfig = new Dictionary<string, DeviceSettings>();

        private bool lightMode = false;
        private static readonly string logFilePath = Path.Combine(AppContext.BaseDirectory, "log.txt");
        private readonly System.Threading.SemaphoreSlim logSemaphore = new System.Threading.SemaphoreSlim(1, 1);

        public MyApplicationContext()
        {
            AppConfig.Load();
            Localization.Initialize();
            deviceConfig = DeviceConfig.Load();
            EnsureStartupShortcut();

            Log("XBatteryStatus V" + (Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0") + " started");
            Log("Settings: language=" + AppConfig.Language + " (current=" + Localization.Current + "), pollInterval=" + AppConfig.PollInterval + "s, popupScale=" + AppConfig.PopupScale + ", popupTitle='" + AppConfig.PopupTitle + "', popupSubtitle='" + AppConfig.PopupSubtitle + "', sound='" + AppConfig.Sound + "', startup=" + AppConfig.Startup);
            Log($"Loaded device config for {deviceConfig.Count} device(s):");
            foreach (var kv in deviceConfig)
            {
                Log($"  [{kv.Key}] enabled={kv.Value.Enabled}, levels=[{string.Join(",", kv.Value.Levels ?? new[] { 0 })}], customName='{kv.Value.CustomName}'");
            }

            lightMode = IsLightMode();
            SetIcon(-1, "?");
            notifyIcon.Text = Localization.Tr("StatusLooking");
            notifyIcon.Visible = true;

            BuildContextMenu();

            InitializeBluetoothRadioAsync();
            FindBleDevices();

            UpdateTimer = new Timer();
            UpdateTimer.Tick += new EventHandler((x, y) => PollBatteries());
            UpdateTimer.Interval = GetPollIntervalMs();
            UpdateTimer.Start();

            DiscoverTimer = new Timer();
            DiscoverTimer.Tick += new EventHandler((x, y) => FindBleDevices());
            DiscoverTimer.Interval = 60000;
            DiscoverTimer.Start();
        }

        // ---------- context menu ----------

        private void BuildContextMenu()
        {
            contextMenu = new ContextMenuStrip();

            themeButton = new ToolStripMenuItem("Theme");
            themeButton.DropDownItems.Add("Auto", null, ThemeClicked);
            themeButton.DropDownItems.Add("Light", null, ThemeClicked);
            themeButton.DropDownItems.Add("Dark", null, ThemeClicked);
            UpdateThemeButton();
            contextMenu.Items.Add(themeButton);

            languageButton = new ToolStripMenuItem("Language");
            languageButton.DropDownItems.Add("Auto", null, LanguageClicked);
            foreach (var language in Enum.GetValues<AppLanguage>())
            {
                if (language == AppLanguage.Auto) continue;
                languageButton.DropDownItems.Add(Localization.LanguageName(language), null, LanguageClicked);
            }
            UpdateLanguageButton();
            contextMenu.Items.Add(languageButton);

            devicesButton = new ToolStripMenuItem("Devices", null, DevicesClicked);
            contextMenu.Items.Add(devicesButton);

            popupButton = new ToolStripMenuItem("Popup Settings", null, PopupClicked);
            contextMenu.Items.Add(popupButton);

            startupButton = new ToolStripMenuItem("Startup", null, StartupClicked);
            startupButton.Checked = AppConfig.Startup;
            contextMenu.Items.Add(startupButton);

            soundButton = new ToolStripMenuItem("Sound");
            RefreshSoundMenu();
            contextMenu.Items.Add(soundButton);
            contextMenu.Opening += (s, e) => RefreshSoundMenu();

            exitButton = new ToolStripMenuItem("Exit", null, new EventHandler(ExitClicked));
            contextMenu.Items.Add(exitButton);

            notifyIcon.ContextMenuStrip = contextMenu;

            UpdateMenuTexts();
        }

        private void UpdateMenuTexts()
        {
            if (contextMenu == null) return;

            themeButton.Text = Localization.Tr("Theme");
            ((ToolStripMenuItem)themeButton.DropDownItems[0]).Text = Localization.Tr("Auto");
            ((ToolStripMenuItem)themeButton.DropDownItems[1]).Text = Localization.Tr("Light");
            ((ToolStripMenuItem)themeButton.DropDownItems[2]).Text = Localization.Tr("Dark");

            languageButton.Text = Localization.Tr("Language");
            for (int i = 0; i < languageButton.DropDownItems.Count; i++)
            {
                if (i == 0)
                {
                    ((ToolStripMenuItem)languageButton.DropDownItems[i]).Text = Localization.Tr("Auto");
                }
                else
                {
                    ((ToolStripMenuItem)languageButton.DropDownItems[i]).Text = Localization.LanguageName((AppLanguage)i);
                }
            }

            devicesButton.Text = Localization.Tr("Devices");
            popupButton.Text = Localization.Tr("PopupSettings");
            startupButton.Text = Localization.Tr("Startup");
            soundButton.Text = Localization.Tr("Sound");
            exitButton.Text = Localization.Tr("Exit");

            UpdateStatusText();
        }

        private void LanguageClicked(object sender, EventArgs e)
        {
            int index = languageButton.DropDownItems.IndexOf((ToolStripMenuItem)sender);
            AppLanguage language = index <= 0 ? AppLanguage.Auto : (AppLanguage)index;
            AppConfig.Language = (int)language;
            AppConfig.Save();
            Localization.Initialize();
            UpdateLanguageButton();
            UpdateMenuTexts();
            Log("Language changed to: " + Localization.Current);
        }

        private void UpdateLanguageButton()
        {
            int currentIndex = Localization.Current == AppLanguage.Auto ? 0 : (int)Localization.Current;
            for (int i = 0; i < languageButton.DropDownItems.Count; i++)
            {
                ((ToolStripMenuItem)languageButton.DropDownItems[i]).Checked = i == currentIndex;
            }
        }

        private void DevicesClicked(object sender, EventArgs e)
        {
            using (var form = new DevicesForm(this))
            {
                form.ShowDialog();
            }
        }

        private void PopupClicked(object sender, EventArgs e)
        {
            using (var form = new PopupSettingsForm())
            {
                form.ShowDialog();
            }
        }

        // ---------- startup shortcut ----------

        private const string StartupShortcutName = "XBatteryStatus.lnk";

        private static string StartupShortcutPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), StartupShortcutName);

        private void StartupClicked(object sender, EventArgs e)
        {
            bool enabled = !AppConfig.Startup;
            AppConfig.Startup = enabled;
            AppConfig.Save();
            startupButton.Checked = enabled;
            if (enabled)
            {
                CreateStartupShortcut();
            }
            else
            {
                RemoveStartupShortcut();
            }
            Log("Startup toggle: " + (enabled ? "ON (shortcut created)" : "OFF (shortcut removed)"));
        }

        private void EnsureStartupShortcut()
        {
            if (AppConfig.Startup)
            {
                CreateStartupShortcut();
            }
            else
            {
                RemoveStartupShortcut();
            }
        }

        private static void CreateStartupShortcut()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string shortcutPath = StartupShortcutPath;

                if (File.Exists(shortcutPath))
                {
                    return;
                }

                dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                try
                {
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = exePath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                    shortcut.Description = "XBatteryStatus";
                    shortcut.Save();
                    Marshal.FinalReleaseComObject(shortcut);
                }
                finally
                {
                    Marshal.FinalReleaseComObject(shell);
                }
            }
            catch
            {
            }
        }

        private static void RemoveStartupShortcut()
        {
            try
            {
                if (File.Exists(StartupShortcutPath))
                {
                    File.Delete(StartupShortcutPath);
                }
            }
            catch
            {
            }
        }

        // ---------- alert sound ----------

        private static System.Media.SoundPlayer soundPlayer;

        private static string SoundFolderPath => Path.Combine(AppContext.BaseDirectory, "sound");

        /// <summary>Rebuilds the sound submenu from the wav files in the sound folder next to the exe.</summary>
        private void RefreshSoundMenu()
        {
            if (soundButton == null) return;

            soundButton.DropDownItems.Clear();

            var muteItem = new ToolStripMenuItem(Localization.Tr("Mute"), null, SoundClicked) { Tag = "" };
            muteItem.Checked = string.IsNullOrEmpty(AppConfig.Sound);
            soundButton.DropDownItems.Add(muteItem);

            try
            {
                if (Directory.Exists(SoundFolderPath))
                {
                    foreach (var file in Directory.GetFiles(SoundFolderPath, "*.wav").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                    {
                        string fileName = Path.GetFileName(file);
                        var item = new ToolStripMenuItem(Path.GetFileNameWithoutExtension(fileName), null, SoundClicked) { Tag = fileName };
                        item.Checked = string.Equals(AppConfig.Sound, fileName, StringComparison.OrdinalIgnoreCase);
                        soundButton.DropDownItems.Add(item);
                    }
                }
            }
            catch
            {
            }
        }

        private void SoundClicked(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            AppConfig.Sound = (string)item.Tag ?? "";
            AppConfig.Save();
            RefreshSoundMenu();
        }

        /// <summary>Plays the selected alert sound, if any.</summary>
        public static void PlayAlertSound()
        {
            string fileName = AppConfig.Sound;
            if (string.IsNullOrEmpty(fileName))
            {
                LogStatic("Alert sound: muted (no sound selected)");
                return;
            }

            string path = Path.Combine(SoundFolderPath, fileName);
            if (!File.Exists(path))
            {
                LogStatic($"Alert sound: file not found: {path}");
                return;
            }

            try
            {
                soundPlayer?.Dispose();
                soundPlayer = new System.Media.SoundPlayer(path);
                soundPlayer.Play();
                LogStatic("Alert sound: playing '" + fileName + "'");
            }
            catch (Exception e)
            {
                LogStatic("Alert sound: ERROR - " + e.Message);
            }
        }

        private static void LogStatic(string s)
        {
            if (!AppConfig.Logging) return;
            try
            {
                File.AppendAllText(logFilePath, $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] {s}" + Environment.NewLine);
            }
            catch
            {
            }
        }

        // ---------- bluetooth discovery ----------

        private async void InitializeBluetoothRadioAsync()
        {
            try
            {
                var radios = await Radio.GetRadiosAsync();
                bluetoothRadio = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
                if (bluetoothRadio != null)
                {
                    bluetoothRadio.StateChanged += BluetoothRadio_StateChanged;
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        async private void FindBleDevices()
        {
            if (bluetoothRadio?.State == RadioState.On)
            {
                List<BleDevice> found = new List<BleDevice>();
                HashSet<ulong> seenAddresses = new HashSet<ulong>();

                foreach (var device in await DeviceInformation.FindAllAsync())
                {
                    BluetoothLEDevice bleDevice = null;
                    bool keepDevice = false;
                    try
                    {
                        bleDevice = await BluetoothLEDevice.FromIdAsync(device.Id);

                        if (bleDevice == null) continue;

                        bool isGamepad = false;
                        try
                        {
                            isGamepad = bleDevice.Appearance.SubCategory == BluetoothLEAppearanceSubcategories.Gamepad;
                        }
                        catch
                        {
                        }

                        using (GattDeviceService service = bleDevice.GetGattService(BatteryServiceGuid))
                        {
                            if (service != null)
                            {
                                GattCharacteristic characteristic = service.GetCharacteristics(BatteryLevelGuid).FirstOrDefault();
                                if (characteristic != null)
                                {
                                    // dedupe by Bluetooth address: Windows may expose multiple instances for the same physical device
                                    if (!seenAddresses.Add(bleDevice.BluetoothAddress))
                                    {
                                        continue;
                                    }

                                    // reuse the existing entry so LastBattery, event wiring and config survive
                                    // the periodic discovery scan (avoids re-triggering first-read alerts)
                                    string addressKey = bleDevice.BluetoothAddress.ToString("X16");
                                    var existing = pairedDevices.FirstOrDefault(d => d.AddressKey == addressKey);
                                    if (existing != null)
                                    {
                                        found.Add(existing);
                                        continue; // dispose the duplicate BluetoothLEDevice in finally
                                    }

                                    var entry = new BleDevice(bleDevice, isGamepad);
                                    deviceConfig.TryGetValue(entry.AddressKey, out var config);
                                    if (config == null)
                                    {
                                        config = new DeviceSettings { Enabled = isGamepad };
                                        deviceConfig[entry.AddressKey] = config;
                                    }
                                    entry.Config = config;
                                    found.Add(entry);
                                    keepDevice = true;
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        if (!keepDevice && bleDevice != null)
                        {
                            bleDevice.Dispose();
                        }
                    }
                }

                var newDevices = found.Except(pairedDevices).ToList();
                var removedDevices = pairedDevices.Except(found).ToList();

                foreach (var device in newDevices)
                {
                    device.Device.ConnectionStatusChanged += ConnectionStatusChanged;
                }

                foreach (var device in removedDevices)
                {
                    if (device != null)
                    {
                        device.Device.ConnectionStatusChanged -= ConnectionStatusChanged;
                        device.Dispose();
                    }
                }

                pairedDevices = found;
                DeviceConfig.Save(deviceConfig);

                Log($"Discovery: {found.Count} device(s) with battery service found, {removedDevices.Count} removed");
                foreach (var device in found)
                {
                    Log($"  found [{device.AddressKey}] name='{device.DeviceName}', gamepad={device.IsGamepad}, enabled={device.Config.Enabled}, levels=[{string.Join(",", device.Config.Levels ?? new[] { 0 })}], connected={device.Connected}");
                }

                if (pairedDevices.Count == 0)
                {
                    SetIcon(-1, "!");
                    notifyIcon.Text = Localization.Tr("StatusNoDevices");
                }
                else
                {
                    PollBatteries();
                }
            }
            else
            {
                Log("Discovery: Bluetooth radio is OFF, skipping device scan");
                SetIcon(-1, "!");
                notifyIcon.Text = Localization.Tr("StatusBleOff");
            }
        }

        private void BluetoothRadio_StateChanged(Radio sender, object args)
        {
            FindBleDevices();
        }

        private void ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            var device = pairedDevices.FirstOrDefault(d => d.Device == sender);
            if (device == null) return;

            if (sender.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                Log($"Device CONNECTED: {device.DeviceName} [{device.AddressKey}] (last battery {(device.LastBattery >= 0 ? device.LastBattery + "%" : "unknown")})");
                AcquireBatteryService(device);
                PollBatteries();
            }
            else
            {
                Log($"Device DISCONNECTED: {device.DeviceName} [{device.AddressKey}] (last battery {(device.LastBattery >= 0 ? device.LastBattery + "%" : "unknown")})");
                // keep LastBattery: on reconnect the crossing check naturally fires if the battery
                // is now below a threshold, without re-alerting when it is unchanged
                UpdateStatusText();
            }
        }

        /// <summary>Re-acquires the battery service/characteristic of a freshly connected device.</summary>
        private void AcquireBatteryService(BleDevice device)
        {
            try
            {
                if (device.BatteryService != null)
                {
                    device.BatteryService.Dispose();
                    device.BatteryService = null;
                }
                device.BatteryCharacteristic = null;

                GattDeviceService service = device.Device.GetGattService(BatteryServiceGuid);
                if (service != null)
                {
                    GattCharacteristic characteristic = service.GetCharacteristics(BatteryLevelGuid).FirstOrDefault();
                    if (characteristic != null)
                    {
                        device.BatteryService = service;
                        device.BatteryCharacteristic = characteristic;
                    }
                    else
                    {
                        service.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        // ---------- polling ----------

        public List<BleDevice> GetDevices()
        {
            return pairedDevices;
        }

        private int GetPollIntervalMs()
        {
            return Math.Max(3000, AppConfig.PollInterval) * 1000;
        }

        public void ApplyDeviceConfig()
        {
            DeviceConfig.Save(deviceConfig);
            UpdateTimer.Interval = GetPollIntervalMs();
            Log($"Device config applied: pollInterval={AppConfig.PollInterval}s");
            foreach (var device in pairedDevices)
            {
                Log($"  {device.DisplayName} [{device.AddressKey}]: enabled={device.Config.Enabled}, levels=[{string.Join(",", device.Config.Levels ?? new[] { 0 })}], customName='{device.Config.CustomName}'");
            }
            PollBatteries();
        }

        private async void PollBatteries()
        {
            var connectedDevices = pairedDevices.Where(d => d.Connected).ToList();
            if (connectedDevices.Count == 0)
            {
                Log("Poll: no connected devices");
                SetIcon(-1, "!");
                notifyIcon.Text = Localization.Tr("StatusDisconnected");
                return;
            }

            BleDevice trayDevice = connectedDevices.FirstOrDefault(d => d.IsGamepad) ?? connectedDevices[0];

            foreach (var device in connectedDevices)
            {
                try
                {
                    await ReadBattery(device);
                }
                catch (Exception e)
                {
                    Log($"Poll: unexpected error reading {device.DisplayName}: {e.Message}");
                }
            }

            if (trayDevice.LastBattery >= 0)
            {
                SetIcon(trayDevice.LastBattery);
                notifyIcon.Text = Localization.Tr("StatusBattery", trayDevice.LastBattery + "% - " + trayDevice.DisplayName);
            }
            else
            {
                SetIcon(-1, "!");
                notifyIcon.Text = Localization.Tr("StatusDisconnected");
            }
        }

        private async Task ReadBattery(BleDevice device)
        {
            if (!device.Connected || device.BatteryCharacteristic == null)
            {
                AcquireBatteryService(device);
            }
            if (device.BatteryCharacteristic == null)
            {
                Log($"Read {device.DisplayName} [{device.AddressKey}]: SKIPPED - no battery characteristic");
                return;
            }

            GattReadResult result;
            try
            {
                // 5 second timeout so one hung device can't block the whole poll cycle
                var readTask = device.BatteryCharacteristic.ReadValueAsync().AsTask();
                var completed = await Task.WhenAny(readTask, Task.Delay(5000));
                if (completed != readTask)
                {
                    Log($"Read {device.DisplayName} [{device.AddressKey}]: TIMEOUT after 5s, battery stuck at {(device.LastBattery >= 0 ? device.LastBattery + "%" : "unknown")}");
                    return;
                }
                result = readTask.Result;
            }
            catch (Exception e)
            {
                Log($"Read {device.DisplayName} [{device.AddressKey}]: ERROR - {e.Message}");
                return;
            }

            if (result.Status == GattCommunicationStatus.Success)
            {
                var reader = DataReader.FromBuffer(result.Value);
                int val = reader.ReadByte();

                if (device.LastBattery >= 0 && device.Config.Enabled)
                {
                    CheckBatteryAlerts(device, val);
                }
                else if (device.LastBattery < 0 && device.Config.Enabled)
                {
                    // First reading after connect/startup: a crossing cannot be detected,
                    // but if the battery already sits at/below a threshold (e.g. the controller
                    // slept and reconnected low), alert once anyway.
                    CheckFirstReadAlert(device, val);
                }
                int previous = device.LastBattery;
                device.LastBattery = val;
                Log($"Read {device.DisplayName} [{device.AddressKey}]: {val}% (prev {previous}%, enabled={device.Config.Enabled}, levels=[{string.Join(",", device.Config.Levels ?? new[] { 0 })}])");
            }
            else
            {
                Log($"Read {device.DisplayName} [{device.AddressKey}]: FAILED with status {result.Status}");
            }
        }

        private void CheckFirstReadAlert(BleDevice device, int val)
        {
            int[] levels = device.Config.Levels;
            if (levels == null || levels.Length != 3) levels = new[] { 35, 30, 25 };

            foreach (int level in levels)
            {
                if (level >= 1 && level <= 100 && val <= level)
                {
                    Log($"ALERT TRIGGERED for {device.DisplayName} [{device.AddressKey}]: first read after connect is {val}% which is at/below level {level}%");
                    BatteryAlertOverlay.ShowAlert(val, device.DisplayName);
                    break;
                }
            }
        }

        private void CheckBatteryAlerts(BleDevice device, int val)
        {
            int[] levels = device.Config.Levels;
            if (levels == null || levels.Length != 3) levels = new[] { 35, 30, 25 };

            foreach (int level in levels)
            {
                if (level >= 1 && level <= 100 && device.LastBattery > level && val <= level)
                {
                    Log($"ALERT TRIGGERED for {device.DisplayName} [{device.AddressKey}]: {device.LastBattery}% -> {val}% crossed level {level}%");
                    BatteryAlertOverlay.ShowAlert(val, device.DisplayName);
                    break;
                }
            }
        }

        // ---------- tray icon ----------

        private void UpdateStatusText()
        {
            var connected = pairedDevices.Where(d => d.Connected).ToList();
            if (connected.Count == 0)
            {
                notifyIcon.Text = Localization.Tr("StatusDisconnected");
            }
            else
            {
                var trayDevice = connected.FirstOrDefault(d => d.IsGamepad) ?? connected[0];
                notifyIcon.Text = trayDevice.LastBattery >= 0
                    ? Localization.Tr("StatusBattery", trayDevice.LastBattery + "% - " + trayDevice.DisplayName)
                    : Localization.Tr("StatusBattery", trayDevice.DisplayName);
            }
        }

        // ---------- misc UI handlers ----------

        private void ExitClicked(object sender, EventArgs e)
        {
            Exit();
        }

        protected override void ExitThreadCore()
        {
            Exit();
            base.ExitThreadCore();
        }

        private void Exit()
        {
            foreach (var device in pairedDevices)
            {
                if (device != null)
                {
                    device.Device.ConnectionStatusChanged -= ConnectionStatusChanged;
                    device.Dispose();
                }
            }
            pairedDevices.Clear();

            if (notifyIcon.Icon != null)
            {
                DestroyIcon(notifyIcon.Icon.Handle);
                notifyIcon.Icon.Dispose();
                notifyIcon.Icon = null;
            }

            notifyIcon.Visible = false;

            logSemaphore?.Dispose();

            Application.Exit();
        }

        private void ThemeClicked(object sender, EventArgs e)
        {
            if (sender == themeButton.DropDownItems[1]) { AppConfig.Theme = 1; }
            else if (sender == themeButton.DropDownItems[2]) { AppConfig.Theme = 2; }
            else { AppConfig.Theme = 0; }
            AppConfig.Save();
            UpdateThemeButton();
        }

        private void UpdateThemeButton()
        {
            if (AppConfig.Theme == 1)
            {
                ((ToolStripMenuItem)themeButton.DropDownItems[0]).Checked = false;
                ((ToolStripMenuItem)themeButton.DropDownItems[1]).Checked = true;
                ((ToolStripMenuItem)themeButton.DropDownItems[2]).Checked = false;
            }
            else if (AppConfig.Theme == 2)
            {
                ((ToolStripMenuItem)themeButton.DropDownItems[0]).Checked = false;
                ((ToolStripMenuItem)themeButton.DropDownItems[1]).Checked = false;
                ((ToolStripMenuItem)themeButton.DropDownItems[2]).Checked = true;
            }
            else
            {
                ((ToolStripMenuItem)themeButton.DropDownItems[0]).Checked = true;
                ((ToolStripMenuItem)themeButton.DropDownItems[1]).Checked = false;
                ((ToolStripMenuItem)themeButton.DropDownItems[2]).Checked = false;
            }

            FindBleDevices();
        }

        public bool IsLightMode()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            if (key != null)
            {
                object registryValueObject = key.GetValue("AppsUseLightTheme");

                if (registryValueObject != null)
                {
                    int registryValue = (int)registryValueObject;
                    return registryValue == 1;
                }
            }

            return true;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        public void SetIcon(int val, string s = "")
        {
            Icon oldIcon = notifyIcon.Icon;
            IntPtr oldHandle = IntPtr.Zero;

            if (oldIcon != null)
            {
                oldHandle = oldIcon.Handle;
            }

            notifyIcon.Icon = GetIcon(val, s);

            if (oldIcon != null)
            {
                oldIcon.Dispose();
                if (oldHandle != IntPtr.Zero)
                {
                    DestroyIcon(oldHandle);
                }
            }
        }

        public Icon GetIcon(int val, string s = "")
        {
            using (var icon = (Bitmap)Properties.Resources.icon00.Clone())
            {
                try
                {
                    if (val >= 0)
                    {
                        AddPercentage(icon, val);
                    }
                    else
                    {
                        if (s == "!")
                        {
                            AddSymbol(icon, Properties.Resources.symbolE);
                        }
                        else if (s == "?")
                        {
                            AddSymbol(icon, Properties.Resources.symbolQ);
                        }
                    }

                    if (!((AppConfig.Theme == 0 && !lightMode) || AppConfig.Theme == 1))
                    {
                        InvertBitmap(icon);
                    }

                    IntPtr hIcon = icon.GetHicon();
                    try
                    {
                        return (Icon)Icon.FromHandle(hIcon).Clone();
                    }
                    finally
                    {
                        DestroyIcon(hIcon);
                    }
                }
                catch
                {
                    return (Icon)SystemIcons.Application.Clone();
                }
            }
        }

        public Bitmap AddPercentage(Bitmap bitmap, int val)
        {
            int y_start = 7 + (int)((100 - val) / 5.0 + 0.5);

            for (int y = y_start; y < 27; y++)
            {
                for (int x = 20; x < 28; x++)
                {
                    Color pixelColor = Color.FromArgb(255, 255, 255, 255);
                    if (pixelColor.A > 0)
                    {
                        bitmap.SetPixel(x, y, pixelColor);
                    }
                }
            }

            return bitmap;
        }

        public Bitmap AddSymbol(Bitmap bitmap, Bitmap symbol)
        {
            int x_start = 19;
            int y_start = 6;

            for (int y = 0; y < symbol.Height; y++)
            {
                for (int x = 0; x < symbol.Width; x++)
                {
                    Color pixelColor = symbol.GetPixel(x, y);
                    if (pixelColor.A > 0)
                    {
                        bitmap.SetPixel(x + x_start, y + y_start, pixelColor);
                    }
                }
            }

            return bitmap;
        }

        public Bitmap InvertBitmap(Bitmap bitmap)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixelColor = bitmap.GetPixel(x, y);
                    Color invertedColor = Color.FromArgb(pixelColor.A, 255 - pixelColor.R, 255 - pixelColor.G, 255 - pixelColor.B);
                    bitmap.SetPixel(x, y, invertedColor);
                }
            }
            return bitmap;
        }

        private async void Log(string s)
        {
#if DEBUG
            Console.WriteLine(s);
#else
            if (!AppConfig.Logging) return;
            await logSemaphore.WaitAsync();
            try
            {
                // keep the log file under 10 MB: when exceeded, keep the last half
                if (File.Exists(logFilePath) && new FileInfo(logFilePath).Length > 10 * 1024 * 1024)
                {
                    var lines = File.ReadAllLines(logFilePath);
                    File.WriteAllLines(logFilePath, lines.Skip(lines.Length / 2));
                }

                string logEntry = $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] {s}";
                await File.AppendAllTextAsync(logFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
            }
            finally
            {
                logSemaphore.Release();
            }
#endif
        }

        private void LogError(Exception e)
        {
            Log(e.StackTrace);
            Log(e.Message);
            Log("");
        }
    }
}
