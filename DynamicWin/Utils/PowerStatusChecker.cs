using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DynamicWin.Utils
{

    public class PowerStatusChecker
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte Reserved1;
            public uint BatteryLifeTime;
            public uint BatteryFullLifeTime;

            public string GetACLineStatusString()
            {
                return ACLineStatus switch
                {
                    0 => "Offline",
                    1 => "Online",
                    _ => "Unknown",
                };
            }

            public string GetBatteryFlagString()
            {
                return BatteryFlag switch
                {
                    1 => "High, more than 66 percent",
                    2 => "Low, less than 33 percent",
                    4 => "Critical, less than five percent",
                    8 => "Charging",
                    128 => "No system battery",
                    _ => "Unknown",
                };
            }

            public string GetBatteryLifePercent()
            {
                return BatteryLifePercent == 255 ? "Unknown" : BatteryLifePercent + "%";
            }

            public string GetBatteryLifeTime()
            {
                return BatteryLifeTime == uint.MaxValue ? "Unknown" : BatteryLifeTime + " seconds";
            }

            public string GetBatteryFullLifeTime()
            {
                return BatteryFullLifeTime == uint.MaxValue ? "Unknown" : BatteryFullLifeTime + " seconds";
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("ACLineStatus: " + GetACLineStatusString());
                sb.AppendLine("Battery Flag: " + GetBatteryFlagString());
                sb.AppendLine("Battery Life: " + GetBatteryLifePercent());
                sb.AppendLine("Battery Left: " + GetBatteryLifeTime());
                sb.AppendLine("Battery Full: " + GetBatteryFullLifeTime());
                return sb.ToString();
            }
        }

        [DllImport("kernel32.dll")]
        private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

        public static SYSTEM_POWER_STATUS GetPowerStatus()
        {
            if (GetSystemPowerStatus(out SYSTEM_POWER_STATUS status))
            {
                return status;
            }
            else
            {
                throw new Exception("Unable to get power status.");
            }
        }
    }
}
