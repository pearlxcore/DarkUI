using DarkUI.Config;
using DarkUI.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>Vertical alignment of a <see cref="DarkTextBox"/>'s content.</summary>
    public enum DarkTextVAlign
    {
        Top,
        Center
    }

    /// <summary>
    /// A dark-themed TextBox (native Win32 EDIT control).
    ///
    /// Text selection INTENTIONALLY uses the native Windows system selection
    /// colors. The EDIT control has no per-window customization point for the
    /// active selection highlight (see docs/TextSelectionTheming.md) — DarkUI
    /// leaves it native rather than using fragile paint/overlay hacks.
    ///
    /// Themed surfaces: normal background, normal foreground, border.
    /// </summary>
    public class DarkTextBox : TextBox
    {
        private const int WM_VSCROLL = 0x0115;
        private const int WM_HSCROLL = 0x0114;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_PASTE = 0x0302;
        private const int EM_SETRECT = 0x00B3;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);

        private readonly DarkScrollBarHost _scrollHost;
        private bool _scrollQueued;
        private string _placeholder = "";

        private DarkTextVAlign _textVAlign = DarkTextVAlign.Top;
        private bool _forcingCenter;
        private bool _originalMultiline;
        private bool _originalWordWrap;
        private ScrollBars _originalScrollBars;
        private bool _originalScrollBarsSet;

        [Category("Appearance")]
        [Description("Vertical alignment of the text. Center uses a single-line hosting mode internally.")]
        [DefaultValue(DarkTextVAlign.Top)]
        public DarkTextVAlign TextVAlign
        {
            get => _textVAlign;
            set
            {
                if (_textVAlign == value)
                    return;
                _textVAlign = value;
                ApplyVerticalAlignment();
            }
        }

        [Category("Appearance")]
        [DefaultValue("")]
        public string Placeholder
        {
            get { return _placeholder; }
            set
            {
                if (_placeholder == value) return;
                _placeholder = value ?? "";
                Invalidate();
            }
        }

        #region Constructor Region

        public DarkTextBox()
        {
            BackColor = Colors.LightBackground;
            ForeColor = Colors.LightText;
            Padding = new Padding(2, 2, 2, 2);
            BorderStyle = BorderStyle.FixedSingle;
            // Default themed edit context menu (replaceable by the app)
            ContextMenuStrip = DarkEditMenu.Create(this);
            _scrollHost = new DarkScrollBarHost(this, new TextBoxScrollContent(this));
            MultilineChanged += OnMultilineChanged;
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode) ApplyThemeColors();
            // The native FixedSingle border turns blue when the box has
            // focus — paint it with the theme border instead.
            NativeFocusBorder.Apply(Handle);
            // The scroll host applies WS_CLIPCHILDREN + a frame change the
            // first time, which resets the formatting rectangle — so set the
            // vertical centering AFTER it.
            _scrollHost.Update();
            ApplyVerticalAlignment();
            // Re-apply once layout/padding settles (the first frame can reset
            // the formatting rectangle).
            BeginInvoke((MethodInvoker)(() =>
            {
                if (!IsDisposed)
                    ApplyContentRect();
            }));
        }

        private void OnMultilineChanged(object sender, EventArgs e) => _scrollHost.Update();

        private void ApplyVerticalAlignment()
        {
            if (_textVAlign == DarkTextVAlign.Center && !_forcingCenter)
            {
                _originalMultiline = Multiline;
                _originalWordWrap = WordWrap;
                _originalScrollBars = ScrollBars;
                _originalScrollBarsSet = true;

                if (!_originalMultiline)
                {
                    _forcingCenter = true;
                    base.Multiline = true;
                    base.WordWrap = false;
                    base.ScrollBars = ScrollBars.None;
                }
            }
            else if (_textVAlign == DarkTextVAlign.Top && _forcingCenter)
            {
                _forcingCenter = false;
                base.Multiline = _originalMultiline;
                base.WordWrap = _originalWordWrap;
                if (_originalScrollBarsSet)
                    base.ScrollBars = _originalScrollBars;
            }

            ApplyContentRect();
        }

        private void ApplyContentRect()
        {
            if (!IsHandleCreated)
                return;

            int top = 0;
            int bottom = ClientSize.Height;
            if (_forcingCenter)
            {
                int lineHeight = Font.Height;
                top = Math.Max(1, (ClientSize.Height - lineHeight) / 2);
                bottom = Math.Max(top, ClientSize.Height - top);
            }

            var rect = new RECT
            {
                Left = 0,
                Top = top,
                Right = ClientSize.Width,
                Bottom = bottom
            };
            SendMessage(Handle, EM_SETRECT, IntPtr.Zero, ref rect);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.ThemeChanged -= OnThemeChanged;
                _scrollHost.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_forcingCenter)
                ApplyContentRect();
            _scrollHost.Update();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_forcingCenter)
                ApplyContentRect();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Single-line centering is emulated with a multiline EDIT, so
            // Enter must not insert a newline.
            if (_forcingCenter && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return))
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            QueueScrollUpdate();
        }

        private void QueueScrollUpdate()
        {
            if (_scrollQueued || !IsHandleCreated)
                return;
            _scrollQueued = true;
            BeginInvoke((MethodInvoker)(() =>
            {
                _scrollQueued = false;
                if (!IsDisposed)
                    _scrollHost.Update();
            }));
        }

        private void OnThemeChanged(object sender, EventArgs e) => ApplyThemeColors();

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (!DesignMode) ApplyThemeColors();
        }

        private void ApplyThemeColors()
        {
            BackColor = Colors.LightBackground;
            ForeColor = Enabled ? Colors.LightText : Colors.DisabledText;
            Invalidate(true);
        }

        // ── Placeholder ─────────────────────────────────────────
        // The native EDIT control draws its own text, so the placeholder is
        // painted as a WM_PAINT overlay after the native paint — only when the
        // box is empty and unfocused.
        protected override void WndProc(ref Message m)
        {
            // Emulated single-line centering uses a multiline EDIT — flatten
            // pasted newlines so it cannot become multi-line.
            if (m.Msg == WM_PASTE && _forcingCenter)
            {
                var pasted = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
                SelectedText = pasted.Replace("\r", string.Empty).Replace("\n", " ");
                return;
            }

            base.WndProc(ref m);

            if (m.Msg == WM_VSCROLL || m.Msg == WM_HSCROLL || m.Msg == WM_MOUSEWHEEL)
            {
                _scrollHost.HideNativeScrollBars();
                QueueScrollUpdate();
            }

            if (m.Msg == 0x000F) // WM_PAINT — overlays after the native paint
            {
                // Disabled: the native EDIT paints its text with system gray
                // (not the theme's DisabledText) — draw it over with the
                // theme color so the disabled state follows the theme.
                if (!Enabled && Text.Length > 0)
                {
                    using (var g = CreateGraphics())
                    {
                        var rect = new Rectangle(
                            ClientRectangle.Left + Padding.Left + 1,
                            ClientRectangle.Top,
                            ClientRectangle.Width - Padding.Horizontal - 2,
                            ClientRectangle.Height);
                        TextRenderer.DrawText(g, Text, Font, rect, Colors.DisabledText, OverlayTextFlags());
                    }
                }
                else if (ShouldShowPlaceholder())
                {
                    using (var g = CreateGraphics())
                    {
                        var rect = new Rectangle(
                            ClientRectangle.Left + Padding.Left + 1,
                            ClientRectangle.Top,
                            ClientRectangle.Width - Padding.Horizontal - 2,
                            ClientRectangle.Height);
                        TextRenderer.DrawText(g, Placeholder, Font, rect, Colors.DisabledText, OverlayTextFlags());
                    }
                }
            }
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

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
            _scrollHost.Update();
        }

        private bool ShouldShowPlaceholder()
            => !string.IsNullOrEmpty(Placeholder) && !Focused && Text.Length == 0;

        private TextFormatFlags OverlayTextFlags()
        {
            var flags = TextFormatFlags.Left | TextFormatFlags.EndEllipsis;
            flags |= _textVAlign == DarkTextVAlign.Center ? TextFormatFlags.VerticalCenter : TextFormatFlags.Top;
            return flags;
        }

        #endregion

        // ── Designer freeze guard ─────────────────────────────────────
        // Theme-owned colors must not be serialized by the designer — a
        // frozen value in Designer.cs would stop the control from
        // following ThemeManager.
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color BackColor
        {
            get => Colors.LightBackground;
            set => base.BackColor = Colors.LightBackground;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color ForeColor
        {
            get => Enabled ? Colors.LightText : Colors.DisabledText;
            set => base.ForeColor = Enabled ? Colors.LightText : Colors.DisabledText;
        }

        [ReadOnly(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Padding Padding
        {
            get { return base.Padding; }
            set { base.Padding = value; }
        }

        [DefaultValue(BorderStyle.FixedSingle)]
        public new BorderStyle BorderStyle
        {
            get { return base.BorderStyle; }
            set { base.BorderStyle = value; }
        }
    }
}
