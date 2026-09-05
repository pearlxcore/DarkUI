using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Animation
{
    /// <summary>
    /// Lightweight shared UI-thread animation clock for Theme V2 controls.
    /// Each motion invalidates only its owning control; no per-control timers are created.
    /// </summary>
    public sealed class ThemeMotion : IDisposable
    {
        private static readonly List<ThemeMotion> Active = new();
        private static readonly Timer Clock = new() { Interval = 15 };

        private readonly Control _owner;
        private readonly Action _updated;
        private double _from;
        private double _to;
        private long _startedAt;
        private int _duration;
        private bool _isActive;

        static ThemeMotion() => Clock.Tick += (_, _) => Tick();

        public ThemeMotion(Control owner, Action updated)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _updated = updated ?? throw new ArgumentNullException(nameof(updated));
        }

        public double Value { get; private set; }

        public void AnimateTo(double target, int duration = 170)
        {
            target = Math.Clamp(target, 0d, 1d);
            if (_owner.IsDisposed) return;
            if ((_isActive && Math.Abs(_to - target) < 0.0001d) ||
                (!_isActive && Math.Abs(Value - target) < 0.0001d))
                return;

            _from = Value;
            _to = target;
            _duration = Math.Max(1, duration);
            _startedAt = Stopwatch.GetTimestamp();
            if (!_isActive)
            {
                _isActive = true;
                Active.Add(this);
            }
            if (!Clock.Enabled) Clock.Start();
            _updated();
        }

        public void SnapTo(double value)
        {
            Value = Math.Clamp(value, 0d, 1d);
            Stop();
            _updated();
        }

        public void Stop()
        {
            if (!_isActive) return;
            _isActive = false;
            Active.Remove(this);
            if (Active.Count == 0) Clock.Stop();
        }

        public void Dispose() => Stop();

        public static Color Blend(Color from, Color to, double amount)
        {
            amount = Math.Clamp(amount, 0d, 1d);
            return Color.FromArgb(
                (int)Math.Round(from.A + ((to.A - from.A) * amount)),
                (int)Math.Round(from.R + ((to.R - from.R) * amount)),
                (int)Math.Round(from.G + ((to.G - from.G) * amount)),
                (int)Math.Round(from.B + ((to.B - from.B) * amount)));
        }

        private static void Tick()
        {
            var now = Stopwatch.GetTimestamp();
            for (var index = Active.Count - 1; index >= 0; index--)
            {
                var motion = Active[index];
                if (motion._owner.IsDisposed)
                {
                    motion._isActive = false;
                    Active.RemoveAt(index);
                    continue;
                }

                var elapsed = (now - motion._startedAt) * 1000d / Stopwatch.Frequency;
                var progress = Math.Clamp(elapsed / motion._duration, 0d, 1d);
                var eased = 1d - Math.Pow(1d - progress, 3d); // ease-out cubic
                motion.Value = motion._from + ((motion._to - motion._from) * eased);
                motion._updated();
                if (progress >= 1d)
                {
                    motion.Value = motion._to;
                    motion._isActive = false;
                    Active.RemoveAt(index);
                }
            }
            if (Active.Count == 0) Clock.Stop();
        }
    }
}
