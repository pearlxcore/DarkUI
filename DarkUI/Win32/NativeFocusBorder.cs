using DarkUI.Config;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace DarkUI.Win32
{
    /// <summary>
    /// Replaces the native blue focus outline Windows paints around focused
    /// native windows (combo dropdown lists, list boxes) with the theme
    /// border color. The color is read from Colors.* at paint time, so it
    /// follows live theme switches.
    /// </summary>
    internal static class NativeFocusBorder
    {
        private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam,
            UIntPtr uIdSubclass, UIntPtr dwRefData);

        // Static — rooted for the lifetime of the process, no per-control
        // state needed: the paint reads Colors.* directly.
        private static readonly SUBCLASSPROC _subclassProc = SubclassProc;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COMBOBOXINFO
        {
            public int cbSize;
            public RECT rcItem;
            public RECT rcButton;
            public int stateButton;
            public IntPtr hwndCombo;
            public IntPtr hwndItem;
            public IntPtr hwndList;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetComboBoxInfo(IntPtr hWnd, ref COMBOBOXINFO pcbi);

        [DllImport("comctl32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass,
            UIntPtr uIdSubclass, UIntPtr dwRefData);

        [DllImport("comctl32.dll")]
        private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam,
            IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        private const int WM_NCPAINT = 0x0085;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_CLIENTEDGE = 0x0200;
        private const uint RDW_FRAME = 0x0400;
        private const uint RDW_INVALIDATE = 0x0001;
        private const uint RDW_UPDATENOW = 0x0100;

        /// <summary>
        /// Subclasses the given native window so its border is painted with
        /// the theme color instead of the native focus outline. Safe to call
        /// repeatedly — a window already subclassed by us is left alone.
        /// </summary>
        public static void Apply(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;
            if (SetWindowSubclass(hWnd, _subclassProc, (UIntPtr)1, UIntPtr.Zero))
                RedrawWindow(hWnd, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE | RDW_UPDATENOW);
        }

        /// <summary>
        /// Applies the themed border to the dropdown list window of a combo
        /// box. The list is a separate native popup recreated for each
        /// dropdown session, so this must be called every time the dropdown
        /// opens.
        /// </summary>
        public static void ApplyToComboList(IntPtr comboHwnd)
        {
            if (comboHwnd == IntPtr.Zero) return;
            var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf<COMBOBOXINFO>() };
            if (GetComboBoxInfo(comboHwnd, ref info) && info.hwndList != IntPtr.Zero)
                Apply(info.hwndList);
        }

        private static IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam,
            UIntPtr uIdSubclass, UIntPtr dwRefData)
        {
            if (uMsg == WM_NCPAINT)
            {
                IntPtr hdc = GetWindowDC(hWnd);
                try
                {
                    var rc = new RECT();
                    GetWindowRect(hWnd, ref rc);
                    int w = rc.Right - rc.Left;
                    int h = rc.Bottom - rc.Top;
                    int thickness = (GetWindowLong(hWnd, GWL_EXSTYLE).ToInt64() & WS_EX_CLIENTEDGE) != 0 ? 2 : 1;
                    using (var g = Graphics.FromHdc(hdc))
                    using (var pen = new Pen(Colors.GreySelection, thickness))
                        g.DrawRectangle(pen, thickness / 2f, thickness / 2f, w - thickness, h - thickness);
                }
                finally
                {
                    ReleaseDC(hWnd, hdc);
                }
                return (IntPtr)1;
            }
            return DefSubclassProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData);
        }
    }
}
