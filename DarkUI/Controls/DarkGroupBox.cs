using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    [Designer(typeof(DarkGroupBoxDesigner))]
    public class DarkGroupBox : GroupBox
    {
        private Color _borderColor = Colors.LightBorder;

        public DarkGroupBox()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint, true);
            Paint += DarkGroupBox_Paint;
            ForeColor = Colors.LightText;
            BackColor = Colors.GreyBackground;
            Padding = Padding.Empty;
            ResizeRedraw = true;
            DoubleBuffered = true;
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode) ApplyThemeColors();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e) => ApplyThemeColors();

        private void ApplyThemeColors()
        {
            _borderColor = Colors.LightBorder;
            BackColor = Colors.GreyBackground;
            ForeColor = Colors.LightText;
            Invalidate();
        }

        private void DarkGroupBox_Paint(object sender, PaintEventArgs e)
        {
            if (Parent != null)
                e.Graphics.Clear(Parent.BackColor);
            Size tSize = TextRenderer.MeasureText(Text, Font);
            Rectangle borderRect = ClientRectangle;
            borderRect.Y = borderRect.Y + tSize.Height / 2 + 1;
            borderRect.Height = borderRect.Height - tSize.Height / 2 - 1;
            e.Graphics.FillRectangle(new SolidBrush(BackColor), borderRect);
            ControlPaint.DrawBorder(e.Graphics, borderRect, _borderColor, ButtonBorderStyle.Solid);
            Rectangle textRect = ClientRectangle;
            textRect.X = textRect.X + 6;
            textRect.Y += borderRect.Top;
            textRect.Width = tSize.Width + 2;
            textRect.Height = tSize.Height - borderRect.Top;
            e.Graphics.FillRectangle(new SolidBrush(BackColor), textRect);
            textRect = ClientRectangle;
            textRect.X = textRect.X + 8;
            textRect.Width = tSize.Width + 6;
            textRect.Height = tSize.Height + 1;
            e.Graphics.DrawString(Text, Font, new SolidBrush(Enabled ? ForeColor : Colors.DisabledText), textRect);

        }

        [Category("Appearance")]
        [ReadOnly(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor
        {
            get { return _borderColor; }
            set
            {
                _borderColor = value;
                Invalidate(); // causes control to be redrawn
            }
        }

        // ── Designer freeze guard ─────────────────────────────────────
        // Theme-owned colors must not be serialized by the designer — a
        // frozen value in Designer.cs would stop the control from
        // following ThemeManager.
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [ReadOnly(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color BackColor
        {
            get => Colors.GreyBackground;
            set => base.BackColor = Colors.GreyBackground;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [ReadOnly(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color ForeColor
        {
            get => Colors.LightText;
            set => base.ForeColor = Colors.LightText;
        }
    }
}
