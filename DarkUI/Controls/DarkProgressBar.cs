using DarkUI.Config;
using DarkUI.Animation;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    public enum DarkProgressBarMode
    {
        NoText,
        Percentage,
        XOfN
    }

    public class DarkProgressBar : ProgressBar
    {
        private static readonly StringFormat _textAlignment = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        private DarkProgressBarMode _textMode = DarkProgressBarMode.Percentage;
        private readonly ThemeMotion _progressMotion;
        private bool _smoothTransition = true;
        private int _transitionDuration = 240;

        [Category("Appearance")]
        [DefaultValue(DarkProgressBarMode.Percentage)]
        public DarkProgressBarMode TextMode
        {
            get { return _textMode; }
            set
            {
                if (value == _textMode)
                    return;
                _textMode = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Font Font { get { return base.Font; } set { base.Font = value; } }

        /// <summary>
        /// Smoothly animates the visible fill to each new Value. Value itself
        /// changes immediately, so completion checks are never delayed.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(true)]
        public bool SmoothTransition
        {
            get { return _smoothTransition; }
            set
            {
                if (_smoothTransition == value)
                    return;
                _smoothTransition = value;
                if (!value)
                    SnapProgressToValue();
                Invalidate();
            }
        }

        /// <summary>Duration, in milliseconds, of a determinate fill transition.</summary>
        [Category("Behavior")]
        [DefaultValue(240)]
        public int TransitionDuration
        {
            get { return _transitionDuration; }
            set { _transitionDuration = Math.Max(1, value); }
        }

        private bool _marquee;
        private float _marqueePosition;
        private System.Windows.Forms.Timer _marqueeTimer;

        /// <summary>
        /// Indeterminate mode: a fixed-width segment travels across the bar
        /// (a true marquee - nothing fills from 0 to 100, Value is never
        /// used). Opt-in; determinate Value-based rendering is unchanged
        /// while this is false.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(false)]
        public bool Marquee
        {
            get { return _marquee; }
            set
            {
                if (_marquee == value)
                    return;
                _marquee = value;
                if (_marquee)
                {
                    _marqueePosition = -SegmentWidth(); // enter from the left
                    StartMarqueeTimer();
                }
                else
                {
                    StopMarqueeTimer();
                }
                Invalidate();
            }
        }

        /// <summary>Marquee frame interval in milliseconds (default 30).</summary>
        [Category("Appearance")]
        [DefaultValue(30)]
        public int MarqueeAnimationSpeed { get; set; } = 30;

        private float SegmentWidth()
        {
            return Math.Max(16f, ClientSize.Width * 0.25f);
        }

        private void StartMarqueeTimer()
        {
            if (_marqueeTimer != null)
                return;
            _marqueeTimer = new System.Windows.Forms.Timer { Interval = Math.Max(1, MarqueeAnimationSpeed) };
            _marqueeTimer.Tick += (_, _) =>
            {
                if (IsDisposed || !_marquee)
                    return;
                _marqueePosition += 2f;
                // The segment travels from -width to past the right edge and
                // only then wraps - the next one enters after the previous
                // fully exited, so there is no visible jump/reset.
                if (_marqueePosition > ClientSize.Width)
                    _marqueePosition = -SegmentWidth();
                Invalidate();
            };
            _marqueeTimer.Start();
        }

        private void StopMarqueeTimer()
        {
            if (_marqueeTimer == null)
                return;
            _marqueeTimer.Stop();
            _marqueeTimer.Dispose();
            _marqueeTimer = null;
        }

        public DarkProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            _progressMotion = new ThemeMotion(this, Invalidate);
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SnapProgressToValue();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.ThemeChanged -= OnThemeChanged;
                StopMarqueeTimer();
                _progressMotion.Dispose();
            }
            base.Dispose(disposing);
        }

        // The WndProc paint reads Colors.* live, so a theme change only
        // needs a repaint to pick the new palette up.
        private void OnThemeChanged(object sender, EventArgs e) => Invalidate();

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Prevent progress bar flickering
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            // ProgressBar is native-backed. Letting the native control paint
            // before the DarkUI drawing pass briefly exposes its light theme,
            // especially during frequent Value updates. Own the paint message
            // completely so each progress change has one double-buffered pass.
            if (m.Msg == 0x000F)
            {
                using Graphics g = Graphics.FromHwnd(Handle);
                PaintProgress(g);
                m.Result = IntPtr.Zero;
                return;
            }

            // The control paints its entire client area for WM_PAINT, so the
            // separate erase pass only produces a visible background flash.
            if (m.Msg == 0x0014)
            {
                m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);
        }

        private void PaintProgress(Graphics g)
        {
            var rect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
            g.Clear(Colors.MediumBackground);

            // Indeterminate marquee: a fixed-width segment travels across
            // the bar while Value remains unchanged.
            if (_marquee)
            {
                float segWidth = SegmentWidth();
                var segRect = new Rectangle((int)_marqueePosition, rect.Top + 2, (int)segWidth, rect.Height - 4);
                using (var grad = new LinearGradientBrush(segRect, Colors.BlueHighlight, Colors.BlueSelection, LinearGradientMode.Vertical))
                    g.FillRectangle(grad, segRect);
                using (var gloss = new Pen(Color.FromArgb(70, 255, 255, 255)))
                    g.DrawLine(gloss, segRect.Left, segRect.Top, segRect.Right - 1, segRect.Top);
            }
            else
            {
                float percentage = DisplayedProgress();
                var fillRect = new Rectangle(rect.Left + 2, rect.Top + 2, (int)((rect.Width - 4) * percentage), rect.Height - 4);
                if (fillRect.Width > 0)
                {
                    using (var grad = new LinearGradientBrush(fillRect, Colors.BlueHighlight, Colors.BlueSelection, LinearGradientMode.Vertical))
                        g.FillRectangle(grad, fillRect);
                    using (var gloss = new Pen(Color.FromArgb(70, 255, 255, 255)))
                        g.DrawLine(gloss, fillRect.Left, fillRect.Top, fillRect.Right - 1, fillRect.Top);
                }
            }

            using (var p = new Pen(Colors.DarkBorder))
                g.DrawRectangle(p, new Rectangle(rect.Left, rect.Top, rect.Width - 1, rect.Height - 1));

            if (!_marquee && _textMode != DarkProgressBarMode.NoText)
            {
                float percentage = (Value - Minimum) / (float)(Maximum - Minimum);
                using (var b = new SolidBrush(Enabled ? Colors.LightText : Colors.DisabledText))
                {
                    switch (_textMode)
                    {
                        case DarkProgressBarMode.Percentage:
                            g.DrawString(float.IsNaN(percentage) ? "N/A" : Math.Round(percentage * 100) + "%", Font, b, ClientRectangle, _textAlignment);
                            break;

                        case DarkProgressBarMode.XOfN:
                            g.DrawString((Minimum == 0 ? Value + 1 : Value) + " / " + (Minimum == 0 ? Maximum + 1 : Maximum), Font, b, ClientRectangle, _textAlignment);
                            break;

                        default:
                            throw new NotImplementedException("Text mode: " + _textMode);
                    }
                }
            }
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
    }
}
