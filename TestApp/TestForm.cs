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
                Location = new Point(12, 348), Size = new Size(1160, 510),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
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

            Controls.Add(treeLabel);
            Controls.Add(_tree);
            Controls.Add(listLabel);
            Controls.Add(_list);
            Controls.Add(groupedLabel);
            Controls.Add(_grouped);
            Controls.Add(_btnAddNodes);
            Controls.Add(_btnExpandAll);
            Controls.Add(_btnCollapseAll);
            Controls.Add(_btnAddItems);
            Controls.Add(_btnGroupedData);
            Controls.Add(_btnClear);

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
