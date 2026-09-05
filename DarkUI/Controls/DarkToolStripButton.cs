using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A themed ToolStripButton. Colours are applied once the control is
    /// hosted on a ToolStrip (OnOwnerChanged), never in the constructor.
    /// </summary>
    public class DarkToolStripButton : ToolStripButton
    {
        private bool _disposed;

        public DarkToolStripButton() : base()
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        public DarkToolStripButton(string text) : base(text)
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        public DarkToolStripButton(string text, Image image) : base(text, image)
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        public DarkToolStripButton(string text, Image image, EventHandler onClick) : base(text, image, onClick)
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void OnOwnerChanged(EventArgs e)
        {
            base.OnOwnerChanged(e);
            if (Owner != null)
                UpdateColors();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                ThemeManager.ThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e) => UpdateColors();

        private void UpdateColors()
        {
            if (Owner == null) return;
            ForeColor = Colors.LightText;
            Invalidate();
        }

        // ── Designer freeze guard ─────────────────────────────────────
        // Theme-owned colors must not be serialized by the designer — a
        // frozen value in Designer.cs would stop the control from
        // following ThemeManager.
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color ForeColor
        {
            get => Colors.LightText;
            set => base.ForeColor = Colors.LightText;
        }
    }
}
