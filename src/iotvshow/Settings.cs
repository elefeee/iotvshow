using System.IO;
using System.Xml.Serialization;

namespace DreamScene2
{
    public class Settings
    {
        static string s_filePath = Helper.GetPathForUserAppDataFolder("settings.xml");
        static Settings s_settings;

        private Settings() { }
        public bool FirstRun { get; set; } = true;
        public bool IsMuted { get; set; }                 // 是否静音（默认 false = 有声音）
        public bool DisableWebSecurity { get; set; } = true; // WebView2 关同源策略，便于注入脚本
		public int Volume { get; set; } = 50;  // 默认 50%
		public string CurrentSkin { get; set; } = "default";
		public string CurrentEffect { get; set; } = "none";

        public static Settings Load()
        {
            if (s_settings != null) return s_settings;
            if (!File.Exists(s_filePath))
            {
                s_settings = new Settings();
                return s_settings;
            }
            using (FileStream fs = File.OpenRead(s_filePath))
            {
                XmlSerializer ser = new XmlSerializer(typeof(Settings));
                s_settings = (Settings)ser.Deserialize(fs);
            }
            return s_settings;
        }

        public static void Save()
        {
            using (FileStream fs = File.Create(s_filePath))
            {
                XmlSerializer ser = new XmlSerializer(typeof(Settings));
                ser.Serialize(fs, s_settings);
            }
        }
    }
}
