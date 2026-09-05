using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A dark-themed calendar. Wraps MonthCalendar with the dark palette applied.
    /// </summary>
    public class DarkCalendar : UserControl
    {
        private readonly MonthCalendar _calendar = new MonthCalendar();

        public event DateRangeEventHandler DateChanged;

        [Category("Behavior")]
        [Browsable(false)]
        public DateTime SelectionStart
        {
            get { return _calendar.SelectionStart; }
            set { _calendar.SelectionStart = value; }
        }

        [Category("Behavior")]
        [Browsable(false)]
        public DateTime SelectionEnd
        {
            get { return _calendar.SelectionEnd; }
            set { _calendar.SelectionEnd = value; }
        }

        [Category("Behavior")]
        public DateTime TodayDate
        {
            get { return _calendar.TodayDate; }
            set { _calendar.TodayDate = value; }
        }

        [Category("Behavior")]
        [DefaultValue(1)]
        public int MaxSelectionCount
        {
            get { return _calendar.MaxSelectionCount; }
            set { _calendar.MaxSelectionCount = value; }
        }

        public DarkCalendar()
        {
            BackColor = Colors.GreyBackground;
            ApplyCalendarColors();
            ThemeManager.ThemeChanged += OnThemeChanged;

            _calendar.DateChanged += (s, e) => DateChanged?.Invoke(this, e);

            Controls.Add(_calendar);
            _calendar.Dock = DockStyle.Fill;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            ApplyCalendarColors();
            Invalidate(true);
        }

        private void ApplyCalendarColors()
        {
            // Dark theme colors (MonthCalendar exposes a subset of themed colors)
            _calendar.BackColor = Colors.GreyBackground;
            _calendar.ForeColor = Colors.LightText;
            _calendar.TitleBackColor = Colors.MediumBackground;
            _calendar.TitleForeColor = Colors.LightText;
            _calendar.TrailingForeColor = Colors.DisabledText;
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
    }
}
