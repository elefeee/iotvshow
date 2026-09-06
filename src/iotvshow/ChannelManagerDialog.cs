using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DreamScene2
{
    public partial class ChannelManagerDialog : Form
    {
        DataGridView _grid;
        ComboBox _cmbGroup;
        Button _btnAdd, _btnEdit, _btnDelete, _btnOpen, _btnDeleteGroup, _btnFilter;
        Label _lblTip;   // ← 底部说明
        public Action<string, string> OnOpenUrl;

        public ChannelManagerDialog()
        {
            this.Text = "频道管理";
            this.Size = new Size(560, 455);   // ← 高度加大，给说明留位
            this.StartPosition = FormStartPosition.CenterScreen;

            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.ShowInTaskbar = false;

            BuildUI();
            LoadGrid();
            RefreshCombo();
        }

        void BuildUI()
        {
            _cmbGroup = new ComboBox
            {
                Location = new Point(12, 8),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            _btnFilter = new Button { Text = "筛选", Location = new Point(160, 7), Width = 72 };
            _btnFilter.Click += (s, e) => LoadGrid();

            var btnNewGroup = new Button { Text = "新建分组", Location = new Point(236, 7), Width = 84 };
            btnNewGroup.Click += (s, e) => NewGroup();

            _btnDeleteGroup = new Button { Text = "删除分组", Location = new Point(324, 7), Width = 84 };
            _btnDeleteGroup.Click += (s, e) => DeleteGroup();

            _grid = new DataGridView
            {
                Location = new Point(12, 40),
                Size = new Size(520, 300),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            };
            _grid.Columns.Add("Group", "分组");
            _grid.Columns.Add("Name", "名称");
            _grid.Columns.Add("Url", "URL");
            _grid.Columns["Url"].FillWeight = 200;

            // ===== 底部说明：放在表格下方、按钮上方 =====
            _lblTip = new Label
            {
                Text = "本地视频URL示例 file:///C:/路径/视频.mp4；卫视自行添加网址；可按站点扩展ok.js 。",
                Location = new Point(12, 346),
                Size = new Size(520, 28),
                ForeColor = SystemColors.GrayText,
                Font = new Font(SystemFonts.DefaultFont.FontFamily, 8f),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _btnAdd = new Button { Text = "添加", Location = new Point(12, 380), Width = 80 };
            _btnEdit = new Button { Text = "编辑", Location = new Point(100, 380), Width = 80 };
            _btnDelete = new Button { Text = "删除", Location = new Point(188, 380), Width = 80 };
            _btnOpen = new Button { Text = "打开", Location = new Point(276, 380), Width = 80 };

            var btnClose = new Button
            {
                Text = "关闭",
                Location = new Point(450, 380),
                Width = 80,
                DialogResult = DialogResult.OK
            };

            _btnAdd.Click += (s, e) => EditChannel(null);

            _btnEdit.Click += (s, e) =>
            {
                if (_grid.SelectedRows.Count > 0)
                {
                    var row = _grid.SelectedRows[0];
                    EditChannel(new ChannelEntry
                    {
                        Name = (string)row.Cells["Name"].Value,
                        Url = (string)row.Cells["Url"].Value
                    }, (string)row.Cells["Group"].Value);
                }
            };

            _btnDelete.Click += (s, e) =>
            {
                if (_grid.SelectedRows.Count == 0) return;
                var row = _grid.SelectedRows[0];
                var grp = (string)row.Cells["Group"].Value;
                var name = (string)row.Cells["Name"].Value;

                if (MessageBox.Show($"删除频道「{name}」？", "确认",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    var groups = ChannelConfig.LoadGroups();
                    groups.Find(g => g.Group == grp)?.Channels.RemoveAll(c => c.Name == name);
                    groups.RemoveAll(g => g.Channels.Count == 0);
                    ChannelConfig.SaveGroups(groups);
                    LoadGrid();
                }
            };

            _btnOpen.Click += (s, e) =>
            {
                if (_grid.SelectedRows.Count == 0) return;
                var row = _grid.SelectedRows[0];
                var url = (string)row.Cells["Url"].Value;
                var name = (string)row.Cells["Name"].Value;
                if (!string.IsNullOrEmpty(url))
                    OnOpenUrl?.Invoke(url, name);
            };

            this.Controls.AddRange(new Control[] {
                _cmbGroup, _btnFilter, btnNewGroup, _btnDeleteGroup,
                _grid, _lblTip,
                _btnAdd, _btnEdit, _btnDelete, _btnOpen, btnClose
            });

            this.AcceptButton = btnClose;
        }

        // ===== 后面 LoadGrid / RefreshCombo / NewGroup / DeleteGroup / EditChannel / SimpleInput 全部原封不动 =====
        // （为完整起见我保留，未改动一行逻辑）

        void LoadGrid()
        {
            _grid.Rows.Clear();
            var groups = ChannelConfig.LoadGroups();
            string filter = _cmbGroup.SelectedItem as string;

            foreach (var g in groups)
            {
                if (!string.IsNullOrEmpty(filter) && filter != "全部" && g.Group != filter)
                    continue;

                foreach (var c in g.Channels)
                    _grid.Rows.Add(g.Group, c.Name, c.Url);
            }
        }

        void RefreshCombo()
        {
            string current = _cmbGroup.SelectedItem as string;
            _cmbGroup.Items.Clear();

            foreach (var g in ChannelConfig.LoadGroups())
                _cmbGroup.Items.Add(g.Group);

            if (!string.IsNullOrEmpty(current) && _cmbGroup.Items.Contains(current))
                _cmbGroup.SelectedItem = current;
            else if (_cmbGroup.Items.Count > 0)
                _cmbGroup.SelectedIndex = 0;
        }

        void NewGroup()
        {
            var name = SimpleInput("分组名称:", "新建分组", "");
            if (string.IsNullOrWhiteSpace(name)) return;

            var groups = ChannelConfig.LoadGroups();
            if (!groups.Exists(g => g.Group == name))
            {
                groups.Add(new ChannelGroup
                {
                    Group = name,
                    Channels = new List<ChannelEntry>()
                });
                ChannelConfig.SaveGroups(groups);
                RefreshCombo();
                LoadGrid();
            }
        }

        void DeleteGroup()
        {
            string grp = _cmbGroup.SelectedItem as string;
            if (string.IsNullOrEmpty(grp))
            {
                MessageBox.Show("请先选择要删除的分组。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var groups = ChannelConfig.LoadGroups();
            var target = groups.Find(g => g.Group == grp);
            if (target == null) return;

            bool hasChannels = target.Channels != null && target.Channels.Count > 0;

            string msg = hasChannels
                ? $"分组「{grp}」内有 {target.Channels.Count} 个频道，删除分组会一起删除这些频道，确定吗？"
                : $"确定删除分组「{grp}」？";

            if (MessageBox.Show(msg, "确认删除分组",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            groups.RemoveAll(g => g.Group == grp);
            ChannelConfig.SaveGroups(groups);
            RefreshCombo();
            LoadGrid();
        }

        void EditChannel(ChannelEntry existing, string groupName = null)
        {
            bool isAdd = existing == null;

            using (var dlg = new Form
            {
                Text = isAdd ? "添加频道" : "编辑频道",
                Size = new Size(460, 260),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            })
            {
                int labelX = 12, inputX = 90;
                int y = 18, rowH = 34;
                int inputWidth = 330;

                var lblName = new Label { Text = "名称:", Location = new Point(labelX, y + 6), AutoSize = true };
                var txtName = new TextBox { Location = new Point(inputX, y), Width = inputWidth, Text = existing?.Name ?? "" };
                y += rowH;

                var lblUrl = new Label { Text = "URL:", Location = new Point(labelX, y + 6), AutoSize = true };
                var txtUrl = new TextBox { Location = new Point(inputX, y), Width = inputWidth, Text = existing?.Url ?? "" };
                y += rowH;

                var lblGroup = new Label { Text = "分组:", Location = new Point(labelX, y + 6), AutoSize = true };
                var cmb = new ComboBox { Location = new Point(inputX, y), Width = inputWidth, DropDownStyle = ComboBoxStyle.DropDownList };

                foreach (var g in ChannelConfig.LoadGroups())
                    if (!string.IsNullOrEmpty(g.Group))
                        cmb.Items.Add(g.Group);

                string sel = groupName ?? (_cmbGroup.SelectedItem as string) ?? "";
                if (!string.IsNullOrEmpty(sel) && cmb.Items.Contains(sel))
                    cmb.SelectedItem = sel;
                else if (cmb.Items.Count > 0)
                    cmb.SelectedItem = cmb.Items[0];

                int btnW1 = 100, btnW2 = 80, btnH = 30, gap = 12;
                int totalW = btnW1 + gap + btnW2;
                int startX = (dlg.ClientSize.Width - totalW) / 2;
                int btnY = 180;

                var btnOk = new Button { Text = "确定", Location = new Point(startX, btnY), Width = btnW1, Height = btnH, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "取消", Location = new Point(startX + btnW1 + gap, btnY), Width = btnW2, Height = btnH, DialogResult = DialogResult.Cancel };

                dlg.Controls.AddRange(new Control[] { lblName, txtName, lblUrl, txtUrl, lblGroup, cmb, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog() != DialogResult.OK) return;

                string newName = txtName.Text.Trim();
                if (string.IsNullOrEmpty(newName))
                {
                    MessageBox.Show("名称不能为空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var groups = ChannelConfig.LoadGroups();
                if (!isAdd && existing != null)
                {
                    foreach (var g in groups)
                        g.Channels.RemoveAll(c => c.Name == existing.Name && c.Url == existing.Url);
                }

                string grpName = (cmb.SelectedItem as string)?.Trim();
                if (string.IsNullOrEmpty(grpName)) grpName = "未分组";

                var grp = groups.Find(g => g.Group == grpName);
                if (grp == null)
                {
                    grp = new ChannelGroup { Group = grpName, Channels = new List<ChannelEntry>() };
                    groups.Add(grp);
                }

                grp.Channels.Add(new ChannelEntry { Name = newName, Url = txtUrl.Text.Trim() });
                ChannelConfig.SaveGroups(groups);
                RefreshCombo();
                LoadGrid();
            }
        }

        string SimpleInput(string prompt, string title, string def)
        {
            using (var dlg = new Form
            {
                Text = title,
                Size = new Size(360, 190),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            })
            {
                var lbl = new Label { Text = prompt, Location = new Point(18, 22), AutoSize = true };
                var txt = new TextBox { Location = new Point(18, 55), Width = 310, Text = def ?? "" };

                int btnW1 = 80, btnW2 = 80, btnH = 30, gap = 12;
                int totalW = btnW1 + gap + btnW2;
                int startX = (dlg.ClientSize.Width - totalW) / 2;

                var btnOk = new Button { Text = "确定", Location = new Point(startX, 100), Width = btnW1, Height = btnH, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "取消", Location = new Point(startX + btnW1 + gap, 100), Width = btnW2, Height = btnH, DialogResult = DialogResult.Cancel };

                dlg.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                return dlg.ShowDialog() == DialogResult.OK ? txt.Text.Trim() : null;
            }
        }
    }
}