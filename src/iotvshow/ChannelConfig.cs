using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DreamScene2
{
    public class ChannelEntry
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class ChannelGroup
    {
        public string Group { get; set; }
        public List<ChannelEntry> Channels { get; set; }
    }

    public static class ChannelConfig
    {
       public static string ConfigPath =>
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "channels.json");

        public static List<ChannelGroup> LoadGroups()
        {
            if (!File.Exists(ConfigPath))
                return DefaultGroups();
            try
            {
                var json = File.ReadAllText(ConfigPath);
                var groups = ParseGroups(json);
                return groups.Count > 0 ? groups : DefaultGroups();
            }
            catch { return DefaultGroups(); }
        }

        public static void SaveGroups(List<ChannelGroup> groups)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[");
            for (int i = 0; i < groups.Count; i++)
            {
                sb.AppendLine("  {");
                sb.AppendLine($"    \"group\": \"{Escape(groups[i].Group)}\",");
                sb.AppendLine("    \"channels\": [");
                for (int j = 0; j < groups[i].Channels.Count; j++)
                {
                    sb.Append($"      {{ \"name\": \"{Escape(groups[i].Channels[j].Name)}\", \"url\": \"{Escape(groups[i].Channels[j].Url)}\" }}");
                    if (j < groups[i].Channels.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("    ]");
                sb.Append("  }");
                if (i < groups.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("]");
            File.WriteAllText(ConfigPath, sb.ToString());
        }


        public static List<ChannelEntry> Load()
        {
            var flat = new List<ChannelEntry>();
            foreach (var g in LoadGroups())
                flat.AddRange(g.Channels);
            return flat;
        }

        static string Escape(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        static List<ChannelGroup> ParseGroups(string json)
        {
            var list = new List<ChannelGroup>();
            try
            {
                int pos = 0;
                while (true)
                {
                    var gi = json.IndexOf("\"group\"", pos);
                    if (gi < 0) break;
                    var gc = json.IndexOf(":", gi + 6);
                    var q1 = json.IndexOf("\"", gc + 1);
                    var q2 = json.IndexOf("\"", q1 + 1);
                    var grp = new ChannelGroup { Group = json.Substring(q1 + 1, q2 - q1 - 1), Channels = new List<ChannelEntry>() };

                    var ci = json.IndexOf("\"channels\"", q2);
                    var b1 = json.IndexOf("[", ci);
                    var b2 = json.IndexOf("]", b1);
                    if (b1 < 0 || b2 < 0) break;
                    var body = json.Substring(b1 + 1, b2 - b1 - 1);

                    int cp = 0;
                    while (true)
                    {
                        var ni = body.IndexOf("\"name\"", cp);
                        if (ni < 0) break;
                        var nc = body.IndexOf(":", ni + 6);
                        var nq1 = body.IndexOf("\"", nc + 1);
                        var nq2 = body.IndexOf("\"", nq1 + 1);
                        var ui = body.IndexOf("\"url\"", nq2);
                        if (ui < 0) break;
                        var uc = body.IndexOf(":", ui + 5);
                        var uq1 = body.IndexOf("\"", uc + 1);
                        var uq2 = body.IndexOf("\"", uq1 + 1);
                        grp.Channels.Add(new ChannelEntry
                        {
                            Name = body.Substring(nq1 + 1, nq2 - nq1 - 1),
                            Url = body.Substring(uq1 + 1, uq2 - uq1 - 1)
                        });
                        cp = uq2 + 1;
                    }
                    list.Add(grp);
                    pos = b2 + 1;
                }
            }
            catch { }
            return list;
        }

        static List<ChannelGroup> DefaultGroups()
        {
            return new List<ChannelGroup>
            {
                new ChannelGroup
                {
                    Group = "央视",
                    Channels = new List<ChannelEntry>
                    {
                        new ChannelEntry { Name = "CCTV-13 新闻", Url = "https://tv.cctv.com/live/cctv13/" },
                        new ChannelEntry { Name = "CCTV-1 综合", Url = "https://tv.cctv.com/live/cctv1/" },
                        new ChannelEntry { Name = "CCTV-2 财经", Url = "https://tv.cctv.com/live/cctv2/" },
                        new ChannelEntry { Name = "CCTV-3 综艺", Url = "https://tv.cctv.com/live/cctv3/" },
                        new ChannelEntry { Name = "CCTV-4 中文国际", Url = "https://tv.cctv.com/live/cctv4/" },
                        new ChannelEntry { Name = "CCTV-5 体育", Url = "https://tv.cctv.com/live/cctv5/" },
                        new ChannelEntry { Name = "CCTV-5+ 体育赛事", Url = "https://tv.cctv.com/live/cctv5plus/" },
                        new ChannelEntry { Name = "CCTV-6 电影", Url = "https://tv.cctv.com/live/cctv6/" },
                        new ChannelEntry { Name = "CCTV-7 国防军事", Url = "https://tv.cctv.com/live/cctv7/" },
                        new ChannelEntry { Name = "CCTV-8 电视剧", Url = "https://tv.cctv.com/live/cctv8/" },
                        new ChannelEntry { Name = "CCTV-9 纪录", Url = "https://tv.cctv.com/live/cctv9/" },
                        new ChannelEntry { Name = "CCTV-10 科教", Url = "https://tv.cctv.com/live/cctv10/" },
                        new ChannelEntry { Name = "CCTV-11 戏曲", Url = "https://tv.cctv.com/live/cctv11/" },
                        new ChannelEntry { Name = "CCTV-12 社会与法", Url = "https://tv.cctv.com/live/cctv12/" },
                        new ChannelEntry { Name = "CCTV-14 少儿", Url = "https://tv.cctv.com/live/cctv14/" },
                        new ChannelEntry { Name = "CCTV-15 音乐", Url = "https://tv.cctv.com/live/cctv15/" },
                        new ChannelEntry { Name = "CCTV-16 奥林匹克", Url = "https://tv.cctv.com/live/cctv16/" },
                        new ChannelEntry { Name = "CCTV-17 农业农村", Url = "https://tv.cctv.com/live/cctv17/" },
                    }
                },
                new ChannelGroup
                {
                    Group = "卫视",
                    Channels = new List<ChannelEntry>
                    {
                        new ChannelEntry { Name = "版权原因自行添加", Url = "https://tv.cctv.com/live/cctv13/" },
                    }
                }
            };
        }
    }
}