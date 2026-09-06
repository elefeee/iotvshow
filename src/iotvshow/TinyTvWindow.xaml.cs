using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Windows.Controls;

namespace DreamScene2
{
    public partial class TinyTvWindow : Window
    {
        private bool _initDone;
        private bool _navEventsHooked;
        private bool _closing;
        private bool _requestingFullscreen;
        private string _skinName = "default";
        private string _effectName = "none";

        public WebView2 GetWebView2() => webView;
        public event EventHandler FullscreenRequested;
        public Uri CurrentSource { get; private set; }

        public TinyTvWindow(string skinName = "default", string effectName = "none")
        {
            _skinName = skinName;
            _effectName = effectName;

            InitializeComponent();
            LoadSkin();
            this.MouseDoubleClick += OnDoubleClick;

            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 1 && Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    try { DragMove(); } catch { }
                }
            };

            this.Closing += (s, e) =>
            {
                if (_closing) return;
                _closing = true;
                StopPlayback();
            };
        }

        public void ReloadSkin(string skinName)
        {
            _skinName = skinName;
            LoadSkin();
        }

        public void ReloadEffect(string effectName)
        {
            _effectName = effectName;

            // 如果页面已经加载完，直接注入新效果
            if (webView?.CoreWebView2 != null && !string.IsNullOrEmpty(webView.CoreWebView2.Source))
            {
                if (_effectName == "none")
                {
                    // 移除之前注入的效果：刷新页面让 NavigationCompleted 重新走一遍
                    string url = webView.CoreWebView2.Source;
                    webView.CoreWebView2.Reload();
                }
                else
                {
                    try
                    {
                        string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        string effectCssPath = Path.Combine(exeDir, "effects", _effectName, "style.css");
                        if (File.Exists(effectCssPath))
                        {
                            string css = File.ReadAllText(effectCssPath);
                            if (!string.IsNullOrEmpty(css))
                            {
                                string cssInject = "var s=document.createElement('style');s.textContent=" + EscapeForJs(css) + ";document.head.appendChild(s);";
                                webView.CoreWebView2.ExecuteScriptAsync(cssInject);
                            }
                        }
                    }
                    catch { }
                }
            }
            // 如果还没加载完，NavigationCompleted 里会用新的 _effectName 自动注入
        }

        private void LoadSkin()
        {
            var config = LoadSkinConfig();

            this.Width = config.WindowWidth;
            this.Height = config.WindowHeight;
            this.MinWidth = config.WindowWidth;
            this.MinHeight = config.WindowHeight;
            this.MaxWidth = config.WindowWidth;
            this.MaxHeight = config.WindowHeight;

            string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string imgPath = Path.Combine(exeDir, "skins", _skinName, config.SkinImage);
            if (File.Exists(imgPath))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(imgPath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                SkinImage.Source = bmp;
            }
            SkinImage.Width = config.WindowWidth;
            SkinImage.Height = config.WindowHeight;

            Canvas.SetLeft(webView, config.VideoX);
            Canvas.SetTop(webView, config.VideoY);
            webView.Width = config.VideoWidth;
            webView.Height = config.VideoHeight;
        }

        private class SkinConfig
        {
            public string SkinImage { get; set; } = "tvskin.png";
            public int WindowWidth { get; set; } = 307;
            public int WindowHeight { get; set; } = 178;
            public int VideoX { get; set; } = 12;
            public int VideoY { get; set; } = 9;
            public int VideoWidth { get; set; } = 284;
            public int VideoHeight { get; set; } = 160;

            public static SkinConfig Parse(string json)
            {
                var cfg = new SkinConfig();
                if (string.IsNullOrEmpty(json)) return cfg;

                string GetValue(string key)
                {
                    var idx = json.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
                    if (idx < 0) return null;
                    var colon = json.IndexOf(':', idx + key.Length + 2);
                    if (colon < 0) return null;
                    int i = colon + 1;
                    while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
                    if (i >= json.Length) return null;
                    bool quoted = json[i] == '"';
                    if (quoted) i++;
                    int start = i;
                    if (quoted)
                    {
                        var end = json.IndexOf('"', start);
                        return end > start ? json.Substring(start, end - start) : null;
                    }
                    else
                    {
                        int end = start;
                        while (end < json.Length && json[end] != ',' && json[end] != '}')
                            end++;
                        return json.Substring(start, end - start).Trim();
                    }
                }

                string s;
                if ((s = GetValue("SkinImage")) != null) cfg.SkinImage = s;
                int tmp;
                if ((s = GetValue("WindowWidth")) != null && int.TryParse(s, out tmp)) cfg.WindowWidth = tmp;
                if ((s = GetValue("WindowHeight")) != null && int.TryParse(s, out tmp)) cfg.WindowHeight = tmp;
                if ((s = GetValue("VideoX")) != null && int.TryParse(s, out tmp)) cfg.VideoX = tmp;
                if ((s = GetValue("VideoY")) != null && int.TryParse(s, out tmp)) cfg.VideoY = tmp;
                if ((s = GetValue("VideoWidth")) != null && int.TryParse(s, out tmp)) cfg.VideoWidth = tmp;
                if ((s = GetValue("VideoHeight")) != null && int.TryParse(s, out tmp)) cfg.VideoHeight = tmp;

                return cfg;
            }
        }

        private SkinConfig LoadSkinConfig()
        {
            try
            {
                string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string configPath = Path.Combine(exeDir, "skins", _skinName, "skin.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var cfg = SkinConfig.Parse(json);
                    if (cfg != null) return cfg;
                }
            }
            catch { }
            return new SkinConfig();
        }

        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_requestingFullscreen || _closing) return;
            _requestingFullscreen = true;
            e.Handled = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try { FullscreenRequested?.Invoke(this, EventArgs.Empty); }
                catch { }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        public string GetCurrentSourceUrl()
        {
            try { return webView?.CoreWebView2?.Source; }
            catch { return null; }
        }

        private string LoadOkJs()
        {
            try
            {
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                var path = Path.Combine(exeDir, "ok.js");
                if (File.Exists(path))
                    return File.ReadAllText(path);
            }
            catch { }
            return null;
        }

        public async Task NavigateAsync(Uri uri)
        {
            if (uri == null) return;
            CurrentSource = uri;

            if (!_initDone)
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DreamScene2", "WebView2_TinyTv");

                var env = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder,
                    options: new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required"));

                await webView.EnsureCoreWebView2Async(env);

                try
                {
                    webView.CoreWebView2.Settings.IsScriptEnabled = true;
                    webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                    webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                }
                catch { }

                _initDone = true;
            }

            webView.Source = uri;

            if (!_navEventsHooked)
            {
                _navEventsHooked = true;
                webView.CoreWebView2.NavigationCompleted += async (s, args) =>
                {
                    await Task.Delay(1200);
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        if (webView?.CoreWebView2 == null) return;

                        var js = LoadOkJs();
                        if (!string.IsNullOrEmpty(js))
                        {
                            var header = "window.GM_addStyle=function(css){var s=document.createElement('style');s.textContent=css;(document.head||document.documentElement).appendChild(s);};";
                            await webView.CoreWebView2.ExecuteScriptAsync(header + "\n" + js);
                        }

                        // 注入效果 CSS（用最新的 _effectName）
                        if (!string.IsNullOrEmpty(_effectName) && _effectName != "none")
                        {
                            try
                            {
                                string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                                string effectCssPath = Path.Combine(exeDir, "effects", _effectName, "style.css");
                                if (File.Exists(effectCssPath))
                                {
                                    string css = File.ReadAllText(effectCssPath);
                                    if (!string.IsNullOrEmpty(css))
                                    {
                                        string cssInject = "var s=document.createElement('style');s.textContent=" + EscapeForJs(css) + ";document.head.appendChild(s);";
                                        await webView.CoreWebView2.ExecuteScriptAsync(cssInject);
                                    }
                                }
                            }
                            catch { }
                        }
                    });
                };
            }
        }

        private static string EscapeForJs(string s)
        {
            return "`" + s.Replace("\\", "\\\\").Replace("`", "\\`").Replace("$", "\\$") + "`";
        }

        public void StopPlayback()
        {
            try
            {
                webView?.CoreWebView2?.ExecuteScriptAsync(@"
                    (function(){
                        var v=document.querySelector('video')||document.getElementById('h5player_player');
                        if(v){try{v.pause();v.muted=true;v.removeAttribute('src');if(v.load)v.load();}catch(e){}}
                    })();");
            }
            catch { }
            try { webView?.CoreWebView2?.Stop(); } catch { }
        }

        public void Shutdown()
        {
            if (_closing) return;
            _closing = true;
            StopPlayback();
            try { Close(); } catch { }
        }
    }
}