using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace DarkUI.Win32
{
    /// <summary>
    /// Win32 NM_CUSTOMDRAW plumbing for ListView / TreeView selection theming.
    ///
    /// The native controls render all content (text, icons, subitems, glyphs);
    /// custom draw only supplies per-item colors (clrText / clrTextBk) so the
    /// selection highlight follows Colors.BlueSelection without manual drawing.
    /// </summary>
    internal static class NativeCustomDraw
    {
        // ── Messages / codes ────────────────────────────────────
        internal const int WM_NOTIFY = 0x004E;
        internal const int NM_CUSTOMDRAW = -12;

        // ── Draw stages ─────────────────────────────────────────
        internal const int CDDS_PREPAINT = 0x00000001;
        internal const int CDDS_ITEMPREPAINT = 0x00010001;
        internal const int CDDS_SUBITEM = 0x00020000;
        internal const int CDDS_ITEMPREPAINT_SUBITEM = CDDS_ITEMPREPAINT | CDDS_SUBITEM; // 0x00030001

        // ── Draw results ────────────────────────────────────────
        internal const int CDRF_DODEFAULT = 0x00000000;
        internal const int CDRF_NEWFONT = 0x00000002;
        internal const int CDRF_NOTIFYITEMDRAW = 0x00000020;
        internal const int CDRF_NOTIFYSUBITEMDRAW = 0x00000020; // same value as ITEMDRAW

        // ── Item states (NMCUSTOMDRAW.uItemState) ───────────────
        internal const int CDIS_SELECTED = 0x0001;
        internal const int CDIS_FOCUS = 0x0010;
        internal const int CDIS_HOT = 0x0040;

        /// <summary>
        /// COLORREF is 0x00BBGGRR (BGR byte order — NOT the same as .NET Color).
        /// </summary>
        internal static int ToColorRef(Color color)
        {
            return color.R | (color.G << 8) | (color.B << 16);
        }

        /// <summary>
        /// True when a WM_NOTIFY message carries an NM_CUSTOMDRAW from the given
        /// native control window. Safe to call before interpreting the payload.
        /// </summary>
        internal static bool IsCustomDraw(IntPtr lParam, IntPtr expectedHwnd)
        {
            var hdr = Marshal.PtrToStructure<NMHDR>(lParam);
            return hdr.code == NM_CUSTOMDRAW && hdr.hwndFrom == expectedHwnd;
        }
    }

    // ── Win32 structures (pointer-size safe) ───────────────────

    [StructLayout(LayoutKind.Sequential)]
    internal struct NMHDR
    {
        public IntPtr hwndFrom;
        public IntPtr idFrom;
        public int code;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NMCUSTOMDRAW
    {
        public NMHDR hdr;
        public int dwDrawStage;
        public IntPtr hdc;
        public RECT rc;
        public IntPtr dwItemSpec;   // item index / handle — IntPtr, not int
        public int uItemState;
        public IntPtr lItemlParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NMLVCUSTOMDRAW
    {
        public NMCUSTOMDRAW nmcd;
        public int clrText;
        public int clrTextBk;
        public int iSubItem;
        public int dwItemType;
        public int clrFace;
        public int iIconEffect;
        public int iIconPhase;
        public int iPartId;
        public int iStateId;
        public RECT rcText;
        public uint uAlign;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NMTVCUSTOMDRAW
    {
        public NMCUSTOMDRAW nmcd;
        public int clrText;
        public int clrTextBk;
        public int iLevel;
    }
}
