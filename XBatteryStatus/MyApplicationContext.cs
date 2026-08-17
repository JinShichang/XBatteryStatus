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
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;
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
        public List<PnpBatteryDevice> pnpDevices = new List<PnpBatteryDevice>();
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

        private bool discoveryRunning;
        private bool rescanScheduled;
        private readonly HashSet<string> probedAddresses = new HashSet<string>();

        /// <summary>Raised after a discovery scan finishes so open UIs (e.g. the devices window) can refresh.</summary>
        public event Action DevicesChanged;

        /// <summary>Fast refresh for open UIs: updates PnP/HFP battery values and reads connected
        /// BLE devices without re-running the slow discovery passes.</summary>
        public async Task RefreshDevicesLightAsync()
        {
            await RefreshPnpBatteries(false);
            foreach (var device in pairedDevices.Where(d => d.Connected))
            {
                try
                {
                    await ReadBattery(device);
                }
                catch
                {
                }
            }
            DevicesChanged?.Invoke();
        }

        /// <summary>Publishes the current discovery result (wiring, config save, tray/UI refresh).
        /// Called after the fast passes so devices appear immediately, and again once the slow
        /// passes finish to pick up any devices they found.</summary>
        private void CommitFoundDevices(List<BleDevice> found, int totalEntries, int bleInterfaces, int fromIdFailed, string phase)
        {
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

            // a physical device may have been added via the PnP pass before the LE passes
            // finished; keep the richer LE/GATT entry and drop the PnP duplicate
            pnpDevices.RemoveAll(p => pairedDevices.Any(q => q.AddressKey == p.AddressKey));
            if (AppConfig.Logging)
            {
                Log($"Discovery ({phase}): PnP battery devices after cleanup: {pnpDevices.Count} -> {string.Join(", ", pnpDevices.Select(p => p.DisplayName + " (" + p.LastBattery + "%)"))}");
            }

            DeviceConfig.Save(deviceConfig);

            Log($"Discovery ({phase}): total entries={totalEntries}, BLE interfaces={bleInterfaces}, FromIdAsync failed={fromIdFailed}, {found.Count} device(s) with battery service, {removedDevices.Count} removed");
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

            DevicesChanged?.Invoke();
        }

        /// <summary>Runs a device scan. Safe to call from anywhere (reentrancy guarded).</summary>
        public async Task RefreshDevicesAsync()
        {
            await ScanDevicesCore();
        }

        async private void FindBleDevices()
        {
            await ScanDevicesCore();
        }

        private async Task ScanDevicesCore()
        {
            if (discoveryRunning) return;

            if (bluetoothRadio?.State != RadioState.On && bluetoothRadio != null)
            {
                Log("Discovery: Bluetooth radio is OFF, skipping device scan");
                SetIcon(-1, "!");
                notifyIcon.Text = Localization.Tr("StatusBleOff");
                return;
            }

            discoveryRunning = true;
            try
            {
                // fast pass: PnP battery property first so the earbuds/audio battery appears in
                // the devices window and tray immediately, before the slower LE passes finish.
                await RefreshPnpBatteries(false);

                List<BleDevice> found = new List<BleDevice>();
                HashSet<ulong> seenAddresses = new HashSet<ulong>();
                int totalEntries = 0;
                int bleInterfaces = 0;
                int fromIdFailed = 0;

                foreach (var device in await DeviceInformation.FindAllAsync())
                {
                    totalEntries++;
                    // BluetoothLEDevice.FromIdAsync fails fast for non-BLE ids; no prefix filter
                    // here because some systems report BLE interfaces with unexpected id formats
                    bleInterfaces++;

                    BluetoothLEDevice bleDevice = null;
                    bool keepDevice = false;
                    try
                    {
                        try
                        {
                            bleDevice = await BluetoothLEDevice.FromIdAsync(device.Id);
                        }
                        catch (Exception ex)
                        {
                            if (AppConfig.Logging && device.Id.Contains("BTH", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(device.Name))
                            {
                                Log($"  FromIdAsync FAILED for '{device.Name}' ({device.Id}): {ex.Message}");
                            }
                            fromIdFailed++;
                            continue;
                        }

                        if (bleDevice == null)
                        {
                            if (AppConfig.Logging && device.Id.Contains("BTH", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(device.Name))
                            {
                                Log($"  FromIdAsync returned null for '{device.Name}' ({device.Id})");
                            }
                            fromIdFailed++;
                            continue;
                        }

                        bool isGamepad = false;
                        try
                        {
                            isGamepad = bleDevice.Appearance.SubCategory == BluetoothLEAppearanceSubcategories.Gamepad;
                        }
                        catch
                        {
                        }

                        bool hasBattery = false;
                        string debugInfo = $"BLE candidate: name='{bleDevice.Name}', connected={bleDevice.ConnectionStatus == BluetoothConnectionStatus.Connected}";

                        // 1) quick check against the cached GATT services (works after the first GATT session)
                        using (GattDeviceService service = bleDevice.GetGattService(BatteryServiceGuid))
                        {
                            if (service != null)
                            {
                                GattCharacteristic characteristic = service.GetCharacteristics(BatteryLevelGuid).FirstOrDefault();
                                hasBattery = characteristic != null;
                            }
                        }
                        debugInfo += $", battery(cached)={hasBattery}";

                        // 2) the cache may be empty for devices whose GATT was never opened before
                        //    (e.g. headphones/speakers paired over classic audio). Perform a real
                        //    service enumeration - it establishes the GATT session itself, so it
                        //    works even while the BLE side reports as not connected.
                        if (!hasBattery)
                        {
                            try
                            {
                                var enumTask = bleDevice.GetGattServicesAsync().AsTask();
                                var completed = await Task.WhenAny(enumTask, Task.Delay(10000));
                                if (completed == enumTask)
                                {
                                    var servicesResult = enumTask.Result;
                                    if (servicesResult.Status == GattCommunicationStatus.Success)
                                    {
                                        hasBattery = servicesResult.Services.Any(s => s.Uuid == BatteryServiceGuid);
                                        if (!hasBattery && AppConfig.Logging)
                                        {
                                            string svcs = string.Join(", ", servicesResult.Services.Take(12).Select(s => s.Uuid.ToString().Substring(4, 8)));
                                            Log($"BLE candidate '{bleDevice.Name}': no standard battery service, exposed: {svcs}");
                                        }
                                        foreach (var s in servicesResult.Services)
                                        {
                                            s.Dispose();
                                        }
                                    }
                                }
                                else
                                {
                                    Log($"BLE candidate '{bleDevice.Name}': service enumeration TIMEOUT after 10s");
                                }
                            }
                            catch
                            {
                            }
                            debugInfo += $", battery(enum)={hasBattery}";
                        }

                        if (hasBattery)
                        {
                            // dedupe by Bluetooth address: Windows may expose multiple instances for the same physical device
                            if (!seenAddresses.Add(bleDevice.BluetoothAddress))
                            {
                                Log(debugInfo + " -> skipped (duplicate address)");
                                continue;
                            }

                            Log(debugInfo + " -> kept");

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
                        else
                        {
                            Log(debugInfo + " -> no battery service");
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

                // second pass: BLE association endpoints. Some BLE devices (e.g. earbuds paired
                // over classic audio) only show up here, which is how Windows itself reads their battery.
                // Note: the IsPaired flag is unreliable here (paired devices often report False),
                // so every endpoint is considered; the async service probe below only runs once
                // per address to avoid poking unrelated nearby devices repeatedly.
                try
                {
                    var bleEndpoints = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector());
                    foreach (var ep in bleEndpoints)
                    {
                        if (AppConfig.Logging)
                        {
                            Log($"  BLE endpoint: name='{ep.Name}', paired={ep.Pairing?.IsPaired == true}, id={ep.Id}");
                        }

                        BluetoothLEDevice epDevice = null;
                        bool keepEp = false;
                        try
                        {
                            epDevice = await BluetoothLEDevice.FromIdAsync(ep.Id);
                            if (epDevice == null) continue;

                            if (!seenAddresses.Add(epDevice.BluetoothAddress))
                            {
                                continue; // already handled by the first pass
                            }

                            bool epHasBattery = false;
                            using (GattDeviceService service = epDevice.GetGattService(BatteryServiceGuid))
                            {
                                if (service != null)
                                {
                                    GattCharacteristic characteristic = service.GetCharacteristics(BatteryLevelGuid).FirstOrDefault();
                                    epHasBattery = characteristic != null;
                                }
                            }

                            string epKey = epDevice.BluetoothAddress.ToString("X16");

                            // only probe the GATT services once per address
                            if (!epHasBattery && probedAddresses.Add(epKey))
                            {
                                try
                                {
                                    var enumTask = epDevice.GetGattServicesAsync().AsTask();
                                    var completed = await Task.WhenAny(enumTask, Task.Delay(10000));
                                    if (completed == enumTask)
                                    {
                                        var servicesResult = enumTask.Result;
                                        if (servicesResult.Status == GattCommunicationStatus.Success)
                                        {
                                            epHasBattery = servicesResult.Services.Any(s => s.Uuid == BatteryServiceGuid);
                                            if (!epHasBattery && AppConfig.Logging)
                                            {
                                                string svcs = string.Join(", ", servicesResult.Services.Take(12).Select(s => s.Uuid.ToString().Substring(4, 8)));
                                                Log($"  BLE endpoint '{epDevice.Name}': no standard battery service, exposed: {svcs}");
                                            }
                                            foreach (var s in servicesResult.Services)
                                            {
                                                s.Dispose();
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }

                            if (epHasBattery)
                            {
                                Log($"  BLE endpoint '{epDevice.Name}': battery found (connected={epDevice.ConnectionStatus == BluetoothConnectionStatus.Connected})");
                            }

                            if (!epHasBattery)
                            {
                                continue;
                            }

                            bool epGamepad = false;
                            try
                            {
                                epGamepad = epDevice.Appearance.SubCategory == BluetoothLEAppearanceSubcategories.Gamepad;
                            }
                            catch
                            {
                            }

                            var existing = pairedDevices.FirstOrDefault(d => d.AddressKey == epKey);
                            if (existing != null)
                            {
                                found.Add(existing);
                                continue;
                            }

                            var epEntry = new BleDevice(epDevice, epGamepad);
                            deviceConfig.TryGetValue(epEntry.AddressKey, out var epConfig);
                            if (epConfig == null)
                            {
                                epConfig = new DeviceSettings { Enabled = epGamepad };
                                deviceConfig[epEntry.AddressKey] = epConfig;
                            }
                            epEntry.Config = epConfig;
                            found.Add(epEntry);
                            keepEp = true;
                        }
                        catch
                        {
                        }
                        finally
                        {
                            if (!keepEp && epDevice != null)
                            {
                                epDevice.Dispose();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("BLE endpoint pass failed: " + ex.Message);
                }

                // fast commit: publish the devices found by the quick passes (PnP property, BLE
                // interface scan, association endpoints) right away so the tray icon and devices
                // window show results within seconds of startup; the slow passes below keep going
                // and commit again when they finish.
                CommitFoundDevices(found, totalEntries, bleInterfaces, fromIdFailed, "fast");

                // third pass: classic paired Bluetooth devices (e.g. earbuds/speakers paired over
                // classic audio) may expose their BLE battery service under the same address, but
                // Windows does not enumerate a BLE interface for them. Connecting directly to the
                // address (like Windows Settings does) reads the GATT battery service anyway.
                try
                {
                    var classicDevices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelector());
                    foreach (var classic in classicDevices)
                    {
                        BluetoothDevice classicDevice = null;
                        try
                        {
                            classicDevice = await BluetoothDevice.FromIdAsync(classic.Id);
                        }
                        catch
                        {
                        }
                        if (classicDevice == null) continue;

                        if (AppConfig.Logging)
                        {
                            Log($"  Classic device: name='{classicDevice.Name}', addr={classicDevice.BluetoothAddress:X12}");
                        }

                        BluetoothLEDevice bridgeDevice = null;
                        bool keepBridge = false;
                        try
                        {
                            bridgeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(classicDevice.BluetoothAddress);
                            if (bridgeDevice == null)
                            {
                                if (AppConfig.Logging)
                                {
                                    Log($"  Classic->BLE '{classicDevice.Name}': FromBluetoothAddressAsync returned null");
                                }
                                continue;
                            }

                            if (!seenAddresses.Add(bridgeDevice.BluetoothAddress))
                            {
                                continue; // already handled by a previous pass
                            }

                            bool bridgeHasBattery = false;
                            using (GattDeviceService service = bridgeDevice.GetGattService(BatteryServiceGuid))
                            {
                                if (service != null)
                                {
                                    GattCharacteristic characteristic = service.GetCharacteristics(BatteryLevelGuid).FirstOrDefault();
                                    bridgeHasBattery = characteristic != null;
                                }
                            }

                            string bridgeKey = bridgeDevice.BluetoothAddress.ToString("X16");

                            if (AppConfig.Logging)
                            {
                                Log($"  Classic->BLE '{bridgeDevice.Name}': created, cached battery={bridgeHasBattery}, connected={bridgeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected}");
                            }

                            if (!bridgeHasBattery && probedAddresses.Add(bridgeKey))
                            {
                                try
                                {
                                    var enumTask = bridgeDevice.GetGattServicesAsync().AsTask();
                                    var completed = await Task.WhenAny(enumTask, Task.Delay(10000));
                                    if (completed == enumTask)
                                    {
                                        var servicesResult = enumTask.Result;
                                        if (AppConfig.Logging)
                                        {
                                            Log($"  Classic->BLE '{bridgeDevice.Name}': enum status={servicesResult.Status}, services={servicesResult.Services.Count}");
                                        }
                                        if (servicesResult.Status == GattCommunicationStatus.Success)
                                        {
                                            bridgeHasBattery = servicesResult.Services.Any(s => s.Uuid == BatteryServiceGuid);
                                            if (!bridgeHasBattery && AppConfig.Logging)
                                            {
                                                string svcs = string.Join(", ", servicesResult.Services.Take(12).Select(s => s.Uuid.ToString().Substring(4, 8)));
                                                Log($"  Classic->BLE '{bridgeDevice.Name}': no standard battery service, exposed: {svcs}");
                                            }
                                            foreach (var s in servicesResult.Services)
                                            {
                                                s.Dispose();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Log($"  Classic->BLE '{bridgeDevice.Name}': enum TIMEOUT after 10s");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log($"  Classic->BLE '{bridgeDevice.Name}': enum ERROR - {ex.Message}");
                                }
                            }

                            if (!bridgeHasBattery)
                            {
                                continue;
                            }

                            Log($"  Classic->BLE '{bridgeDevice.Name}' [{bridgeKey}]: battery service found (connected={bridgeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected})");

                            bool bridgeGamepad = false;
                            try
                            {
                                bridgeGamepad = bridgeDevice.Appearance.SubCategory == BluetoothLEAppearanceSubcategories.Gamepad;
                            }
                            catch
                            {
                            }

                            var bridgeExisting = pairedDevices.FirstOrDefault(d => d.AddressKey == bridgeKey);
                            if (bridgeExisting != null)
                            {
                                found.Add(bridgeExisting);
                                continue;
                            }

                            var bridgeEntry = new BleDevice(bridgeDevice, bridgeGamepad);
                            deviceConfig.TryGetValue(bridgeEntry.AddressKey, out var bridgeConfig);
                            if (bridgeConfig == null)
                            {
                                bridgeConfig = new DeviceSettings { Enabled = bridgeGamepad };
                                deviceConfig[bridgeEntry.AddressKey] = bridgeConfig;
                            }
                            bridgeEntry.Config = bridgeConfig;
                            found.Add(bridgeEntry);
                            keepBridge = true;
                        }
                        catch (Exception ex)
                        {
                            if (AppConfig.Logging)
                            {
                                Log($"  Classic->BLE '{classicDevice.Name}': ERROR - {ex.Message}");
                            }
                        }
                        finally
                        {
                            if (!keepBridge && bridgeDevice != null)
                            {
                                bridgeDevice.Dispose();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("Classic->BLE pass failed: " + ex.Message);
                }

                // fourth pass: BLE advertisement scan. Audio devices like earbuds/speakers are
                // often paired over classic Bluetooth only, so their LE address is unknown to
                // Windows. An advertisement scan reveals their real LE address and advertised
                // service UUIDs; then we can connect to their GATT battery service directly.
                try
                {
                    var advertised = new Dictionary<ulong, string>();
                    var advertisedUuids = new Dictionary<ulong, List<Guid>>();
                    var watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
                    watcher.Received += (s, args) =>
                    {
                        try
                        {
                            string name = args.Advertisement.LocalName ?? string.Empty;
                            lock (advertised)
                            {
                                if (!advertised.ContainsKey(args.BluetoothAddress))
                                {
                                    advertised[args.BluetoothAddress] = name;
                                    advertisedUuids[args.BluetoothAddress] = new List<Guid>(args.Advertisement.ServiceUuids);
                                }
                            }
                        }
                        catch
                        {
                        }
                    };
                    watcher.Start();
                    await Task.Delay(12000);
                    watcher.Stop();

                    var advertisedSnapshot = new List<KeyValuePair<ulong, string>>();
                    var uuidsSnapshot = new Dictionary<ulong, List<Guid>>();
                    lock (advertised)
                    {
                        foreach (var kvp in advertised)
                        {
                            advertisedSnapshot.Add(kvp);
                            uuidsSnapshot[kvp.Key] = advertisedUuids[kvp.Key];
                        }
                    }

                    if (AppConfig.Logging)
                    {
                        foreach (var kvp in advertisedSnapshot)
                        {
                            string uuids = string.Join(",", uuidsSnapshot[kvp.Key].Take(6).Select(u => u.ToString().Substring(4, 8)));
                            Log($"  Advertised: addr={kvp.Key:X12}, name='{kvp.Value}', uuids=[{uuids}]");
                        }
                    }

                    foreach (var kvp in advertisedSnapshot)
                    {
                            if (!seenAddresses.Add(kvp.Key))
                            {
                                continue; // already handled by a previous pass
                            }

                            BluetoothLEDevice advDevice = null;
                            bool keepAdv = false;
                            try
                            {
                                advDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(kvp.Key);
                                if (advDevice == null)
                                {
                                    if (AppConfig.Logging)
                                    {
                                        Log($"  Adv->BLE '{kvp.Value}': FromBluetoothAddressAsync returned null");
                                    }
                                    continue;
                                }

                                bool advHasBattery = false;
                                using (GattDeviceService service = advDevice.GetGattService(BatteryServiceGuid))
                                {
                                    if (service != null)
                                    {
                                        GattCharacteristic characteristic = service.GetCharacteristics(BatteryLevelGuid).FirstOrDefault();
                                        advHasBattery = characteristic != null;
                                    }
                                }

                                string advKey = advDevice.BluetoothAddress.ToString("X16");

                                if (AppConfig.Logging)
                                {
                                    Log($"  Adv->BLE '{advDevice.Name}': created, cached battery={advHasBattery}");
                                }

                                if (!advHasBattery && probedAddresses.Add(advKey))
                                {
                                    try
                                    {
                                        var enumTask = advDevice.GetGattServicesAsync().AsTask();
                                        var completed = await Task.WhenAny(enumTask, Task.Delay(10000));
                                        if (completed == enumTask)
                                        {
                                            var servicesResult = enumTask.Result;
                                            if (AppConfig.Logging)
                                            {
                                                Log($"  Adv->BLE '{advDevice.Name}': enum status={servicesResult.Status}, services={servicesResult.Services.Count}");
                                            }
                                            if (servicesResult.Status == GattCommunicationStatus.Success)
                                            {
                                                advHasBattery = servicesResult.Services.Any(s => s.Uuid == BatteryServiceGuid);
                                                if (!advHasBattery && AppConfig.Logging)
                                                {
                                                    string svcs = string.Join(", ", servicesResult.Services.Take(12).Select(s => s.Uuid.ToString().Substring(4, 8)));
                                                    Log($"  Adv->BLE '{advDevice.Name}': no standard battery service, exposed: {svcs}");
                                                }
                                                foreach (var s in servicesResult.Services)
                                                {
                                                    s.Dispose();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Log($"  Adv->BLE '{advDevice.Name}': enum TIMEOUT after 10s");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log($"  Adv->BLE '{advDevice.Name}': enum ERROR - {ex.Message}");
                                    }
                                }

                                if (!advHasBattery)
                                {
                                    continue;
                                }

                                Log($"  Adv->BLE '{advDevice.Name}' [{advKey}]: battery service found (connected={advDevice.ConnectionStatus == BluetoothConnectionStatus.Connected})");

                                bool advGamepad = false;
                                try
                                {
                                    advGamepad = advDevice.Appearance.SubCategory == BluetoothLEAppearanceSubcategories.Gamepad;
                                }
                                catch
                                {
                                }

                                var advExisting = pairedDevices.FirstOrDefault(d => d.AddressKey == advKey);
                                if (advExisting != null)
                                {
                                    found.Add(advExisting);
                                    continue;
                                }

                                var advEntry = new BleDevice(advDevice, advGamepad);
                                deviceConfig.TryGetValue(advEntry.AddressKey, out var advConfig);
                                if (advConfig == null)
                                {
                                    advConfig = new DeviceSettings { Enabled = advGamepad };
                                    deviceConfig[advEntry.AddressKey] = advConfig;
                                }
                                advEntry.Config = advConfig;
                                found.Add(advEntry);
                                keepAdv = true;
                            }
                            catch (Exception ex)
                            {
                                if (AppConfig.Logging)
                                {
                                    Log($"  Adv->BLE '{kvp.Value}': ERROR - {ex.Message} (HResult=0x{ex.HResult:X8})");
                                }
                            }
                            finally
                            {
                                if (!keepAdv && advDevice != null)
                                {
                                    advDevice.Dispose();
                                }
                            }
                        }
                }
                catch (Exception ex)
                {
                    Log("Advertisement pass failed: " + ex.Message);
                }

                // fifth pass: Windows exposes earbud/speaker battery via Power Battery child
                // nodes (bthport creates them while the classic audio connection is active).
                try
                {
                    var batteryNodes = await DeviceInformation.FindAllAsync(Battery.GetDeviceSelector());
                    if (AppConfig.Logging)
                    {
                        Log($"Battery pass: {batteryNodes.Count} battery node(s)");
                    }
                    foreach (var b in batteryNodes)
                    {
                        if (AppConfig.Logging)
                        {
                            Log($"  Battery node: name='{b.Name}', id={b.Id}");
                        }
                        Battery battery = await Battery.FromIdAsync(b.Id);
                        if (battery == null)
                        {
                            if (AppConfig.Logging)
                            {
                                Log($"  Battery node '{b.Name}': FromIdAsync returned null");
                            }
                            continue;
                        }
                        var report = battery.GetReport();
                        double percent = -1;
                        if (report.RemainingCapacityInMilliwattHours != null && report.FullChargeCapacityInMilliwattHours != null && report.FullChargeCapacityInMilliwattHours > 0)
                        {
                            percent = (double)report.RemainingCapacityInMilliwattHours.Value / report.FullChargeCapacityInMilliwattHours.Value * 100.0;
                        }
                        if (AppConfig.Logging)
                        {
                            Log($"  Battery node '{b.Name}': remaining={report.RemainingCapacityInMilliwattHours}, full={report.FullChargeCapacityInMilliwattHours}, percent={percent:F0}, status={report.Status}");
                        }
                        battery.ReportUpdated -= BatteryNode_ReportUpdated;
                        battery.ReportUpdated += BatteryNode_ReportUpdated;
                    }
                }
                catch (Exception ex)
                {
                    Log("Battery pass failed: " + ex.Message);
                }

                // full commit: slow passes (classic bridge, advertisement watch, battery nodes)
                // may have found additional devices; publish everything now
                CommitFoundDevices(found, totalEntries, bleInterfaces, fromIdFailed, "full");

                // the Bluetooth radio may have just turned on while the BLE stack was still
                // starting up: if the scan saw no BLE interfaces at all, retry shortly
                if (bleInterfaces == 0 && !rescanScheduled)
                {
                    rescanScheduled = true;
                    Log("Discovery: 0 BLE interfaces seen, scheduling a re-scan in 10s");
                    var rescanTimer = new Timer { Interval = 10000 };
                    rescanTimer.Tick += (s, e) =>
                    {
                        rescanTimer.Stop();
                        rescanTimer.Dispose();
                        rescanScheduled = false;
                        FindBleDevices();
                    };
                    rescanTimer.Start();
                }
            }
            catch (Exception e)
            {
                Log("Discovery: ERROR - " + e.Message);
            }
            finally
            {
                discoveryRunning = false;
            }
        }

        private void BluetoothRadio_StateChanged(Radio sender, object args)
        {
            FindBleDevices();
        }

        private async void BatteryNode_ReportUpdated(Battery sender, object args)
        {
            try
            {
                var report = sender.GetReport();
                double percent = -1;
                if (report.RemainingCapacityInMilliwattHours != null && report.FullChargeCapacityInMilliwattHours != null && report.FullChargeCapacityInMilliwattHours > 0)
                {
                    percent = (double)report.RemainingCapacityInMilliwattHours.Value / report.FullChargeCapacityInMilliwattHours.Value * 100.0;
                }
                Log($"Battery node report updated: remaining={report.RemainingCapacityInMilliwattHours}, full={report.FullChargeCapacityInMilliwattHours}, percent={percent:F0}, status={report.Status}");
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        private async void ConnectionStatusChanged(BluetoothLEDevice sender, object args)        {
            var device = pairedDevices.FirstOrDefault(d => d.Device == sender);
            if (device == null) return;

            if (sender.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                Log($"Device CONNECTED: {device.DeviceName} [{device.AddressKey}] (last battery {(device.LastBattery >= 0 ? device.LastBattery + "%" : "unknown")})");
                await AcquireBatteryService(device);
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
        private async Task AcquireBatteryService(BleDevice device)
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
                        return;
                    }
                    service.Dispose();
                }

                // cached lookup failed: perform a real service enumeration (needed for devices
                // whose GATT cache is still empty, e.g. headphones paired over classic audio)
                if (device.Connected)
                {
                    try
                    {
                        var servicesResult = await device.Device.GetGattServicesAsync();
                        if (servicesResult.Status == GattCommunicationStatus.Success)
                        {
                            foreach (var s in servicesResult.Services)
                            {
                                if (s.Uuid == BatteryServiceGuid)
                                {
                                    GattCharacteristic characteristic = s.GetCharacteristics(BatteryLevelGuid).FirstOrDefault();
                                    if (characteristic != null)
                                    {
                                        device.BatteryService = s;
                                        device.BatteryCharacteristic = characteristic;
                                        break;
                                    }
                                }
                                s.Dispose();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log($"AcquireBatteryService enum failed for {device.DeviceName}: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        // ---------- polling ----------

        public List<IBatteryDevice> GetDevices()
        {
            var list = new List<IBatteryDevice>();
            list.AddRange(pairedDevices);
            list.AddRange(pnpDevices);
            return list;
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
            foreach (var device in GetDevices())
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
                Log("Poll: no connected Bluetooth devices");
            }

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

            await RefreshPnpBatteries(true);

            PollXInput();
            UpdateTrayIcon();
        }

        /// <summary>
        /// Reads the PnP battery property ({104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2) that Windows
        /// publishes for Bluetooth devices (LE and classic audio/HFP). Devices are created on first
        /// sight; the live value is pushed into LastBattery each call. Alerts only fire when
        /// fireAlerts is true (polling), not during a discovery refresh.
        /// </summary>
        private async Task RefreshPnpBatteries(bool fireAlerts, HashSet<string> knownLeKeys = null)
        {
            try
            {
                const string batteryKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
                var batteryDevices = await DeviceInformation.FindAllAsync(null, new[] { batteryKey }, DeviceInformationKind.Device);

                // connected state: the classic Bluetooth link must be up for the battery to be current.
                // Windows keeps showing the last known value in the property even after disconnect.
                var classicConnected = new Dictionary<string, bool>();
                try
                {
                    var classicDevs = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelector());
                    foreach (var cd in classicDevs)
                    {
                        try
                        {
                            var bd = await BluetoothDevice.FromIdAsync(cd.Id);
                            if (bd != null)
                            {
                                classicConnected["0000" + bd.BluetoothAddress.ToString("X12")] = bd.ConnectionStatus == BluetoothConnectionStatus.Connected;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }

                var seenKeys = new HashSet<string>();
                foreach (var d in batteryDevices)
                {
                    if (!d.Properties.TryGetValue(batteryKey, out var value) || value == null) continue;
                    if (!d.Id.Contains("BTH", StringComparison.OrdinalIgnoreCase)) continue;

                    string address = null;
                    foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(d.Id, "Dev_([0-9A-Fa-f]{12})|&0&([0-9A-Fa-f]{12})_"))
                    {
                        address = m.Groups[1].Success ? m.Groups[1].Value : (m.Groups[2].Success ? m.Groups[2].Value : null);
                        if (address != null) break;
                    }
                    if (address == null) continue;

                    string addrKey = "0000" + address.ToUpperInvariant();
                    seenKeys.Add(addrKey);

                    // physical device already covered by the LE/GATT path (current scan or last scan)
                    bool knownLe = pairedDevices.Any(p => p.AddressKey == addrKey) || (knownLeKeys != null && knownLeKeys.Contains(addrKey));
                    if (knownLe) continue;

                    var existing = pnpDevices.FirstOrDefault(p => p.AddressKey == addrKey);
                    if (existing == null)
                    {
                        existing = new PnpBatteryDevice(d.Id, d.Name);
                        deviceConfig.TryGetValue(addrKey, out var cfg);
                        if (cfg == null)
                        {
                            cfg = new DeviceSettings();
                            deviceConfig[addrKey] = cfg;
                        }
                        existing.Config = cfg;
                        pnpDevices.Add(existing);
                        Log($"PnP battery device discovered: '{d.Name}' [{addrKey}] ({d.Id})");
                    }

                    if (int.TryParse(value.ToString(), out int percent) && percent >= 0 && percent <= 100)
                    {
                        if (fireAlerts && existing.LastBattery >= 0 && existing.Config.Enabled)
                        {
                            CheckBatteryLevelCrossing(existing.Config, existing.LastBattery, percent, existing.DisplayName, existing.AddressKey);
                        }
                        else if (fireAlerts && existing.LastBattery < 0 && existing.Config.Enabled)
                        {
                            CheckFirstReadLevelCrossing(existing.Config, percent, existing.DisplayName, existing.AddressKey);
                        }
                        existing.LastBattery = percent;
                        existing.HasValue = true;
                        Log($"PnP battery read {existing.DisplayName} [{existing.AddressKey}]: {percent}% (enabled={existing.Config.Enabled}, levels=[{string.Join(",", existing.Config.Levels ?? new[] { 0 })}])");
                    }
                }

                // drop PnP entries for physical devices now covered by the LE/GATT path
                var leKeys = new HashSet<string>(pairedDevices.Select(p => p.AddressKey));
                if (knownLeKeys != null) leKeys.UnionWith(knownLeKeys);
                pnpDevices.RemoveAll(p => leKeys.Contains(p.AddressKey));

                // mark devices whose battery property disappeared as disconnected (keep last value)
                foreach (var device in pnpDevices)
                {
                    bool seen = seenKeys.Contains(device.AddressKey);
                    bool connected = seen && classicConnected.TryGetValue(device.AddressKey, out bool conn) && conn;
                    if (!connected && device.HasValue)
                    {
                        Log($"PnP battery device gone: {device.DisplayName} [{device.AddressKey}] (last {device.LastBattery}%)");
                    }
                    device.HasValue = seen;
                    device.IsConnected = connected;
                }
            }
            catch (Exception ex)
            {
                Log("PnP battery pass failed: " + ex.Message);
            }
        }

        private void CheckBatteryLevelCrossing(DeviceSettings config, int lastBattery, int val, string displayName, string addressKey)
        {
            int[] levels = config.Levels;
            if (levels == null || levels.Length != 3) levels = new[] { 35, 30, 25 };

            foreach (int level in levels)
            {
                if (level >= 1 && level <= 100 && lastBattery > level && val <= level)
                {
                    Log($"ALERT TRIGGERED for {displayName} [{addressKey}]: {lastBattery}% -> {val}% crossed level {level}%");
                    BatteryAlertOverlay.ShowAlert(val, displayName);
                    break;
                }
            }
        }

        private void CheckFirstReadLevelCrossing(DeviceSettings config, int val, string displayName, string addressKey)
        {
            int[] levels = config.Levels;
            if (levels == null || levels.Length != 3) levels = new[] { 35, 30, 25 };

            foreach (int level in levels)
            {
                if (level >= 1 && level <= 100 && val <= level)
                {
                    Log($"ALERT TRIGGERED for {displayName} [{addressKey}]: first read is {val}% which is at/below level {level}%");
                    BatteryAlertOverlay.ShowAlert(val, displayName);
                    break;
                }
            }
        }

        // ---------- XInput (wireless adapter) ----------

        private readonly int[] xinputLastLevel = { -1, -1, -1, -1 };
        private readonly bool[] xinputConnected = { false, false, false, false };

        /// <summary>Polls the Xbox Wireless Adapter slots and triggers alerts when a level is crossed.</summary>
        private void PollXInput()
        {
            for (int slot = 0; slot < XInputHelper.MaxSlots; slot++)
            {
                var level = XInputHelper.GetWirelessBatteryLevel(slot);
                if (level == null)
                {
                    if (xinputConnected[slot])
                    {
                        xinputConnected[slot] = false;
                        Log($"XInput slot {slot + 1}: DISCONNECTED (last level {(xinputLastLevel[slot] >= 0 ? LevelName((XInputLevel)xinputLastLevel[slot]) : "unknown")})");
                    }
                    continue;
                }

                bool wasConnected = xinputConnected[slot];
                xinputConnected[slot] = true;

                var config = GetXInputConfig(slot);
                int val = (int)level.Value;
                int prev = xinputLastLevel[slot];

                if (config.Enabled)
                {
                    if (prev >= 0)
                    {
                        CheckXInputAlerts(slot, config, prev, val);
                    }
                    else if (IsAtOrBelowConfigured(val, config))
                    {
                        // first read after connect: alert once if already at/below a configured level
                        Log($"XInput slot {slot + 1}: ALERT TRIGGERED - first read after connect is {LevelName(level.Value)} which is at/below a configured level");
                        BatteryAlertOverlay.ShowAlert(MapLevelToPercent(level.Value), XInputDisplayName(slot));
                    }
                }

                xinputLastLevel[slot] = val;
                Log($"XInput slot {slot + 1}: {LevelName(level.Value)} (prev {(prev >= 0 ? LevelName((XInputLevel)prev) : "unknown")}, connected={(wasConnected ? "yes" : "no")}, enabled={config.Enabled}, levels=[{string.Join(",", config.Levels ?? new[] { -1 })}])");
            }
        }

        private void CheckXInputAlerts(int slot, DeviceSettings config, int prev, int val)
        {
            int[] levels = config.Levels;
            if (levels == null || levels.Length != 3) levels = new[] { 2, 1, 0 };

            foreach (int level in levels)
            {
                if (level >= 0 && level <= 3 && prev > level && val <= level)
                {
                    Log($"XInput slot {slot + 1}: ALERT TRIGGERED: {LevelName((XInputLevel)prev)} -> {LevelName((XInputLevel)val)} crossed {LevelName((XInputLevel)level)}");
                    BatteryAlertOverlay.ShowAlert(MapLevelToPercent((XInputLevel)val), XInputDisplayName(slot));
                    break;
                }
            }
        }

        private static bool IsAtOrBelowConfigured(int val, DeviceSettings config)
        {
            int[] levels = config.Levels;
            if (levels == null || levels.Length != 3) levels = new[] { 2, 1, 0 };
            foreach (int level in levels)
            {
                if (level >= 0 && level <= 3 && val <= level) return true;
            }
            return false;
        }

        private DeviceSettings GetXInputConfig(int slot)
        {
            string key = "xinput:" + slot;
            if (!deviceConfig.TryGetValue(key, out var config))
            {
                config = new DeviceSettings { Enabled = true, Levels = new[] { 2, 1, 0 } }; // Medium / Low / Empty
                deviceConfig[key] = config;
            }
            return config;
        }

        public DeviceSettings GetXInputConfigPublic(int slot) => GetXInputConfig(slot);

        public bool IsXInputConnected(int slot)
        {
            return slot >= 0 && slot < XInputHelper.MaxSlots && xinputConnected[slot] && xinputLastLevel[slot] >= 0;
        }

        public int GetXInputLevel(int slot)
        {
            return (slot >= 0 && slot < XInputHelper.MaxSlots) ? xinputLastLevel[slot] : -1;
        }

        public string XInputDisplayName(int slot)
        {
            return Localization.Tr("XboxControllerSlot", slot + 1) + " (" + Localization.Tr("Adapter") + ")";
        }

        public static string LevelName(XInputLevel level)
        {
            return level switch
            {
                XInputLevel.Full => Localization.Tr("BatteryFull"),
                XInputLevel.Medium => Localization.Tr("BatteryMedium"),
                XInputLevel.Low => Localization.Tr("BatteryLow"),
                _ => Localization.Tr("BatteryEmpty")
            };
        }

        public static int MapLevelToPercent(XInputLevel level)
        {
            return level switch
            {
                XInputLevel.Full => 100,
                XInputLevel.Medium => 66,
                XInputLevel.Low => 33,
                _ => 0
            };
        }

        /// <summary>Updates the tray icon: Bluetooth gamepad first, then Bluetooth, then the adapter.</summary>
        private void UpdateTrayIcon()
        {
            var lines = new List<string>();
            int? xinputTray = null;
            PnpBatteryDevice pnpTray = null;

            var bleConnected = pairedDevices.Where(d => d.Connected).ToList();
            foreach (var device in bleConnected)
            {
                lines.Add(device.LastBattery >= 0
                    ? Localization.Tr("StatusBattery", device.LastBattery + "% - " + device.DisplayName)
                    : Localization.Tr("StatusDisconnected"));
            }

            for (int slot = 0; slot < XInputHelper.MaxSlots; slot++)
            {
                if (IsXInputConnected(slot))
                {
                    lines.Add(XInputDisplayName(slot) + ": " + LevelName((XInputLevel)xinputLastLevel[slot]));
                    xinputTray ??= slot;
                }
            }

            foreach (var device in pnpDevices.Where(d => d.IsConnected))
            {
                lines.Add(device.LastBattery >= 0
                    ? Localization.Tr("StatusBattery", device.LastBattery + "% - " + device.DisplayName)
                    : Localization.Tr("StatusDisconnected"));
                pnpTray ??= device;
            }

            if (lines.Count == 0)
            {
                SetIcon(-1, "!");
                notifyIcon.Text = Localization.Tr("StatusDisconnected");
                return;
            }

            // icon graphic source priority: gamepad BLE > first BLE > XInput > PnP
            BleDevice iconBle = bleConnected.FirstOrDefault(d => d.IsGamepad) ?? bleConnected.FirstOrDefault();
            if (iconBle != null)
            {
                if (iconBle.LastBattery >= 0)
                {
                    SetIcon(iconBle.LastBattery);
                }
                else
                {
                    SetIcon(-1, "!");
                }
            }
            else if (xinputTray != null)
            {
                SetIcon(MapLevelToPercent((XInputLevel)xinputLastLevel[xinputTray.Value]));
            }
            else if (pnpTray != null && pnpTray.LastBattery >= 0)
            {
                SetIcon(pnpTray.LastBattery);
            }
            else
            {
                SetIcon(-1, "!");
            }

            // multi-line tooltip: one line per connected device (separated by newline)
            notifyIcon.Text = string.Join("\n", lines);
        }

        private async Task ReadBattery(BleDevice device)
        {
            if (!device.Connected || device.BatteryCharacteristic == null)
            {
                await AcquireBatteryService(device);
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
            UpdateTrayIcon();
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

        private async void Log(string s)        {
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

