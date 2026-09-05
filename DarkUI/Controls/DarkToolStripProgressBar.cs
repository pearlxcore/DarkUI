using DarkUI.Config;
using DarkUI.Animation;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A themed ToolStripProgressBar for status bars and tool strips.
    /// Hosts a custom-painted ProgressBar; the fill follows
    /// Colors.BlueSelection (the theme accent).
    /// Exposes the full ToolStripProgressBar surface — Value, Minimum,
    /// Maximum, Style, MarqueeAnimationSpeed, Marquee, Step, Increment,
    /// PerformStep — so a plain progress bar swaps to this type without
    /// touching call sites.
    /// </summary>
    public class DarkToolStripProgressBar : ToolStripControlHost
    {
        private readonly DarkInnerProgressBar _bar;

        public DarkToolStripProgressBar()
            : base(new DarkInnerProgressBar())
        {
            _bar = (DarkInnerProgressBar)Control;
            Size = new Size(120, 14);
        }

        [DefaultValue(0)]
        public int Value
        {
            get { return _bar.Value; }
            set { _bar.Value = value; }
        }

        [DefaultValue(0)]
        public int Minimum
        {
            get { return _bar.Minimum; }
            set { _bar.Minimum = value; }
        }

        [DefaultValue(100)]
        public int Maximum
        {
            get { return _bar.Maximum; }
            set { _bar.Maximum = value; }
        }

        [DefaultValue(ProgressBarStyle.Blocks)]
        public ProgressBarStyle Style
        {
            get { return _bar.Style; }
            set { _bar.Style = value; }
        }

        [DefaultValue(100)]
        public int MarqueeAnimationSpeed
        {
            get { return _bar.MarqueeAnimationSpeed; }
            set { _bar.MarqueeAnimationSpeed = value; }
        }

        [DefaultValue(10)]
        public int Step
        {
            get { return _bar.Step; }
            set { _bar.Step = value; }
        }

        [DefaultValue(true)]
        public bool SmoothTransition
        {
            get { return _bar.SmoothTransition; }
            set { _bar.SmoothTransition = value; }
        }

        [DefaultValue(240)]
        public int TransitionDuration
        {
            get { return _bar.TransitionDuration; }
            set { _bar.TransitionDuration = value; }
        }

        public bool Marquee
        {
            get { return Style == ProgressBarStyle.Marquee; }
            set { Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks; }
        }

        public void Increment(int value)
        {
            _bar.Increment(value);
        }

        public void PerformStep()
        {
            _bar.PerformStep();
        }

        private sealed class DarkInnerProgressBar : ProgressBar
        {
            private bool _disposed;
            private readonly Timer _marqueeTimer = new Timer { Interval = 15 };
            private readonly Stopwatch _marqueeWatch = Stopwatch.StartNew();
            private readonly ThemeMotion _progressMotion;
            private int _marqueePos;
            private bool _smoothTransition = true;
            private int _transitionDuration = 240;

            // Marquee speed: 0.15px per ms ≈ 150px/s.
            private const double MarqueeSpeed = 0.15;

            public DarkInnerProgressBar()
            {
                // Paint entirely through WinForms' buffered pipeline
                // (UserPaint + double buffer = one full-surface flip per
                // frame, no erase flash); the native bar never paints.
                // Subscription deferred to OnHandleCreated — subscribing
                // here would run during the ToolStripControlHost base()
                // call chain and crash AddRange.
                SetStyle(ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.ResizeRedraw, true);
                _progressMotion = new ThemeMotion(this, Invalidate);
                // WinForms Timer drives the cadence (WM_TIMER wakes the
                // message pump); the position itself is time-based (see
                // OnMarqueeTick) so tick jitter never accumulates.
                _marqueeTimer.Tick += OnMarqueeTick;
            }

            public bool SmoothTransition
            {
                get { return _smoothTransition; }
                set
                {
                    if (_smoothTransition == value) return;
                    _smoothTransition = value;
                    if (!value) SnapProgressToValue();
                    Invalidate();
                }
            }

            public int TransitionDuration
            {
                get { return _transitionDuration; }
                set { _transitionDuration = Math.Max(1, value); }
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                ThemeManager.ThemeChanged += OnThemeChanged;
                SnapProgressToValue();
                SyncMarquee();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && !_disposed)
                {
                    _disposed = true;
                    ThemeManager.ThemeChanged -= OnThemeChanged;
                    _marqueeTimer?.Dispose();
                    _progressMotion.Dispose();
                }
                base.Dispose(disposing);
            }

            private void OnThemeChanged(object sender, EventArgs e)
            {
                if (!IsHandleCreated) return;
                Invalidate();
            }

            protected override void OnStyleChanged(EventArgs e)
            {
                base.OnStyleChanged(e);
                SyncMarquee();
            }

            private void SyncMarquee()
            {
                if (Style == ProgressBarStyle.Marquee && IsHandleCreated && !DesignMode)
                {
                    // Restart the clock so the block enters from the left.
                    _marqueeWatch.Restart();
                    _marqueeTimer.Start();
                    Invalidate();
                }
                else
                {
                    _marqueeTimer.Stop();
                    SnapProgressToValue();
                }
            }

            // Timer cadence, but the position is TIME-BASED: a late tick
            // advances the block by exactly the right distance, so irregular
            // WM_TIMER scheduling never shows up as irregular motion.
            private void OnMarqueeTick(object sender, EventArgs e)
            {
                if (Style != ProgressBarStyle.Marquee || !IsHandleCreated || DesignMode)
                    return;

                int width = ClientSize.Width;
                if (width <= 0) return;

                int blockW = Math.Max(20, width / 4);
                int total = Math.Max(1, width - 2 + blockW); // seamless wrap: block fully off both edges only at the loop point
                int pos = (int)(_marqueeWatch.ElapsedMilliseconds * MarqueeSpeed) % total;
                if (pos == _marqueePos) return; // nothing moved — skip the repaint
                _marqueePos = pos;
                Invalidate();
            }

            // Fully painted in OnPaint — no background clearing needed.
            protected override void OnPaintBackground(PaintEventArgs e) { }

            protected override void OnPaint(PaintEventArgs e)
            {
                PaintBar(e.Graphics);
            }

            // The native bar must never paint itself: WM_ERASEBKGND is
            // swallowed, and WM_PRINTCLIENT / WM_PRINT (used by
            // ToolStripControlHost when repainting the strip) get the
            // themed paint into the provided HDC.
            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case 0x0014: // WM_ERASEBKGND — already fully painted
                        m.Result = (IntPtr)1;
                        return;
                    case 0x0313: // WM_PRINTCLIENT — strip buffer repaint
                    case 0x0317: // WM_PRINT
                        if (m.WParam != IntPtr.Zero)
                        {
                            using (var g = Graphics.FromHdc(m.WParam))
                                PaintBar(g);
                        }
                        m.Result = IntPtr.Zero;
                        return;
                    default:
                        base.WndProc(ref m);
                        return;
                }
            }

            private void PaintBar(Graphics g)
            {
                var rect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);

                // Track — recessed: darker inner edge line at the top
                using (var bg = new SolidBrush(Colors.MediumBackground))
                    g.FillRectangle(bg, rect);
                using (var p = new Pen(Colors.DarkBackground))
                    g.DrawLine(p, rect.Left, rect.Top, rect.Right - 1, rect.Top);

                if (Style == ProgressBarStyle.Marquee)
                {
                    // Single block, seamless loop: the cycle is
                    // innerWidth + blockW, so the block is fully off-screen
                    // only at the exact wrap instant. Clipping is exact on
                    // both edges — it grows in from the left and shrinks out
                    // the right, with no dead period.
                    int blockW = Math.Max(20, rect.Width / 4);
                    int inner = rect.Width - 2;
                    int total = Math.Max(1, inner + blockW);
                    int x = _marqueePos - blockW;
                    int visLeft = Math.Max(x, 0);
                    int visRight = Math.Min(x + blockW, inner);
                    if (visRight > visLeft)
                    {
                        // Solid block — plain theme accent, no edge blending.
                        using (var b = new SolidBrush(Colors.BlueSelection))
                            g.FillRectangle(b, new Rectangle(visLeft + 1, rect.Top + 1,
                                visRight - visLeft, rect.Height - 2));
                    }
                }
                else
                {
                    float pct = DisplayedProgress();
                    int w = (int)((rect.Width - 2) * pct);
                    if (w > 0)
                    {
                        // Disabled keeps the theme fill — same accent as
                        // enabled, so the bar never goes grey.
                        DrawFill(g, new Rectangle(rect.Left + 1, rect.Top + 1,
                            w, rect.Height - 2));
                    }
                }

                // Border
                using (var p = new Pen(Colors.DarkBorder))
                    g.DrawRectangle(p, rect.Left, rect.Top, rect.Width - 1, rect.Height - 1);
            }

            private float TargetProgress()
            {
                int range = Maximum - Minimum;
                return range <= 0 ? 0f : Math.Clamp((Value - Minimum) / (float)range, 0f, 1f);
            }

            private float DisplayedProgress()
            {
                float target = TargetProgress();
                if (!_smoothTransition || DesignMode)
                {
                    if (Math.Abs(_progressMotion.Value - target) > 0.0001d)
                        _progressMotion.SnapTo(target);
                }
                else
                {
                    _progressMotion.AnimateTo(target, _transitionDuration);
                }
                return (float)_progressMotion.Value;
            }

            private void SnapProgressToValue()
            {
                _progressMotion.SnapTo(TargetProgress());
            }

            // Slightly 3D fill: top-to-bottom gradient (lighter at top) plus
            // a glossy highlight line along the top edge.
            private static void DrawFill(Graphics g, Rectangle r)
            {
                using (var grad = new LinearGradientBrush(r, Colors.BlueHighlight, Colors.BlueSelection, LinearGradientMode.Vertical))
                    g.FillRectangle(grad, r);
                using (var gloss = new Pen(Color.FromArgb(70, 255, 255, 255)))
                    g.DrawLine(gloss, r.Left, r.Top, r.Right - 1, r.Top);
            }
        }
    }
}
