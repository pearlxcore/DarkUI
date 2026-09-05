using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A dark-themed horizontal track bar (slider).
    /// </summary>
    public class DarkTrackBar : Control
    {
        private int _minimum;
        private int _maximum = 100;
        private int _value;
        private bool _dragging;
        private bool _hovered;

        public event EventHandler ValueChanged;

        [Category("Behavior")]
        [DefaultValue(0)]
        public int Minimum
        {
            get { return _minimum; }
            set
            {
                _minimum = value;
                if (_value < _minimum) Value = _minimum;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(100)]
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                if (_value > _maximum) Value = _maximum;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(0)]
        public int Value
        {
            get { return _value; }
            set
            {
                int v = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_value == v) return;
                _value = v;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [Category("Appearance")]
        [DefaultValue(4)]
        public int TrackHeight { get; set; } = 4;

        [Category("Appearance")]
        [DefaultValue(14)]
        public int ThumbSize { get; set; } = 14;

        public DarkTrackBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.Selectable | ControlStyles.UserMouse, true);
            Height = 24;
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e) => Invalidate();

        private float PositionToValue(int x)
        {
            int pad = ThumbSize / 2;
            int usable = Width - pad * 2;
            if (usable <= 0) return _minimum;
            float ratio = (x - pad) / (float)usable;
            return _minimum + ratio * (_maximum - _minimum);
        }

        private int ValueToPosition(int value)
        {
            int pad = ThumbSize / 2;
            int usable = Width - pad * 2;
            if (_maximum == _minimum) return pad;
            float ratio = (value - _minimum) / (float)(_maximum - _minimum);
            return pad + (int)(ratio * usable);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || !Enabled) return;
            _dragging = true;
            Value = (int)PositionToValue(e.X);
            Focus();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                Value = (int)PositionToValue(e.X);
                return;
            }
            bool over = e.X >= ValueToPosition(_value) - ThumbSize / 2 - 2 &&
                        e.X <= ValueToPosition(_value) + ThumbSize / 2 + 2;
            if (over != _hovered)
            {
                _hovered = over;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
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

            int cy = Height / 2;
            int thumbPos = ValueToPosition(_value);

            // Track
            using (var pen = new Pen(Colors.DarkBorder, TrackHeight))
                g.DrawLine(pen, ThumbSize / 2, cy, Width - ThumbSize / 2, cy);

            // Filled portion (before thumb)
            if (_maximum != _minimum)
            {
                using (var pen = new Pen(Enabled ? Colors.BlueSelection : Colors.GreySelection, TrackHeight))
                    g.DrawLine(pen, ThumbSize / 2, cy, thumbPos, cy);
            }

            // Thumb
            Color thumb = !Enabled ? Colors.GreySelection
                : (_dragging ? Colors.ActiveControl : (_hovered ? Colors.GreyHighlight : Colors.LightBackground));
            using (var brush = new SolidBrush(thumb))
                g.FillEllipse(brush, thumbPos - ThumbSize / 2, cy - ThumbSize / 2, ThumbSize, ThumbSize);
            using (var pen = new Pen(Colors.DarkBorder))
                g.DrawEllipse(pen, thumbPos - ThumbSize / 2, cy - ThumbSize / 2, ThumbSize, ThumbSize);
        }
    }
}
