using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PortableUwfManager
{
    internal static class SystemSizing
    {
        public static long GetTotalPhysicalMemoryMb()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        using (obj)
                        {
                            var bytes = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                            return bytes / 1024 / 1024;
                        }
                    }
                }
            }
            catch
            {
            }

            return 0;
        }

        public static string GetSystemVolume()
        {
            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            if (!String.IsNullOrWhiteSpace(systemDrive) && systemDrive.Length >= 2 && systemDrive[1] == ':')
            {
                return Char.ToUpperInvariant(systemDrive[0]) + ":";
            }

            var systemRoot = Path.GetPathRoot(Environment.SystemDirectory);
            if (!String.IsNullOrEmpty(systemRoot))
            {
                return systemRoot.TrimEnd('\\');
            }

            return String.Empty;
        }

        public static long GetFreeSpaceMb(string volume)
        {
            try
            {
                var root = volume;
                if (!root.EndsWith("\\", StringComparison.Ordinal))
                {
                    root += "\\";
                }
                var drive = new DriveInfo(root);
                return drive.AvailableFreeSpace / 1024 / 1024;
            }
            catch
            {
            return 0;
        }
        }
    }

    internal sealed class WorkloadProfile
    {
        public readonly string Key;
        public readonly int WeightPercent;
        public readonly int DiskFreeSpacePercent;

        private WorkloadProfile(string key, int weightPercent, int diskFreeSpacePercent)
        {
            Key = key;
            WeightPercent = weightPercent;
            DiskFreeSpacePercent = diskFreeSpacePercent;
        }

        public static WorkloadProfile FromIndex(int index)
        {
            if (index <= 0)
            {
                return new WorkloadProfile("light", 8, 25);
            }
            if (index >= 2)
            {
                return new WorkloadProfile("heavy", 18, 60);
            }
            return new WorkloadProfile("normal", 12, 40);
        }

        public string DisplayName()
        {
            if (Key == "light")
            {
                return UiText.T("가벼움", "Light");
            }
            if (Key == "heavy")
            {
                return UiText.T("무거움", "Heavy");
            }
            return UiText.T("보통", "Normal");
        }
    }

    internal static class SizingRules
    {
        public static int RecommendRamOverlayMb(long totalRamMb, WorkloadProfile profile)
        {
            if (totalRamMb <= 0)
            {
                return 0;
            }

            if (profile == null)
            {
                profile = WorkloadProfile.FromIndex(1);
            }

            long byPercent = totalRamMb * profile.WeightPercent / 100;
            long reserveForWindows = totalRamMb / 4;
            long upperBound = totalRamMb - reserveForWindows;
            if (upperBound < 1024)
            {
                return 0;
            }

            return ClampAndRound(byPercent, 1024, upperBound, 256);
        }

        public static int RecommendDiskOverlayMb(long freeDiskMb, WorkloadProfile profile)
        {
            if (freeDiskMb <= 1024)
            {
                return 0;
            }

            if (profile == null)
            {
                profile = WorkloadProfile.FromIndex(1);
            }

            long keepFree = freeDiskMb / 5;
            long upperBound = freeDiskMb - keepFree;
            if (upperBound < 1024)
            {
                return 0;
            }

            long byPercent = freeDiskMb * profile.DiskFreeSpacePercent / 100;
            return ClampAndRound(byPercent, 1024, upperBound, 1024);
        }

        private static int ClampAndRound(long valueMb, long minimumMb, long maximumMb, int quantumMb)
        {
            if (valueMb < minimumMb)
            {
                valueMb = minimumMb;
            }
            if (valueMb > maximumMb)
            {
                valueMb = maximumMb;
            }

            long rounded = (valueMb / quantumMb) * quantumMb;
            if (rounded < minimumMb)
            {
                rounded = minimumMb;
            }
            if (rounded > Int32.MaxValue)
            {
                return Int32.MaxValue;
            }
            return (int)rounded;
        }
    }
}
