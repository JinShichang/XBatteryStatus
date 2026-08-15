using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace XBatteryStatus
{
    /// <summary>Per device user configuration.</summary>
    public class DeviceSettings
    {
        public bool Enabled { get; set; }
        public int[] Levels { get; set; } = { 35, 30, 25 };
        public string CustomName { get; set; } = "";
    }

    /// <summary>
    /// A paired Bluetooth LE device exposing the battery service (0x180F / 0x2A19).
    /// </summary>
    public class BleDevice : IDisposable
    {
        public BluetoothLEDevice Device { get; }
        public GattDeviceService BatteryService { get; set; }
        public GattCharacteristic BatteryCharacteristic { get; set; }
        public bool IsGamepad { get; }
        public DeviceSettings Config { get; set; } = new DeviceSettings();
        public int LastBattery { get; set; } = -1;

        public BleDevice(BluetoothLEDevice device, bool isGamepad)
        {
            Device = device;
            IsGamepad = isGamepad;
        }

        public string Id => Device.DeviceId;

        /// <summary>Stable key for per device configuration (unique per physical device).</summary>
        public string AddressKey => Device.BluetoothAddress.ToString("X16");

        public string DeviceName => Device.Name;

        public string DisplayName => string.IsNullOrWhiteSpace(Config.CustomName) ? Device.Name : Config.CustomName;

        public bool Connected => Device.ConnectionStatus == BluetoothConnectionStatus.Connected;

        public void Dispose()
        {
            BatteryService?.Dispose();
            BatteryService = null;
            BatteryCharacteristic = null;
            Device.Dispose();
        }
    }

    /// <summary>Persists per device settings to a JSON file in the config folder next to the exe.</summary>
    public static class DeviceConfig
    {
        private static readonly string configPath = Path.Combine(AppConfig.ConfigDir, "devices.json");

        public static Dictionary<string, DeviceSettings> Load()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, DeviceSettings>>(json);
                    if (loaded != null)
                    {
                        foreach (var settings in loaded.Values)
                        {
                            settings.Levels ??= new[] { 35, 30, 25 };
                            if (settings.Levels.Length != 3)
                            {
                                settings.Levels = new[] { 35, 30, 25 };
                            }
                        }
                        return loaded;
                    }
                }
            }
            catch
            {
            }
            return new Dictionary<string, DeviceSettings>();
        }

        public static void Save(Dictionary<string, DeviceSettings> config)
        {
            try
            {
                string directory = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string json = JsonSerializer.Serialize(config);
                File.WriteAllText(configPath, json);
            }
            catch
            {
            }
        }
    }
}
