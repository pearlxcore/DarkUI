using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A dark-themed RichTextBox.
    ///
    /// Text selection INTENTIONALLY uses the native Windows/RichEdit selection
    /// colors. SelectionBackColor/SelectionColor modify persistent character
    /// formatting — they do NOT configure the transient native selection
    /// overlay, and must never be used to emulate theme-colored selection.
    /// Doing so stacks a second highlight and corrupts RTF formatting
    /// (see docs/TextSelectionTheming.md).
    ///
    /// Theme switching only updates control-level BackColor/ForeColor and forces
    /// the native window to erase + repaint (RedrawWindow) so no stale pixels
    /// remain. SelectionStart/Length, caret, scroll and document formatting are
    /// never touched.
    /// </summary>
    public class DarkRichTextBox : RichTextBox
    {
        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr rcUpdate, IntPtr hrgnUpdate, uint flags);
        private const uint RDW_INVALIDATE = 0x0001;
        private const uint RDW_ERASE = 0x0004;
        private const uint RDW_ALLCHILDREN = 0x0080;
        private const uint RDW_UPDATENOW = 0x0100;

        public DarkRichTextBox()
        {
            BackColor = Colors.GreyBackground;
            ForeColor = Colors.LightText;
            BorderStyle = BorderStyle.None;
            HideSelection = false;

            // Default themed edit context menu (replaceable by the app)
            ContextMenuStrip = DarkEditMenu.Create(this);

            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e) => ApplyThemeColors();

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            ApplyThemeColors();
        }

        private void ApplyThemeColors()
        {
            // Control-level colors only — never touch document character formatting.
            BackColor = Colors.GreyBackground;
            ForeColor = Enabled ? Colors.LightText : Colors.DisabledText;

            // RichEdit caches its painted surface; a color change may not
            // repaint regions with previously drawn pixels. Force erase + repaint.
            if (IsHandleCreated)
            {
                RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero,
                    RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN | RDW_UPDATENOW);
            }
        }

        // ── Designer freeze guard ─────────────────────────────────────
        // Theme-owned colors must not be serialized by the designer — a
        // frozen value in Designer.cs would stop the control from
        // following ThemeManager.
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color BackColor
        {
            get => Colors.GreyBackground;
            set => base.BackColor = Colors.GreyBackground;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color ForeColor
        {
            get => Enabled ? Colors.LightText : Colors.DisabledText;
            set => base.ForeColor = Enabled ? Colors.LightText : Colors.DisabledText;
        }
    }
}
