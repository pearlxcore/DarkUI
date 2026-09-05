using DarkUI.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A breadcrumb navigation bar (segment ▸ segment ▸ segment).
    /// </summary>
    public class DarkBreadcrumbBar : Control
    {
        private readonly List<string> _items = new();
        private int _hoveredIndex = -1;
        private const int SegmentPadding = 8;
        private const int ChevronWidth = 18;

        public event EventHandler<int> SegmentClicked;
        public event EventHandler<int> SegmentDoubleClicked;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<string> Items => _items;

        public DarkBreadcrumbBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
            Height = 28;
            BackColor = Colors.GreyBackground;
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e) => Invalidate();

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

        public void SetItems(IEnumerable<string> items)
        {
            _items.Clear();
            _items.AddRange(items ?? Enumerable.Empty<string>());
            _hoveredIndex = -1;
            Invalidate();
        }

        public void Add(string segment) { _items.Add(segment); Invalidate(); }
        public void Clear() { _items.Clear(); _hoveredIndex = -1; Invalidate(); }

        private List<Rectangle> GetSegmentRects()
        {
            var rects = new List<Rectangle>();
            int x = 4;
            using (var g = CreateGraphics())
            {
                foreach (var item in _items)
                {
                    var w = (int)g.MeasureString(item, Font).Width + SegmentPadding * 2;
                    rects.Add(new Rectangle(x, 0, w, Height));
                    x += w + ChevronWidth;
                }
            }
            return rects;
        }

        private int HitTestIndex(Point pt)
        {
            var rects = GetSegmentRects();
            for (int i = 0; i < rects.Count; i++)
                if (rects[i].Contains(pt))
                    return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int idx = HitTestIndex(e.Location);
            if (idx != _hoveredIndex)
            {
                _hoveredIndex = idx;
                Cursor = idx >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredIndex = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            int idx = HitTestIndex(e.Location);
            if (idx >= 0) SegmentClicked?.Invoke(this, idx);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button != MouseButtons.Left) return;
            int idx = HitTestIndex(e.Location);
            if (idx >= 0) SegmentDoubleClicked?.Invoke(this, idx);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using (var b = new SolidBrush(BackColor))
                g.FillRectangle(b, ClientRectangle);

            var rects = GetSegmentRects();
            for (int i = 0; i < _items.Count; i++)
            {
                var r = rects[i];
                if (i == _hoveredIndex)
                {
                    using (var b = new SolidBrush(Colors.GreySelection))
                        g.FillRectangle(b, r);
                }
                TextRenderer.DrawText(g, _items[i], Font, r, Enabled ? Colors.LightText : Colors.DisabledText,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

                // Chevron separator
                if (i < _items.Count - 1)
                {
                    int cx = r.Right + ChevronWidth / 2 - 3;
                    int cy = Height / 2;
                    using (var pen = new Pen(Colors.DisabledText))
                    {
                        g.DrawLine(pen, cx - 3, cy - 3, cx, cy);
                        g.DrawLine(pen, cx - 3, cy + 3, cx, cy);
                    }
                }
            }
        }
    }
}
