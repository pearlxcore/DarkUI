using DarkUI.Config;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DarkUI.Experiments.WindowlessRichEdit
{
    /// <summary>
    /// ITextHost implemented as a MANUAL native vtable with __thiscall delegates.
    ///
    /// Why not [ComImport]+CCW? textserv.h declares ITextHost methods without
    /// STDMETHODCALLTYPE (they are __thiscall in C++). The .NET CCW's argument
    /// marshaling for out-params crashed deterministically inside
    /// CreateTextServices (AccessViolation at TxGetScrollBars). A hand-built
    /// vtable gives byte-exact control over every slot — the approach proven in
    /// "Using Windowless Rich Edit Controls Directly from C#" (CodeProject).
    ///
    /// x64 note: ThisCall == default on x64, so delegate marshaling is exact.
    /// All BOOL params/returns are declared as int (0/1) to avoid bool-size doubt.
    /// </summary>
    internal sealed class ManualTextHost : IDisposable
    {
        // ── Delegate types (ThisCall) ───────────────────────────
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DQI(IntPtr self, ref Guid riid, out IntPtr ppv);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate uint DAddRef(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate uint DRelease(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr DGetDC(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DReleaseDC(IntPtr self, IntPtr hdc);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DShowScrollBar(IntPtr self, int fnBar, int fShow);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DEnableScrollBar(IntPtr self, int fuSBFlags, int fuArrowflags);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DSetScrollRange(IntPtr self, int fnBar, int nMinPos, int nMaxPos, int fRedraw);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DSetScrollPos(IntPtr self, int fnBar, int nPos, int fRedraw);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void DInvalidateRect(IntPtr self, IntPtr prc, int fErase);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void DViewChange(IntPtr self, int fUpdate);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DCreateCaret(IntPtr self, IntPtr hbmp, int xWidth, int yHeight);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DShowCaret(IntPtr self, int fShow);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DSetCaretPos(IntPtr self, int x, int y);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DSetTimer(IntPtr self, uint idTimer, uint uTimeout);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void DKillTimer(IntPtr self, uint idTimer);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void DScrollWindowEx(IntPtr self, int dx, int dy, IntPtr prcScroll, IntPtr prcClip, IntPtr hrgnUpdate, IntPtr prcUpdate, uint fuScroll);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void DSetCapture(IntPtr self, int fCapture);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void DSetFocus(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void DSetCursor(IntPtr self, IntPtr hcur, int fText);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DScreenToClient(IntPtr self, ref POINT ppt);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DClientToScreen(IntPtr self, ref POINT ppt);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void DActivate(IntPtr self, int lState);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void DDeactivate(IntPtr self, int lState);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DGetClientRect(IntPtr self, ref RECT prc);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void DGetViewInset(IntPtr self, ref RECT prc);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DGetCharFormat(IntPtr self, out IntPtr ppCF);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DGetParaFormat(IntPtr self, out IntPtr ppPF);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate uint DGetPropertyBits(IntPtr self, uint dwMask);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DGetAcceleratorPos(IntPtr self, out int pcp);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate uint DGetExtent(IntPtr self, ref SIZE lpExtent);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DGetSysColor(IntPtr self, int nIndex);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DGetBackStyle(IntPtr self, out int pstyle);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DGetMaxLength(IntPtr self, out uint plength);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DGetScrollBars(IntPtr self, out uint pdwScrollBar);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DGetPasswordChar(IntPtr self, out ushort pch);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DGetSelectionBarWidth(IntPtr self, out int plSelBarWidth);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int DGetEditStyle(IntPtr self, uint dwItem, out uint pdwData);

        // ── Static delegate instances (keep alive for GC) ───────
        private static readonly DQI FnQi = QI;
        private static readonly DAddRef FnAddRef = AddRef;
        private static readonly DRelease FnRelease = Release;
        private static readonly DGetDC FnGetDC = GetDC;
        private static readonly DReleaseDC FnReleaseDC = ReleaseDC;
        private static readonly DShowScrollBar FnShowScrollBar = ShowScrollBar;
        private static readonly DEnableScrollBar FnEnableScrollBar = EnableScrollBar;
        private static readonly DSetScrollRange FnSetScrollRange = SetScrollRange;
        private static readonly DSetScrollPos FnSetScrollPos = SetScrollPos;
        private static readonly DInvalidateRect FnInvalidateRect = InvalidateRect;
        private static readonly DViewChange FnViewChange = ViewChange;
        private static readonly DCreateCaret FnCreateCaret = CreateCaret;
        private static readonly DShowCaret FnShowCaret = ShowCaret;
        private static readonly DSetCaretPos FnSetCaretPos = SetCaretPos;
        private static readonly DSetTimer FnSetTimer = SetTimer;
        private static readonly DKillTimer FnKillTimer = KillTimer;
        private static readonly DScrollWindowEx FnScrollWindowEx = ScrollWindowEx;
        private static readonly DSetCapture FnSetCapture = SetCapture;
        private static readonly DSetFocus FnSetFocus = SetFocus;
        private static readonly DSetCursor FnSetCursor = SetCursor;
        private static readonly DScreenToClient FnScreenToClient = ScreenToClient;
        private static readonly DClientToScreen FnClientToScreen = ClientToScreen;
        private static readonly DActivate FnActivate = Activate;
        private static readonly DDeactivate FnDeactivate = Deactivate;
        private static readonly DGetClientRect FnGetClientRect = GetClientRect;
        private static readonly DGetViewInset FnGetViewInset = GetViewInset;
        private static readonly DGetCharFormat FnGetCharFormat = GetCharFormat;
        private static readonly DGetParaFormat FnGetParaFormat = GetParaFormat;
        private static readonly DGetPropertyBits FnGetPropertyBits = GetPropertyBits;
        private static readonly DGetAcceleratorPos FnGetAcceleratorPos = GetAcceleratorPos;
        private static readonly DGetExtent FnGetExtent = GetExtent;
        private static readonly DGetSysColor FnGetSysColor = GetSysColor;
        private static readonly DGetBackStyle FnGetBackStyle = GetBackStyle;
        private static readonly DGetMaxLength FnGetMaxLength = GetMaxLength;
        private static readonly DGetScrollBars FnGetScrollBars = GetScrollBars;
        private static readonly DGetPasswordChar FnGetPasswordChar = GetPasswordChar;
        private static readonly DGetSelectionBarWidth FnGetSelectionBarWidth = GetSelectionBarWidth;
        private static readonly DGetEditStyle FnGetEditStyle = GetEditStyle;

        private static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");
        private static readonly Guid IID_ITextHost = new("C5BDD8D0-D26E-11CE-A89E-00AA006CADC5");

        // ── Instance state ───────────────────────────────────────
        private static ManualTextHost s_current; // single-instance prototype

        private readonly Control _ctrl;
        private readonly Dictionary<uint, Timer> _timers = new();
        private readonly byte[] _charFormatBuffer;
        private readonly GCHandle _charFormatPin; // MUST stay pinned — RichEdit reads this pointer during every layout/paint
        private IntPtr _vtablePtr;
        private IntPtr _self;
        private bool _disposed;

        public IntPtr HostPtr => _self;

        public ManualTextHost(Control ctrl)
        {
            _ctrl = ctrl;
            s_current = this;

            _charFormatBuffer = new byte[Marshal.SizeOf<CHARFORMATW>()];
            _charFormatPin = GCHandle.Alloc(_charFormatBuffer, GCHandleType.Pinned);
            UpdateCharFormatColor();

            // Build vtable (order = textserv.h, proven reachable by RichEdit)
            IntPtr[] slots =
            {
                Marshal.GetFunctionPointerForDelegate(FnQi),
                Marshal.GetFunctionPointerForDelegate(FnAddRef),
                Marshal.GetFunctionPointerForDelegate(FnRelease),
                Marshal.GetFunctionPointerForDelegate(FnGetDC),
                Marshal.GetFunctionPointerForDelegate(FnReleaseDC),
                Marshal.GetFunctionPointerForDelegate(FnShowScrollBar),
                Marshal.GetFunctionPointerForDelegate(FnEnableScrollBar),
                Marshal.GetFunctionPointerForDelegate(FnSetScrollRange),
                Marshal.GetFunctionPointerForDelegate(FnSetScrollPos),
                Marshal.GetFunctionPointerForDelegate(FnInvalidateRect),
                Marshal.GetFunctionPointerForDelegate(FnViewChange),
                Marshal.GetFunctionPointerForDelegate(FnCreateCaret),
                Marshal.GetFunctionPointerForDelegate(FnShowCaret),
                Marshal.GetFunctionPointerForDelegate(FnSetCaretPos),
                Marshal.GetFunctionPointerForDelegate(FnSetTimer),
                Marshal.GetFunctionPointerForDelegate(FnKillTimer),
                Marshal.GetFunctionPointerForDelegate(FnScrollWindowEx),
                Marshal.GetFunctionPointerForDelegate(FnSetCapture),
                Marshal.GetFunctionPointerForDelegate(FnSetFocus),
                Marshal.GetFunctionPointerForDelegate(FnSetCursor),
                Marshal.GetFunctionPointerForDelegate(FnScreenToClient),
                Marshal.GetFunctionPointerForDelegate(FnClientToScreen),
                Marshal.GetFunctionPointerForDelegate(FnActivate),
                Marshal.GetFunctionPointerForDelegate(FnDeactivate),
                Marshal.GetFunctionPointerForDelegate(FnGetClientRect),
                Marshal.GetFunctionPointerForDelegate(FnGetViewInset),
                Marshal.GetFunctionPointerForDelegate(FnGetCharFormat),
                Marshal.GetFunctionPointerForDelegate(FnGetParaFormat),
                Marshal.GetFunctionPointerForDelegate(FnGetPropertyBits),
                Marshal.GetFunctionPointerForDelegate(FnGetAcceleratorPos),
                Marshal.GetFunctionPointerForDelegate(FnGetExtent),
                Marshal.GetFunctionPointerForDelegate(FnGetSysColor),
                Marshal.GetFunctionPointerForDelegate(FnGetBackStyle),
                Marshal.GetFunctionPointerForDelegate(FnGetMaxLength),
                Marshal.GetFunctionPointerForDelegate(FnGetScrollBars),
                Marshal.GetFunctionPointerForDelegate(FnGetPasswordChar),
                Marshal.GetFunctionPointerForDelegate(FnGetSelectionBarWidth),
                Marshal.GetFunctionPointerForDelegate(FnGetEditStyle),
            };

            _vtablePtr = Marshal.AllocHGlobal(slots.Length * IntPtr.Size);
            for (int i = 0; i < slots.Length; i++)
                Marshal.WriteIntPtr(_vtablePtr, i * IntPtr.Size, slots[i]);

            _self = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(_self, _vtablePtr);
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
            IntPtr ptr = _charFormatPin.AddrOfPinnedObject();
            Marshal.StructureToPtr(cf, ptr, false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var t in _timers.Values) t.Dispose();
            _timers.Clear();
            if (_self != IntPtr.Zero) { Marshal.FreeHGlobal(_self); _self = IntPtr.Zero; }
            if (_vtablePtr != IntPtr.Zero) { Marshal.FreeHGlobal(_vtablePtr); _vtablePtr = IntPtr.Zero; }
            if (_charFormatPin.IsAllocated) _charFormatPin.Free();
            if (ReferenceEquals(s_current, this)) s_current = null;
        }

        private static ManualTextHost H(IntPtr self) => s_current;

        private static void Log(string msg)
        {
            try
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wre_trace.log"),
                    $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
            }
            catch { }
        }

        // ── IUnknown ─────────────────────────────────────────────
        private static int QI(IntPtr self, ref Guid riid, out IntPtr ppv)
        {
            Log($"QI {riid}");
            if (riid == IID_IUnknown || riid == IID_ITextHost)
            {
                ppv = self;
                return NativeRichEdit.S_OK;
            }
            ppv = IntPtr.Zero;
            return unchecked((int)0x80004002); // E_NOINTERFACE
        }

        private static uint AddRef(IntPtr self) => 1;
        private static uint Release(IntPtr self) => 1;

        // ── DC / scrollbars / invalidation ──────────────────────
        private static IntPtr GetDC(IntPtr self)
        {
            Log("GetDC");
            return NativeRichEdit.GetDC(H(self)._ctrl.Handle);
        }
        private static int ReleaseDC(IntPtr self, IntPtr hdc)
        {
            Log("ReleaseDC");
            return NativeRichEdit.ReleaseDC(H(self)._ctrl.Handle, hdc);
        }
        private static int ShowScrollBar(IntPtr self, int fnBar, int fShow) {
            Log("ShowScrollBar");
            return 0;;
        }
        private static int EnableScrollBar(IntPtr self, int fuSBFlags, int fuArrowflags) {
            Log("EnableScrollBar");
            return 0;;
        }
        private static int SetScrollRange(IntPtr self, int fnBar, int nMinPos, int nMaxPos, int fRedraw) {
            Log("SetScrollRange");
            return 0;;
        }
        private static int SetScrollPos(IntPtr self, int fnBar, int nPos, int fRedraw) {
            Log("SetScrollPos");
            return 0;;
        }

        private static void InvalidateRect(IntPtr self, IntPtr prc, int fErase)
        {
            var h = H(self);
            if (prc == IntPtr.Zero) { h._ctrl.Invalidate(); return; }
            var r = Marshal.PtrToStructure<RECT>(prc);
            h._ctrl.Invalidate(new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top));
        }

        private static void ViewChange(IntPtr self, int fUpdate) {
            Log("ViewChange");
            H(self)._ctrl.Invalidate();
        }

        // ── Caret / timers / window services ─────────────────────
        private static int CreateCaret(IntPtr self, IntPtr hbmp, int xWidth, int yHeight)
        {
            Log($"CreateCaret w={xWidth} h={yHeight}");
            return NativeRichEdit.CreateCaret(H(self)._ctrl.Handle, IntPtr.Zero, Math.Max(1, xWidth), Math.Max(1, yHeight)) ? 1 : 0;
        }
        private static int ShowCaret(IntPtr self, int fShow)
        {
            Log($"ShowCaret({fShow})");
            return (fShow != 0 ? NativeRichEdit.ShowCaret(H(self)._ctrl.Handle) : NativeRichEdit.HideCaret(H(self)._ctrl.Handle)) ? 1 : 0;
        }
        private static int SetCaretPos(IntPtr self, int x, int y)
        {
            Log($"SetCaretPos({x},{y})");
            return NativeRichEdit.SetCaretPos(x, y) ? 1 : 0;
        }

        private static int SetTimer(IntPtr self, uint idTimer, uint uTimeout)
        {
            var h = H(self);
            if (h._timers.TryGetValue(idTimer, out var existing)) existing.Stop();
            var t = new Timer { Interval = (int)uTimeout };
            t.Tick += (s, e) =>
            {
                t.Stop();
                if (h._ctrl is DarkWindowlessRichEditPrototype proto)
                    proto.ForwardMessage(NativeRichEdit.WM_TIMER, new IntPtr(idTimer), IntPtr.Zero);
            };
            t.Start();
            h._timers[idTimer] = t;
            return 1;
        }

        private static void KillTimer(IntPtr self, uint idTimer)
        {
            var h = H(self);
            if (h._timers.TryGetValue(idTimer, out var t)) { t.Dispose(); h._timers.Remove(idTimer); }
        }

        private static void ScrollWindowEx(IntPtr self, int dx, int dy, IntPtr prcScroll, IntPtr prcClip, IntPtr hrgnUpdate, IntPtr prcUpdate, uint fuScroll) { }
        private static void SetCapture(IntPtr self, int fCapture) {
            Log("SetCapture");
            H(self)._ctrl.Capture = fCapture != 0;
        }
        private static void SetFocus(IntPtr self)
        {
            Log("SetFocus");
            var c = H(self)._ctrl;
            if (!c.Focused) c.Focus();
        }
        private static void SetCursor(IntPtr self, IntPtr hcur, int fText) {
            Log("SetCursor");
            Cursor.Current = fText != 0 ? Cursors.IBeam : Cursors.Default;
        }

        private static int ScreenToClient(IntPtr self, ref POINT ppt)
        {
            var p = H(self)._ctrl.PointToClient(new Point(ppt.X, ppt.Y));
            ppt.X = p.X; ppt.Y = p.Y;
            return 1;
        }

        private static int ClientToScreen(IntPtr self, ref POINT ppt)
        {
            var p = H(self)._ctrl.PointToScreen(new Point(ppt.X, ppt.Y));
            ppt.X = p.X; ppt.Y = p.Y;
            return 1;
        }

        private static void Activate(IntPtr self, int lState) { }
        private static void Deactivate(IntPtr self, int lState) { }

        // ── Geometry / formatting ────────────────────────────────
        private static int GetClientRect(IntPtr self, ref RECT prc)
        {
            Log("GetClientRect");
            var r = H(self)._ctrl.ClientRectangle;
            prc.Left = r.Left; prc.Top = r.Top; prc.Right = r.Right; prc.Bottom = r.Bottom;
            return NativeRichEdit.S_OK;
        }

        private static void GetViewInset(IntPtr self, ref RECT prc)
        {
            Log("GetViewInset");
            prc.Left = prc.Top = prc.Right = prc.Bottom = 0;
        }

        private static int GetCharFormat(IntPtr self, out IntPtr ppCF)
        {
            Log("GetCharFormat");
            var h = H(self);
            ppCF = h._charFormatPin.AddrOfPinnedObject();
            return 1; // BOOL TRUE
        }

        private static int GetParaFormat(IntPtr self, out IntPtr ppPF)
        {
            Log("GetParaFormat");
            ppPF = IntPtr.Zero;
            return 0; // BOOL FALSE — default paragraph format
        }

        private static uint GetPropertyBits(IntPtr self, uint dwMask)
        {
            Log("GetPropertyBits");
            return NativeRichEdit.TXTBIT_RICHTEXT | NativeRichEdit.TXTBIT_MULTILINE | NativeRichEdit.TXTBIT_AUTOWORDSEL;
        }

        private static int GetAcceleratorPos(IntPtr self, out int pcp)
        {
            Log("GetAcceleratorPos");
            pcp = -1;
            return NativeRichEdit.S_OK;
        }

        private static uint GetExtent(IntPtr self, ref SIZE lpExtent)
        {
            Log("GetExtent");
            lpExtent.cx = H(self)._ctrl.Width;
            lpExtent.cy = H(self)._ctrl.Height;
            return (uint)NativeRichEdit.S_OK;
        }

        // ── THE CRITICAL METHOD ─────────────────────────────────
        private static int GetSysColor(IntPtr self, int nIndex)
        {
            Log($"TxGetSysColor({nIndex})");
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

        private static int GetBackStyle(IntPtr self, out int pstyle)
        {
            Log("GetBackStyle");
            pstyle = NativeRichEdit.TXTBACK_TRANSPARENT;
            return NativeRichEdit.S_OK;
        }

        private static int GetMaxLength(IntPtr self, out uint plength)
        {
            Log("GetMaxLength");
            plength = 0x7FFFFFFF;
            return NativeRichEdit.S_OK;
        }

        private static int GetScrollBars(IntPtr self, out uint pdwScrollBar)
        {
            Log("TxGetScrollBars");
            pdwScrollBar = 0;
            return NativeRichEdit.S_OK;
        }

        private static int GetPasswordChar(IntPtr self, out ushort pch)
        {
            Log("GetPasswordChar");
            pch = 0;
            return NativeRichEdit.S_OK;
        }

        private static int GetSelectionBarWidth(IntPtr self, out int plSelBarWidth)
        {
            Log("GetSelectionBarWidth");
            plSelBarWidth = 0;
            return NativeRichEdit.S_OK;
        }

        private static int GetEditStyle(IntPtr self, uint dwItem, out uint pdwData)
        {
            Log("GetEditStyle");
            pdwData = 0;
            return NativeRichEdit.S_OK;
        }
    }
}
