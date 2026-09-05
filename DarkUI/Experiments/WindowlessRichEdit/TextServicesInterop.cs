using System;
using System.Runtime.InteropServices;

// ─────────────────────────────────────────────────────────────
// Windowless RichEdit interop — ISOLATED EXPERIMENT, not wired
// into any production control.
//
// Interface layout source (vtable order):
//   - Windows SDK textserv.h (ITextHost / ITextServices)
//   - Reference C# implementation: CodeProject, "Using Windowless
//     Rich Edit Controls Directly from C#"
//     https://www.codeproject.com/Articles/1278723/
//   - Microsoft Learn ITextHost:
//     https://learn.microsoft.com/en-us/windows/win32/api/textserv/nl-textserv-itexthost
//
// NOTE on calling convention: textserv.h methods are declared
// WITHOUT STDMETHODCALLTYPE, i.e. __thiscall in C++ on x86.
// .NET COM interop uses __stdcall. On x64 the two conventions
// are identical, so standard [ComImport] interop is safe here.
// An x86 build would require a manual __thiscall vtable — out of
// scope for this prototype (machine is x64).
// ─────────────────────────────────────────────────────────────

namespace DarkUI.Experiments.WindowlessRichEdit
{
    internal static class NativeRichEdit
    {
        internal const int COLOR_WINDOW = 5;
        internal const int COLOR_WINDOWTEXT = 8;
        internal const int COLOR_HIGHLIGHT = 13;
        internal const int COLOR_HIGHLIGHTTEXT = 14;

        internal const uint TXTBIT_RICHTEXT = 0x00000001;
        internal const uint TXTBIT_MULTILINE = 0x00000010;
        internal const uint TXTBIT_READONLY = 0x00000020;
        internal const uint TXTBIT_USEPASSWORD = 0x00000080;
        internal const uint TXTBIT_HIDESELECTION = 0x00000100;
        internal const uint TXTBIT_AUTOWORDSEL = 0x00000200;
        internal const uint TXTBIT_AUTODETECTURL = 0x00000800;
        internal const uint TXTBIT_DISABLED = 0x00001000;

        internal const int TXTBACK_TRANSPARENT = 0;
        internal const int TXTBACK_OPAQUE = 1;

        internal const int DVASPECT_CONTENT = 1;

        internal const uint CFM_SIZE = 0x80000000;
        internal const uint CFM_FACE = 0x20000000;
        internal const uint CFM_COLOR = 0x40000000;
        internal const uint CFE_AUTOCOLOR = 0x40000000;

        internal const int DLGC_WANTARROWS = 0x0001;
        internal const int DLGC_WANTCHARS = 0x0080;

        internal const int S_OK = 0;

        // Common window messages forwarded to RichEdit
        internal const int WM_KEYDOWN = 0x0100;
        internal const int WM_KEYUP = 0x0101;
        internal const int WM_CHAR = 0x0102;
        internal const int WM_SYSKEYDOWN = 0x0104;
        internal const int WM_SYSKEYUP = 0x0105;
        internal const int WM_SYSCHAR = 0x0106;
        internal const int WM_SETCURSOR = 0x0020;
        internal const int WM_SIZE = 0x0005;
        internal const int WM_SETFOCUS = 0x0007;
        internal const int WM_KILLFOCUS = 0x0008;
        internal const int WM_SETTEXT = 0x000C;
        internal const int WM_GETTEXT = 0x000D;
        internal const int WM_GETTEXTLENGTH = 0x000E;
        internal const int WM_GETDLGCODE = 0x0087;
        internal const int WM_MOUSEMOVE = 0x0200;
        internal const int WM_LBUTTONDOWN = 0x0201;
        internal const int WM_LBUTTONUP = 0x0202;
        internal const int WM_LBUTTONDBLCLK = 0x0203;
        internal const int WM_MOUSEWHEEL = 0x020A;
        internal const int WM_TIMER = 0x0113;

        /// <summary>COLORREF is 0x00BBGGRR — NOT Color.ToArgb().</summary>
        internal static int ToColorRef(System.Drawing.Color c)
            => c.R | (c.G << 8) | (c.B << 16);

        [DllImport("msftedit.dll", EntryPoint = "CreateTextServices", ExactSpelling = true)]
        internal static extern int CreateTextServices(
            IntPtr punkOuter,
            IntPtr pITextHost, // manual-vtable host object pointer
            out IntPtr ppUnk);

        [DllImport("user32.dll")]
        internal static extern int GetSysColor(int nIndex);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        internal static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitmap, int nWidth, int nHeight);

        [DllImport("user32.dll")]
        internal static extern bool ShowCaret(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool HideCaret(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool SetCaretPos(int x, int y);

        [DllImport("user32.dll")]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
    }

    // ── Structures (pointer-size safe) ─────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECTL
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SIZE
    {
        public int cx, cy;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CHARFORMATW
    {
        public uint cbSize;
        public uint dwMask;
        public uint dwEffects;
        public int yHeight;
        public int yOffset;
        public int crTextColor;
        public byte bCharSet;
        public byte bPitchAndFamily;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szFaceName;
    }

    // ── COM interfaces (exact vtable order from textserv.h) ────

    [ComImport]
    [Guid("8D33F740-CF58-11CE-A89D-00AA006CADC5")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITextServices
    {
        // IUnknown (QueryInterface/AddRef/Release) is implicit — declaring them
        // explicitly can double-count the vtable base and offset every method.

        // ITextServices (v1 prefix — stable across RichEdit versions)
        [PreserveSig] int TxSendMessage(uint msg, IntPtr wParam, IntPtr lParam, out IntPtr plresult);
        [PreserveSig] int TxDraw(int dwDrawAspect, int lindex, IntPtr pvAspect, IntPtr ptd,
            IntPtr hdcDraw, IntPtr hicTargetDev, ref RECTL prcBounds, IntPtr prcWBounds,
            bool bOptimize, bool bValidate, bool bDCIsChild, uint dwFlags);
        [PreserveSig] int TxGetNaturalSize(int dwAspect, IntPtr hdcFrom, IntPtr hdcTo, IntPtr ptd,
            ref int pwidth, ref int pheight, ref int ptns);
        [PreserveSig] int TxGetExtent(int dwDrawAspect, int lindex, IntPtr ptd, ref SIZE lpsizel);
        [PreserveSig] int TxSetExtent(int dwDrawAspect, int lindex, IntPtr ptd, ref SIZE lpsizel);
        [PreserveSig] int TxGetText(out IntPtr pbstr);
        [PreserveSig] int TxSetText([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        [PreserveSig] int TxGetCurTargetPos(out int plx, out int ply);
        [PreserveSig] int TxGetBaseLinePos(out int plx, out int ply);
        // TxGetNaturalSize2 (v2) — appended at this slot in RichEdit 4.1+
        [PreserveSig] int TxGetNaturalSize2(int dwAspect, IntPtr hdcFrom, IntPtr hdcTo, IntPtr ptd,
            ref int pwidth, ref int pheight, ref int ptns);
        [PreserveSig] int TxShowScrollBar(int fnBar, bool fShow);
        [PreserveSig] int TxEnableScrollBar(int fuSBFlags, int fuArrowflags);
        [PreserveSig] int TxSetScrollRange(int fnBar, int nMinPos, int nMaxPos, bool fRedraw);
        [PreserveSig] int TxGetScrollInfo(int fnBar, out int pnMin, out int pnMax, out int pnPage, out int pnPos);
        [PreserveSig] int TxGetClientRect(ref RECT prc);
        [PreserveSig] int TxSetScrollPos(int fnBar, int nPos, bool fRedraw);
        [PreserveSig] int TxGetSelection([MarshalAs(UnmanagedType.Interface)] out object ppsel);
        [PreserveSig] int TxGetLastError(int phr);
    }

    [ComImport]
    [Guid("C5BDD8D0-D26E-11CE-A89E-00AA006CADC5")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITextHost
    {
        // IUnknown
        [PreserveSig] int QueryInterface(ref Guid riid, out IntPtr ppvObject);
        [PreserveSig] uint AddRef();
        [PreserveSig] uint Release();

        // ITextHost — exact order from textserv.h
        [PreserveSig] IntPtr TxGetDC();
        [PreserveSig] int TxReleaseDC(IntPtr hdc);
        [PreserveSig] bool TxShowScrollBar(int fnBar, bool fShow);
        [PreserveSig] bool TxEnableScrollBar(int fuSBFlags, int fuArrowflags);
        [PreserveSig] bool TxSetScrollRange(int fnBar, int nMinPos, int nMaxPos, bool fRedraw);
        [PreserveSig] bool TxSetScrollPos(int fnBar, int nPos, bool fRedraw);
        [PreserveSig] void TxInvalidateRect(IntPtr prc, bool fErase);
        [PreserveSig] void TxViewChange(bool fUpdate);
        [PreserveSig] bool TxCreateCaret(IntPtr hbmp, int xWidth, int yHeight);
        [PreserveSig] bool TxShowCaret(bool fShow);
        [PreserveSig] bool TxSetCaretPos(int x, int y);
        [PreserveSig] bool TxSetTimer(uint idTimer, uint uTimeout);
        [PreserveSig] void TxKillTimer(uint idTimer);
        [PreserveSig] void TxScrollWindowEx(int dx, int dy, IntPtr prcScroll, IntPtr prcClip, IntPtr hrgnUpdate, IntPtr prcUpdate, uint fuScroll);
        [PreserveSig] void TxSetCapture(bool fCapture);
        [PreserveSig] void TxSetFocus();
        [PreserveSig] void TxSetCursor(IntPtr hcur, bool fText);
        [PreserveSig] bool TxScreenToClient(ref POINT ppt);
        [PreserveSig] bool TxClientToScreen(ref POINT ppt);
        [PreserveSig] void TxActivate(int lState);
        [PreserveSig] void TxDeactivate(int lState);
        [PreserveSig] int TxGetClientRect(ref RECT prc);
        [PreserveSig] void TxGetViewInset(ref RECT prc);
        [PreserveSig] bool TxGetCharFormat(out IntPtr ppCF);
        [PreserveSig] bool TxGetParaFormat(out IntPtr ppPF);
        [PreserveSig] uint TxGetPropertyBits(uint dwMask);
        [PreserveSig] int TxGetAcceleratorPos(out int pcp);
        [PreserveSig] uint TxGetExtent(ref SIZE lpExtent);
        [PreserveSig] int TxGetSysColor(int nIndex);
        [PreserveSig] int TxGetBackStyle(out int pstyle);
        [PreserveSig] int TxGetMaxLength(out uint plength);
        [PreserveSig] int TxGetScrollBars(out uint pdwScrollBar);
        [PreserveSig] int TxGetPasswordChar(out ushort pch);
        [PreserveSig] int TxGetSelectionBarWidth(out int plSelBarWidth);
        [PreserveSig] int TxGetEditStyle(uint dwItem, out uint pdwData);
    }
}
