using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace DreamScene2
{
    public partial class WebWindow : Window, IPlayer, IPlayerControl, IPlayerInteractive
    {
        private readonly WebView2 _webView = new WebView2();
        private Uri _source;
        private bool _isMuted;
        private double _volume = 1.0;
        private string _cacheDir;
		private string _pendingEffect = "none";
        const string DefaultUrl = "https://tv.cctv.com/live/cctv13/";
        public static bool TryGetWebView2Version(out string version)
        {
            version = null;
            try
            {
                var env = CoreWebView2Environment.GetAvailableBrowserVersionString();
                version = env;
                return !string.IsNullOrEmpty(env);
            }
            catch { return false; }
        }

        public void ApplyEffect(string effectName)
        {
            _pendingEffect = effectName ?? "none";
            _ = Dispatcher.InvokeAsync(async () =>
            {
                if (_webView?.CoreWebView2 == null) return;

                // 先清掉之前的效果样式
                await _webView.CoreWebView2.ExecuteScriptAsync(@"
                    (function(){
                        var styles = document.querySelectorAll('style[data-iotv-effect]');
                        styles.forEach(function(s){ s.remove(); });
                    })();");

                if (string.IsNullOrEmpty(effectName) || effectName == "none")
                    return;

                string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string effectCssPath = Path.Combine(exeDir, "effects", effectName, "style.css");
                if (!File.Exists(effectCssPath)) return;

                string css = File.ReadAllText(effectCssPath);
                if (string.IsNullOrEmpty(css)) return;

                        // 转义给 JS 用
                string escaped = System.Text.RegularExpressions.Regex.Replace(css, @"[\\`$]", m =>
                {
                 if (m.Value == "\\") return "\\\\";
                 if (m.Value == "`") return "\\`";
                 if (m.Value == "$") return "\\$";
                 return m.Value;
                });

                string script = @"
                 var s=document.createElement('style');
                 s.setAttribute('data-iotv-effect','');
                 s.textContent=`" + escaped + @"`;
                 document.head.appendChild(s);";

        await _webView.CoreWebView2.ExecuteScriptAsync(script);
    });
}

        // 真停流：JS pause+断 src + Stop + about:blank
        public void HardStop()
        {
            try
            {
                _ = Dispatcher.InvokeAsync(async () =>
                {
                    if (_webView?.CoreWebView2 == null) return;
                    await _webView.CoreWebView2.ExecuteScriptAsync(@"
                        (function(){
                            var v=document.querySelector('video')||document.getElementById('h5player_player');
                            if(v){try{v.pause();v.muted=true;v.removeAttribute('src');if(v.load)v.load();}catch(e){}}
                        })();");
                });
            }
            catch { }
            try { _webView?.CoreWebView2?.Stop(); } catch { }
            try { _webView?.CoreWebView2?.Navigate("about:blank"); } catch { }
            IsPlaying = false;
        }

        // 切回大窗：Show + 导航同 url + Play
        public void ResumeFrom(Uri url)
        {
            this.Show();
            _source = url;
            if (_webView?.CoreWebView2 != null)
            {
                _webView.CoreWebView2.Navigate(url.ToString());
                Play();
            }
        }

        public WebWindow()
        {
            InitializeComponent();
            _cacheDir = Path.Combine(Path.GetTempPath(), "DreamScene2_WebView2_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_cacheDir);
            this.Content = _webView;
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = System.Windows.Media.Brushes.Black;
            this.ShowInTaskbar = false;

            _webView.CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = _cacheDir,
                AdditionalBrowserArguments = "--autoplay-policy=no-user-gesture-required " +
                                             "--disable-features=IsolateOrigins,site-per-process " +
                                             "--disable-disk-cache " +
                                             "--disable-application-cache " +
                                             "--disk-cache-size=1 " +
                                             "--media-cache-size=1 " +
                                             "--incognito"
            };

            this.Loaded += WebWindow_Loaded;
        }

        public bool IsPlaying { get; private set; } = true;

        public Uri Source
        {
            get => _source;
            set
            {
                _source = value;
                if (_webView?.CoreWebView2 != null && value != null)
                    _webView.CoreWebView2.Navigate(value.ToString());
            }
        }

        public string GetCurrentSourceUrl()
        {
            try { return _webView?.CoreWebView2?.Source; }
            catch { return null; }
        }

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;
                _ = Dispatcher.InvokeAsync(async () =>
                {
                    if (_webView.CoreWebView2 == null) return;
                    await _webView.CoreWebView2.ExecuteScriptAsync(
                        $"(function(){{var v=document.querySelector('video')||document.getElementById('h5player_player');if(v)v.muted={(_isMuted?"true":"false")};}})();");
                });
            }
        }

        public double Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                _ = Dispatcher.InvokeAsync(async () =>
                {
                    if (_webView.CoreWebView2 == null) return;
                    await _webView.CoreWebView2.ExecuteScriptAsync(
                        $"(function(){{var v=document.querySelector('video')||document.getElementById('h5player_player');if(v)v.volume={value.ToString(System.Globalization.CultureInfo.InvariantCulture)};}})();");
                });
            }
        }

        public void Play()
        {
            _ = Dispatcher.InvokeAsync(async () =>
            {
                if (_webView.CoreWebView2 == null) return;
                await _webView.CoreWebView2.ExecuteScriptAsync(@"
                    (function(){
                        var v=document.querySelector('video')||document.getElementById('h5player_player');
                        if(v){v.muted=false;var p=v.play();if(p)p.counter&&p.catch(function(){});}
                    })();");
                IsPlaying = true;
            });
        }

        // 软暂停：页面还活着，但视频停+静音（切小窗前先用这个）
        public void Pause()
        {
            _ = Dispatcher.InvokeAsync(async () =>
            {
                if (_webView.CoreWebView2 == null) return;
                await _webView.CoreWebView2.ExecuteScriptAsync(@"
                    (function(){
                        var v=document.querySelector('video')||document.getElementById('h5player_player');
                        if(v){v.pause();v.muted=true;}
                    })();");
                IsPlaying = false;
            });
        }

        public IntPtr GetHandle() => new WindowInteropHelper(this).Handle;
        public IntPtr GetMessageHandle() => GetHandle();

        public void SetPosition(System.Drawing.Rectangle rect)
        {
            this.Left = rect.X;
            this.Top = rect.Y;
            this.Width = rect.Width;
            this.Height = rect.Height;
        }

        // 真正退出程序才调这个
        public void Shutdown()
        {
            try
            {
                _ = Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        if (_webView?.CoreWebView2 != null)
                            await _webView.CoreWebView2.ExecuteScriptAsync(@"
                                (function(){var v=document.querySelector('video');if(v){v.pause();v.muted=true;v.removeAttribute('src');if(v.load)v.load();}})();");
                    }
                    catch { }
                });
                _webView?.CoreWebView2?.Stop();
                _webView?.CoreWebView2?.Navigate("about:blank");
                _webView?.Dispose();
            }
            catch { }
            this.Close();
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                try { if (Directory.Exists(_cacheDir)) Directory.Delete(_cacheDir, true); } catch { }
            });
        }

        private async void WebWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _webView.EnsureCoreWebView2Async(null);

            var settings = _webView.CoreWebView2.Settings;
            settings.IsScriptEnabled = true;
            settings.AreDefaultScriptDialogsEnabled = true;
            settings.IsWebMessageEnabled = true;
            settings.AreDevToolsEnabled = false;

            try { settings.IsGeneralAutofillEnabled = false; } catch { }
            try { settings.IsPasswordAutosaveEnabled = false; } catch { }

            string js = LoadOkJs();
            if (!string.IsNullOrEmpty(js))
            {
                await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(js);
                await _webView.CoreWebView2.ExecuteScriptAsync(js);
            }

            if (_source != null)
                _webView.CoreWebView2.Navigate(_source.ToString());
            else
                _webView.CoreWebView2.Navigate(DefaultUrl);
                _webView.CoreWebView2.NavigationCompleted += async (s, args) =>
                {
                    string js2 = LoadOkJs();
                    if (!string.IsNullOrEmpty(js2))
                    await _webView.CoreWebView2.ExecuteScriptAsync(js2);
                    await TryPlayVideo();

                    // ★ 导航完成后自动重新注入效果
                    if (!string.IsNullOrEmpty(_pendingEffect) && _pendingEffect != "none")
                    ApplyEffect(_pendingEffect);
                };

            await TryPlayVideo();
        }

        private string LoadOkJs()
        {
            try
            {
                string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string[] candidates = new[] { Path.Combine(exeDir, "ok.JS"), Path.Combine(exeDir, "ok.js") };
                foreach (var p in candidates)
                {
                    if (File.Exists(p))
                    {
                        string raw = File.ReadAllText(p);
                        int endHeader = raw.IndexOf("==/UserScript==");
                        if (endHeader != -1)
                        {
                            int start = raw.IndexOf('\n', endHeader) + 1;
                            if (start > 0 && start < raw.Length)
                                raw = raw.Substring(start).Trim();
                        }
                        return "(function(){window.GM_addStyle=function(css){var s=document.createElement('style');s.textContent=css;document.head.appendChild(s);};"
                             + raw + "})();";
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("LoadOkJs: " + ex.Message); }
            return null;
        }

        private async Task TryPlayVideo()
        {
            try
            {
                if (_webView.CoreWebView2 == null) return;
                await _webView.CoreWebView2.ExecuteScriptAsync(@"
                    (function(){
                        var v=document.querySelector('video')||document.getElementById('h5player_player');
                        if(v){v.muted=false;var p=v.play();if(p)p.catch(function(){});}
                    })();");
                IsPlaying = true;
            }
            catch { }
        }
    }
}