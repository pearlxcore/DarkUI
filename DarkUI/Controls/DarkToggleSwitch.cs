using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A modern on/off toggle switch with a sliding knob.
    /// </summary>
    public class DarkToggleSwitch : Control
    {
        private bool _checked;
        private bool _hovered;

        public event EventHandler CheckedChanged;

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public DarkToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
            Size = new Size(44, 22);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left)
                Checked = !Checked;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Track
            Color track = _checked ? Colors.BlueSelection
                : (_hovered ? Colors.GreyHighlight : Colors.MediumBackground);
            using (var path = RoundedRect(rect, Height / 2))
            using (var brush = new SolidBrush(track))
                g.FillPath(brush, path);
            using (var path = RoundedRect(rect, Height / 2))
            using (var pen = new Pen(Colors.DarkBorder))
                g.DrawPath(pen, path);

            // Knob
            int knobSize = Height - 6;
            int x = _checked ? Width - knobSize - 3 : 3;
            int y = (Height - knobSize) / 2;
            using (var path = RoundedRect(new Rectangle(x, y, knobSize, knobSize), knobSize / 2))
            using (var brush = new SolidBrush(Colors.LightText))
                g.FillPath(brush, path);

            // Disabled
            if (!Enabled)
            {
                using (var brush = new SolidBrush(Color.FromArgb(120, Colors.MediumBackground)))
                    g.FillRectangle(brush, rect);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
