using DarkUI.Config;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>Shared Windows XP Luna surfaces for the opt-in special edition.</summary>
    internal static class WindowsXpLunaRenderer
    {
        internal static bool IsActive => ThemeManager.Active.DepthStyle == ThemeDepthStyle.WindowsXpLuna;

        internal static void DrawSurface(Graphics graphics, Rectangle bounds, Color fill, Color border, bool pressed = false, bool selected = false)
        {
            if (bounds.Width < 2 || bounds.Height < 2) return;
            var rect = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            Color top = Shift(fill, pressed ? -18 : 38);
            Color middle = selected ? Shift(ThemeManager.Active.BlueBackground, 15) : Shift(fill, 10);
            Color bottom = pressed ? Shift(fill, -30) : Shift(fill, -14);
            using (var brush = new LinearGradientBrush(rect, top, bottom, LinearGradientMode.Vertical))
                graphics.FillRectangle(brush, rect);
            using (var highlight = new Pen(Color.FromArgb(170, Color.White)))
                graphics.DrawLine(highlight, rect.Left + 2, rect.Top + 1, rect.Right - 2, rect.Top + 1);
            using (var pen = new Pen(selected ? ThemeManager.Active.BlueHighlight : border))
                graphics.DrawRectangle(pen, rect);
        }

        internal static void DrawBackground(Graphics graphics, Rectangle bounds)
        {
            // Full-opacity White washed out the strip on dark themes; derive the
            // light stop from the theme's base so text stays readable everywhere.
            using var brush = new LinearGradientBrush(bounds, ControlPaint.Light(Colors.GreyBackground, 0.2f), Colors.GreyBackground, LinearGradientMode.Vertical);
            graphics.FillRectangle(brush, bounds);
        }

        private static Color Shift(Color color, int amount) => Color.FromArgb(
            Math.Clamp(color.R + amount, 0, 255), Math.Clamp(color.G + amount, 0, 255), Math.Clamp(color.B + amount, 0, 255));
    }
}
