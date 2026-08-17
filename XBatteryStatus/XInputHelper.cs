using System;
using System.Runtime.InteropServices;

namespace XBatteryStatus
{
    /// <summary>
    /// XInput battery level (0 = Empty, 1 = Low, 2 = Medium, 3 = Full).
    /// </summary>
    public enum XInputLevel
    {
        Empty = 0,
        Low = 1,
        Medium = 2,
        Full = 3
    }

    /// <summary>
    /// Reads the battery level of Xbox controllers connected through the Xbox Wireless Adapter
    /// using P/Invoke to xinput1_4.dll (XInputGetBatteryInformation). No external packages needed.
    /// </summary>
    internal static class XInputHelper
    {
        public const int MaxSlots = 4;

        private const uint ErrorDeviceNotConnected = 1167;
        private const byte DeviceTypeGamepad = 0;        // XINPUT_DEVTYPE_GAMEPAD
        private const byte BatteryTypeDisconnected = 0;
        private const byte BatteryTypeWired = 1;
        private const byte BatteryTypeUnknown = 4;

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_BATTERY_INFORMATION
        {
            public byte BatteryType;
            public byte BatteryLevel;
        }

        [DllImport("xinput1_4.dll")]
        private static extern uint XInputGetBatteryInformation(uint dwUserIndex, byte devType, out XINPUT_BATTERY_INFORMATION pBatteryInfo);

        /// <summary>
        /// Returns the wireless battery level of the given slot (0-3), or null when the slot
        /// is empty, wired or in an unknown state. Wired controllers are intentionally ignored.
        /// </summary>
        public static XInputLevel? GetWirelessBatteryLevel(int slot)
        {
            if (slot < 0 || slot >= MaxSlots) return null;

            try
            {
                var info = new XINPUT_BATTERY_INFORMATION();
                uint result = XInputGetBatteryInformation((uint)slot, DeviceTypeGamepad, out info);

                // only slots that return success may carry valid battery data
                if (result != 0)
                {
                    return null;
                }

                if (info.BatteryType == BatteryTypeDisconnected ||
                    info.BatteryType == BatteryTypeWired ||
                    info.BatteryType == BatteryTypeUnknown)
                {
                    return null;
                }

                int level = info.BatteryLevel;
                if (level < 0 || level > 3) return null;

                return (XInputLevel)level;
            }
            catch
            {
                return null;
            }
        }
    }
}
