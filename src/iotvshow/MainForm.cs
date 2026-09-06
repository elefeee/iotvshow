using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace DreamScene2
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get; private set; }

        readonly Settings _settings = Settings.Load();
        IPlayer _player;
        IntPtr _desktopWindowHandle;
        Screen _screen;
        ToolStripMenuItem _pauseMenuItem;
        ToolStripMenuItem _currentChannelMenuItem;
        private ContextMenuStrip _trayMenu;
        ChannelManagerDialog _channelManagerInstance;

        int _argX = 0, _argY = 0;
        int _argW = 0, _argH = 0;
        public string[] CommandLineArgs { get; set; }
        public string PlayPath { get; set; }

        readonly int[] _volumeLevels = { 0, 25, 50, 75, 100 };
        ToolStripMenuItem[] _volumeMenuItems;

        private TinyTvWindow _tinyWindow;
        private string _lastUrl;

        public async void EnterTinyMode()
        {
            if (_tinyWindow != null)
            {
                try { if (_tinyWindow.IsVisible) return; } catch { }
            }

            string currentUrl = (_player as WebWindow)?.GetCurrentSourceUrl()
                              ?? GetCurrentUrl();
            _lastUrl = currentUrl;

            _trayMenu?.Close();

            if (_player != null)
            {
                var wnd = _player as WebWindow;
                if (wnd != null)
                {
                    wnd.HardStop();
                    wnd.Hide();
                }
                else
                {
                    try { ((IPlayerControl)_player).Pause(); } catch { }
                }
            }

            var w = new TinyTvWindow(_settings.CurrentSkin, _settings.CurrentEffect);
            _tinyWindow = w;

            w.FullscreenRequested += (s, e) =>
            {
                if (_tinyWindow != w) return;
                string url = w.GetCurrentSourceUrl() ?? _lastUrl ?? currentUrl;

                w.StopPlayback();
                w.Hide();
                _tinyWindow = null;

                BeginInvoke(new Action(() =>
                {
                    var wnd = _player as WebWindow;
                    if (wnd != null)
                    {
                        wnd.Show();
                        wnd.ResumeFrom(new Uri(url));
                    }
                    else
                    {
                        OpenWeb(url);
                    }
                }));
            };

            w.Closing += (s, e) =>
            {
                if (_tinyWindow != w) return;
                string url = w.GetCurrentSourceUrl() ?? _lastUrl ?? currentUrl;
                _tinyWindow = null;

                BeginInvoke(new Action(() =>
                {
                    var wnd = _player as WebWindow;
                    if (wnd != null)
                    {
                        wnd.Show();
                        wnd.ResumeFrom(new Uri(url));
                    }
                    else
                    {
                        OpenWeb(url);
                    }
                }));
            };

            w.Show();

            try
            {
                await w.NavigateAsync(new Uri(currentUrl));
            }
            catch (Exception ex)
            {
                MessageBox.Show("小窗导航失败：\n" + ex.Message, "IOTV", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                w.Hide();
                _tinyWindow = null;
                var wnd = _player as WebWindow;
                if (wnd != null) { wnd.Show(); wnd.ResumeFrom(new Uri(currentUrl)); }
                else OpenWeb(currentUrl);
            }
        }

        public MainForm()
        {
            Application.ThreadException += (s, ex) =>
            {
                System.Diagnostics.Debug.WriteLine("WinForms thread exception: " + ex.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                System.Diagnostics.Debug.WriteLine("AppDomain unhandled: " + ex.ExceptionObject);
            };
            Instance = this;
            InitializeComponent();
            this.Text = Constants.MainWindowTitle;
            BuildTrayMenu();
        }

        void RefreshSkinMenu(ToolStripMenuItem skinItem)
        {
            skinItem.DropDownItems.Clear();
            string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string skinsDir = Path.Combine(exeDir, "skins");

            if (!Directory.Exists(skinsDir))
                Directory.CreateDirectory(skinsDir);

            var folders = Directory.GetDirectories(skinsDir);
            if (folders.Length == 0)
            {
                string defaultDir = Path.Combine(skinsDir, "default");
                Directory.CreateDirectory(defaultDir);
                folders = new[] { defaultDir };
            }

            foreach (var folder in folders)
            {
                string name = Path.GetFileName(folder);
                bool isCurrent = name == _settings.CurrentSkin;
                var item = new ToolStripMenuItem(name, null, (s, e) =>
                {
                    _settings.CurrentSkin = name;
                    Settings.Save();
                    RefreshSkinMenu(skinItem);

                    // 直接换皮，不重建窗口
                    if (_tinyWindow != null && _tinyWindow.IsVisible)
                    {
                        _tinyWindow.ReloadSkin(name);
                    }
                });
                item.Checked = isCurrent;
                skinItem.DropDownItems.Add(item);
            }
        }

        void RefreshEffectMenu(ToolStripMenuItem effectItem)
        {
            effectItem.DropDownItems.Clear();
            string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string effectsDir = Path.Combine(exeDir, "effects");

            var noneItem = new ToolStripMenuItem("无效果", null, (s, e) =>
            {
                _settings.CurrentEffect = "none";
                Settings.Save();
                RefreshEffectMenu(effectItem);

                // 小窗
                if (_tinyWindow != null && _tinyWindow.IsVisible)
                {
                    _tinyWindow.ReloadEffect("none");
                }
                // ★ 大窗（嵌入模式）
                var webWnd = _player as WebWindow;
                if (webWnd != null)
                {
                    webWnd.ApplyEffect("none");
                }
            });
            noneItem.Checked = _settings.CurrentEffect == "none";
            effectItem.DropDownItems.Add(noneItem);
            effectItem.DropDownItems.Add(new ToolStripSeparator());

            if (!Directory.Exists(effectsDir))
                Directory.CreateDirectory(effectsDir);

            var folders = Directory.GetDirectories(effectsDir);
            foreach (var folder in folders)
            {
                string name = Path.GetFileName(folder);
                bool isCurrent = name == _settings.CurrentEffect;
                var item = new ToolStripMenuItem(name, null, (s, e) =>
                {
                    _settings.CurrentEffect = name;
                    Settings.Save();
                    RefreshEffectMenu(effectItem);

                    // 小窗
                    if (_tinyWindow != null && _tinyWindow.IsVisible)
                    {
                        _tinyWindow.ReloadEffect(name);
                    }
                    // ★ 大窗（嵌入模式）
                    var webWnd = _player as WebWindow;
                    if (webWnd != null)
                    {
                        webWnd.ApplyEffect(name);
                    }
                });
                item.Checked = isCurrent;
                effectItem.DropDownItems.Add(item);
            }
        }

        // ============ 托盘菜单 ============
        void BuildTrayMenu()
        {
            _trayMenu = new ContextMenuStrip();

            _trayMenu.Items.Add("关于", null, (s, e) =>
            {
                using (var aboutDlg = new Form())
                {
                    aboutDlg.Text = "关于 IOTV Show";
                    aboutDlg.ClientSize = new Size(420, 280);
                    aboutDlg.StartPosition = FormStartPosition.CenterScreen;
                    aboutDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                    aboutDlg.MaximizeBox = false;
                    aboutDlg.MinimizeBox = false;
                    aboutDlg.ControlBox = true;
                    aboutDlg.ShowInTaskbar = false;
                    aboutDlg.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 8.25F, SystemFonts.MessageBoxFont.Style);

                    using (var stream = typeof(MainForm).Assembly.GetManifestResourceStream("iotvshow.Assets.icon.ico"))
                    {
                        if (stream != null)
                            aboutDlg.Icon = new Icon(stream);
                    }

                    var picIcon = new PictureBox
                    {
                        Location = new Point(18, 18),
                        Size = new Size(32, 32),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BackColor = Color.Transparent
                    };
                    using (var stream = typeof(MainForm).Assembly.GetManifestResourceStream("iotvshow.Assets.icon.ico"))
                    {
                        if (stream != null)
                            picIcon.Image = new Icon(stream, 32, 32).ToBitmap();
                    }

                    var lblTitle = new Label
                    {
                        Text = "IOTV Show 电视墙",
                        Location = new Point(62, 18),
                        AutoSize = true,
                        Font = new Font(aboutDlg.Font, FontStyle.Bold)
                    };
                    var lblVersion = new Label
                    {
                        Text = "版本 " + Constants.Version,
                        Location = new Point(62, 42),
                        AutoSize = true,
                        ForeColor = SystemColors.GrayText
                    };
                    var lblDesc = new Label
                    {
                        Text = "仅供学习交流使用。",
                        Location = new Point(20, 72),
                        AutoSize = true
                    };
                    var link = new LinkLabel
                    {
                        Text = "英都阀门 https://www.iovalve.com",
                        Location = new Point(20, 98),
                        AutoSize = true,
                        TabStop = true
                    };
                    link.LinkClicked += (s2, e2) =>
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "https://www.iovalve.com",
                                UseShellExecute = true
                            });
                        }
                        catch { }
                    };
                    var lblCopyright = new Label
                    {
                        Text = "© " + DateTime.Now.Year + " IOTV. All rights reserved.",
                        Location = new Point(20, 128),
                        AutoSize = true,
                        ForeColor = SystemColors.GrayText
                    };
                    var lblNotice = new Label
                    {
                        Text = "频道内容版权归各电视台/平台所有。\r\n本软件不提供任何直播源，仅调用公开网页。",
                        Location = new Point(20, 152),
                        AutoSize = true,
                        UseMnemonic = false
                    };
                    var line = new Panel
                    {
                        Location = new Point(20, aboutDlg.ClientSize.Height - 52),
                        Size = new Size(aboutDlg.ClientSize.Width - 40, 1),
                        BackColor = SystemColors.ControlDark,
                        Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                    };
                    var btnOk = new Button
                    {
                        Text = "确定",
                        DialogResult = DialogResult.OK,
                        Size = new Size(88, 28),
                        Location = new Point((aboutDlg.ClientSize.Width - 88) / 2, aboutDlg.ClientSize.Height - 44),
                        Anchor = AnchorStyles.Bottom
                    };
                    aboutDlg.Controls.AddRange(new Control[] { picIcon, lblTitle, lblVersion, lblDesc, link, lblCopyright, lblNotice, line, btnOk });
                    aboutDlg.AcceptButton = btnOk;
                    aboutDlg.ShowDialog();
                }
            });
            _trayMenu.Items.Add(new ToolStripSeparator());

            _currentChannelMenuItem = new ToolStripMenuItem("当前：") { Enabled = false };
            _trayMenu.Items.Add(_currentChannelMenuItem);
            _trayMenu.Items.Add(new ToolStripSeparator());

            var channelItem = new ToolStripMenuItem("切换频道");
            RefreshChannelMenu(channelItem);
            _trayMenu.Items.Add(channelItem);
            _trayMenu.Items.Add(new ToolStripSeparator());

            _trayMenu.Items.Add("管理频道...", null, (s, e) =>
            {
                if (_channelManagerInstance != null && !_channelManagerInstance.IsDisposed)
                {
                    _channelManagerInstance.BringToFront();
                    return;
                }
                _channelManagerInstance = new ChannelManagerDialog();
                _channelManagerInstance.OnOpenUrl = async (url, name) =>
                {
                   _currentChannelMenuItem.Text = "当前：" + (string.IsNullOrEmpty(name) ? url : name);
                   if (_tinyWindow != null && _tinyWindow.IsVisible)
                   {
                     _lastUrl = url;
                     await _tinyWindow.NavigateAsync(new Uri(url));
                   }
                   else
                   {
                     OpenWeb(url);
                   }
                };
                _channelManagerInstance.FormClosed += (s2, e2) => { _channelManagerInstance = null; };
                _channelManagerInstance.ShowDialog();
                RefreshChannelMenu(channelItem);
            });
            _trayMenu.Items.Add(new ToolStripSeparator());

            var volumeItem = new ToolStripMenuItem("音量");
            _volumeMenuItems = new ToolStripMenuItem[_volumeLevels.Length];
            for (int i = 0; i < _volumeLevels.Length; i++)
            {
                int level = _volumeLevels[i];
                _volumeMenuItems[i] = new ToolStripMenuItem(
                    level == 0 ? "静音" : level + "%",
                    null,
                    (s, e) => SetVolume(level));
                volumeItem.DropDownItems.Add(_volumeMenuItems[i]);
            }
            _trayMenu.Items.Add(volumeItem);

            var effectItem = new ToolStripMenuItem("效果");
            RefreshEffectMenu(effectItem);
            _trayMenu.Items.Add(effectItem);

            _trayMenu.Items.Add(new ToolStripSeparator());

            _trayMenu.Items.Add("小电视机", null, (s, e) => EnterTinyMode());

            var skinItem = new ToolStripMenuItem("皮肤");
            RefreshSkinMenu(skinItem);
            _trayMenu.Items.Add(skinItem);

            _trayMenu.Items.Add(new ToolStripSeparator());

            _pauseMenuItem = new ToolStripMenuItem("暂停", null, (object s, EventArgs e) => TogglePause());
            _trayMenu.Items.Add(_pauseMenuItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("退出", null, (object s, EventArgs e) => Application.Exit());

            notifyIcon1.ContextMenuStrip = _trayMenu;
            notifyIcon1.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    _trayMenu.Show(Cursor.Position);
            };

            UpdateVolumeCheck(_settings.Volume);
            ApplyVolumeToPlayer();
        }

        void SetVolume(int level)
        {
            _settings.Volume = level;
            Settings.Save();
            UpdateVolumeCheck(level);
            ApplyVolumeToPlayer();
        }

        void UpdateVolumeCheck(int level)
        {
            for (int i = 0; i < _volumeLevels.Length; i++)
                _volumeMenuItems[i].Checked = (_volumeLevels[i] == level);
        }

        void ApplyVolumeToPlayer()
        {
            if (_player != null)
            {
                var ctrl = (IPlayerControl)_player;
                ctrl.IsMuted = (_settings.Volume == 0);
                ctrl.Volume = _settings.Volume / 100.0;
            }

            if (_tinyWindow != null && _tinyWindow.IsVisible)
            {
                try
                {
                    var wv = _tinyWindow.GetWebView2();
                    if (wv?.CoreWebView2 != null)
                    {
                        double vol = _settings.Volume / 100.0;
                        string js = $"(function(){{ var v=document.querySelector('video'); if(v) v.volume={vol.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}; }})();";
                        wv.CoreWebView2.ExecuteScriptAsync(js);
                    }
                }
                catch { }
            }
        }

        void RefreshChannelMenu(ToolStripMenuItem channelItem)
        {
            channelItem.DropDownItems.Clear();
            foreach (var grp in ChannelConfig.LoadGroups())
            {
                var grpItem = new ToolStripMenuItem(grp.Group);
                foreach (var ch in grp.Channels)
                {
                    var name = ch.Name; var url = ch.Url;
                    grpItem.DropDownItems.Add(name, null, (s, e) => SwitchChannel(url, name));
                }
                channelItem.DropDownItems.Add(grpItem);
            }
        }

        async void SwitchChannel(string url, string channelName)
        {
            _currentChannelMenuItem.Text = "当前：" + channelName;

            if (_tinyWindow != null && _tinyWindow.IsVisible)
            {
                _lastUrl = url;
                await _tinyWindow.NavigateAsync(new Uri(url));
            }
            else
            {
                OpenWeb(url);
            }
        }

        void TogglePause()
        {
            if (_player == null) return;
            var ctrl = (IPlayerControl)_player;
            if (ctrl.IsPlaying) { Pause_(); _pauseMenuItem.Text = "恢复"; }
            else { Play_(); _pauseMenuItem.Text = "暂停"; }
        }

        // ============ 启动 ============
        private void MainForm_Load(object sender, EventArgs e)
        {
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.DispatcherUnhandledException += (s, ex) =>
                {
                    System.Diagnostics.Debug.WriteLine("WPF unhandled: " + ex.Exception);
                    ex.Handled = true;
                };
            }
            else
            {
                System.Windows.Application.Current.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
            }

            var args = CommandLineArgs ?? Environment.GetCommandLineArgs();
            if (args.Length >= 5)
            {
                int.TryParse(args[1], out _argX);
                int.TryParse(args[2], out _argY);
                int.TryParse(args[3], out _argW);
                int.TryParse(args[4], out _argH);
            }

            try { _originalWallpaperPath = NativeMethods.GetCurrentWallpaperPath(); }
            catch { _originalWallpaperPath = null; }

            _desktopWindowHandle = NativeMethods.DS2_GetDesktopWindowHandle();
            _screen = Screen.PrimaryScreen;

            using (var stream = typeof(MainForm).Assembly.GetManifestResourceStream("iotvshow.Assets.icon.ico"))
            {
                if (stream != null) notifyIcon1.Icon = new Icon(stream);
            }
            notifyIcon1.Text = "IOTV Show";
            notifyIcon1.Visible = true;

            if (!File.Exists(ChannelConfig.ConfigPath))
            {
                var defaults = ChannelConfig.LoadGroups();
                ChannelConfig.SaveGroups(defaults);
            }

            string url = !string.IsNullOrEmpty(PlayPath)
                ? PlayPath
                : ChannelConfig.LoadGroups()[0].Channels[0].Url;

            OpenWeb(url);
        }

        string GetCurrentUrl()
        {
            string text = _currentChannelMenuItem?.Text?.Replace("当前：", "")?.Trim();
            if (!string.IsNullOrEmpty(text) && Uri.IsWellFormedUriString(text, UriKind.Absolute))
                return text;
            try { return ChannelConfig.LoadGroups()[0].Channels[0].Url; }
            catch { return "https://tv.cctv.com/live/cctv13/"; }
        }

        // ============ 核心 ============
        void OpenWeb(string url)
        {
            if (!WebWindow.TryGetWebView2Version(out _))
            {
                MessageBox.Show("未检测到 WebView2 运行时",
                    Constants.ProjectName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            IntPtr hwnd;
            if (_player == null)
            {
                var webWindow = new WebWindow() { Width = _screen.Bounds.Width, Height = _screen.Bounds.Height };
                _player = (IPlayer)webWindow;
                webWindow.Show();
                hwnd = webWindow.GetHandle();
                NativeMethods.SetParent(hwnd, _desktopWindowHandle);
            }
            else
            {
                hwnd = _player.GetHandle();
            }

            if (_argW > 0 && _argH > 0)
                NativeMethods.MoveWindow(hwnd, _argX, _argY, _argW, _argH, true);
            else
            {
                var b = _screen.Bounds;
                NativeMethods.MoveWindow(hwnd, b.Left, b.Top, b.Width, b.Height, true);
            }

            ((IPlayer)_player).SetPosition(_argW > 0 && _argH > 0
                ? new Rectangle(_argX, _argY, _argW, _argH)
                : _screen.Bounds);

            var wnd = _player as WebWindow;
            if (wnd != null)
                wnd.ResumeFrom(new Uri(url));
            else
            {
                ((IPlayerControl)_player).Source = new Uri(url);
                ((IPlayerControl)_player).Play();
            }
            ApplyVolumeToPlayer();
            _pauseMenuItem.Text = "暂停";
        }

        void CloseWindow()
        {
            if (_player == null) return;
            try
            {
                var ctrl = (IPlayerControl)_player;
                ctrl.Pause();
                ctrl.Source = null;
            }
            catch { }
            ((IPlayer)_player).Shutdown();
            _player = null;
        }

        void Play_()
        {
            ((IPlayerControl)_player).Play();
            _pauseMenuItem.Text = "暂停";
        }

        void Pause_()
        {
            ((IPlayerControl)_player).Pause();
            _pauseMenuItem.Text = "恢复";
        }

        // ============ 关闭 ============
        string _originalWallpaperPath;

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_originalWallpaperPath) && File.Exists(_originalWallpaperPath))
                    NativeMethods.SetWallpaperPath(_originalWallpaperPath);
            }
            catch { }

            notifyIcon1.Visible = false;

            try { _tinyWindow?.Shutdown(); _tinyWindow = null; }
            catch { }

            if (_player != null)
            {
                var wnd = _player as WebWindow;
                if (wnd != null) wnd.HardStop();
                else { try { ((IPlayerControl)_player).Pause(); } catch { } }
            }

            try { (_player as WebWindow)?.Shutdown(); } catch { }
            _player = null;

            try
            {
                if (System.Windows.Application.Current != null)
                    System.Windows.Application.Current.Shutdown();
            }
            catch { }

            Settings.Save();
        }

        // ============ WM_COPYDATA ============
        const uint WM_COPYDATA = 0x004A;
        static readonly uint WM_SETXYWH = RegisterWindowMessage("DreamScene2_SetXYWH");

        [DllImport("User32.dll")]
        static extern uint RegisterWindowMessage(string lpString);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)WM_SETXYWH)
            {
                if (m.WParam.ToInt64() == 0xFFFF)
                {
                    Application.Exit();
                    return;
                }
            }
            if (m.Msg == WM_COPYDATA)
            {
                var cds = Marshal.PtrToStructure<COPYDATASTRUCT>(m.LParam);
                if (cds.dwData.ToInt64() == 0x51554954) // QUIT
                {
                    Application.Exit();
                    return;
                }
                if (cds.dwData.ToInt64() == 0x58445748) // XYWH
                {
                    byte[] data = new byte[cds.cbData];
                    Marshal.Copy(cds.lpData, data, 0, cds.cbData);
                    _argX = BitConverter.ToInt32(data, 0);
                    _argY = BitConverter.ToInt32(data, 4);
                    _argW = BitConverter.ToInt32(data, 8);
                    _argH = BitConverter.ToInt32(data, 12);
                    if (_player != null)
                    {
                        IntPtr hwnd = _player.GetHandle();
                        NativeMethods.MoveWindow(hwnd, _argX, _argY, _argW, _argH, true);
                        ((IPlayer)_player).SetPosition(new Rectangle(_argX, _argY, _argW, _argH));
                    }
                }
                m.Result = new IntPtr(1);
                return;
            }
            base.WndProc(ref m);
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