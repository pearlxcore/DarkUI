using DarkUI.Config;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DarkUI.Renderers
{
    /// <summary>
    /// Shared experimental surface renderer for controls that opt into the
    /// geometry tokens on <see cref="Theme"/>. Legacy controls are unchanged.
    /// </summary>
    public static class ThemeSurfaceRenderer
    {
        public static GraphicsPath CreatePath(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            radius = Math.Max(0, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2f));
            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            var diameter = radius * 2f;
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Creates a surface path with rounded top corners and square bottom corners.</summary>
        public static GraphicsPath CreateTopRoundedPath(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            radius = Math.Max(0, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2f));
            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            var diameter = radius * 2f;
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddLine(bounds.Right, bounds.Top + radius, bounds.Right, bounds.Bottom);
            path.AddLine(bounds.Right, bounds.Bottom, bounds.Left, bounds.Bottom);
            path.AddLine(bounds.Left, bounds.Bottom, bounds.Left, bounds.Top + radius);
            path.CloseFigure();
            return path;
        }

        /// <summary>Draws the canonical outer border after content has been clipped and painted.</summary>
        public static void DrawBorder(Graphics graphics, Rectangle bounds, Color border, Theme theme)
        {
            var scale = graphics.DpiX / 96f;
            var thickness = Math.Max(1f, (theme?.BorderThickness ?? 1) * scale);
            var inset = thickness / 2f;
            var surfaceBounds = new RectangleF(bounds.Left + inset, bounds.Top + inset,
                Math.Max(0, bounds.Width - thickness), Math.Max(0, bounds.Height - thickness));
            using var path = CreatePath(surfaceBounds, (theme?.CornerRadius ?? 0) * scale);
            using var pen = new Pen(border, thickness) { Alignment = PenAlignment.Center };
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawPath(pen, path);
        }

        public static void DrawSurface(Graphics graphics, Rectangle bounds, Color canvas, Color fill, Color border,
            Theme theme, bool elevated = false)
        {
            // One renderer owns the full pixel lifecycle. This matters for custom
            // WinForms controls because a rounded path intentionally leaves corners
            // outside its fill; those pixels must always be initialized to the parent.
            graphics.Clear(canvas);
            if (bounds.Width <= 1 || bounds.Height <= 1)
                return;

            var scale = graphics.DpiX / 96f;
            var radius = (theme?.CornerRadius ?? 0) * scale;
            var thickness = Math.Max(1f, (theme?.BorderThickness ?? 1) * scale);
            var style = theme?.SurfaceStyle ?? ThemeSurfaceStyle.Classic;
            var state = graphics.Save();
            try
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                // The half-stroke inset is the canonical geometry for every layer.
                // It keeps the stroke entirely inside ClientRectangle without an
                // extra right/bottom gap or a clipped anti-aliased edge.
                var penInset = thickness / 2f;
                var inner = new RectangleF(
                    bounds.Left + penInset,
                    bounds.Top + penInset,
                    Math.Max(0, bounds.Width - thickness),
                    Math.Max(0, bounds.Height - thickness));

                using var path = CreatePath(inner, radius);
                using var fillBrush = new SolidBrush(fill);
                using var borderPen = new Pen(border, thickness) { Alignment = PenAlignment.Center };
                graphics.FillPath(fillBrush, path);
                graphics.DrawPath(borderPen, path);

                if (style == ThemeSurfaceStyle.Glass)
                {
                    using var highlight = new Pen(Color.FromArgb(80, Color.White));
                    graphics.SetClip(path);
                    graphics.DrawLine(highlight, inner.Left + radius, inner.Top + 0.5f, inner.Right - radius, inner.Top + 0.5f);
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }
    }
}
