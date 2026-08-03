using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A button showing a color swatch; clicking opens a themed palette popup.
    /// </summary>
    public class DarkColorButton : Control
    {
        private Color _color = Colors.BlueSelection;
        private bool _hovered;

        public event EventHandler ColorChanged;

        [Category("Appearance")]
        public Color Color
        {
            get { return _color; }
            set
            {
                if (_color == value) return;
                _color = value;
                Invalidate();
                ColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private static readonly Color[] _palette =
        {
            Color.FromArgb(220, 80, 80), Color.FromArgb(220, 180, 60), Color.FromArgb(120, 200, 90),
            Color.FromArgb(100, 160, 220), Color.FromArgb(160, 120, 220), Color.FromArgb(240, 140, 60),
            Color.FromArgb(75, 110, 175), Color.FromArgb(92, 92, 92), Color.FromArgb(220, 220, 220),
            Color.FromArgb(60, 63, 65), Color.FromArgb(43, 43, 43), Color.FromArgb(178, 178, 178),
        };

        public DarkColorButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
            Size = new Size(70, 26);
            Cursor = Cursors.Hand;
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

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left || !Enabled) return;
            ShowPalette();
        }

        private void ShowPalette()
        {
            using (var popup = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                BackColor = Colors.MediumBackground,
                Size = new Size(200, 120),
                Location = PointToScreen(new Point(0, Height + 2)),
                TopMost = true,
            })
            {
                int cols = 6, cell = 28, pad = 6;
                for (int i = 0; i < _palette.Length; i++)
                {
                    var c = _palette[i];
                    var btn = new Panel
                    {
                        Bounds = new Rectangle(pad + (i % cols) * cell, pad + (i / cols) * cell, cell - 4, cell - 4),
                        BackColor = c,
                        Cursor = Cursors.Hand,
                        Tag = c,
                    };
                    btn.Click += (s, ev) =>
                    {
                        Color = (Color)((Panel)s).Tag;
                        popup.Close();
                    };
                    popup.Controls.Add(btn);
                }
                popup.Deactivate += (s, ev) => popup.Close();
                popup.ShowDialog(this);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var b = new SolidBrush(_hovered && Enabled ? Colors.LightBackground : Colors.MediumBackground))
                g.FillRectangle(b, rect);
            using (var pen = new Pen(Enabled ? Colors.LightBorder : Colors.DarkBorder))
                g.DrawRectangle(pen, rect);

            // Swatch
            var swatch = new Rectangle(6, (Height - 12) / 2, 18, 12);
            using (var b = new SolidBrush(Enabled ? _color : Colors.GreySelection))
                g.FillRectangle(b, swatch);
            using (var pen = new Pen(Colors.DarkBorder))
                g.DrawRectangle(pen, swatch);

            // Color hex text
            TextRenderer.DrawText(g, _color.ToArgb().ToString("X6").Substring(2), Font,
                new Rectangle(30, 0, Width - 34, Height), Enabled ? Colors.LightText : Colors.DisabledText,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }
}
