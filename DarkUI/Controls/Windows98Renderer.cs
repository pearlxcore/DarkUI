using DarkUI.Config;
using System.Drawing;

namespace DarkUI.Controls
{
    /// <summary>Classic hard-bevel surfaces for the Windows 98 special edition.</summary>
    internal static class Windows98Renderer
    {
        internal static bool IsActive => ThemeManager.Active.DepthStyle == ThemeDepthStyle.Windows98Classic;

        internal static void DrawSurface(Graphics graphics, Rectangle bounds, Color fill, bool pressed = false, bool selected = false)
        {
            if (bounds.Width < 3 || bounds.Height < 3) return;
            var rect = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            using (var brush = new SolidBrush(selected ? ThemeManager.Active.BlueBackground : fill))
                graphics.FillRectangle(brush, rect);

            Color light = pressed ? Color.FromArgb(80, 80, 80) : Color.White;
            Color shadow = pressed ? Color.White : Color.FromArgb(80, 80, 80);
            using var topLeft = new Pen(light);
            using var bottomRight = new Pen(shadow);
            graphics.DrawLine(topLeft, rect.Left, rect.Bottom, rect.Left, rect.Top);
            graphics.DrawLine(topLeft, rect.Left, rect.Top, rect.Right, rect.Top);
            graphics.DrawLine(bottomRight, rect.Right, rect.Top, rect.Right, rect.Bottom);
            graphics.DrawLine(bottomRight, rect.Right, rect.Bottom, rect.Left, rect.Bottom);
        }
    }
}
