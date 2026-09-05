using DarkUI.Config;
using DarkUI.Renderers;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    public class DarkMenuStrip : MenuStrip
    {
        #region Constructor Region

        public DarkMenuStrip()
        {
            Renderer = new DarkMenuRenderer();
            Padding = new Padding(3, 2, 0, 2);
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        #endregion

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode) ApplyTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            ApplyTheme();
            Invalidate(true);
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

        private void ApplyTheme()
        {
            BackColor = Colors.GreyBackground;
            ForeColor = Colors.LightText;
            foreach (ToolStripItem item in Items)
                ApplyToItem(item);
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
