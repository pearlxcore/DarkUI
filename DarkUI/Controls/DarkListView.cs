using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    public class DarkListView : UserControl
    {
        private const int B = 1;
        private readonly InnerList _list = new InnerList();
        private readonly DarkScrollBar _vScrollBar = new DarkScrollBar { ScrollOrientation = DarkScrollOrientation.Vertical };
        private readonly DarkScrollBar _hScrollBar = new DarkScrollBar { ScrollOrientation = DarkScrollOrientation.Horizontal };
        private readonly Timer _deferTimer = new Timer { Interval = 500 };
        private int _scrollSize => Consts.ScrollBarSize;
        private bool _updateLayout;
        private bool _layoutPending;

        private class InnerList : ListView
        {
            [DllImport("user32.dll")] static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool show);
            const int SB_BOTH = 3;
            const int WM_VSCROLL = 0x115;
            const int WM_HSCROLL = 0x114;
            const int LVM_INSERTITEMW = 0x104D;
            const int LVM_DELETEITEM = 0x1008;
            const int LVM_DELETEALLITEMS = 0x1009;

            public event Action ScrollStateChanged;
            public event Action ItemsChanged;

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if (!IsHandleCreated) return;
                ShowScrollBar(Handle, SB_BOTH, false);
                if (m.Msg == WM_VSCROLL || m.Msg == WM_HSCROLL)
                    ScrollStateChanged?.Invoke();
                else if (m.Msg == LVM_INSERTITEMW || m.Msg == LVM_DELETEITEM || m.Msg == LVM_DELETEALLITEMS)
                    ItemsChanged?.Invoke();
            }
        }

        [DllImport("user32.dll")] static extern bool GetScrollInfo(IntPtr hWnd, int fnBar, ref SCROLLINFO lpsi);
        [DllImport("user32.dll")] static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        const int SB_VERT = 1;
        const int SB_HORZ = 0;
        const int SIF_ALL = 0x1 | 0x2 | 0x4;
        const int LVM_SCROLL = 0x1014;
        [StructLayout(LayoutKind.Sequential)] struct SCROLLINFO { public int cbSize, fMask, nMin, nMax, nPage, nPos, nTrackPos; }

        public ListView.ListViewItemCollection Items => _list.Items;
        public ListView.SelectedListViewItemCollection SelectedItems => _list.SelectedItems;
        public ListView.ColumnHeaderCollection Columns => _list.Columns;
        public ImageList SmallImageList { get => _list.SmallImageList; set => _list.SmallImageList = value; }
        public ImageList LargeImageList { get => _list.LargeImageList; set => _list.LargeImageList = value; }
        public View View { get => _list.View; set => _list.View = value; }
        public bool AllowDrop { get => _list.AllowDrop; set => _list.AllowDrop = value; }
        public bool FullRowSelect { get => _list.FullRowSelect; set => _list.FullRowSelect = value; }
        public bool MultiSelect { get => _list.MultiSelect; set => _list.MultiSelect = value; }
        public new ContextMenuStrip ContextMenuStrip { get => _list.ContextMenuStrip; set => _list.ContextMenuStrip = value; }
        public System.Collections.IComparer ListViewItemSorter { get => _list.ListViewItemSorter; set => _list.ListViewItemSorter = value; }
        public bool UseCompatibleStateImageBehavior { get; set; }
        public ListViewItem GetItemAt(int x, int y) => _list.GetItemAt(x, y);

        public event ColumnWidthChangingEventHandler ColumnWidthChanging;
        public event EventHandler ItemActivate;
        public event MouseEventHandler MouseDoubleClick;
        public event ColumnClickEventHandler ColumnClick;
        public event ItemDragEventHandler ItemDrag;
        public event EventHandler SelectedIndexChanged;
        public new event MouseEventHandler MouseClick;
        public new event EventHandler DoubleClick;

        public DarkListView()
        {
            base.BackColor = Colors.LightBorder;
            _list.BorderStyle = BorderStyle.None;
            _list.BackColor = Colors.GreyBackground;
            _list.ForeColor = Colors.LightText;
            _list.FullRowSelect = true;
            _list.HideSelection = false;
            _list.View = View.Details;
            _list.OwnerDraw = true;

            _list.DrawColumnHeader += (s, e) =>
            {
                int fillRight = e.Bounds.Right;
                if (e.ColumnIndex == _list.Columns.Count - 1 && e.Bounds.Right < _list.ClientRectangle.Width)
                    fillRight = _list.ClientRectangle.Width;
                var fillBounds = new Rectangle(e.Bounds.X, e.Bounds.Y, fillRight - e.Bounds.X, e.Bounds.Height);
                using var bg = new SolidBrush(Colors.DarkBackground);
                e.Graphics.FillRectangle(bg, fillBounds);
                using var hi = new Pen(Color.FromArgb(90, 95, 100));
                e.Graphics.DrawLine(hi, e.Bounds.Left, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Top);
                e.Graphics.DrawLine(hi, e.Bounds.Left, e.Bounds.Top, e.Bounds.Left, e.Bounds.Bottom - 1);
                using var sh = new Pen(Colors.DarkBorder);
                e.Graphics.DrawLine(sh, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right - 1, e.Bounds.Bottom - 1);
                e.Graphics.DrawLine(sh, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom - 1);
                var tr = new Rectangle(e.Bounds.X + 3, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, Font, tr, Colors.LightText,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
            };
            _list.DrawItem += (s, e) =>
            {
                if (!Enabled)
                {
                    // Disabled: paint the dark body + dimmed text manually —
                    // DrawDefault would fall back to system colors (white body).
                    using var b = new SolidBrush(Colors.GreyBackground);
                    e.Graphics.FillRectangle(b, e.Bounds);
                    var tr = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
                    TextRenderer.DrawText(e.Graphics, e.Item.Text, Font, tr, Colors.DisabledText,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    return;
                }
                e.DrawDefault = true;
            };
            _list.DrawSubItem += (s, e) =>
            {
                if (!Enabled)
                {
                    using var b = new SolidBrush(Colors.GreyBackground);
                    e.Graphics.FillRectangle(b, e.Bounds);
                    var tr = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
                    TextRenderer.DrawText(e.Graphics, e.SubItem.Text, Font, tr, Colors.DisabledText,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    return;
                }
                e.DrawDefault = true;
            };

            _list.ColumnWidthChanging += (s, e) => ColumnWidthChanging?.Invoke(this, e);
            _list.ColumnWidthChanged += (s, e) => UpdateScrollBarLayout();
            _list.ItemActivate += (s, e) => ItemActivate?.Invoke(this, e);
            _list.MouseClick += (s, e) => MouseClick?.Invoke(this, e);
            _list.MouseDoubleClick += (s, e) => MouseDoubleClick?.Invoke(this, e);
            _list.DoubleClick += (s, e) => DoubleClick?.Invoke(this, e);
            _list.ColumnClick += (s, e) => ColumnClick?.Invoke(this, e);
            _list.ItemDrag += (s, e) => ItemDrag?.Invoke(this, e);
            _list.SelectedIndexChanged += (s, e) => SelectedIndexChanged?.Invoke(this, e);

            _list.ScrollStateChanged += () =>
            {
                if (!_updateLayout && IsHandleCreated)
                    UpdateScrollBarLayout();
            };

            _list.ItemsChanged += () => ScheduleLayout();

            _list.MouseWheel += (s, e) =>
            {
                if (ModifierKeys.HasFlag(Keys.Shift))
                {
                    if (_hScrollBar.Visible)
                        _hScrollBar.Value = Math.Max(0, Math.Min(_hScrollBar.Maximum,
                            _hScrollBar.Value - Math.Sign(e.Delta)));
                }
                else
                {
                    if (_vScrollBar.Visible)
                        _vScrollBar.Value = Math.Max(0, Math.Min(_vScrollBar.Maximum,
                            _vScrollBar.Value - Math.Sign(e.Delta)));
                }
            };

            _vScrollBar.BackColor = Colors.MediumBackground;
            _vScrollBar.Minimum = 0; _vScrollBar.Maximum = 100;
            _vScrollBar.ValueChanged += (s, e) =>
            {
                if (!_list.IsHandleCreated || _updateLayout || _list.Items.Count == 0) return;
                _updateLayout = true;
                int idx = Math.Max(0, Math.Min(_list.Items.Count - 1, e.Value));
                _list.TopItem = _list.Items[idx];

                SCROLLINFO hsi = new SCROLLINFO { cbSize = Marshal.SizeOf<SCROLLINFO>(), fMask = SIF_ALL };
                GetScrollInfo(_list.Handle, SB_HORZ, ref hsi);
                int delta = _hScrollBar.Value - hsi.nPos;
                if (delta != 0)
                    SendMessage(_list.Handle, LVM_SCROLL, (IntPtr)delta, IntPtr.Zero);

                _updateLayout = false;
            };

            _hScrollBar.BackColor = Colors.MediumBackground;
            _hScrollBar.Minimum = 0; _hScrollBar.Maximum = 100;
            _hScrollBar.ValueChanged += (s, e) =>
            {
                if (!_list.IsHandleCreated || _updateLayout) return;
                _updateLayout = true;
                SCROLLINFO cur = new SCROLLINFO { cbSize = Marshal.SizeOf<SCROLLINFO>(), fMask = SIF_ALL };
                GetScrollInfo(_list.Handle, SB_HORZ, ref cur);
                int delta = e.Value - cur.nPos;
                if (delta != 0)
                    SendMessage(_list.Handle, LVM_SCROLL, (IntPtr)delta, IntPtr.Zero);
                _updateLayout = false;
            };

            _deferTimer.Tick += (s, e) =>
            {
                _deferTimer.Stop();
                if (!Disposing && IsHandleCreated) UpdateScrollBarLayout();
            };

            Controls.Add(_list);
            Controls.Add(_vScrollBar);
            Controls.Add(_hScrollBar);

            UpdateScrollBarLayout();

            SizeChanged += (s, e) =>
            {
                if (ClientSize.Width > 50 && ClientSize.Height > 50)
                {
                    UpdateScrollBarLayout();
                    _deferTimer.Stop();
                    _deferTimer.Start();
                }
            };

            EventHandler idle = null;
            idle = (s, e) => { Application.Idle -= idle; if (!Disposing && IsHandleCreated) UpdateScrollBarLayout(); };
            Application.Idle += idle;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            // Re-assert dark colors — a disabled ListView can repaint its body
            // with system colors otherwise.
            _list.BackColor = Colors.GreyBackground;
            _list.ForeColor = Colors.LightText;
            _list.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _deferTimer?.Dispose();
            base.Dispose(disposing);
        }

        public void RefreshLayout()
        {
            if (!Disposing && IsHandleCreated) UpdateScrollBarLayout();
        }

        public new void BeginUpdate() => _list.BeginUpdate();
        public new void EndUpdate()
        {
            _list.EndUpdate();
            ScheduleLayout();
        }

        private void ScheduleLayout()
        {
            if (_layoutPending || !IsHandleCreated) return;
            _layoutPending = true;
            BeginInvoke(() =>
            {
                _layoutPending = false;
                if (!Disposing && IsHandleCreated) UpdateScrollBarLayout();
            });
        }

        private void UpdateScrollBarLayout()
        {
            if (_updateLayout || !IsHandleCreated || !_list.IsHandleCreated) return;
            try
            {
                _updateLayout = true;

                int cw = ClientSize.Width - B * 2;
                int ch = ClientSize.Height - B * 2;
                if (cw < 20 || ch < 20) return;

                for (int pass = 0; pass < 2; pass++)
                {
                    SCROLLINFO vsi = new SCROLLINFO { cbSize = Marshal.SizeOf<SCROLLINFO>(), fMask = SIF_ALL };
                    GetScrollInfo(_list.Handle, SB_VERT, ref vsi);
                    bool vVis = _list.Items.Count > vsi.nPage && vsi.nPage > 0;

                    SCROLLINFO hsi = new SCROLLINFO { cbSize = Marshal.SizeOf<SCROLLINFO>(), fMask = SIF_ALL };
                    GetScrollInfo(_list.Handle, SB_HORZ, ref hsi);
                    bool hVis = hsi.nMax > hsi.nPage && hsi.nPage > 0;

                    int vbw = vVis ? _scrollSize : 0;
                    int hbh = hVis ? _scrollSize : 0;

                    if (vVis)
                    {
                        _vScrollBar.ViewSize = vsi.nPage;
                        _vScrollBar.Maximum = _list.Items.Count;
                        if (_vScrollBar.Value != vsi.nPos) _vScrollBar.Value = vsi.nPos;
                        _vScrollBar.Bounds = new Rectangle(B + cw - _scrollSize, B, _scrollSize, ch - hbh);
                    }
                    _vScrollBar.Visible = vVis;

                    if (hVis)
                    {
                        _hScrollBar.ViewSize = hsi.nPage;
                        _hScrollBar.Maximum = hsi.nMax;
                        if (_hScrollBar.Value != hsi.nPos) _hScrollBar.Value = hsi.nPos;
                        _hScrollBar.Bounds = new Rectangle(B, B + ch - _scrollSize, cw - vbw, _scrollSize);
                    }
                    _hScrollBar.Visible = hVis;

                    _list.Bounds = new Rectangle(B, B, cw - vbw, ch - hbh);
                }

                _list.SendToBack();
                _vScrollBar.BringToFront();
                _hScrollBar.BringToFront();

                if (_vScrollBar.Visible || _hScrollBar.Visible) _list.Invalidate();

                if (_vScrollBar.Visible && _list.Items.Count > 0)
                {
                    int idx = Math.Max(0, Math.Min(_list.Items.Count - 1, _vScrollBar.Value));
                    if (_list.TopItem == null || _list.TopItem.Index != idx)
                        _list.TopItem = _list.Items[idx];
                }
                if (_hScrollBar.Visible)
                {
                    SCROLLINFO curHsi = new SCROLLINFO { cbSize = Marshal.SizeOf<SCROLLINFO>(), fMask = SIF_ALL };
                    GetScrollInfo(_list.Handle, SB_HORZ, ref curHsi);
                    int delta = _hScrollBar.Value - curHsi.nPos;
                    if (delta != 0)
                        SendMessage(_list.Handle, LVM_SCROLL, (IntPtr)delta, IntPtr.Zero);
                }
                else
                {
                    SCROLLINFO curHsi = new SCROLLINFO { cbSize = Marshal.SizeOf<SCROLLINFO>(), fMask = SIF_ALL };
                    GetScrollInfo(_list.Handle, SB_HORZ, ref curHsi);
                    if (curHsi.nPos != 0)
                        SendMessage(_list.Handle, LVM_SCROLL, (IntPtr)(-curHsi.nPos), IntPtr.Zero);
                }
                // Distribute columns evenly across available width
                if (_list.Columns.Count > 0 && _list.ClientSize.Width > 40)
                {
                    int each = _list.ClientSize.Width / _list.Columns.Count;
                    for (int i = 0; i < _list.Columns.Count; i++)
                        _list.Columns[i].Width = each;
                }
            }
            finally { _updateLayout = false; }
        }
    }
}
