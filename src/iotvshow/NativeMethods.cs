using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DreamScene2
{
    public static class NativeMethods
    {
        [DllImport("User32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("User32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("User32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("User32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("User32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("DS2Native.dll")]
        public static extern ulong DS2_GetLastInputTickCount();

        [DllImport("DS2Native.dll")]
        public static extern IntPtr DS2_GetDesktopWindowHandle();

        [DllImport("DS2Native.dll")]
        public static extern int DS2_TestScreen(RECT rect);

        [DllImport("DS2Native.dll")]
        public static extern void DS2_SetWindowPosition(IntPtr hWnd, RECT rect);

        [DllImport("DS2Native.dll")]
        public static extern void DS2_RestoreLastWindowPosition();

        [DllImport("DS2Native.dll")]
        public static extern void DS2_RefreshDesktop(int animated = 0);

        [DllImport("DS2Native.dll")]
        public static extern void DS2_ToggleShowDesktopIcons();

        [DllImport("DS2Native.dll")]
        public static extern int DS2_IsVisibleDesktopIcons();

        [DllImport("DS2Native.dll")]
        public static extern int DS2_StartForwardMouseKeyboardMessage(IntPtr hWnd);

        [DllImport("DS2Native.dll")]
        public static extern void DS2_EndForwardMouseKeyboardMessage();

        [DllImport("DS2Native.dll")]
        public static extern void DS2_ToggleProcess(uint dwPID, int bResumeProcess);
		
		// ===== 壁纸还原（纯 Win32，SPI 读/写当前壁纸路径）=====
        public const int SPI_GETDESKWALLPAPER = 0x0073;
        public const int SPI_SETDESKWALLPAPER = 0x0014;
        public const int SPIF_UPDATEINIFILE = 0x01;
        public const int SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SystemParametersInfo(int uAction, int uParam, StringBuilder lpvParam, int fuWinIni);

        /// <summary>读取当前壁纸文件路径</summary>
        public static string GetCurrentWallpaperPath()
        {
            var sb = new StringBuilder(512);
            SystemParametersInfo(SPI_GETDESKWALLPAPER, sb.Capacity, sb, 0);
            return sb.ToString();
        }

        /// <summary>设置壁纸（退出时还原）</summary>
        public static void SetWallpaperPath(string path)
        {
            var sb = new StringBuilder(path ?? string.Empty, 512);
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, sb, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }
    }

    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }
}
