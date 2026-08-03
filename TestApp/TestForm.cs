using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DarkUI.Controls;
using DarkUI.Forms;

namespace TestApp
{
    public class TestForm : DarkForm
    {
        private readonly DarkTreeView _tree;
        private readonly DarkListView _list;
        private readonly DarkGroupedListView _grouped;
        private readonly DarkButton _btnAddNodes;
        private readonly DarkButton _btnAddItems;
        private readonly DarkButton _btnClear;
        private readonly DarkButton _btnExpandAll;
        private readonly DarkButton _btnCollapseAll;
        private readonly DarkButton _btnGroupedData;
        private readonly DarkButton _btnToggleEnable;

        public TestForm()
        {
            Text = "DarkUI Scroll Test";
            Size = new Size(1200, 950);
            MinimumSize = new Size(600, 500);

            // ── Left: DarkTreeView ──────────────────────────
            var treeLabel = new DarkLabel
            {
                Text = "DarkTreeView",
                Location = new Point(12, 12), AutoSize = true
            };
            _tree = new DarkTreeView
            {
                Location = new Point(12, 34), Size = new Size(380, 280),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                ShowLines = true, ShowRootLines = true
            };

            // ── Right-top: DarkListView ─────────────────────
            var listLabel = new DarkLabel
            {
                Text = "DarkListView",
                Location = new Point(410, 12), AutoSize = true
            };
            _list = new DarkListView
            {
                Location = new Point(410, 34), Size = new Size(760, 280),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details, FullRowSelect = true, MultiSelect = true
            };
            _list.Columns.Add("Name", 200);
            _list.Columns.Add("Type", 100);
            _list.Columns.Add("Size", 90);
            _list.Columns.Add("Modified", 140);
            _list.Columns.Add("Path", 300);

            // ── Bottom: DarkGroupedListView ─────────────────
            var groupedLabel = new DarkLabel
            {
                Text = "DarkGroupedListView — grouped files by type, click group headers to collapse",
                Location = new Point(12, 324), AutoSize = true
            };
            _grouped = new DarkGroupedListView
            {
                Location = new Point(12, 348), Size = new Size(1160, 330),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _grouped.MultiSelect = true;
            _grouped.GroupHeaderClicked += (rowIdx, groupName, args) =>
            {
                var files = _grouped.GetGroupFilePaths(rowIdx);
                var cm = new DarkUI.Controls.DarkContextMenu();
                cm.Items.Add(new ToolStripMenuItem($"Group: {groupName}  ({files.Count} files)") { Enabled = false });
                cm.Items.Add(new ToolStripSeparator());
                cm.Items.Add("Extract All", null, (s, ev) =>
                    DarkUI.Forms.DarkMessageBox.ShowInformation($"Extract {files.Count} files from '{groupName}'", "Extract All"));
                cm.Show(Cursor.Position);
            };

            // ── New controls demo row ────────────────────────
            var newLabel = new DarkLabel
            {
                Text = "New controls:",
                Location = new Point(12, 688), AutoSize = true
            };

            var toggle = new DarkToggleSwitch { Location = new Point(90, 684), Size = new Size(44, 22) };
            var toggleLbl = new DarkLabel { Text = "Toggle", Location = new Point(140, 688), AutoSize = true };

            var track = new DarkTrackBar { Location = new Point(200, 684), Size = new Size(140, 24), Maximum = 100, Value = 40 };
            var trackLbl = new DarkLabel { Text = "Slider", Location = new Point(346, 688), AutoSize = true };
            track.ValueChanged += (s, e) => trackLbl.Text = $"Slider: {track.Value}";

            var search = new DarkSearchBox { Location = new Point(400, 684), Size = new Size(160, 26) };
            var searchLbl = new DarkLabel { Text = "Search", Location = new Point(566, 688), AutoSize = true };

            var badge = new DarkBadge { Location = new Point(620, 684), Text = "12 new" };

            var colorBtn = new DarkColorButton { Location = new Point(700, 684), Size = new Size(90, 26) };

            var toastBtn = new DarkButton { Text = "Toast", Location = new Point(800, 684), Size = new Size(60, 26) };
            toastBtn.Click += (s, e) => DarkToast.Show("Operation completed successfully!", DarkToast.ToastIcon.Success, 4000, true);

            var overlayBtn = new DarkButton { Text = "Overlay", Location = new Point(866, 684), Size = new Size(70, 26) };
            var overlay = new DarkLoadingOverlay { Dock = DockStyle.Fill, Message = "Working…" };
            overlayBtn.Click += (s, e) =>
            {
                if (overlay.Visible) overlay.Hide();
                else overlay.Show();
            };

            var breadcrumb = new DarkBreadcrumbBar { Location = new Point(12, 716), Size = new Size(1160, 28), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            breadcrumb.SetItems(new[] { "C:", "Projects", "DarkUI", "Controls" });
            breadcrumb.SegmentClicked += (s, idx) => DarkToast.Show($"Breadcrumb: {breadcrumb.Items[idx]}", DarkToast.ToastIcon.Info, 2000, false);

            var calendar = new DarkCalendar { Location = new Point(12, 752), Size = new Size(230, 160), Visible = false };

            var toolTip = new DarkToolTip();
            toolTip.SetToolTip(toggle, "Dark toggle switch");
            toolTip.SetToolTip(track, "Dark track bar");
            toolTip.SetToolTip(search, "Dark search box");
            toolTip.SetToolTip(badge, "Dark badge");
            toolTip.SetToolTip(colorBtn, "Dark color button");
            toolTip.SetToolTip(breadcrumb, "Dark breadcrumb bar");

            // ── Buttons ─────────────────────────────────────
            _btnAddNodes = new DarkButton
            {
                Text = "Add Tree Nodes",
                Location = new Point(12, 868), Size = new Size(120, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnAddNodes.Click += (s, e) => AddTreeNodes();

            _btnExpandAll = new DarkButton
            {
                Text = "Expand All",
                Location = new Point(138, 868), Size = new Size(100, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnExpandAll.Click += (s, e) => _tree.ExpandAll();

            _btnCollapseAll = new DarkButton
            {
                Text = "Collapse All",
                Location = new Point(244, 868), Size = new Size(100, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnCollapseAll.Click += (s, e) => _tree.CollapseAll();

            _btnAddItems = new DarkButton
            {
                Text = "Add List Items",
                Location = new Point(410, 868), Size = new Size(120, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnAddItems.Click += (s, e) => AddListItems();

            _btnGroupedData = new DarkButton
            {
                Text = "Load Grouped Data",
                Location = new Point(536, 868), Size = new Size(140, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnGroupedData.Click += (s, e) => LoadGroupedData();

            _btnClear = new DarkButton
            {
                Text = "Clear All",
                Location = new Point(682, 868), Size = new Size(100, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnClear.Click += (s, e) =>
            {
                _tree.Nodes.Clear(); _tree.RefreshLayout();
                _list.Items.Clear(); _list.RefreshLayout();
                _grouped.Clear();
            };

            _btnToggleEnable = new DarkButton
            {
                Text = "Disable All",
                Location = new Point(788, 868), Size = new Size(100, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnToggleEnable.Click += (s, e) =>
            {
                bool disable = _tree.Enabled; // currently enabled → disable all
                _tree.Enabled = !disable;
                _list.Enabled = !disable;
                _grouped.Enabled = !disable;
                _btnToggleEnable.Text = disable ? "Enable All" : "Disable All";
            };

            Controls.Add(treeLabel);
            Controls.Add(_tree);
            Controls.Add(listLabel);
            Controls.Add(_list);
            Controls.Add(groupedLabel);
            Controls.Add(_grouped);
            Controls.Add(newLabel);
            Controls.Add(toggle);
            Controls.Add(toggleLbl);
            Controls.Add(track);
            Controls.Add(trackLbl);
            Controls.Add(search);
            Controls.Add(searchLbl);
            Controls.Add(badge);
            Controls.Add(colorBtn);
            Controls.Add(toastBtn);
            Controls.Add(breadcrumb);
            Controls.Add(calendar);
            Controls.Add(overlayBtn);
            Controls.Add(overlay);
            Controls.Add(_btnAddNodes);
            Controls.Add(_btnExpandAll);
            Controls.Add(_btnCollapseAll);
            Controls.Add(_btnAddItems);
            Controls.Add(_btnGroupedData);
            Controls.Add(_btnClear);
            Controls.Add(_btnToggleEnable);

            Load += (s, e) =>
            {
                AddTreeNodes();
                AddListItems();
                LoadGroupedData();
            };
        }

        private void AddTreeNodes()
        {
            _tree.BeginUpdate();
            try
            {
                var root = new TreeNode("Root") { Name = "Root" };
                for (int i = 0; i < 15; i++)
                {
                    var folder = new TreeNode($"Folder {i + 1}") { Name = $"Folder{i + 1}" };
                    int subCount = i % 5 == 0 ? 10 : 3;
                    for (int j = 0; j < subCount; j++)
                    {
                        var sub = new TreeNode($"SubFolder {folder.Name}.{j + 1}") { Name = $"SubFolder_{i}_{j}" };
                        if (j % 3 == 0)
                            for (int k = 0; k < 6; k++)
                                sub.Nodes.Add(new TreeNode(
                                    $"Deep_Item_{k + 1} — long name to test horizontal scroll behaviour in tree view")
                                { Name = $"Deep_{i}_{j}_{k}" });
                        folder.Nodes.Add(sub);
                    }
                    root.Nodes.Add(folder);
                }
                for (int i = 0; i < 5; i++)
                    root.Nodes.Add(new TreeNode($"Extra_Root_Item_{i + 1}"));
                _tree.Nodes.Add(root);
            }
            finally { _tree.EndUpdate(); }
        }

        private void AddListItems()
        {
            _list.BeginUpdate();
            try
            {
                string[] types = { "Folder", "File", "Shortcut", "Symlink" };
                string[] ext = { ".cs", ".xaml", ".json", ".xml", ".csproj", ".sln", ".md", ".txt", ".png", ".dll" };
                var rng = new Random(42);
                int start = _list.Items.Count;
                for (int i = 0; i < 100; i++)
                {
                    int idx = start + i;
                    string name = types[rng.Next(types.Length)] == "Folder"
                        ? $"Project_Folder_{idx + 1}"
                        : $"File_{idx + 1}{ext[rng.Next(ext.Length)]}";
                    string type = name.Contains("Folder") ? "Folder" : ext[rng.Next(ext.Length)];
                    string size = type == "Folder" ? "" : $"{rng.Next(1, 999)} KB";
                    string mod = DateTime.Now.AddDays(-rng.Next(0, 365)).ToString("yyyy-MM-dd HH:mm");
                    string path = $@"C:\Projects\DarkUI\Src\SubDir_{rng.Next(1, 20)}\{name}";
                    var item = new ListViewItem(name) { Name = $"Item{idx}" };
                    item.SubItems.Add(type);
                    item.SubItems.Add(size);
                    item.SubItems.Add(mod);
                    item.SubItems.Add(path);
                    _list.Items.Add(item);
                }
            }
            finally { _list.EndUpdate(); }
        }

        private void LoadGroupedData()
        {
            _grouped.DefineColumns(
                ("Name", 280),
                ("Size", 80),
                ("Type", 80),
                ("Date Modified", 140)
            );

            var rng = new Random(99);
            var files = Enumerable.Range(1, 200).Select(i => new FileItem
            {
                Name = $"File_{i:D3}{new[] { ".cs", ".xaml", ".json", ".xml", ".png", ".txt", ".md", ".dll" }[rng.Next(8)]}",
                Size = rng.Next(1, 5000),
                Ext = new[] { "Source", "Data", "Image", "Text", "Source", "Data", "Source", "Binary" }[rng.Next(8)],
                Modified = DateTime.Now.AddDays(-rng.Next(0, 730))
            }).ToList();

            _grouped.SetGroups(files,
                groupBy: f => f.Ext,
                columnsFunc: f => new[] { f.Name, $"{f.Size} KB", f.Ext, f.Modified.ToString("yyyy-MM-dd") }
            );
        }

        private class FileItem
        {
            public string Name { get; set; }
            public int Size { get; set; }
            public string Ext { get; set; }
            public DateTime Modified { get; set; }
        }
    }
}
