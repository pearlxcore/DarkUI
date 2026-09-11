using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A dark-themed RichTextBox with DarkUI-themed scrollbars.
    ///
    /// A native <see cref="RichTextBox"/> is hosted inside this control and
    /// filled to its bounds. The native RichEdit scrollbars keep the control's
    /// scroll state intact, and are visually covered by two sibling
    /// <see cref="DarkScrollBar"/>s sized to the native scrollbar metrics. The
    /// themed bars mirror the native range (<c>GetScrollInfo</c>) and drive it
    /// (<c>EM_SETSCROLLPOS</c>); text is inset with <c>EM_SETRECT</c>.
    ///
    /// Text selection INTENTIONALLY uses the native Windows/RichEdit selection
    /// colors (see docs/TextSelectionTheming.md).
    /// </summary>
    public class DarkRichTextBox : UserControl
    {
        #region Native interop

        [DllImport("user32.dll")] private static extern bool GetScrollInfo(IntPtr hWnd, int nBar, ref SCROLLINFO lpsi);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct SCROLLINFO { public int cbSize, fMask, nMin, nMax, nPage, nPos, nTrackPos; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        private const int SB_HORZ = 0;
        private const int SB_VERT = 1;
        private const uint SIF_ALL = 0x17;

        private const int EM_GETSCROLLPOS = 0x04DD;
        private const int EM_SETSCROLLPOS = 0x04DE;
        private const int EM_SETRECT = 0x00B3;

        #endregion

        private readonly RichTextBox _rtb = new RichTextBox();
        private readonly DarkScrollBar _vScrollBar = new DarkScrollBar { ScrollOrientation = DarkScrollOrientation.Vertical };
        private readonly DarkScrollBar _hScrollBar = new DarkScrollBar { ScrollOrientation = DarkScrollOrientation.Horizontal };

        private bool _updating;
        private bool _syncing;
        private bool _syncQueued;
        private Padding _textPadding = new Padding(3);

        public DarkRichTextBox()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            BackColor = Colors.GreyBackground;

            _rtb.Name = "rtbBase";
            _rtb.BorderStyle = BorderStyle.None;
            _rtb.ScrollBars = RichTextBoxScrollBars.Both;
            _rtb.WordWrap = true;
            _rtb.HideSelection = false;
            _rtb.DetectUrls = false;
            _rtb.BackColor = Colors.GreyBackground;
            _rtb.ForeColor = Colors.LightText;
            _rtb.ContextMenuStrip = DarkEditMenu.Create(_rtb);
            _rtb.TextChanged += (s, e) => { UpdateScrollBars(); OnTextChanged(e); };
            _rtb.VScroll += (s, e) => QueueScrollSync();
            _rtb.HScroll += (s, e) => QueueScrollSync();

            _vScrollBar.BackColor = Colors.MediumBackground;
            _vScrollBar.ValueChanged += VScrollBar_ValueChanged;
            _hScrollBar.BackColor = Colors.MediumBackground;
            _hScrollBar.ValueChanged += HScrollBar_ValueChanged;

            Controls.Add(_vScrollBar);
            Controls.Add(_hScrollBar);
            Controls.Add(_rtb);

            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        // ── Public surface ──────────────────────────────────────────

        /// <summary>The hosted native RichTextBox (for members not forwarded).</summary>
        [Browsable(false)]
        public RichTextBox InnerRichTextBox => _rtb;

        [Browsable(false)]
        public DarkScrollBar VerticalScrollBar => _vScrollBar;

        [Browsable(false)]
        public DarkScrollBar HorizontalScrollBar => _hScrollBar;

        [DefaultValue(3)]
        public Padding TextPadding
        {
            get => _textPadding;
            set
            {
                _textPadding = value;
                ApplyTextPadding();
            }
        }

        // ── Lifecycle ───────────────────────────────────────────────

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode)
                UpdateScrollBars();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _rtb.Enabled = Enabled;
            _vScrollBar.Enabled = Enabled;
            _hScrollBar.Enabled = Enabled;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBars();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            _rtb.Font = Font;
            UpdateScrollBars();
        }

        private void OnThemeChanged(object sender, EventArgs e) => ApplyThemeColors();

        private void ApplyThemeColors()
        {
            BackColor = Colors.GreyBackground;
            _rtb.BackColor = Colors.GreyBackground;
            _rtb.ForeColor = Enabled ? Colors.LightText : Colors.DisabledText;
            _vScrollBar.BackColor = Colors.MediumBackground;
            _hScrollBar.BackColor = Colors.MediumBackground;
            Invalidate(true);
        }

        // ── Scrolling ───────────────────────────────────────────────

        private void VScrollBar_ValueChanged(object sender, ScrollValueEventArgs e)
        {
            if (_syncing || !_rtb.IsHandleCreated)
                return;
            var pt = GetScrollPoint();
            SetScrollPoint(pt.X, e.Value);
            SyncScrollBars();
        }

        private void HScrollBar_ValueChanged(object sender, ScrollValueEventArgs e)
        {
            if (_syncing || !_rtb.IsHandleCreated)
                return;
            var pt = GetScrollPoint();
            SetScrollPoint(e.Value, pt.Y);
            SyncScrollBars();
        }

        private void QueueScrollSync()
        {
            // The inner RichTextBox can raise VScroll/HScroll before this
            // wrapper's handle exists (e.g. during construction). BeginInvoke
            // requires our handle, so skip until it is created — layout will
            // sync the bars once shown.
            if (_syncQueued || !IsHandleCreated || !_rtb.IsHandleCreated)
                return;
            _syncQueued = true;
            BeginInvoke((System.Windows.Forms.MethodInvoker)(() =>
            {
                _syncQueued = false;
                if (!IsDisposed && _rtb.IsHandleCreated)
                    SyncScrollBars();
            }));
        }

        private void UpdateScrollBars()
        {
            if (_updating || !_rtb.IsHandleCreated)
                return;

            _updating = true;
            try
            {
                _rtb.Bounds = ClientRectangle;

                var vInfo = GetInfo(SB_VERT);
                var hInfo = GetInfo(SB_HORZ);
                bool vVisible = vInfo.nMax > vInfo.nPage;
                bool hVisible = !_rtb.WordWrap && hInfo.nMax > hInfo.nPage;

                int vw = SystemInformation.VerticalScrollBarWidth;
                int hh = SystemInformation.HorizontalScrollBarHeight;

                if (vVisible)
                    _vScrollBar.Bounds = new Rectangle(ClientSize.Width - vw, 0, vw, ClientSize.Height);
                _vScrollBar.Visible = vVisible;

                if (hVisible)
                    _hScrollBar.Bounds = new Rectangle(0, ClientSize.Height - hh,
                        Math.Max(0, ClientSize.Width - (vVisible ? vw : 0)), hh);
                _hScrollBar.Visible = hVisible;

                // The hosted RichEdit must sit behind the themed bars so they
                // cover its native scrollbars.
                _rtb.SendToBack();
                _vScrollBar.BringToFront();
                _hScrollBar.BringToFront();

                ApplyTextPadding();
                SyncScrollBars();
            }
            finally
            {
                _updating = false;
            }
        }

        private void ApplyTextPadding()
        {
            if (!_rtb.IsHandleCreated)
                return;
            const int vw = 0; // bars sit over the native gutter, outside the text client
            var client = _rtb.ClientSize;
            var rect = new RECT
            {
                Left = _textPadding.Left,
                Top = _textPadding.Top,
                Right = Math.Max(_textPadding.Left, client.Width - vw - _textPadding.Right),
                Bottom = Math.Max(_textPadding.Top, client.Height - _textPadding.Bottom)
            };
            SendMessage(_rtb.Handle, EM_SETRECT, IntPtr.Zero, ref rect);
        }

        private void SyncScrollBars()
        {
            if (!_rtb.IsHandleCreated)
                return;

            _syncing = true;
            try
            {
                var v = GetInfo(SB_VERT);
                SetBarRange(_vScrollBar, Math.Max(v.nMax, v.nPage + 1), v.nPage);
                _vScrollBar.Value = v.nPos;

                var h = GetInfo(SB_HORZ);
                SetBarRange(_hScrollBar, Math.Max(h.nMax, h.nPage + 1), h.nPage);
                _hScrollBar.Value = h.nPos;
            }
            finally
            {
                _syncing = false;
            }
        }

        private static void SetBarRange(DarkScrollBar bar, int maximum, int viewSize)
        {
            if (bar.Minimum != 0)
                bar.Minimum = 0;
            if (bar.Maximum != maximum)
                bar.Maximum = maximum;
            if (bar.ViewSize != viewSize)
                bar.ViewSize = viewSize;
        }

        private SCROLLINFO GetInfo(int bar)
        {
            var si = new SCROLLINFO { cbSize = Marshal.SizeOf<SCROLLINFO>(), fMask = (int)SIF_ALL };
            GetScrollInfo(_rtb.Handle, bar, ref si);
            return si;
        }

        private POINT GetScrollPoint()
        {
            var pt = new POINT();
            var handle = GCHandle.Alloc(pt, GCHandleType.Pinned);
            try
            {
                SendMessage(_rtb.Handle, EM_GETSCROLLPOS, IntPtr.Zero, handle.AddrOfPinnedObject());
                return Marshal.PtrToStructure<POINT>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        private void SetScrollPoint(int x, int y)
        {
            var pt = new POINT { X = x, Y = y };
            var handle = GCHandle.Alloc(pt, GCHandleType.Pinned);
            try
            {
                SendMessage(_rtb.Handle, EM_SETSCROLLPOS, IntPtr.Zero, handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        // ── Forwarded RichTextBox members ───────────────────────────

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color BackColor
        {
            get => Colors.GreyBackground;
            set => base.BackColor = Colors.GreyBackground;
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color ForeColor
        {
            get => Enabled ? Colors.LightText : Colors.DisabledText;
            set => base.ForeColor = Enabled ? Colors.LightText : Colors.DisabledText;
        }

        [Browsable(true)]
        [DefaultValue(true)]
        public bool WordWrap
        {
            get => _rtb.WordWrap;
            set
            {
                if (_rtb.WordWrap == value)
                    return;
                _rtb.WordWrap = value;
                UpdateScrollBars();
            }
        }

        [Browsable(true)]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get => _rtb.ReadOnly;
            set => _rtb.ReadOnly = value;
        }

        [Browsable(true)]
        [DefaultValue(false)]
        public bool DetectUrls
        {
            get => _rtb.DetectUrls;
            set => _rtb.DetectUrls = value;
        }

        [Browsable(true)]
        public override string Text
        {
            get => _rtb.Text;
            set => _rtb.Text = value;
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Rtf
        {
            get => _rtb.Rtf;
            set => _rtb.Rtf = value;
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedText
        {
            get => _rtb.SelectedText;
            set => _rtb.SelectedText = value;
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionStart
        {
            get => _rtb.SelectionStart;
            set => _rtb.SelectionStart = value;
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionLength
        {
            get => _rtb.SelectionLength;
            set => _rtb.SelectionLength = value;
        }

        [Browsable(false)]
        public Color SelectionColor
        {
            get => _rtb.SelectionColor;
            set => _rtb.SelectionColor = value;
        }

        [Browsable(false)]
        public Font SelectionFont
        {
            get => _rtb.SelectionFont;
            set => _rtb.SelectionFont = value;
        }

        [Browsable(false)]
        public bool Modified => _rtb.Modified;

        [Browsable(false)]
        public bool CanUndo => _rtb.CanUndo;

        [Browsable(false)]
        public bool CanRedo => _rtb.CanRedo;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new ContextMenuStrip ContextMenuStrip
        {
            get => _rtb.ContextMenuStrip;
            set => _rtb.ContextMenuStrip = value;
        }

        public void AppendText(string text) => _rtb.AppendText(text);
        public void Clear() => _rtb.Clear();
        public void ClearUndo() => _rtb.ClearUndo();
        public void Copy() => _rtb.Copy();
        public void Cut() => _rtb.Cut();
        public void Paste() => _rtb.Paste();
        public bool CanPaste(DataFormats.Format format) => _rtb.CanPaste(format);
        public void Undo() => _rtb.Undo();
        public void Redo() => _rtb.Redo();
        public void Select(int start, int length) => _rtb.Select(start, length);
        public void SelectAll() => _rtb.SelectAll();
        public void ScrollToCaret() => _rtb.ScrollToCaret();
        public int Find(string str) => _rtb.Find(str);
        public int Find(string str, RichTextBoxFinds options) => _rtb.Find(str, options);
        public void LoadFile(string path) => _rtb.LoadFile(path);
        public void LoadFile(string path, RichTextBoxStreamType fileType) => _rtb.LoadFile(path, fileType);
        public void SaveFile(string path) => _rtb.SaveFile(path);
        public void SaveFile(string path, RichTextBoxStreamType fileType) => _rtb.SaveFile(path, fileType);
        public int GetCharIndexFromPosition(Point pt) => _rtb.GetCharIndexFromPosition(pt);
        public int GetLineFromCharIndex(int index) => _rtb.GetLineFromCharIndex(index);
        public Point GetPositionFromCharIndex(int index) => _rtb.GetPositionFromCharIndex(index);
        public int GetFirstCharIndexFromLine(int lineNumber) => _rtb.GetFirstCharIndexFromLine(lineNumber);
        public int GetFirstCharIndexOfCurrentLine() => _rtb.GetFirstCharIndexOfCurrentLine();

        public new event EventHandler TextChanged { add => _rtb.TextChanged += value; remove => _rtb.TextChanged -= value; }
        public event EventHandler SelectionChanged { add => _rtb.SelectionChanged += value; remove => _rtb.SelectionChanged -= value; }
        public event LinkClickedEventHandler LinkClicked { add => _rtb.LinkClicked += value; remove => _rtb.LinkClicked -= value; }
        public event EventHandler ModifiedChanged { add => _rtb.ModifiedChanged += value; remove => _rtb.ModifiedChanged -= value; }
    }
}
