using DarkUI.Config;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DarkUI.Experiments.WindowlessRichEdit
{
    /// <summary>
    /// ISOLATED PROOF-OF-CONCEPT. Windowless RichEdit hosted through
    /// ITextServices/ITextHost. The host's TxGetSysColor returns per-theme
    /// selection colors; RichEdit itself renders all content (glyphs,
    /// selection, caret geometry). Not used by any production control.
    /// </summary>
    public class DarkWindowlessRichEditPrototype : UserControl
    {
        private ITextServices _services;
        private ManualTextHost _host;
        private bool _disposed;

        public DarkWindowlessRichEditPrototype()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
            BackColor = Colors.GreyBackground;
            TabStop = true;
            Size = new Size(300, 80);

            ThemeManager.ThemeChanged += OnThemeChanged;

            _host = new ManualTextHost(this);
            CreateServices();
            SetText("Select this text and change the theme.  ThemeColor→");
        }

        private void CreateServices()
        {
            TraceLog.Log("CreateTextServices enter");
            IntPtr punk;
            int hr = NativeRichEdit.CreateTextServices(IntPtr.Zero, _host.HostPtr, out punk);
            TraceLog.Log($"CreateTextServices hr=0x{hr:X8} punk=0x{punk.ToInt64():X}");
            if (hr != NativeRichEdit.S_OK || punk == IntPtr.Zero)
            {
                _services = null;
                return;
            }
            _services = Marshal.GetObjectForIUnknown(punk) as ITextServices;
            TraceLog.Log($"GetObjectForIUnknown -> services null? {_services == null}");
            // NOTE: do NOT Marshal.Release(punk) here — releasing the raw reference
            // after GetObjectForIUnknown destroyed the RichEdit object and made the
            // RCW dangle (crash in coreclr on the next RCW call). One reference is
            // intentionally leaked for the prototype lifetime.
            TraceLog.Log("CreateServices done");
        }

        public string TextContent
        {
            get
            {
                if (_services == null) return "";
                IntPtr lr;
                _services.TxSendMessage(NativeRichEdit.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero, out lr);
                int len = lr.ToInt32();
                IntPtr buf = Marshal.AllocHGlobal((len + 1) * 2);
                try
                {
                    _services.TxSendMessage(NativeRichEdit.WM_GETTEXT, (IntPtr)(len + 1), buf, out lr);
                    return Marshal.PtrToStringUni(buf);
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
        }

        public void SetText(string text)
        {
            if (_services == null) return;
            IntPtr ptr = Marshal.StringToHGlobalUni(text ?? "");
            try
            {
                IntPtr lr;
                TraceLog.Log("TxSendMessage(WM_SETTEXT) enter");
                int hr = _services.TxSendMessage(NativeRichEdit.WM_SETTEXT, IntPtr.Zero, ptr, out lr);
                TraceLog.Log($"TxSendMessage(WM_SETTEXT) hr=0x{hr:X8} lr=0x{lr.ToInt64():X}");
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }

        /// <summary>Used by TextHost to pump timer events back into RichEdit.</summary>
        internal void ForwardMessage(uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (_services == null) return;
            IntPtr lr;
            _services.TxSendMessage(msg, wParam, lParam, out lr);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            BackColor = Colors.GreyBackground;
            _host?.UpdateCharFormatColor();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                ThemeManager.ThemeChanged -= OnThemeChanged;
                if (_services != null)
                {
                    try { Marshal.ReleaseComObject(_services); } catch { }
                    _services = null;
                }
                _host?.Dispose();
                _host = null;
            }
            base.Dispose(disposing);
        }

        // ── Painting: delegate entirely to RichEdit ─────────────
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Transparent backstyle — we own the background.
            using var b = new SolidBrush(BackColor);
            e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_services == null) return;

            TraceLog.Log("OnPaint TxDraw enter");
            IntPtr hdc = e.Graphics.GetHdc();
            try
            {
                var bounds = new RECTL
                {
                    Left = 0, Top = 0,
                    Right = ClientSize.Width, Bottom = ClientSize.Height
                };
                _services.TxDraw(NativeRichEdit.DVASPECT_CONTENT, -1, IntPtr.Zero, IntPtr.Zero,
                    hdc, hdc, ref bounds, IntPtr.Zero,
                    false, false, true /*bDCIsChild*/, 0);
            }
            finally
            {
                e.Graphics.ReleaseHdc(hdc);
            }
            TraceLog.Log("OnPaint TxDraw done");
        }

        // ── Input: forward to RichEdit via TxSendMessage ────────
        protected override void WndProc(ref Message m)
        {
            if (_services != null)
            {
                switch (m.Msg)
                {
                    case NativeRichEdit.WM_KEYDOWN:
                    case NativeRichEdit.WM_KEYUP:
                    case NativeRichEdit.WM_CHAR:
                    case NativeRichEdit.WM_SYSKEYDOWN:
                    case NativeRichEdit.WM_SYSKEYUP:
                    case NativeRichEdit.WM_SYSCHAR:
                    case NativeRichEdit.WM_LBUTTONDOWN:
                    case NativeRichEdit.WM_LBUTTONUP:
                    case NativeRichEdit.WM_MOUSEMOVE:
                    case NativeRichEdit.WM_LBUTTONDBLCLK:
                    case NativeRichEdit.WM_MOUSEWHEEL:
                    case NativeRichEdit.WM_SETTEXT:
                    case NativeRichEdit.WM_GETTEXT:
                    case NativeRichEdit.WM_GETTEXTLENGTH:
                    case NativeRichEdit.WM_GETDLGCODE:
                    case NativeRichEdit.WM_SETCURSOR:
                    {
                        TraceLog.Log($"forward 0x{m.Msg:X}");
                        IntPtr lr;
                        _services.TxSendMessage((uint)m.Msg, m.WParam, m.LParam, out lr);
                        TraceLog.Log($"forward 0x{m.Msg:X} done lr=0x{lr.ToInt64():X}");
                        m.Result = lr;
                        return;
                    }
                    case NativeRichEdit.WM_SIZE:
                    {
                        base.WndProc(ref m);
                        TraceLog.Log("TxSendMessage(WM_SIZE) enter");
                        IntPtr lr;
                        int hr = _services.TxSendMessage((uint)m.Msg, m.WParam, m.LParam, out lr);
                        TraceLog.Log($"TxSendMessage(WM_SIZE) hr=0x{hr:X8} lr=0x{lr.ToInt64():X}");
                        m.Result = lr;
                        return;
                    }
                    case NativeRichEdit.WM_SETFOCUS:
                    {
                        base.WndProc(ref m); // raise GotFocus
                        TraceLog.Log($"forward WM_SETFOCUS 0x{m.Msg:X}");
                        IntPtr lr;
                        _services.TxSendMessage((uint)m.Msg, m.WParam, m.LParam, out lr);
                        m.Result = lr;
                        Invalidate();
                        return;
                    }
                    case NativeRichEdit.WM_KILLFOCUS:
                    {
                        base.WndProc(ref m); // raise LostFocus
                        TraceLog.Log($"forward WM_KILLFOCUS 0x{m.Msg:X}");
                        IntPtr lr;
                        _services.TxSendMessage((uint)m.Msg, m.WParam, m.LParam, out lr);
                        m.Result = lr;
                        Invalidate();
                        return;
                    }
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left: case Keys.Right: case Keys.Up: case Keys.Down:
                case Keys.PageUp: case Keys.PageDown:
                case Keys.Home: case Keys.End:
                    return true;
            }
            return base.IsInputKey(keyData);
        }
    }
}
