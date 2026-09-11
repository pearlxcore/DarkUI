using DarkUI.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// Raised when a group header row is clicked in a <see cref="DarkDataGridView"/>.
    /// </summary>
    public class DarkDataGridViewGroupHeaderEventArgs : EventArgs
    {
        public int RowIndex { get; }
        public string Key { get; }
        public string Label { get; }
        public DataGridViewRow Row { get; }
        public MouseButtons Button { get; }
        public bool Toggled { get; }

        public DarkDataGridViewGroupHeaderEventArgs(int rowIndex, string key, string label,
            DataGridViewRow row, MouseButtons button, bool toggled)
        {
            RowIndex = rowIndex;
            Key = key;
            Label = label;
            Row = row;
            Button = button;
            Toggled = toggled;
        }
    }

    public partial class DarkDataGridView
    {
        private sealed class GroupBucket
        {
            public string Key;
            public object KeyObject;
            public string Label;
            public readonly List<DataGridViewRow> Rows = new List<DataGridViewRow>();
            public readonly List<object> Items = new List<object>();
        }

        private sealed class GroupInfo
        {
            public string Key;
            public string Label;
            public int Count;
        }

        private const int GroupHeaderTextPadding = 8;

        private readonly HashSet<DataGridViewRow> _groupRows = new HashSet<DataGridViewRow>();
        private readonly Dictionary<DataGridViewRow, GroupInfo> _groupInfo = new Dictionary<DataGridViewRow, GroupInfo>();
        private readonly HashSet<string> _collapsedKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<DataGridViewColumn> _columnsForcedProgrammatic = new List<DataGridViewColumn>();

        // Suppresses scrollbar/layout work while the row collection is rebuilt.
        private bool _updating;
        private bool _fixingSelection;
        private bool _suppressSelection;

        private int _sortColumnIndex = -1;
        private bool _sortAscending = true;

        private int _groupHeaderColumnIndex;
        private bool _groupHeaderColumnNameSet;
        private string _groupHeaderColumnName;
        private float _groupHeaderHeight = 26f;
        private bool _autoSortGroups = true;

        private Font _groupHeaderFont;
        private Color _groupGradientTop;
        private Color _groupGradientBottom;
        private Color _groupTopLine;
        private Color _groupBottomLine;

        /// <summary>
        /// Raised for a left-click (after the group toggles) or a right-click
        /// (context menu request) on a group header row.
        /// </summary>
        public event EventHandler<DarkDataGridViewGroupHeaderEventArgs> GroupHeaderClicked;

        /// <summary>Raised after the grouped row layout is rebuilt.</summary>
        public event EventHandler GroupsChanged;

        /// <summary>
        /// Builds a group header label from the key and item count.
        /// Defaults to <c>"Key  (count)"</c>.
        /// </summary>
        public Func<object, int, string> GroupLabelFormatter { get; set; }

        /// <summary>
        /// Optional comparer for item cell values used by <see cref="SortGroups"/>.
        /// Defaults to <see cref="IComparable"/> with a case-insensitive string fallback.
        /// </summary>
        public Comparison<object> GroupCellValueComparer { get; set; }

        /// <summary>True while the grid contains at least one group header row.</summary>
        public bool IsGrouped => _groupRows.Count > 0;

        /// <summary>Number of groups currently shown.</summary>
        public int GroupCount => _groupRows.Count;

        /// <summary>
        /// Column index whose cell shows the group label. Defaults to 0.
        /// Ignored when <see cref="GroupHeaderColumnName"/> is set.
        /// </summary>
        public int GroupHeaderColumnIndex
        {
            get => _groupHeaderColumnIndex;
            set => _groupHeaderColumnIndex = value;
        }

        /// <summary>
        /// Column name whose cell shows the group label. Takes precedence over
        /// <see cref="GroupHeaderColumnIndex"/> while the column exists.
        /// </summary>
        public string GroupHeaderColumnName
        {
            get => _groupHeaderColumnName;
            set
            {
                _groupHeaderColumnName = value;
                _groupHeaderColumnNameSet = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>Height of a group header row in pixels. Defaults to 26.</summary>
        public float GroupHeaderHeight
        {
            get => _groupHeaderHeight;
            set
            {
                _groupHeaderHeight = Math.Max(1f, value);
                foreach (var row in _groupRows.ToList())
                    if (row.Index >= 0)
                        row.Height = (int)Math.Round(_groupHeaderHeight);
                _base?.Invalidate();
            }
        }

        /// <summary>
        /// When true (default), clicking a column header while grouped sorts the
        /// items inside each group without scattering the group headers.
        /// </summary>
        public bool AutoSortGroups
        {
            get => _autoSortGroups;
            set => _autoSortGroups = value;
        }

        private void InitGrouping()
        {
            ApplyGroupTheme();
            _base.CellPainting += BaseGroupCellPainting;
            _base.CellMouseClick += BaseGroupCellMouseClick;
            _base.SelectionChanged += BaseGroupSelectionChanged;
            _base.ColumnHeaderMouseClick += BaseGroupColumnHeaderMouseClick;
            _base.FontChanged += delegate { ApplyGroupTheme(); };
            ThemeManager.ThemeChanged += OnGroupThemeChanged;
            Disposed += delegate
            {
                ThemeManager.ThemeChanged -= OnGroupThemeChanged;
                _groupHeaderFont?.Dispose();
            };
        }

        private void OnGroupThemeChanged(object sender, EventArgs e) => ApplyGroupTheme();

        private void ApplyGroupTheme()
        {
            _groupGradientTop = Colors.MediumBackground;
            _groupGradientBottom = Colors.DarkBackground;
            _groupTopLine = Colors.LightBorder;
            _groupBottomLine = Colors.DarkBorder;

            RebuildGroupHeaderFont();

            foreach (var row in _groupRows.ToList())
                if (row.Index >= 0)
                    ApplyGroupHeaderRowStyle(row);

            _base?.Invalidate();
        }

        private void RebuildGroupHeaderFont()
        {
            _groupHeaderFont?.Dispose();
            _groupHeaderFont = new Font(_base.Font, FontStyle.Bold);
        }

        private void ApplyGroupHeaderRowStyle(DataGridViewRow row)
        {
            var style = row.DefaultCellStyle;
            style.BackColor = _groupGradientTop;
            style.ForeColor = Colors.LightText;
            style.SelectionBackColor = _groupGradientTop;
            style.SelectionForeColor = Colors.LightText;
            style.Font = _groupHeaderFont ?? _base.Font;
        }

        private int ResolveGroupHeaderColumnIndex()
        {
            if (_groupHeaderColumnNameSet && !string.IsNullOrEmpty(_groupHeaderColumnName)
                && _base.Columns.Contains(_groupHeaderColumnName))
                return _base.Columns[_groupHeaderColumnName].Index;

            if (_groupHeaderColumnIndex >= 0 && _groupHeaderColumnIndex < _base.Columns.Count)
                return _groupHeaderColumnIndex;

            return 0;
        }

        // ── Public grouping API ─────────────────────────────────

        /// <summary>Groups the existing item rows by the value of a column.</summary>
        public void GroupBy(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= _base.Columns.Count)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));
            GroupByCore(row => row.Cells[columnIndex].Value, null);
        }

        /// <summary>Groups the existing item rows by the value of a named column.</summary>
        public void GroupBy(string columnName)
        {
            if (string.IsNullOrEmpty(columnName) || !_base.Columns.Contains(columnName))
                throw new ArgumentException("Unknown column: " + columnName, nameof(columnName));
            GroupBy(_base.Columns[columnName].Index);
        }

        /// <summary>Groups the existing item rows by the value of a column.</summary>
        public void GroupBy(DataGridViewColumn column)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));
            GroupBy(column.Index);
        }

        /// <summary>
        /// Groups the existing item rows by a key derived from each row's
        /// <see cref="DataGridViewRow.Tag"/>. Rows whose tag is not a
        /// <typeparamref name="T"/> are placed in a single unlabelled group.
        /// </summary>
        public void GroupBy<T>(Func<T, object> selector, IComparer<object> groupComparer = null)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            GroupByCore(row => row.Tag is T item ? selector(item) : (object)null, groupComparer);
        }

        /// <summary>
        /// Clears the grid and rebuilds it as groups of <paramref name="items"/>.
        /// Each item row is created by <paramref name="fillRow"/>.
        /// </summary>
        public void SetGroups<T>(IEnumerable<T> items, Func<T, object> groupSelector,
            Action<DataGridViewRow, T> fillRow, IComparer<object> groupComparer = null)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (groupSelector == null)
                throw new ArgumentNullException(nameof(groupSelector));
            ThrowIfDataBound();
            if (_base.Columns.Count == 0)
                return;
            EnsureGroupSortMode();

            var order = new List<GroupBucket>();
            var lookup = new Dictionary<string, GroupBucket>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                object keyObject = groupSelector(item);
                string key = KeyToString(keyObject);
                if (!lookup.TryGetValue(key, out var bucket))
                {
                    bucket = new GroupBucket { Key = key, KeyObject = keyObject };
                    lookup.Add(key, bucket);
                    order.Add(bucket);
                }
                bucket.Items.Add(item);
            }
            order.Sort((a, b) => (groupComparer ?? DefaultGroupKeyComparer).Compare(a.KeyObject, b.KeyObject));

            _updating = true;
            try
            {
                DetachAllRows();
                _groupRows.Clear();
                _groupInfo.Clear();

                foreach (var bucket in order)
                {
                    bool collapsed = _collapsedKeys.Contains(bucket.Key);
                    CreateGroupHeaderRow(bucket.Key, bucket.KeyObject, bucket.Items.Count, collapsed);
                    foreach (var item in bucket.Items)
                    {
                        var row = _base.Rows[_base.Rows.Add()];
                        row.Tag = item;
                        fillRow?.Invoke(row, (T)item);
                        row.Visible = !collapsed;
                    }
                }
            }
            finally
            {
                _updating = false;
            }

            EnsureCurrentCellVisible();
            UpdateScrollBarLayout();
            _base.Invalidate();
            GroupsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Removes all group header rows and restores the flat row order.</summary>
        public void ClearGroups()
        {
            if (_groupRows.Count == 0)
            {
                RestoreForcedSortMode();
                return;
            }

            _updating = true;
            try
            {
                foreach (var hdr in _groupRows.ToList())
                {
                    if (hdr.Index < 0)
                        continue;
                    for (int r = hdr.Index + 1; r < _base.Rows.Count && !_groupRows.Contains(_base.Rows[r]); r++)
                        _base.Rows[r].Visible = true;
                    _base.Rows.Remove(hdr);
                    hdr.Dispose();
                }
                _groupRows.Clear();
                _groupInfo.Clear();
                RestoreForcedSortMode();
            }
            finally
            {
                _updating = false;
            }

            UpdateScrollBarLayout();
            _base.Invalidate();
            GroupsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ExpandAll() => SetAllGroupsCollapsed(false);

        public void CollapseAll() => SetAllGroupsCollapsed(true);

        public bool IsGroupRow(DataGridViewRow row) => row != null && _groupRows.Contains(row);

        public bool IsGroupRow(int rowIndex) =>
            rowIndex >= 0 && rowIndex < _base.Rows.Count && _groupRows.Contains(_base.Rows[rowIndex]);

        public bool IsGroupCollapsed(int groupRowIndex)
        {
            if (!TryGetGroupInfo(groupRowIndex, out _, out var info))
                return false;
            return _collapsedKeys.Contains(info.Key);
        }

        public void ExpandGroup(int groupRowIndex)
        {
            if (TryGetGroupInfo(groupRowIndex, out var row, out var info))
                SetGroupCollapsed(row, info, false);
        }

        public void CollapseGroup(int groupRowIndex)
        {
            if (TryGetGroupInfo(groupRowIndex, out var row, out var info))
                SetGroupCollapsed(row, info, true);
        }

        public void ToggleGroup(int groupRowIndex)
        {
            if (TryGetGroupInfo(groupRowIndex, out var row, out var info))
                SetGroupCollapsed(row, info, !_collapsedKeys.Contains(info.Key));
        }

        /// <summary>Returns the index of the group header that owns the given row, or -1.</summary>
        public int GetGroupHeaderIndexForRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _base.Rows.Count)
                return -1;

            int owner = -1;
            foreach (var hdr in _groupRows.OrderBy(r => r.Index))
            {
                if (hdr.Index < 0)
                    continue;
                if (hdr.Index <= rowIndex)
                    owner = hdr.Index;
                else
                    break;
            }
            return owner;
        }

        /// <summary>Returns the item rows directly belonging to the given group header.</summary>
        public IList<DataGridViewRow> GetGroupItemRows(int groupRowIndex)
        {
            var items = new List<DataGridViewRow>();
            if (!TryGetGroupInfo(groupRowIndex, out var hdr, out _))
                return items;

            for (int r = hdr.Index + 1; r < _base.Rows.Count && !_groupRows.Contains(_base.Rows[r]); r++)
                items.Add(_base.Rows[r]);
            return items;
        }

        /// <summary>Returns the typed tags of the item rows in the given group.</summary>
        public IList<T> GetGroupItems<T>(int groupRowIndex) where T : class
        {
            var items = new List<T>();
            foreach (var row in GetGroupItemRows(groupRowIndex))
                if (row.Tag is T item)
                    items.Add(item);
            return items;
        }

        /// <summary>Returns the group label for the given group header row.</summary>
        public string GetGroupLabel(int groupRowIndex)
        {
            return TryGetGroupInfo(groupRowIndex, out _, out var info) ? info.Label : null;
        }

        /// <summary>
        /// Sorts the item rows inside each group by a column, keeping the group
        /// order and collapse state intact.
        /// </summary>
        public void SortGroups(int columnIndex, ListSortDirection direction)
        {
            if (_groupRows.Count == 0)
                return;
            _sortColumnIndex = columnIndex;
            _sortAscending = direction == ListSortDirection.Ascending;
            SortGroupsInternal(columnIndex, direction, true);
        }

        // ── Internals ───────────────────────────────────────────

        private void ThrowIfDataBound()
        {
            if (_base.DataSource != null)
                throw new InvalidOperationException(
                    "Grouping is only supported for unbound rows. Clear DataSource before grouping.");
        }

        // The DataGridView's built-in header-click sort rearranges ALL rows and
        // would scatter the group headers. Switch automatic columns to
        // programmatic so only the group-aware sorter runs, and remember them so
        // ClearGroups can restore the original behaviour.
        private void EnsureGroupSortMode()
        {
            foreach (DataGridViewColumn column in _base.Columns)
            {
                if (column.SortMode == DataGridViewColumnSortMode.Automatic)
                {
                    column.SortMode = DataGridViewColumnSortMode.Programmatic;
                    _columnsForcedProgrammatic.Add(column);
                }
            }
        }

        private void RestoreForcedSortMode()
        {
            foreach (var column in _columnsForcedProgrammatic)
            {
                if (column.DataGridView == _base && column.SortMode == DataGridViewColumnSortMode.Programmatic)
                    column.SortMode = DataGridViewColumnSortMode.Automatic;
            }
            _columnsForcedProgrammatic.Clear();
        }

        private void GroupByCore(Func<DataGridViewRow, object> keySelector, IComparer<object> groupComparer)
        {
            ThrowIfDataBound();
            if (_base.Columns.Count == 0)
                return;
            EnsureGroupSortMode();

            var itemRows = _base.Rows.Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow && !_groupRows.Contains(r))
                .ToList();

            var order = new List<GroupBucket>();
            var lookup = new Dictionary<string, GroupBucket>(StringComparer.Ordinal);
            foreach (var row in itemRows)
            {
                object keyObject = keySelector(row);
                string key = KeyToString(keyObject);
                if (!lookup.TryGetValue(key, out var bucket))
                {
                    bucket = new GroupBucket { Key = key, KeyObject = keyObject };
                    lookup.Add(key, bucket);
                    order.Add(bucket);
                }
                bucket.Rows.Add(row);
            }

            order.Sort((a, b) => (groupComparer ?? DefaultGroupKeyComparer).Compare(a.KeyObject, b.KeyObject));
            RebuildGroups(order);
        }

        private void SortGroupsInternal(int columnIndex, ListSortDirection direction, bool updateGlyph)
        {
            var comparer = GroupCellValueComparer ?? DefaultGroupCellComparer;
            var buckets = new List<GroupBucket>();

            foreach (var hdr in _groupRows.OrderBy(r => r.Index))
            {
                if (hdr.Index < 0)
                    continue;

                _groupInfo.TryGetValue(hdr, out var info);
                var bucket = new GroupBucket { Key = info?.Key, Label = info?.Label };
                for (int r = hdr.Index + 1; r < _base.Rows.Count && !_groupRows.Contains(_base.Rows[r]); r++)
                    bucket.Rows.Add(_base.Rows[r]);

                bucket.Rows.Sort((x, y) =>
                {
                    object vx = x.Cells.Count > columnIndex ? x.Cells[columnIndex].Value : null;
                    object vy = y.Cells.Count > columnIndex ? y.Cells[columnIndex].Value : null;
                    int cmp = comparer(vx, vy);
                    return direction == ListSortDirection.Ascending ? cmp : -cmp;
                });

                buckets.Add(bucket);
            }

            RebuildGroups(buckets, updateGlyph ? columnIndex : -1, direction);
        }

        private void RebuildGroups(List<GroupBucket> buckets, int glyphColumn = -1,
            ListSortDirection glyphDirection = ListSortDirection.Ascending)
        {
            if (buckets.Count == 0)
            {
                ClearGroups();
                return;
            }

            _updating = true;
            try
            {
                DetachAllRows();
                _groupRows.Clear();
                _groupInfo.Clear();

                foreach (var bucket in buckets)
                {
                    bool collapsed = _collapsedKeys.Contains(bucket.Key);
                    CreateGroupHeaderRow(bucket.Key, bucket.KeyObject ?? bucket.Key, bucket.Rows.Count, collapsed);
                    foreach (var row in bucket.Rows)
                    {
                        _base.Rows.Add(row);
                        row.Visible = !collapsed;
                    }
                }
            }
            finally
            {
                _updating = false;
            }

            if (glyphColumn >= 0)
                UpdateSortGlyph(glyphColumn, glyphDirection);

            EnsureCurrentCellVisible();
            UpdateScrollBarLayout();
            _base.Invalidate();
            GroupsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void DetachAllRows()
        {
            var oldHeaders = _groupRows.ToList();
            var all = _base.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
            foreach (var row in all)
                _base.Rows.Remove(row);

            // The old header rows are not reused; release their cells.
            foreach (var header in oldHeaders)
                header.Dispose();
        }

        private DataGridViewRow CreateGroupHeaderRow(string key, object keyObject, int count, bool collapsed)
        {
            string label = FormatGroupLabel(keyObject, key, count);
            var row = _base.Rows[_base.Rows.Add()];
            _groupRows.Add(row);
            _groupInfo[row] = new GroupInfo { Key = key, Label = label, Count = count };
            row.ReadOnly = true;
            row.Height = (int)Math.Round(_groupHeaderHeight);
            row.Tag = label;
            ApplyGroupHeaderRowStyle(row);
            return row;
        }

        private string FormatGroupLabel(object keyObject, string key, int count)
        {
            if (GroupLabelFormatter != null)
                return GroupLabelFormatter(keyObject, count);
            return key + "  (" + count + ")";
        }

        private void SetGroupCollapsed(DataGridViewRow hdr, GroupInfo info, bool collapsed)
        {
            if (hdr.Index < 0)
                return;

            if (collapsed)
                _collapsedKeys.Add(info.Key);
            else
                _collapsedKeys.Remove(info.Key);

            _updating = true;
            try
            {
                for (int r = hdr.Index + 1; r < _base.Rows.Count && !_groupRows.Contains(_base.Rows[r]); r++)
                    _base.Rows[r].Visible = !collapsed;
            }
            finally
            {
                _updating = false;
            }

            EnsureCurrentCellVisible();
            UpdateScrollBarLayout();
            _base.InvalidateRow(hdr.Index);
        }

        private void SetAllGroupsCollapsed(bool collapsed)
        {
            if (_groupRows.Count == 0)
                return;

            _updating = true;
            try
            {
                foreach (var hdr in _groupRows.ToList())
                {
                    if (hdr.Index < 0 || !_groupInfo.TryGetValue(hdr, out var info))
                        continue;

                    if (collapsed)
                        _collapsedKeys.Add(info.Key);
                    else
                        _collapsedKeys.Remove(info.Key);

                    for (int r = hdr.Index + 1; r < _base.Rows.Count && !_groupRows.Contains(_base.Rows[r]); r++)
                        _base.Rows[r].Visible = !collapsed;
                }
            }
            finally
            {
                _updating = false;
            }

            EnsureCurrentCellVisible();
            UpdateScrollBarLayout();
            _base.Invalidate();
        }

        private bool TryGetGroupInfo(int groupRowIndex, out DataGridViewRow row, out GroupInfo info)
        {
            row = null;
            info = null;
            if (groupRowIndex < 0 || groupRowIndex >= _base.Rows.Count)
                return false;
            row = _base.Rows[groupRowIndex];
            return _groupInfo.TryGetValue(row, out info);
        }

        private void UpdateSortGlyph(int columnIndex, ListSortDirection direction)
        {
            foreach (DataGridViewColumn col in _base.Columns)
            {
                if (col.SortMode == DataGridViewColumnSortMode.Programmatic)
                    col.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            if (columnIndex < 0 || columnIndex >= _base.Columns.Count)
                return;

            var target = _base.Columns[columnIndex];
            if (target.SortMode == DataGridViewColumnSortMode.Programmatic)
                target.HeaderCell.SortGlyphDirection =
                    direction == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending;
        }

        private static string KeyToString(object key)
        {
            if (key == null)
                return string.Empty;
            if (key is string s)
                return s;
            return Convert.ToString(key, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static readonly Comparison<object> DefaultGroupCellComparer = (a, b) =>
        {
            if (ReferenceEquals(a, b))
                return 0;
            if (a == null)
                return -1;
            if (b == null)
                return 1;
            if (a.GetType() == b.GetType() && a is IComparable comparableA)
            {
                try { return comparableA.CompareTo(b); }
                catch { /* fall through to string compare */ }
            }
            return string.Compare(a.ToString(), b.ToString(), StringComparison.CurrentCultureIgnoreCase);
        };

        private static readonly IComparer<object> DefaultGroupKeyComparer =
            Comparer<object>.Create(DefaultGroupCellComparer);

        // ── Mouse / selection / painting ────────────────────────

        private void BaseGroupCellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _base.Rows.Count)
                return;

            var row = _base.Rows[e.RowIndex];
            if (!_groupRows.Contains(row))
                return;

            _groupInfo.TryGetValue(row, out var info);

            bool toggled = false;
            if (e.Button == MouseButtons.Left)
            {
                ToggleGroup(e.RowIndex);
                toggled = true;
            }

            var args = new DarkDataGridViewGroupHeaderEventArgs(e.RowIndex, info?.Key, info?.Label,
                row, e.Button, toggled);
            GroupHeaderClicked?.Invoke(this, args);
        }

        private void BaseGroupSelectionChanged(object sender, EventArgs e)
        {
            if (_updating || _fixingSelection || _suppressSelection || _groupRows.Count == 0)
                return;

            _fixingSelection = true;
            try
            {
                var toDeselect = _base.SelectedRows.Cast<DataGridViewRow>()
                    .Where(r => _groupRows.Contains(r))
                    .ToList();
                foreach (var row in toDeselect)
                    row.Selected = false;
            }
            finally
            {
                _fixingSelection = false;
            }
        }

        private void BaseGroupColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || e.ColumnIndex < 0)
                return;
            if (!_autoSortGroups || _groupRows.Count == 0)
                return;

            var direction = _sortColumnIndex == e.ColumnIndex && _sortAscending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
            _sortColumnIndex = e.ColumnIndex;
            _sortAscending = direction == ListSortDirection.Ascending;
            SortGroupsInternal(e.ColumnIndex, direction, true);
        }

        private void BaseGroupCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.RowIndex >= _base.Rows.Count)
                return;

            var row = _base.Rows[e.RowIndex];
            if (!_groupRows.Contains(row))
                return;

            var bounds = e.CellBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                e.Handled = true;
                return;
            }

            _groupInfo.TryGetValue(row, out var info);

            Color top = _groupGradientTop;
            Color bottom = _groupGradientBottom;
            Color topLine = _groupTopLine;
            Color bottomLine = _groupBottomLine;
            Color textColor = Colors.LightText;

            if (!Enabled)
            {
                top = bottom = Colors.GreyBackground;
                textColor = Colors.DisabledText;
            }

            using (var brush = new LinearGradientBrush(bounds, top, bottom, LinearGradientMode.Vertical))
                e.Graphics.FillRectangle(brush, bounds);

            using (var pen = new Pen(topLine))
                e.Graphics.DrawLine(pen, bounds.Left, bounds.Top, bounds.Right - 1, bounds.Top);
            using (var pen = new Pen(bottomLine))
                e.Graphics.DrawLine(pen, bounds.Left, bounds.Bottom - 1, bounds.Right - 1, bounds.Bottom - 1);

            if (e.ColumnIndex == ResolveGroupHeaderColumnIndex())
            {
                bool collapsed = info != null && _collapsedKeys.Contains(info.Key);
                string label = info?.Label ?? row.Tag as string ?? string.Empty;
                string display = (collapsed ? "\u25B6  " : "\u25BC  ") + label;

                var textBounds = new Rectangle(bounds.X + GroupHeaderTextPadding, bounds.Y,
                    Math.Max(0, bounds.Width - GroupHeaderTextPadding * 2), bounds.Height);
                TextRenderer.DrawText(e.Graphics, display, _groupHeaderFont, textBounds, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            e.Handled = true;
        }

        // ── Keyboard navigation ─────────────────────────────────

        private bool HandleGroupNavigation(KeyEventArgs e)
        {
            if (_groupRows.Count == 0 || _base.RowCount == 0 || _base.Columns.Count == 0)
                return false;

            int row = _base.CurrentRow?.Index ?? 0;
            int col = Math.Max(0, Math.Min(_base.CurrentCell?.ColumnIndex ?? 0, _base.Columns.Count - 1));
            int target;

            switch (e.KeyCode)
            {
                case Keys.Down:
                    target = NextVisibleRowExpanding(row, 1);
                    break;
                case Keys.Up:
                    target = NextVisibleRowExpanding(row, -1);
                    break;
                case Keys.Home:
                    target = NextNonGroupRow(-1, 1);
                    break;
                case Keys.End:
                    target = NextNonGroupRow(_base.RowCount, -1);
                    break;
                case Keys.PageDown:
                    target = NextNonGroupRow(row, Math.Max(1, _base.DisplayedRowCount(false) - 1));
                    break;
                case Keys.PageUp:
                    target = NextNonGroupRow(row, -Math.Max(1, _base.DisplayedRowCount(false) - 1));
                    break;
                default:
                    return false;
            }

            if (target < 0 || target >= _base.RowCount)
                return false;
            if (target == row)
            {
                e.Handled = true;
                return true;
            }
            if (col < _base.Rows[target].Cells.Count)
            {
                _base.CurrentCell = _base.Rows[target].Cells[col];
                _base.Rows[target].Selected = true;
                e.Handled = true;
                return true;
            }
            return false;
        }

        private int NextNonGroupRow(int fromRow, int delta)
        {
            int dir = delta > 0 ? 1 : -1;
            int r = fromRow + delta;
            while (r >= 0 && r < _base.RowCount)
            {
                if (!_groupRows.Contains(_base.Rows[r]) && _base.Rows[r].Visible)
                    return r;
                r += dir;
            }
            return fromRow;
        }

        private int NextVisibleRowExpanding(int fromRow, int delta)
        {
            int dir = delta > 0 ? 1 : -1;
            int r = fromRow + delta;
            while (r >= 0 && r < _base.RowCount)
            {
                if (_groupRows.Contains(_base.Rows[r]))
                {
                    if (dir > 0 && IsGroupCollapsed(r))
                    {
                        ExpandGroup(r);
                        return r + 1;
                    }
                    r += dir;
                    continue;
                }

                if (_base.Rows[r].Visible)
                    return r;

                if (dir < 0)
                {
                    int hdr = GetGroupHeaderIndexForRow(r);
                    if (hdr >= 0)
                    {
                        ExpandGroup(hdr);
                        return LastItemOfGroup(hdr);
                    }
                }
                r += dir;
            }
            return fromRow;
        }

        private int LastItemOfGroup(int hdrRow)
        {
            int next = _base.RowCount;
            foreach (var h in _groupRows)
                if (h.Index > hdrRow && h.Index < next)
                    next = h.Index;
            return Math.Max(next - 1, hdrRow + 1);
        }

        private void EnsureCurrentCellVisible()
        {
            var current = _base.CurrentCell;
            if (current == null || current.RowIndex < 0 || current.RowIndex >= _base.Rows.Count)
                return;
            if (_base.Rows[current.RowIndex].Visible)
                return;
            if (_base.Columns.Count == 0)
                return;

            int col = Math.Max(0, Math.Min(current.ColumnIndex, _base.Columns.Count - 1));
            int target = NextNonGroupRow(current.RowIndex, -1);
            if (target == current.RowIndex || !_base.Rows[target].Visible)
                target = NextNonGroupRow(current.RowIndex, 1);

            if (target < 0 || target >= _base.RowCount || !_base.Rows[target].Visible
                || _groupRows.Contains(_base.Rows[target]))
                return;

            _suppressSelection = true;
            try
            {
                _base.CurrentCell = _base.Rows[target].Cells[Math.Min(col, _base.Rows[target].Cells.Count - 1)];
            }
            finally
            {
                _suppressSelection = false;
            }
        }
    }
}
