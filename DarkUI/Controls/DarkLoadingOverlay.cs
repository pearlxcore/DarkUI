using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A dimmed overlay panel with an indeterminate spinner and an optional message.
    /// Place over a control (Dock = Fill) and call Show()/Hide().
    /// </summary>
    public class DarkLoadingOverlay : Panel
    {
        private readonly Timer _spinnerTimer = new Timer { Interval = 40 };
        private int _angle;
        private bool _spinning;
        private string _message = "Loading…";

        [Category("Appearance")]
        [DefaultValue("Loading…")]
        public string Message
        {
            get { return _message; }
            set { _message = value ?? ""; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool ShowSpinner { get; set; } = true;

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool BlockInput { get; set; } = true;

        public DarkLoadingOverlay()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint, true);
            BackColor = Color.FromArgb(140, Colors.DarkBackground);
            Visible = false;
            _spinnerTimer.Tick += (s, e) => { _angle = (_angle + 6) % 360; Invalidate(); };
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            BackColor = Color.FromArgb(140, Colors.DarkBackground);
            Invalidate();
        }

        public new void Show()
        {
            Visible = true;
            BringToFront();
            if (ShowSpinner) { _spinning = true; _spinnerTimer.Start(); }
        }

        public new void Hide()
        {
            _spinnerTimer.Stop();
            _spinning = false;
            Visible = false;
        }

        public void SetMessage(string message)
        {
            Message = message;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _spinnerTimer?.Dispose();
                ThemeManager.ThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Dimmed backdrop
            using (var b = new SolidBrush(BackColor))
                g.FillRectangle(b, ClientRectangle);

            int centerX = Width / 2;
            int centerY = Height / 2;

            int textH = 0;
            if (!string.IsNullOrEmpty(_message))
            {
                using (var b = new SolidBrush(Colors.LightText))
                    g.DrawString(_message, Font, b, new PointF(0, centerY + 34),
                        new StringFormat { Alignment = StringAlignment.Center });
                textH = 20;
            }

            if (_spinning && ShowSpinner)
            {
                // Arc spinner
                int radius = 14;
                var rect = new Rectangle(centerX - radius, centerY - radius - (textH > 0 ? 12 : 0), radius * 2, radius * 2);
                using (var pen = new Pen(Colors.GreySelection, 3))
                    g.DrawArc(pen, rect, 0, 360);
                using (var pen = new Pen(Colors.BlueHighlight, 3))
                    g.DrawArc(pen, rect, _angle, 100);
            }
        }
    }
}
