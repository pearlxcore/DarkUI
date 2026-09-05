using DarkUI.Config;
using DarkUI.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
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
        private string _placeholder = "";

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
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode) ApplyThemeColors();
            // The native FixedSingle border turns blue when the box has
            // focus — paint it with the theme border instead.
            NativeFocusBorder.Apply(Handle);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
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
            base.WndProc(ref m);

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
                        TextRenderer.DrawText(g, Text, Font, rect, Colors.DisabledText,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
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
                        TextRenderer.DrawText(g, Placeholder, Font, rect, Colors.DisabledText,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
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
        }

        private bool ShouldShowPlaceholder()
            => !string.IsNullOrEmpty(Placeholder) && !Focused && Text.Length == 0;

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
