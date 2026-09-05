using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A themed bottom container for form actions and supplementary content.
    /// Child controls can be added and arranged normally in the designer.
    /// </summary>
    public class DarkFooterBar : Panel
    {
        protected override Size DefaultSize => new Size(400, 40);

        public DarkFooterBar()
        {
            Dock = DockStyle.Bottom;
            Height = 40;
            Padding = Padding.Empty;
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
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

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var brush = new SolidBrush(Colors.GreyBackground))
                e.Graphics.FillRectangle(brush, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (var darkBorder = new Pen(Colors.DarkBorder))
                e.Graphics.DrawLine(darkBorder, ClientRectangle.Left, 0, ClientRectangle.Right, 0);

            using (var lightBorder = new Pen(Colors.LightBorder))
                e.Graphics.DrawLine(lightBorder, ClientRectangle.Left, 1, ClientRectangle.Right, 1);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            base.BackColor = Colors.GreyBackground;
            Invalidate(true);
        }

        // Theme-owned color must not be frozen into Designer.cs.
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color BackColor
        {
            get => Colors.GreyBackground;
            set => base.BackColor = Colors.GreyBackground;
        }
    }
}
