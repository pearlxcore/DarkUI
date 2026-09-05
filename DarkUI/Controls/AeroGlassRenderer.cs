using DarkUI.Config;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DarkUI.Controls
{
    /// <summary>Shared Aero Glass Revival surfaces for opt-in DarkUI controls.</summary>
    internal static class AeroGlassRenderer
    {
        internal static bool IsActive => ThemeManager.Active.DepthStyle == ThemeDepthStyle.AeroGlass;

        internal static void DrawSurface(Graphics graphics, Rectangle bounds, Color fill, Color border,
            int radius = 7, bool pressed = false, bool accent = false)
        {
            if (bounds.Width < 2 || bounds.Height < 2) return;

            Rectangle drawBounds = new Rectangle(bounds.X, bounds.Y,
                Math.Max(1, bounds.Width - 1), Math.Max(1, bounds.Height - 1));
            using GraphicsPath path = RoundedRectangle(drawBounds, Math.Min(radius, Math.Min(drawBounds.Width, drawBounds.Height) / 2));

            Color top = Shift(fill, pressed ? -10 : 28);
            Color bottom = Shift(fill, pressed ? -24 : -12);
            using (var brush = new LinearGradientBrush(drawBounds, top, bottom, LinearGradientMode.Vertical))
                graphics.FillPath(brush, path);

            if (!pressed)
            {
                Rectangle sheen = new Rectangle(drawBounds.X + 1, drawBounds.Y + 1,
                    Math.Max(1, drawBounds.Width - 2), Math.Max(1, drawBounds.Height / 2));
                using GraphicsPath sheenPath = RoundedRectangle(sheen, Math.Max(2, radius - 2));
                using var sheenBrush = new LinearGradientBrush(sheen,
                    Color.FromArgb(74, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical);
                graphics.FillPath(sheenBrush, sheenPath);
            }

            using (var outer = new Pen(accent ? ThemeManager.Active.BlueHighlight : border))
                graphics.DrawPath(outer, path);

            if (!pressed)
            {
                using var inner = new Pen(Color.FromArgb(85, Color.White));
                Rectangle innerBounds = Rectangle.Inflate(drawBounds, -1, -1);
                if (innerBounds.Width > 2 && innerBounds.Height > 2)
                {
                    using GraphicsPath innerPath = RoundedRectangle(innerBounds, Math.Max(1, radius - 1));
                    graphics.DrawPath(inner, innerPath);
                }
            }
        }

        internal static void DrawPanelBackground(Graphics graphics, Rectangle bounds)
        {
            using var brush = new LinearGradientBrush(bounds,
                Shift(Colors.GreyBackground, 12), Shift(Colors.GreyBackground, -6), LinearGradientMode.Vertical);
            graphics.FillRectangle(brush, bounds);
        }

        internal static Color Shift(Color color, int amount) => Color.FromArgb(
            Math.Clamp(color.R + amount, 0, 255),
            Math.Clamp(color.G + amount, 0, 255),
            Math.Clamp(color.B + amount, 0, 255));

        private static GraphicsPath RoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = Math.Max(1, radius * 2);
            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
