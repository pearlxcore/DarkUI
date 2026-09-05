using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A tooltip that renders with the dark theme instead of the default light balloon.
    /// </summary>
    public class DarkToolTip : ToolTip
    {
        public DarkToolTip()
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
            SetStyle();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e) => SetStyle();

        private void SetStyle()
        {
            try
            {
                OwnerDraw = true;
                ForeColor = Colors.LightText;
                BackColor = Colors.MediumBackground;
                // Remove first so re-applying on a theme change never double-subscribes.
                Draw -= DarkToolTip_Draw;
                Draw += DarkToolTip_Draw;
                Popup -= DarkToolTip_Popup;
                Popup += DarkToolTip_Popup;
            }
            catch
            {
                // OwnerDraw unsupported on this platform — fall back to default.
            }
        }

        // ── Designer freeze guard ─────────────────────────────────────
        // Theme-owned colors must not be serialized by the designer — a
        // frozen value in Designer.cs would stop the tooltip from
        // following ThemeManager.
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color BackColor
        {
            get => Colors.MediumBackground;
            set => base.BackColor = Colors.MediumBackground;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color ForeColor
        {
            get => Colors.LightText;
            set => base.ForeColor = Colors.LightText;
        }

        private void DarkToolTip_Popup(object sender, PopupEventArgs e)
        {
            using (var g = e.AssociatedControl?.CreateGraphics())
            {
                var size = g?.MeasureString(GetToolTip(e.AssociatedControl), e.AssociatedControl?.Font ?? SystemFonts.DefaultFont);
                if (size.HasValue)
                {
                    e.ToolTipSize = new Size((int)size.Value.Width + 16, (int)size.Value.Height + 8);
                }
            }
        }

        private void DarkToolTip_Draw(object sender, DrawToolTipEventArgs e)
        {
            e.DrawBackground();
            e.Graphics.FillRectangle(new SolidBrush(Colors.MediumBackground), e.Bounds);
            using (var pen = new Pen(Colors.LightBorder))
                e.Graphics.DrawRectangle(pen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
            TextRenderer.DrawText(e.Graphics, e.ToolTipText, e.Font,
                new Rectangle(e.Bounds.X + 6, e.Bounds.Y + 2, e.Bounds.Width - 12, e.Bounds.Height - 4),
                Colors.LightText, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
