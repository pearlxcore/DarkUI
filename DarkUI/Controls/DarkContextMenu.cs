using DarkUI.Config;
using DarkUI.Renderers;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    public class DarkContextMenu : ContextMenuStrip
    {
        #region Constructor Region

        public DarkContextMenu()
        {
            Renderer = new DarkMenuRenderer();
            ThemeManager.ThemeChanged += OnThemeChanged;
            // Re-apply right before every open — the renderer paints live Colors,
            // but BackColor/ForeColor stored on the strip and items are set at
            // creation time and would otherwise stay stale after a theme change.
            Opening += (s, e) => ApplyTheme();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e) => ApplyTheme();

        private void ApplyTheme()
        {
            BackColor = Colors.GreyBackground;
            ForeColor = Colors.LightText;
            foreach (ToolStripItem item in Items)
                ApplyToItem(item);
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
            get => Colors.LightText;
            set => base.ForeColor = Colors.LightText;
        }

        private static void ApplyToItem(ToolStripItem item)
        {
            item.BackColor = Colors.GreyBackground;
            item.ForeColor = Colors.LightText;
            if (item is ToolStripMenuItem mi)
            {
                mi.DropDown.BackColor = Colors.GreyBackground;
                mi.DropDown.ForeColor = Colors.LightText;
                foreach (ToolStripItem sub in mi.DropDownItems)
                    ApplyToItem(sub);
            }
        }
    }
}
