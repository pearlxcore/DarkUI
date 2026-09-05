using DarkUI.Config;
using DarkUI.Renderers;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    public class DarkStatusStrip : StatusStrip
    {
        #region Constructor Region

        public DarkStatusStrip()
        {
            AutoSize = false;
            BackColor = Colors.GreyBackground;
            ForeColor = Colors.LightText;
            // Symmetric padding — an asymmetric top/bottom (5/3) shifts the
            // vertical centering band down and makes tall items (24px
            // buttons) sit visibly off-center.
            Padding = new Padding(0, 4, 0, 4);
            Size = new Size(Size.Width, 24);
            SizingGrip = false;
            // Themed hover/pressed/separator/arrow painting for all items
            // (the default system renderer would draw them in light colors).
            Renderer = new DarkStatusStripRenderer();
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode) ApplyThemeColors();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e) => ApplyThemeColors();

        private void ApplyThemeColors()
        {
            BackColor = Colors.GreyBackground;
            ForeColor = Colors.LightText;
            foreach (ToolStripItem it in Items) it.ForeColor = Colors.LightText;
            Invalidate(true);
        }

        #endregion

        #region Paint Region

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;

            using (var b = new SolidBrush(Colors.GreyBackground))
            {
                g.FillRectangle(b, ClientRectangle);
            }

            using (var p = new Pen(Colors.DarkBorder))
            {
                g.DrawLine(p, ClientRectangle.Left, 0, ClientRectangle.Right, 0);
            }

            using (var p = new Pen(Colors.LightBorder))
            {
                g.DrawLine(p, ClientRectangle.Left, 1, ClientRectangle.Right, 1);
            }
        }

        #endregion

        [Browsable(true)]
        [DefaultValue(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public override bool AutoSize
        {
            get { return base.AutoSize; }
            set { base.AutoSize = value; }
        }

        [DefaultValue(false)]
        [Description("StatusStripSizingGripDescr")]
        public new bool SizingGrip
        {
            get { return base.SizingGrip; }
            set { base.SizingGrip = value; }
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
    }
}
