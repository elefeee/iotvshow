using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DreamScene2
{
    internal static class Program
    {
        [DllImport("Kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("User32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        [DllImport("User32.dll")]
        static extern uint RegisterWindowMessage(string lpString);

        const uint WM_COPYDATA = 0x004A;
        static readonly uint WM_SETXYWH = RegisterWindowMessage("DreamScene2_SetXYWH");
        static readonly string MutexName = "DreamScene2_SingleInstance_Mutex";

        [STAThread]
        static void Main(string[] args)
        {
            // --quit 优雅退出已有实例
            if (args.Length > 0 && (args[0] == "--quit" || args[0] == "-q"))
            {
                IntPtr hwnd = NativeMethods.FindWindow(null, Constants.MainWindowTitle);
                if (hwnd != IntPtr.Zero)
                {
                   var cds = MakeQuitCopydata();
                   SendMessage(hwnd, WM_SETXYWH, new IntPtr(0xFFFF), ref cds);
                }
                return;
            }

            // 单实例检测
             bool createdNew;
             using (var mutex = new System.Threading.Mutex(true, MutexName, out createdNew))
            {
               if (!createdNew)
               {
                    // 已有实例 → 通过 WM_COPYDATA 传 xywh
                    IntPtr hwnd = NativeMethods.FindWindow(null, Constants.MainWindowTitle);
                    if (hwnd != IntPtr.Zero && args.Length >= 5)
                    {
                      var cds = new COPYDATASTRUCT();
                      cds.dwData = new IntPtr(0x58445748); // "XYWH"
                      byte[] data = new byte[16];
                      BitConverter.GetBytes(int.Parse(args[1])).CopyTo(data, 0);
                      BitConverter.GetBytes(int.Parse(args[2])).CopyTo(data, 4);
                      BitConverter.GetBytes(int.Parse(args[3])).CopyTo(data, 8);
                      BitConverter.GetBytes(int.Parse(args[4])).CopyTo(data, 12);
                      cds.cbData = data.Length;
                      var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                      cds.lpData = handle.AddrOfPinnedObject();
                      SendMessage(hwnd, WM_COPYDATA, IntPtr.Zero, ref cds);
                      handle.Free();
                    }
                    return;
                }

                // 首次启动
                string dllPath = Helper.GetPathForStartupFolder("DS2Native.dll");
                LoadLibrary(dllPath);

                #if NETCOREAPP3_0_OR_GREATER
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                #endif
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                var mainForm = new MainForm();
                if (args.Length >= 5)
                    mainForm.CommandLineArgs = args;
                    // Program.cs 里原来 WpfApp 相关的那行，改成：
                if (System.Windows.Application.Current == null)
                {
                    new System.Windows.Application();
                }
                    Application.Run(mainForm);
            }
        }

        static COPYDATASTRUCT MakeQuitCopydata()
        {
            var cds = new COPYDATASTRUCT();
            cds.dwData = new IntPtr(0x51554954); // "QUIT"
            cds.cbData = 0;
            cds.lpData = IntPtr.Zero;
            return cds;
        }

         [StructLayout(LayoutKind.Sequential)]
         struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }
    }
}