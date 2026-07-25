using DarkUI.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    public class DarkGroupedListView : UserControl
    {
        private readonly DataGridView _base = new DataGridView();
        private readonly DarkScrollBar _vScrollBar = new DarkScrollBar { ScrollOrientation = DarkScrollOrientation.Vertical };
        private readonly DarkScrollBar _hScrollBar = new DarkScrollBar { ScrollOrientation = DarkScrollOrientation.Horizontal };
        private static readonly PropertyInfo _doubleBuffer = typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly List<int> _groupRows = new();
        private (string name, int width)[] _cachedColumns;
        private bool _updating;
        private bool _updateLayout;
        private int _sortCol = -1;
        private bool _sortAsc = true;
        private int _scrollSize => Consts.ScrollBarSize;

        public event EventHandler SelectedItemChanged;
        public event Action<int, string, MouseEventArgs> GroupHeaderClicked;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedFilePath { get; private set; }

        private ContextMenuStrip _contextMenu;

        public DarkGroupedListView()
        {
            BackColor = Colors.LightBorder;

            _base.Name = "baseView";
            _base.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            _base.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _base.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            _base.RowHeadersVisible = false;
            _base.AllowUserToResizeRows = false;
            _base.BorderStyle = BorderStyle.None;
            _base.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _base.ScrollBars = ScrollBars.None;
            _base.EnableHeadersVisualStyles = false;
            _doubleBuffer.SetValue(_base, true, null);
            _base.BackgroundColor = Colors.GreyBackground;
            _base.GridColor = Colors.GreyBackground;
            _base.CellBorderStyle = DataGridViewCellBorderStyle.None;
            _base.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Colors.DarkBackground, ForeColor = Colors.LightText,
                SelectionBackColor = Colors.DarkBackground, SelectionForeColor = Colors.LightText,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            _base.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Colors.GreyBackground, ForeColor = Colors.LightText,
                SelectionBackColor = Colors.BlueSelection, SelectionForeColor = Colors.LightText
            };
            _base.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Colors.GreyBackground, ForeColor = Colors.LightText,
                SelectionBackColor = Colors.BlueSelection, SelectionForeColor = Colors.LightText
            };
            _base.ReadOnly = true;
            _base.AllowUserToAddRows = false;
            _base.AllowUserToDeleteRows = false;
            _base.MultiSelect = false;
            _base.RowTemplate.Height = 22;

            _base.RowPostPaint += (s, e) =>
            {
                if (e.RowIndex >= 0 && _groupRows.Contains(e.RowIndex))
                {
                    var r = _base.GetRowDisplayRectangle(e.RowIndex, false);
                    using var hiPen = new Pen(Color.FromArgb(80, 85, 90));
                    e.Graphics.DrawLine(hiPen, r.Left, r.Top, r.Right - 1, r.Top);
                    using var shPen = new Pen(Colors.DarkBorder);
                    e.Graphics.DrawLine(shPen, r.Left, r.Bottom - 1, r.Right - 1, r.Bottom - 1);
                }
            };

            _base.RowsAdded += (s, e) => UpdateScrollBarLayout();
            _base.RowsRemoved += (s, e) => UpdateScrollBarLayout();
            _base.ColumnWidthChanged += (s, e) => UpdateScrollBarLayout();
            _base.Scroll += (s, e) =>
            {
                if (_updateLayout) return;
                if (_hScrollBar.Value != _base.HorizontalScrollingOffset)
                    _hScrollBar.Value = _base.HorizontalScrollingOffset;
                if (_vScrollBar.Value != _base.FirstDisplayedScrollingRowIndex)
                    _vScrollBar.Value = _base.FirstDisplayedScrollingRowIndex;
            };

            // Sort within groups by rebuilding rows.
            // DataGridView.Sort() rearranges ALL rows including group headers,
            // which destroys the grouped structure.
            _base.ColumnHeaderMouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Left || _base.Rows.Count == 0) return;
                if (e.ColumnIndex < 0 || _groupRows.Count == 0) return;

                // ── Extract group data ──────────────────────────
                var groups = new List<(string headerText, bool collapsed, List<DataGridViewRow> items)>();
                for (int g = 0; g < _groupRows.Count; g++)
                {
                    int hdr = _groupRows[g];
                    string txt = _base.Rows[hdr].Cells[0].Value?.ToString() ?? "";
                    bool collapsed = txt.StartsWith("▶");
                    var items = new List<DataGridViewRow>();
                    int end = g + 1 < _groupRows.Count ? _groupRows[g + 1] : _base.Rows.Count;
                    for (int r = hdr + 1; r < end; r++)
                        items.Add(_base.Rows[r]);
                    groups.Add((txt, collapsed, items));
                }

                // ── Sort items within each group ────────────────
                int colIdx = e.ColumnIndex;
                var dir = _sortCol == colIdx && _sortAsc
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
                _sortCol = colIdx;
                _sortAsc = dir == ListSortDirection.Ascending;

                foreach (var (_, _, items) in groups)
                {
                    string colName = colIdx >= 0 && colIdx < _base.Columns.Count
                        ? _base.Columns[colIdx].Name : "";
                    items.Sort((a, b) =>
                    {
                        var va = a.Cells[colIdx].Value;
                        var vb = b.Cells[colIdx].Value;
                        int cmp;
                        if (colName == "Size")
                            cmp = CompareSizeStrings(va?.ToString(), vb?.ToString());
                        else
                            cmp = Comparer<object>.Default.Compare(va, vb);
                        return _sortAsc ? cmp : -cmp;
                    });
                }

                // ── Rebuild all rows in grouped order ───────────
                _updating = true;
                _base.Rows.Clear();
                _groupRows.Clear();
                foreach (var (headerText, collapsed, items) in groups)
                {
                    int hdrIdx = _base.Rows.Add();
                    _groupRows.Add(hdrIdx);
                    var hdr = _base.Rows[hdrIdx];
                    hdr.Height = 26;
                    hdr.DefaultCellStyle.BackColor = Colors.MediumBackground;
                    hdr.DefaultCellStyle.ForeColor = Colors.LightText;
                    hdr.DefaultCellStyle.SelectionBackColor = Colors.MediumBackground;
                    hdr.DefaultCellStyle.SelectionForeColor = Colors.LightText;
                    hdr.DefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
                    hdr.Cells[0].Value = collapsed
                        ? headerText.Replace("▼", "▶")
                        : headerText.Replace("▶", "▼");

                    foreach (var item in items)
                    {
                        int r = _base.Rows.Add();
                        _base.Rows[r].Tag = item.Tag;
                        for (int c = 0; c < _base.Columns.Count && c < item.Cells.Count; c++)
                            _base.Rows[r].Cells[c].Value = item.Cells[c].Value;
                        if (item.Cells[3] is DataGridViewImageCell img)
                            _base.Rows[r].Cells[3] = new DataGridViewImageCell
                            { Value = img.Value, ImageLayout = DataGridViewImageCellLayout.Normal };
                        _base.Rows[r].Visible = !collapsed;
                    }
                }
                _updating = false;
                UpdateScrollBarLayout();
            };

            _base.CellMouseClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;

                if (_groupRows.Contains(e.RowIndex))
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        ToggleGroup(e.RowIndex);
                    }
                    else if (e.Button == MouseButtons.Right)
                    {
                        string raw = _base.Rows[e.RowIndex].Cells[0].Value?.ToString() ?? "";
                        string groupName = raw;
                        if (groupName.StartsWith("▼ ") || groupName.StartsWith("▶ "))
                            groupName = groupName.Substring(2);
                        int paren = groupName.LastIndexOf("  (");
                        if (paren > 0) groupName = groupName.Substring(0, paren);
                        GroupHeaderClicked?.Invoke(e.RowIndex, groupName, e);
                    }
                    return;
                }

                var tag = _base.Rows[e.RowIndex].Tag;
                if (tag != null)
                {
                    var t = tag.GetType();
                    var p = t.GetProperty("Path") ?? t.GetProperty("FilePath");
                    SelectedFilePath = p?.GetValue(tag)?.ToString() ?? tag.ToString();
                }
                SelectedItemChanged?.Invoke(this, EventArgs.Empty);

                // Show context menu on item right-click
                if (e.Button == MouseButtons.Right && _contextMenu != null)
                    _contextMenu.Show(Cursor.Position);
            };

            _base.MouseWheel += (s, e) =>
            {
                if (_vScrollBar.Visible)
                    _vScrollBar.Value = Math.Max(0, Math.Min(_vScrollBar.Maximum,
                        _vScrollBar.Value - Math.Sign(e.Delta)));
            };

            // ── Vertical scrollbar ────────────────────────────
            _vScrollBar.BackColor = Colors.MediumBackground;
            _vScrollBar.Minimum = 0; _vScrollBar.Maximum = 0;
            _vScrollBar.ValueChanged += (s, e) =>
            {
                if (_updateLayout || _base.RowCount == 0) return;
                _updateLayout = true;
                int target = Math.Max(0, Math.Min(_base.RowCount - 1, e.Value));
                while (target < _base.RowCount && !_base.Rows[target].Visible) target++;
                if (target < _base.RowCount)
                    _base.FirstDisplayedScrollingRowIndex = target;
                _updateLayout = false;
            };

            // ── Horizontal scrollbar ──────────────────────────
            _hScrollBar.BackColor = Colors.MediumBackground;
            _hScrollBar.Minimum = 0; _hScrollBar.Maximum = 0;
            _hScrollBar.ValueChanged += (s, e) =>
            {
                if (_updateLayout) return;
                _updateLayout = true;
                _base.HorizontalScrollingOffset = Math.Max(0, Math.Min(
                    Math.Max(TotalColumnsWidth - 1, 0), e.Value));
                _updateLayout = false;
            };

            Controls.Add(_base);
            Controls.Add(_vScrollBar);
            Controls.Add(_hScrollBar);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _base?.Dispose();
                _hScrollBar?.Dispose();
                _vScrollBar?.Dispose();
            }
            base.Dispose(disposing);
        }

        // ── Public API ──────────────────────────────────────────

        public ContextMenuStrip ContextMenuStrip
        {
            get => _contextMenu;
            set { _contextMenu = value; /* do NOT set _base.ContextMenuStrip — it suppresses CellMouseClick */ }
        }

        public bool MultiSelect
        {
            get => _base.MultiSelect;
            set => _base.MultiSelect = value;
        }

        public void DefineColumns(params (string name, int width)[] cols)
        {
            _cachedColumns = cols;
            _base.Columns.Clear();
            for (int i = 0; i < cols.Length; i++)
            {
                bool fillMode = cols[i].width <= 0;
                var col = new DataGridViewTextBoxColumn
                {
                    Name = cols[i].name,
                    HeaderText = cols[i].name,
                    Width = fillMode ? 100 : cols[i].width,
                    SortMode = DataGridViewColumnSortMode.Programmatic,
                    AutoSizeMode = fillMode
                        ? DataGridViewAutoSizeColumnMode.Fill
                        : DataGridViewAutoSizeColumnMode.None
                };
                if (i > 0) col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                _base.Columns.Add(col);
            }
        }

        public void SetGroups<T>(IEnumerable<T> items, Func<T, string> groupBy,
            Func<T, string[]> columnsFunc, Func<T, Image[]> imagesFunc = null)
        {
            _updating = true;
            _base.Rows.Clear();
            _groupRows.Clear();

            // Define columns from cache if not already defined
            if (_cachedColumns != null && _base.Columns.Count == 0)
                DefineColumns(_cachedColumns);
            if (_base.Columns.Count == 0)
            {
                _updating = false;
                UpdateScrollBarLayout();
                return;
            }

            if (items == null)
            {
                _updating = false;
                UpdateScrollBarLayout();
                return;
            }

            var grouped = items.GroupBy(groupBy).OrderBy(g => g.Key);
            foreach (var grp in grouped)
            {
                int hdrIdx = _base.Rows.Add();
                _groupRows.Add(hdrIdx);
                var hdr = _base.Rows[hdrIdx];
                hdr.Height = 26;
                hdr.DefaultCellStyle.BackColor = Colors.MediumBackground;
                hdr.DefaultCellStyle.ForeColor = Colors.LightText;
                hdr.DefaultCellStyle.SelectionBackColor = Colors.MediumBackground;
                hdr.DefaultCellStyle.SelectionForeColor = Colors.LightText;
                hdr.DefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
                hdr.Cells[0].Value = $"▼ {grp.Key}  ({grp.Count()})";

                foreach (var item in grp)
                {
                    var cols = columnsFunc(item);
                    var imgs = imagesFunc?.Invoke(item);
                    int r = _base.Rows.Add();
                    _base.Rows[r].Tag = item;
                    for (int c = 0; c < cols.Length && c < _base.Columns.Count; c++)
                        _base.Rows[r].Cells[c].Value = cols[c];
                    if (imgs != null && 3 < imgs.Length && imgs[3] != null && 3 < _base.Columns.Count)
                        _base.Rows[r].Cells[3] = new DataGridViewImageCell
                        {
                            Value = imgs[3],
                            ImageLayout = DataGridViewImageCellLayout.Normal
                        };
                }
            }
            _updating = false;
            UpdateScrollBarLayout();
        }

        public void Clear() { _base.Rows.Clear(); _groupRows.Clear(); UpdateScrollBarLayout(); }
        public void ExpandAll() { SetAllGroups(false); }
        public void CollapseAll() { SetAllGroups(true); }

        private void SetAllGroups(bool collapse)
        {
            foreach (int hdrIdx in _groupRows)
            {
                string txt = _base.Rows[hdrIdx].Cells[0].Value?.ToString() ?? "";
                bool currentlyExpanded = txt.StartsWith("▼");
                if (collapse == currentlyExpanded)
                    ToggleGroup(hdrIdx);
            }
        }

        public List<T> GetGroupItems<T>(int headerRowIndex) where T : class
        {
            var items = new List<T>();
            for (int r = headerRowIndex + 1;
                 r < _base.Rows.Count && !_groupRows.Contains(r); r++)
            {
                if (_base.Rows[r].Tag is T item)
                    items.Add(item);
            }
            return items;
        }

        public List<string> GetGroupFilePaths(int headerRowIndex)
        {
            var paths = new List<string>();
            for (int r = headerRowIndex + 1;
                 r < _base.Rows.Count && !_groupRows.Contains(r); r++)
            {
                var tag = _base.Rows[r].Tag;
                if (tag != null)
                {
                    var t = tag.GetType();
                    var p = t.GetProperty("Path") ?? t.GetProperty("FilePath");
                    var path = p?.GetValue(tag)?.ToString();
                    if (!string.IsNullOrEmpty(path))
                        paths.Add(path);
                }
            }
            return paths;
        }

        public List<string> GetSelectedFilePaths()
        {
            var paths = new List<string>();
            foreach (DataGridViewRow row in _base.SelectedRows)
            {
                if (_groupRows.Contains(row.Index)) continue;
                var tag = row.Tag;
                if (tag != null)
                {
                    var t = tag.GetType();
                    var p = t.GetProperty("Path") ?? t.GetProperty("FilePath");
                    var path = p?.GetValue(tag)?.ToString();
                    if (!string.IsNullOrEmpty(path))
                        paths.Add(path);
                }
            }
            return paths;
        }

        // ── Layout ───────────────────────────────────────────────

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBarLayout();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateScrollBarLayout();
        }

        private int TotalColumnsWidth =>
            _base.Columns.Cast<DataGridViewColumn>().Sum(c => c.Width);

        private void UpdateScrollBarLayout()
        {
            if (_updating || _updateLayout) return;
            try
            {
                _updateLayout = true;
                var size = ClientSize;
                const int B = 1;

                // Two-pass — scrollbar visibility changes the available space
                for (int pass = 0; pass < 2; pass++)
                {
                    // Vertical
                    int rowCount = _base.RowCount;
                    int visibleRowCount = CountVisibleRows();
                    bool vVis = visibleRowCount < rowCount && rowCount > 0;
                    int vbw = vVis ? _scrollSize : 0;

                    // Horizontal
                    int totalW = TotalColumnsWidth;
                    int availW = size.Width - B * 2 - vbw;
                    bool hVis = totalW > availW && availW > 0;
                    int hbh = hVis ? _scrollSize : 0;

                    var baseBounds = new Rectangle(B, B,
                        size.Width - B * 2 - vbw,
                        size.Height - B * 2 - hbh);

                    if (vVis)
                    {
                        _vScrollBar.ViewSize = visibleRowCount;
                        _vScrollBar.Maximum = Math.Max(visibleRowCount + 1, rowCount);
                        _vScrollBar.Bounds = new Rectangle(
                            size.Width - _scrollSize - B, B, _scrollSize, size.Height - B * 2 - hbh);
                    }
                    _vScrollBar.Visible = vVis;

                    if (hVis)
                    {
                        _hScrollBar.ViewSize = availW;
                        _hScrollBar.Maximum = Math.Max(availW + 1, totalW);
                        _hScrollBar.Bounds = new Rectangle(
                            B, size.Height - _scrollSize - B, size.Width - B * 2 - vbw, _scrollSize);
                    }
                    _hScrollBar.Visible = hVis;

                    _base.Bounds = baseBounds;
                }

                _base.SendToBack();
                _vScrollBar.BringToFront();
                _hScrollBar.BringToFront();
            }
            finally { _updateLayout = false; }
        }

        private int CountVisibleRows()
        {
            int count = 0;
            int remaining = ClientSize.Height - _base.ColumnHeadersHeight;
            if (remaining <= 0) return 0;
            foreach (DataGridViewRow row in _base.Rows)
            {
                if (!row.Visible) continue;
                remaining -= row.Height;
                if (remaining < 0) break;
                count++;
            }
            return count;
        }

        private static int CompareSizeStrings(string a, string b)
        {
            long Parse(string s)
            {
                if (string.IsNullOrEmpty(s)) return 0;
                var parts = s.Split(' ');
                if (parts.Length != 2) return 0;
                if (!double.TryParse(parts[0], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double val)) return 0;
                return parts[1].ToUpperInvariant() switch
                {
                    "B" or "BYTES" => (long)val,
                    "KB" => (long)(val * 1024),
                    "MB" => (long)(val * 1024 * 1024),
                    "GB" => (long)(val * 1024 * 1024 * 1024),
                    "TB" => (long)(val * 1024L * 1024 * 1024 * 1024),
                    _ => 0
                };
            }
            return Parse(a).CompareTo(Parse(b));
        }

        // ── Collapse/expand ─────────────────────────────────────

        private void ToggleGroup(int hdrRow)
        {
            string txt = _base.Rows[hdrRow].Cells[0].Value?.ToString() ?? "";
            bool expanded = txt.StartsWith("▼");
            for (int r = hdrRow + 1; r < _base.Rows.Count && !_groupRows.Contains(r); r++)
                _base.Rows[r].Visible = !expanded;
            _base.Rows[hdrRow].Cells[0].Value = expanded
                ? txt.Replace("▼", "▶") : txt.Replace("▶", "▼");
            UpdateScrollBarLayout();
        }
    }
}
