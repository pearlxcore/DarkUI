using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A themed ToolStripDropDownButton. Opens a DarkContextMenu (fully
    /// themed dropdown) by default. Colours are applied once the control is
    /// hosted on a ToolStrip (OnOwnerChanged), never in the constructor.
    /// </summary>
    public class DarkToolStripDropDownButton : ToolStripDropDownButton
    {
        private bool _disposed;

        // Owned by us (base only disposes auto-generated DropDowns).
        private readonly DarkContextMenu _defaultDropDown = new DarkContextMenu();

        public DarkToolStripDropDownButton() : base()
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
            DropDown = _defaultDropDown;
        }

        public DarkToolStripDropDownButton(string text) : base(text)
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
            DropDown = _defaultDropDown;
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
                if (DropDown == _defaultDropDown)
                    _defaultDropDown.Dispose();
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
