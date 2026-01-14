using System;
using System.IO;

namespace MSRewardsBot.Common.Utilities
{
    public static class RuntimeEnvironment
    {
        public static bool IsLinux()  => OperatingSystem.IsLinux() && !IsDocker();

        public static bool IsWindows() => OperatingSystem.IsWindows();

        public static bool IsDocker()
        {
            if (!OperatingSystem.IsLinux())
            {
                return false;
            }

            if (File.Exists("/.dockerenv"))
            {
                return true;
            }

            try
            {
                string cgroup = File.ReadAllText("/proc/1/cgroup");
                return cgroup.Contains("docker") || cgroup.Contains("containerd");
            }
            catch
            {
                return false;
            }
        }
    }
}
