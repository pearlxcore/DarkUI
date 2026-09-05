using DarkUI.Config;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DarkUI.Experiments.WindowlessRichEdit
{
    internal static class TraceLog
    {
        private static readonly object Lock = new();
        internal static void Log(string msg)
        {
            try
            {
                lock (Lock)
                {
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wre_trace.log"),
                        $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// ITextHost implementation backed by a WinForms control. The critical method
    /// is TxGetSysColor: the RichEdit engine asks the host for system colors
    /// during rendering, which lets us return per-theme selection colors
    /// (COLOR_HIGHLIGHT -> Colors.BlueSelection, COLOR_HIGHLIGHTTEXT ->
    /// Colors.LightText) for THIS control instance only — no global changes.
    /// </summary>
    internal sealed class TextHost : ITextHost
    {
        private readonly Control _ctrl;
        private readonly Dictionary<uint, Timer> _timers = new();
        private readonly GCHandle _charFormatPin;
        private readonly IntPtr _charFormatPtr;

        public TextHost(Control ctrl)
        {
            _ctrl = ctrl;

            // Default character format buffer (Segoe UI 10pt, theme text color)
            var cf = new CHARFORMATW
            {
                cbSize = (uint)Marshal.SizeOf<CHARFORMATW>(),
                dwMask = NativeRichEdit.CFM_SIZE | NativeRichEdit.CFM_FACE | NativeRichEdit.CFM_COLOR,
                dwEffects = 0,
                yHeight = 200, // 10pt in twips
                yOffset = 0,
                crTextColor = NativeRichEdit.ToColorRef(Colors.LightText),
                bCharSet = 1, // ANSI
                bPitchAndFamily = 0,
                szFaceName = "Segoe UI"
            };
            _charFormatPin = GCHandle.Alloc(new byte[Marshal.SizeOf<CHARFORMATW>()], GCHandleType.Pinned);
            _charFormatPtr = _charFormatPin.AddrOfPinnedObject();
            UpdateCharFormatColor();
        }

        public void UpdateCharFormatColor()
        {
            var cf = new CHARFORMATW
            {
                cbSize = (uint)Marshal.SizeOf<CHARFORMATW>(),
                dwMask = NativeRichEdit.CFM_SIZE | NativeRichEdit.CFM_FACE | NativeRichEdit.CFM_COLOR,
                dwEffects = 0,
                yHeight = 200,
                yOffset = 0,
                crTextColor = NativeRichEdit.ToColorRef(Colors.LightText),
                bCharSet = 1,
                bPitchAndFamily = 0,
                szFaceName = "Segoe UI"
            };
            Marshal.StructureToPtr(cf, _charFormatPtr, false);
        }

        public void DisposeHost()
        {
            foreach (var t in _timers.Values) t.Dispose();
            _timers.Clear();
            if (_charFormatPin.IsAllocated) _charFormatPin.Free();
        }

        // ── IUnknown (managed CCW handles refcounts) ────────────
        int ITextHost.QueryInterface(ref Guid riid, out IntPtr ppvObject) 
        {
            TraceLog.Log("QueryInterface called");

            ppvObject = IntPtr.Zero;
            return unchecked((int)0x80004001); // E_NOTIMPL
        }
        uint ITextHost.AddRef() => 1;
        uint ITextHost.Release() => 1;

        // ── Device context ──────────────────────────────────────
        IntPtr ITextHost.TxGetDC() => NativeRichEdit.GetDC(_ctrl.Handle);
        int ITextHost.TxReleaseDC(IntPtr hdc) => NativeRichEdit.ReleaseDC(_ctrl.Handle, hdc);

        // ── Scrollbars ──────────────────────────────────────────
        bool ITextHost.TxShowScrollBar(int fnBar, bool fShow) => false;
        bool ITextHost.TxEnableScrollBar(int fuSBFlags, int fuArrowflags) => false;
        bool ITextHost.TxSetScrollRange(int fnBar, int nMinPos, int nMaxPos, bool fRedraw) => false;
        bool ITextHost.TxSetScrollPos(int fnBar, int nPos, bool fRedraw) => false;

        // ── Invalidation ────────────────────────────────────────
        void ITextHost.TxInvalidateRect(IntPtr prc, bool fErase) 
        {
            TraceLog.Log("TxInvalidateRect called");

            if (prc == IntPtr.Zero) { _ctrl.Invalidate(); return; }
            var r = Marshal.PtrToStructure<RECT>(prc);
            _ctrl.Invalidate(new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top));
        }

        void ITextHost.TxViewChange(bool fUpdate) => _ctrl.Invalidate();

        // ── Caret ───────────────────────────────────────────────
        bool ITextHost.TxCreateCaret(IntPtr hbmp, int xWidth, int yHeight)
            => NativeRichEdit.CreateCaret(_ctrl.Handle, IntPtr.Zero, Math.Max(1, xWidth), Math.Max(1, yHeight));

        bool ITextHost.TxShowCaret(bool fShow)
            => fShow ? NativeRichEdit.ShowCaret(_ctrl.Handle) : NativeRichEdit.HideCaret(_ctrl.Handle);

        bool ITextHost.TxSetCaretPos(int x, int y) => NativeRichEdit.SetCaretPos(x, y);

        // ── Timers ──────────────────────────────────────────────
        bool ITextHost.TxSetTimer(uint idTimer, uint uTimeout) 
        {
            TraceLog.Log("TxSetTimer called");

            if (_timers.TryGetValue(idTimer, out var existing)) existing.Stop();
            var t = new Timer { Interval = (int)uTimeout };
            t.Tick += (s, e) => { t.Stop(); SendToRichEdit(NativeRichEdit.WM_TIMER, new IntPtr(idTimer), IntPtr.Zero); };
            t.Start();
            _timers[idTimer] = t;
            return true;
        }

        void ITextHost.TxKillTimer(uint idTimer) 
        {
            TraceLog.Log("TxKillTimer called");

            if (_timers.TryGetValue(idTimer, out var t)) { t.Dispose(); _timers.Remove(idTimer); }
        }

        private void SendToRichEdit(uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (_ctrl is DarkWindowlessRichEditPrototype proto)
                proto.ForwardMessage(msg, wParam, lParam);
        }

        // ── Misc window services ────────────────────────────────
        void ITextHost.TxScrollWindowEx(int dx, int dy, IntPtr prcScroll, IntPtr prcClip, IntPtr hrgnUpdate, IntPtr prcUpdate, uint fuScroll) 
        {
            TraceLog.Log("TxScrollWindowEx called");
 /* no-op for prototype */ }

        void ITextHost.TxSetCapture(bool fCapture) => _ctrl.Capture = fCapture;

        void ITextHost.TxSetFocus() 
        {
            TraceLog.Log("TxSetFocus called");

            if (!_ctrl.Focused) _ctrl.Focus();
        }

        void ITextHost.TxSetCursor(IntPtr hcur, bool fText)
            => Cursor.Current = fText ? Cursors.IBeam : Cursors.Default;

        bool ITextHost.TxScreenToClient(ref POINT ppt) 
        {
            TraceLog.Log("TxScreenToClient called");

            var p = _ctrl.PointToClient(new Point(ppt.X, ppt.Y));
            ppt.X = p.X; ppt.Y = p.Y;
            return true;
        }

        bool ITextHost.TxClientToScreen(ref POINT ppt) 
        {
            TraceLog.Log("TxClientToScreen called");

            var p = _ctrl.PointToScreen(new Point(ppt.X, ppt.Y));
            ppt.X = p.X; ppt.Y = p.Y;
            return true;
        }

        void ITextHost.TxActivate(int lState) { }
        void ITextHost.TxDeactivate(int lState) { }

        // ── Geometry / formatting ───────────────────────────────
        int ITextHost.TxGetClientRect(ref RECT prc) 
        {
            TraceLog.Log("TxGetClientRect called");

            var r = _ctrl.ClientRectangle;
            prc.Left = r.Left; prc.Top = r.Top; prc.Right = r.Right; prc.Bottom = r.Bottom;
            return NativeRichEdit.S_OK;
        }

        void ITextHost.TxGetViewInset(ref RECT prc) 
        {
            TraceLog.Log("TxGetViewInset called");

            prc.Left = prc.Top = prc.Right = prc.Bottom = 0;
        }

        bool ITextHost.TxGetCharFormat(out IntPtr ppCF) 
        {
            TraceLog.Log("TxGetCharFormat called");

            ppCF = _charFormatPtr;
            return true;
        }

        bool ITextHost.TxGetParaFormat(out IntPtr ppPF) 
        {
            TraceLog.Log("TxGetParaFormat called");

            ppPF = IntPtr.Zero;
            return false; // default paragraph format
        }

        uint ITextHost.TxGetPropertyBits(uint dwMask)
            => NativeRichEdit.TXTBIT_RICHTEXT | NativeRichEdit.TXTBIT_MULTILINE | NativeRichEdit.TXTBIT_AUTOWORDSEL;

        int ITextHost.TxGetAcceleratorPos(out int pcp) 
        {
            TraceLog.Log("TxGetAcceleratorPos called");

            pcp = -1;
            return NativeRichEdit.S_OK;
        }

        uint ITextHost.TxGetExtent(ref SIZE lpExtent) 
        {
            TraceLog.Log("TxGetExtent called");

            lpExtent.cx = _ctrl.Width;
            lpExtent.cy = _ctrl.Height;
            return (uint)NativeRichEdit.S_OK;
        }

        // ── THE CRITICAL METHOD ─────────────────────────────────
        int ITextHost.TxGetSysColor(int nIndex) 
        {
            TraceLog.Log("TxGetSysColor called");

            TraceLog.Log($"TxGetSysColor({nIndex}) -> {(nIndex == NativeRichEdit.COLOR_HIGHLIGHT ? "HIGHLIGHT" : nIndex == NativeRichEdit.COLOR_HIGHLIGHTTEXT ? "HIGHLIGHTTEXT" : nIndex.ToString())}");

            // High contrast: respect the OS (accessibility) — never override.
            if (SystemInformation.HighContrast)
                return NativeRichEdit.GetSysColor(nIndex);

            switch (nIndex)
            {
                case NativeRichEdit.COLOR_HIGHLIGHT:
                    return NativeRichEdit.ToColorRef(Colors.BlueSelection);
                case NativeRichEdit.COLOR_HIGHLIGHTTEXT:
                    return NativeRichEdit.ToColorRef(Colors.LightText);
                case NativeRichEdit.COLOR_WINDOW:
                    return NativeRichEdit.ToColorRef(Colors.GreyBackground);
                case NativeRichEdit.COLOR_WINDOWTEXT:
                    return NativeRichEdit.ToColorRef(Colors.LightText);
                default:
                    return NativeRichEdit.GetSysColor(nIndex);
            }
        }

        int ITextHost.TxGetBackStyle(out int pstyle) 
        {
            TraceLog.Log("TxGetBackStyle called");

            pstyle = NativeRichEdit.TXTBACK_TRANSPARENT; // we paint the background
            return NativeRichEdit.S_OK;
        }

        int ITextHost.TxGetMaxLength(out uint plength) 
        {
            TraceLog.Log("TxGetMaxLength called");

            plength = 0x7FFFFFFF;
            return NativeRichEdit.S_OK;
        }

        int ITextHost.TxGetScrollBars(out uint pdwScrollBar) 
        {
            TraceLog.Log("TxGetScrollBars called");

            pdwScrollBar = 0;
            return NativeRichEdit.S_OK;
        }

        int ITextHost.TxGetPasswordChar(out ushort pch) 
        {
            TraceLog.Log("TxGetPasswordChar called");

            pch = 0;
            return NativeRichEdit.S_OK;
        }

        int ITextHost.TxGetSelectionBarWidth(out int plSelBarWidth) 
        {
            TraceLog.Log("TxGetSelectionBarWidth called");

            plSelBarWidth = 0;
            return NativeRichEdit.S_OK;
        }

        int ITextHost.TxGetEditStyle(uint dwItem, out uint pdwData) 
        {
            TraceLog.Log("TxGetEditStyle called");

            pdwData = 0;
            return NativeRichEdit.S_OK;
        }
    }
}
