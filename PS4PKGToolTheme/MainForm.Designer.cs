using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DarkUI.Config;
using DarkUI.Controls;
using DarkUI.Forms;

namespace PS4PKGToolTheme
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ── Form ──────────────────────────────────────────
            Text = "PS4 PKG Tool — Theme Preview";
            Size = new System.Drawing.Size(1280, 820);
            MinimumSize = new System.Drawing.Size(900, 600);

            // ── Menu Strip ────────────────────────────────────
            var menuStrip = new DarkMenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("Open Game Folder"),
                new ToolStripMenuItem("Refresh Content"),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Exit"),
            });
            var toolsMenu = new ToolStripMenuItem("Tools");
            toolsMenu.DropDownItems.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("Rename PKG"),
                new ToolStripMenuItem("Export PKG List to Excel"),
                new ToolStripMenuItem("View Trophy List"),
            });
            var helpMenu = new ToolStripMenuItem("Help");
            helpMenu.DropDownItems.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("About"),
            });
            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, toolsMenu, helpMenu });
            Controls.Add(menuStrip);

            // ── Status Strip ──────────────────────────────────
            var statusStrip = new DarkStatusStrip();
            var statusLabel = new ToolStripStatusLabel("Ready");
            var statusCount = new ToolStripStatusLabel("0 files loaded");
            var statusTheme = new ToolStripStatusLabel("Theme: Default");
            var statusProgress = new DarkToolStripProgressBar { Minimum = 0, Maximum = 100, Value = 35, Size = new System.Drawing.Size(120, 14) };
            var statusProgTimer = new System.Windows.Forms.Timer { Interval = 60 };
            statusProgTimer.Tick += (s, e) => { statusProgress.Value = (statusProgress.Value + 1) % 101; };
            statusProgTimer.Start();
            var statusBtn = new DarkToolStripButton("Refresh") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            statusBtn.Click += (s, e) => { statusLabel.Text = "Refreshed " + DateTime.Now.ToString("HH:mm:ss"); };
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, statusCount, statusTheme, statusProgress, statusBtn });
            Controls.Add(statusStrip);
            ((Control)statusStrip).Dock = DockStyle.Bottom;

            // ── Main Tab Control ──────────────────────────────
            var mainTabs = new DarkTabControl { Dock = DockStyle.Fill };
            Controls.Add(mainTabs);

            // ── Tab 1: PKG Table ────────────────────────────────
            var tabTable = new TabPage("PKG Table");
            var gridPKG = new DarkDataGridView { Dock = DockStyle.Fill };
            gridPKG.Columns.Add("Filename", "Filename");
            gridPKG.Columns.Add("Title", "Title");
            gridPKG.Columns.Add("Title ID", "Title ID");
            gridPKG.Columns.Add("Region", "Region");
            gridPKG.Columns.Add("Version", "Version");
            gridPKG.Columns.Add("Type", "Type");
            gridPKG.Columns.Add("Category", "Category");
            gridPKG.Columns.Add("Size", "Size");
            tabTable.Controls.Add(gridPKG);
            mainTabs.TabPages.Add(tabTable);
            SeedGridData(gridPKG);

            // DarkSearchBox demo — placeholder, ✕ clear, themed
            var searchBox = new DarkSearchBox
            {
                Location = new System.Drawing.Point(8, 8),
                Size = new System.Drawing.Size(260, 26),
                Placeholder = "Search PKGs…"
            };
            tabTable.Controls.Add(searchBox);
            searchBox.BringToFront();
            gridPKG.Padding = new Padding(0, 42, 0, 0);

            // ── Tab 2: Grouped List (DarkGroupedListView) ───────
            var tabGroup = new TabPage("Grouped List");
            var groupedLV = new DarkGroupedListView { Dock = DockStyle.Fill, MultiSelect = true };
            tabGroup.Controls.Add(groupedLV);
            mainTabs.TabPages.Add(tabGroup);
            SeedGroupedData(groupedLV);

            // ── Tab 2: File Browser ────────────────────────────
            var tabFiles = new TabPage("File Browser");
            var splitFiles = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 320 };
            tabFiles.Controls.Add(splitFiles);

            // Left: Tree view
            var treePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
            var treeLabel = new DarkLabel { Text = "Package Contents", Dock = DockStyle.Top, Height = 22 };
            var fileTree = new DarkTreeView { Dock = DockStyle.Fill, ShowLines = true, ShowRootLines = true };
            treePanel.Controls.Add(fileTree);
            treePanel.Controls.Add(treeLabel);
            splitFiles.Panel1.Controls.Add(treePanel);

            // Right: List view
            var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
            var listLabel = new DarkLabel { Text = "Files", Dock = DockStyle.Top, Height = 22 };
            var fileList = new DarkListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = true };
            fileList.Columns.Add("Name", 180);
            fileList.Columns.Add("Size", 90);
            fileList.Columns.Add("Type", 90);
            fileList.Columns.Add("Modified", 130);
            fileList.SmallImageList = CreateDemoImageList();
            listPanel.Controls.Add(fileList);
            listPanel.Controls.Add(listLabel);
            splitFiles.Panel2.Controls.Add(listPanel);
            mainTabs.TabPages.Add(tabFiles);
            SeedFileTree(fileTree, fileList);

            // ── Tab 3: Trophies ─────────────────────────────────
            var tabTrophies = new TabPage("Trophies");
            var trophyGrid = new DarkDataGridView { Dock = DockStyle.Fill };
            trophyGrid.Columns.Add("Name", "Name");
            trophyGrid.Columns.Add("Type", "Type");
            trophyGrid.Columns.Add("Grade", "Grade");
            trophyGrid.Columns.Add("Unlock", "Unlock %");
            tabTrophies.Controls.Add(trophyGrid);
            mainTabs.TabPages.Add(tabTrophies);
            SeedTrophyData(trophyGrid);

            // ── Tab 4: Progress ──────────────────────────────────
            var tabProgress = new TabPage("Progress");
            var progPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            tabProgress.Controls.Add(progPanel);

            var pbLabel = new DarkLabel { Text = "DarkProgressBar", Location = new System.Drawing.Point(16, 10), AutoSize = true };
            var pb1 = new DarkProgressBar { Location = new System.Drawing.Point(16, 34), Size = new System.Drawing.Size(500, 24), Maximum = 100, Value = 65, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var pb2 = new DarkProgressBar { Location = new System.Drawing.Point(16, 70), Size = new System.Drawing.Size(500, 12), Maximum = 200, Value = 120, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var pb3 = new DarkProgressBar { Location = new System.Drawing.Point(16, 94), Size = new System.Drawing.Size(500, 24), Maximum = 100, Value = 0, Style = System.Windows.Forms.ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var btnProg = new DarkButton { Text = "Animate", Location = new System.Drawing.Point(16, 130), Size = new System.Drawing.Size(90, 26) };
            var progTimer = new System.Windows.Forms.Timer { Interval = 50 };
            progTimer.Tick += (s, e) => { pb1.Value = (pb1.Value + 1) % 101; pb2.Value = (pb2.Value + 2) % 201; };
            btnProg.Click += (s, e) => { if (progTimer.Enabled) { progTimer.Stop(); btnProg.Text = "Animate"; } else { progTimer.Start(); btnProg.Text = "Stop"; } };

            progPanel.Controls.AddRange(new Control[] { pbLabel, pb1, pb2, pb3, btnProg });
            mainTabs.TabPages.Add(tabProgress);

            // ── Tab 5: Images ───────────────────────────────────
            var tabImages = new TabPage("Images");
            var imgLabel = new DarkLabel { Text = "Background Images & Icons", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleCenter, Font = new System.Drawing.Font("Segoe UI", 14f) };
            tabImages.Controls.Add(imgLabel);
            mainTabs.TabPages.Add(tabImages);

            // ── Tab 5: Entries ──────────────────────────────────
            var tabEntries = new TabPage("Entries");
            var entryGrid = new DarkDataGridView { Dock = DockStyle.Fill };
            entryGrid.Columns.Add("ID", "ID");
            entryGrid.Columns.Add("Name", "Name");
            entryGrid.Columns.Add("Type", "Type");
            tabEntries.Controls.Add(entryGrid);
            mainTabs.TabPages.Add(tabEntries);
            SeedEntryData(entryGrid);

            // ── Tab 6: Log ──────────────────────────────────────
            var tabLog = new TabPage("Log");
            var tbLog = new DarkRichTextBox { Dock = DockStyle.Fill, ReadOnly = true };
            tbLog.Text = "[08:00] PS4 PKG Tool initialized\r\n[08:01] No PKG files loaded\r\n[08:02] Ready\r\n\r\nSelect this text to see themed highlighting.";
            tabLog.Controls.Add(tbLog);
            mainTabs.TabPages.Add(tabLog);

            // ── Theme Selector (bottom bar) ────────────────────
            // Persisted to a JSON file next to the exe so the choice survives restarts.
            string themePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme.json");

            var themePanel = new Panel { Dock = DockStyle.Bottom, Height = 32, Padding = new Padding(8, 4, 8, 4) };
            var themeLabel = new DarkLabel { Text = "Theme:", Location = new System.Drawing.Point(8, 8), AutoSize = true };
            var themeCombo = new DarkComboBox { Location = new System.Drawing.Point(62, 4), Size = new System.Drawing.Size(200, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            themeCombo.Items.AddRange(ThemeManager.Presets.Select(p => p.Name).ToArray());

            // Wire the handler FIRST so the restore below actually applies the theme.
            themeCombo.SelectedIndexChanged += (s, e) =>
            {
                var theme = ThemeManager.Presets[themeCombo.SelectedIndex];
                ThemeManager.Apply(theme);
                statusTheme.Text = "Theme: " + theme.Name;
                try { File.WriteAllText(themePath, theme.Name); } catch { }
            };

            // Restore saved theme (fires SelectedIndexChanged → Apply)
            string savedName = "";
            try { if (File.Exists(themePath)) savedName = File.ReadAllText(themePath).Trim(); } catch { }
            int savedIdx = ThemeManager.Presets
                .Select((p, i) => (p, i))
                .FirstOrDefault(x => x.p.Name == savedName).i;
            themeCombo.SelectedIndex = savedIdx >= 0 ? savedIdx : 0;
            themePanel.Controls.Add(themeLabel);
            themePanel.Controls.Add(themeCombo);
            Controls.Add(themePanel);
            themePanel.BringToFront();

            mainTabs.BringToFront();
        }
    }
}
