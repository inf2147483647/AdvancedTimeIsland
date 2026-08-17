using System;
using System.Runtime.InteropServices;

namespace AdvancedTimeIsland.Helpers
{
    public static class DisplayHelper
    {
        private const int ENUM_CURRENT_SETTINGS = -1;

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        public static int GetCurrentDisplayRefreshRate()
        {
            try
            {
                DEVMODE devMode = new DEVMODE();
                devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                
                if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode))
                {
                    return devMode.dmDisplayFrequency;
                }
            }
            catch
            {
            }
            
            return 60;
        }

        public static TimeSpan CalculateHighFrequencyInterval()
        {
            int refreshRate = GetCurrentDisplayRefreshRate();
            
            if (refreshRate <= 0)
                refreshRate = 60;
            
            double intervalMs = 1000.0 / refreshRate;
            
            return TimeSpan.FromMilliseconds(intervalMs);
        }
    }
}
