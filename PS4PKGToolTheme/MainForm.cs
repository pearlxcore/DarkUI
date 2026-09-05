using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DarkUI.Config;
using DarkUI.Controls;
using DarkUI.Forms;

namespace PS4PKGToolTheme
{
    public partial class MainForm : DarkForm
    {
        public MainForm()
        {
            InitializeComponent();
            CenterToScreen();
        }

        private static void SeedGridData(DarkDataGridView grid)
        {
            var rng = new Random(42);
            string[] types = { "Game", "App", "Patch", "DLC", "Game", "App", "DLC", "Patch" };
            string[] regions = { "USA", "EUR", "JPN", "ASIA" };
            string[] categories = { "Full Game", "Application", "Update", "Add-on", "Full Game" };
            string[] titles = { "Marvel's Spider-Man", "God of War", "Horizon Forbidden West", "Ghost of Tsushima",
                                "Ratchet & Clank", "Returnal", "Demon's Souls", "Gran Turismo 7" };

            for (int i = 0; i < 30; i++)
            {
                var row = new System.Windows.Forms.DataGridViewRow();
                row.Cells.Add(new DataGridViewTextBoxCell { Value = $"EP00{rng.Next(1000,9999):D4}-CUSA{rng.Next(10000,99999):D5}_00-{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}" });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = titles[rng.Next(titles.Length)] });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = $"CUSA{rng.Next(10000,99999):D5}" });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = regions[rng.Next(regions.Length)] });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = $"{rng.Next(1,10):D2}.{rng.Next(0,99):D2}" });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = types[rng.Next(types.Length)] });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = categories[rng.Next(categories.Length)] });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = $"{rng.Next(1, 99)}.{rng.Next(0,9)} GB" });
                grid.Rows.Add(row);
            }
        }

        private static void SeedGroupedData(DarkGroupedListView glv)
        {
            glv.DefineColumns(("Filename", 260), ("Title ID", 120), ("Version", 80), ("Size", 100));
            var rng = new Random(99);
            var files = Enumerable.Range(1, 100).Select(i => new DemoFile
            {
                Name = $"PKG_{i:D3}.pkg",
                Title = $"CUSA{rng.Next(10000, 99999):D5}",
                Version = $"{rng.Next(1, 10):D2}.{rng.Next(0, 99):D2}",
                Size = $"{rng.Next(1, 99)}.{rng.Next(0, 9)} GB",
                Cat = new[] { "Game", "App", "Patch", "DLC", "App", "Game" }[rng.Next(6)]
            }).ToList();

            glv.SetGroups(files,
                groupBy: f => f.Cat,
                columnsFunc: f => new[] { f.Name, f.Title, f.Version, f.Size }
            );
        }

        private static void SeedFileTree(DarkTreeView tree, DarkListView list)
        {
            var root = new TreeNode("CUSA12345") { Name = "root" };
            var dirs = new[] { "sce_sys", "sce_module", "app", "data", "assets" };
            foreach (var d in dirs)
            {
                var dir = new TreeNode(d) { Name = d };
                for (int j = 0; j < 5; j++)
                {
                    dir.Nodes.Add(new TreeNode($"file_{j}.{new[]{"prx","elf","json","png","xml"}[j]}"));
                }
                if (d == "sce_sys")
                {
                    var sub = new TreeNode("param") { Name = "param" };
                    sub.Nodes.Add(new TreeNode("param.sfo"));
                    sub.Nodes.Add(new TreeNode("playgo-chunk.dat"));
                    dir.Nodes.Add(sub);
                }
                root.Nodes.Add(dir);
            }
            tree.Nodes.Add(root);

            // Seed list with some items
            string[] names = { "eboot.bin", "param.sfo", "playgo-chunk.dat", "libSceFios2.prx",
                               "libc.prx", "app.exe", "config.json", "texture_atlas.png", "data.xml", "metadata.dat" };
            string[] sizes = { "4.2 MB", "1.5 KB", "256 KB", "890 KB", "340 KB", "12.8 MB", "540 B", "2.1 MB", "8 KB", "64 B" };
            string[] types = { "Binary", "SFO", "Data", "PRX", "PRX", "Executable", "JSON", "PNG", "XML", "Data" };
            for (int i = 0; i < 10; i++)
            {
                // Icon index by extension/type (0=binary,1=data,2=executable,3=json,4=image)
                int iconIdx = types[i] switch
                {
                    "Binary" or "PRX" or "SFO" => 0,
                    "Data" => 1,
                    "Executable" => 2,
                    "JSON" or "XML" => 3,
                    _ => 4
                };
                var item = new ListViewItem(names[i], iconIdx);
                item.SubItems.Add(sizes[i]);
                item.SubItems.Add(types[i]);
                item.SubItems.Add(DateTime.Now.AddDays(-i * 30).ToString("yyyy-MM-dd"));
                list.Items.Add(item);
            }
        }

        /// <summary>
        /// Programmatic 16x16 demo icons — a folder plus four colored file glyphs.
        /// </summary>
        private static ImageList CreateDemoImageList()
        {
            var il = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
            il.Images.Add(MakeIcon(Color.FromArgb(220, 180, 60), Color.FromArgb(90, 70, 20)));   // 0 binary (folder-ish)
            il.Images.Add(MakeIcon(Color.FromArgb(100, 160, 220), Color.FromArgb(40, 70, 110))); // 1 data
            il.Images.Add(MakeIcon(Color.FromArgb(120, 200, 90), Color.FromArgb(45, 80, 35)));   // 2 executable
            il.Images.Add(MakeIcon(Color.FromArgb(220, 120, 90), Color.FromArgb(90, 45, 30)));   // 3 json/xml
            il.Images.Add(MakeIcon(Color.FromArgb(180, 150, 240), Color.FromArgb(70, 55, 100))); // 4 image
            return il;
        }

        private static Bitmap MakeIcon(Color fill, Color border)
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (var b = new SolidBrush(fill))
                    g.FillRectangle(b, 2, 4, 12, 9);
                using (var p = new Pen(border))
                    g.DrawRectangle(p, 2, 4, 11, 8);
                using (var b = new SolidBrush(border))
                    g.FillRectangle(b, 2, 3, 4, 2);
            }
            return bmp;
        }

        private static void SeedTrophyData(DarkDataGridView grid)
        {
            string[] grades = { "Platinum", "Gold", "Silver", "Silver", "Bronze", "Bronze", "Bronze", "Bronze" };
            string[] names = { "Master of Fate", "Dragon Slayer", "Treasure Hunter",
                               "Speed Demon", "Completionist", "Secret Finder", "No-Hit Champion", "Casual Play" };
            var rng = new Random(7);
            for (int i = 0; i < 8; i++)
            {
                var row = new System.Windows.Forms.DataGridViewRow();
                row.Cells.Add(new DataGridViewTextBoxCell { Value = names[i] });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = grades[i] });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = i == 0 ? "Ultra Rare" : i < 3 ? "Rare" : "Common" });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = $"{rng.Next(1, 80)}.{rng.Next(0, 9)}%" });
                grid.Rows.Add(row);
            }
        }

        private static void SeedEntryData(DarkDataGridView grid)
        {
            for (int i = 0; i < 12; i++)
            {
                var row = new System.Windows.Forms.DataGridViewRow();
                row.Cells.Add(new DataGridViewTextBoxCell { Value = $"{i}" });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = $"Entry_{i:D2}" });
                row.Cells.Add(new DataGridViewTextBoxCell { Value = i % 3 == 0 ? "Directory" : "File" });
                grid.Rows.Add(row);
            }
        }

        private class DemoFile
        {
            public string Name { get; set; } = "";
            public string Title { get; set; } = "";
            public string Version { get; set; } = "";
            public string Size { get; set; } = "";
            public string Cat { get; set; } = "Game";
        }
    }
}
