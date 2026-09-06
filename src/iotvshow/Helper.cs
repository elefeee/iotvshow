using System;
using System.Diagnostics;
using System.IO;

namespace DreamScene2
{
    public static class Helper
    {
        const string ProjectName = "IOTV";
        static string s_appPath;

        public static void OpenLink(string link)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = link,
                UseShellExecute = true
            });
        }

        public static string GetPathForStartupFolder(string subPath)
        {
            string executablePath = Process.GetCurrentProcess().MainModule.FileName;
            string startupPath = Path.GetDirectoryName(executablePath);
            return Path.Combine(startupPath, subPath);
        }

        public static string GetPathForUserAppDataFolder(string subPath)
        {
            if (s_appPath == null)
            {
                s_appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ProjectName);
                if (!Directory.Exists(s_appPath))
                {
                    Directory.CreateDirectory(s_appPath);
                }
            }
            return Path.Combine(s_appPath, subPath);
        }
    }
}