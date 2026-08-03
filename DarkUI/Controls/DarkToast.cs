using DarkUI.Config;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Media;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A borderless, topmost notification banner that slides in from the
    /// top-right of the screen and auto-dismisses. Use the static Show() helpers.
    /// </summary>
    public class DarkToast : Form
    {
        private readonly Timer _lifeTimer = new Timer();
        private readonly Timer _slideTimer = new Timer { Interval = 10 };
        private int _slideY, _targetY;
        private string _message;
        private bool _closing;

        public event EventHandler Dismissed;

        [Flags]
        public enum ToastIcon { None = 0, Info = 1, Success = 2, Warning = 4, Error = 8 }

        private ToastIcon _icon = ToastIcon.None;

        private DarkToast(string message, ToastIcon icon, int durationMs, bool playSound)
        {
            _message = message ?? "";
            _icon = icon;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            ControlBox = false;
            Size = new Size(360, 64);

            if (playSound)
            {
                try
                {
                    if (icon == ToastIcon.Error) SystemSounds.Hand.Play();
                    else if (icon == ToastIcon.Warning) SystemSounds.Exclamation.Play();
                    else SystemSounds.Asterisk.Play();
                }
                catch { }
            }

            // Position off-screen top-right
            var wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(wa.Right - Width - 16, wa.Top + 16 - Height);
            _slideY = wa.Top + 16;
            _targetY = wa.Top + 16;

            _lifeTimer.Interval = Math.Max(500, durationMs);
            _lifeTimer.Tick += (s, e) => Dismiss();
            _lifeTimer.Start();

            _slideTimer.Tick += SlideTick;

            Click += (s, e) => Dismiss();
            MouseDown += (s, e) => Dismiss();
        }

        private void SlideTick(object sender, EventArgs e)
        {
            if (_closing)
            {
                Location = new Point(Location.X, Location.Y + 8);
                if (Location.Y > _targetY + Screen.PrimaryScreen.WorkingArea.Height)
                {
                    _slideTimer.Stop();
                    Dismissed?.Invoke(this, EventArgs.Empty);
                    Close();
                }
                return;
            }

            if (Location.Y < _slideY)
                Location = new Point(Location.X, Location.Y + 8);
            else
            {
                _slideTimer.Stop();
                _lifeTimer.Start();
            }
        }

        private void Dismiss()
        {
            if (_closing) return;
            _closing = true;
            _lifeTimer.Stop();
            _slideTimer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _slideTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 8))
            using (var b = new SolidBrush(Colors.MediumBackground))
                g.FillPath(b, path);
            using (var path = Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 8))
            using (var pen = new Pen(Colors.LightBorder))
                g.DrawPath(pen, path);

            // Accent bar
            Color accent = Colors.BlueHighlight;
            if (_icon == ToastIcon.Success) accent = Color.FromArgb(120, 200, 90);
            else if (_icon == ToastIcon.Warning) accent = Color.FromArgb(220, 180, 60);
            else if (_icon == ToastIcon.Error) accent = Color.FromArgb(220, 80, 80);
            using (var b = new SolidBrush(accent))
                g.FillRectangle(b, 0, 0, 4, Height);

            var tr = new Rectangle(16, 8, Width - 28, Height - 16);
            TextRenderer.DrawText(g, _message, Font, tr, Colors.LightText,
                TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lifeTimer?.Dispose();
                _slideTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
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

        // ── Static API ───────────────────────────────────────────

        public static void Show(string message, ToastIcon icon = ToastIcon.Info,
            int durationMs = 4000, bool playSound = true)
        {
            var toast = new DarkToast(message, icon, durationMs, playSound);
            toast.Show();
        }
    }
}
