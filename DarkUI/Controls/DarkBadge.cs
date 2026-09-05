using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A small pill-shaped count badge (e.g. "3 new").
    /// </summary>
    public class DarkBadge : Control
    {
        private string _text = "";
        private Color _badgeColor = Colors.BlueSelection;
        private Color _textColor = Colors.LightText;
        // Theme defaults at the time the badge was constructed — a theme
        // switch only refreshes colors the user hasn't customized.
        private Color _themeDefaultBadge = Colors.BlueSelection;
        private Color _themeDefaultText = Colors.LightText;

        [Category("Appearance")]
        [DefaultValue("")]
        public override string Text
        {
            get { return _text; }
            set { _text = value ?? ""; Invalidate(); }
        }

        [Category("Appearance")]
        public Color BadgeColor
        {
            get { return _badgeColor; }
            set { _badgeColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color TextColor
        {
            get { return _textColor; }
            set { _textColor = value; Invalidate(); }
        }

        public DarkBadge()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
            AutoSize = true;
            Height = 20;
            Font = new Font("Segoe UI", 9F);
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            // Refresh only the untouched defaults — custom colors are kept.
            if (_badgeColor == _themeDefaultBadge) _badgeColor = Colors.BlueSelection;
            if (_textColor == _themeDefaultText) _textColor = Colors.LightText;
            _themeDefaultBadge = Colors.BlueSelection;
            _themeDefaultText = Colors.LightText;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var size = g.MeasureString(_text, Font);
            int w = (int)size.Width + 16;
            int h = 20;
            var rect = new Rectangle(0, 0, w - 1, h - 1);

            using (var path = RoundRect(rect, h / 2))
            using (var brush = new SolidBrush(Enabled ? _badgeColor : Colors.GreySelection))
                g.FillPath(brush, path);

            TextRenderer.DrawText(g, _text, Font, new Rectangle(0, 0, w, h), Enabled ? _textColor : Colors.DisabledText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            Invalidate();
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
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
