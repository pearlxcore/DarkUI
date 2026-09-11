using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DarkScrollInfo { public int cbSize, fMask, nMin, nMax, nPage, nPos, nTrackPos; }

    internal static class DarkScrollNative
    {
        public const int SB_HORZ = 0;
        public const int SB_VERT = 1;
        public const int SIF_ALL = 0x17;
        public const int WM_HSCROLL = 0x0114;
        public const int SB_THUMBPOSITION = 4;
        public const int EM_LINESCROLL = 0x00B6;

        [DllImport("user32.dll")] public static extern bool GetScrollInfo(IntPtr hWnd, int nBar, ref DarkScrollInfo lpsi);
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public static DarkScrollInfo Info(IntPtr handle, int bar)
        {
            var si = new DarkScrollInfo { cbSize = Marshal.SizeOf<DarkScrollInfo>(), fMask = SIF_ALL };
            if (handle != IntPtr.Zero)
                GetScrollInfo(handle, bar, ref si);
            return si;
        }
    }

    /// <summary>Scroll source for a multiline <see cref="TextBox"/>.</summary>
    internal sealed class TextBoxScrollContent : IDarkScrollContent
    {
        private readonly TextBox _textBox;

        public TextBoxScrollContent(TextBox textBox)
        {
            _textBox = textBox;
        }

        private DarkScrollInfo VInfo => DarkScrollNative.Info(_textBox.IsHandleCreated ? _textBox.Handle : IntPtr.Zero, DarkScrollNative.SB_VERT);
        private DarkScrollInfo HInfo => DarkScrollNative.Info(_textBox.IsHandleCreated ? _textBox.Handle : IntPtr.Zero, DarkScrollNative.SB_HORZ);

        private bool VerticalAllowed => _textBox.Multiline &&
            (_textBox.ScrollBars == ScrollBars.Vertical || _textBox.ScrollBars == ScrollBars.Both);

        private bool HorizontalAllowed => _textBox.Multiline &&
            (_textBox.ScrollBars == ScrollBars.Horizontal || _textBox.ScrollBars == ScrollBars.Both);

        public bool VerticalScrollable => VerticalAllowed && VInfo.nMax > VInfo.nPage;
        public int VerticalValue => VInfo.nPos;
        public int VerticalMaximum => VInfo.nMax;
        public int VerticalViewport => VInfo.nPage;

        public void ScrollVerticalTo(int value)
        {
            if (!_textBox.IsHandleCreated)
                return;
            int delta = value - VerticalValue;
            if (delta != 0)
                DarkScrollNative.SendMessage(_textBox.Handle, DarkScrollNative.EM_LINESCROLL, IntPtr.Zero, (IntPtr)delta);
        }

        public bool HorizontalScrollable => HorizontalAllowed && HInfo.nMax > HInfo.nPage;
        public int HorizontalValue => HInfo.nPos;
        public int HorizontalMaximum => HInfo.nMax;
        public int HorizontalViewport => HInfo.nPage;

        public void ScrollHorizontalTo(int value)
        {
            if (!_textBox.IsHandleCreated)
                return;
            int delta = value - HorizontalValue;
            if (delta != 0)
                DarkScrollNative.SendMessage(_textBox.Handle, DarkScrollNative.EM_LINESCROLL, (IntPtr)delta, IntPtr.Zero);
        }
    }
}
