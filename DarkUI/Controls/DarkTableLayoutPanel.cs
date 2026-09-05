using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A theme-owned table layout container for forms that need stable,
    /// resize-aware rows without falling back to the system panel colour.
    /// </summary>
    public class DarkTableLayoutPanel : TableLayoutPanel
    {
        public DarkTableLayoutPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);
            base.BackColor = Colors.GreyBackground;
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ThemeManager.ThemeChanged -= OnThemeChanged;

            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            base.BackColor = Colors.GreyBackground;
            Invalidate(true);
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color BackColor
        {
            get => Colors.GreyBackground;
            set => base.BackColor = Colors.GreyBackground;
        }
    }
}
