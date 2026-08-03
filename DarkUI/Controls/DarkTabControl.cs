using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    public class DarkTabControl : TabControl
    {
        private int _hoveredTab = -1;

        public DarkTabControl()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint, true);

            base.BackColor = Colors.GreyBackground;
            base.DrawMode = TabDrawMode.OwnerDrawFixed;
            base.SizeMode = TabSizeMode.Fixed;
            base.ItemSize = new Size(160, 28);
            Padding = new Point(0, 0);
            AllowDrop = true;
        }

        #region Hidden Properties

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new TabDrawMode DrawMode
        {
            get => TabDrawMode.OwnerDrawFixed;
            set => base.DrawMode = TabDrawMode.OwnerDrawFixed;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new TabSizeMode SizeMode
        {
            get => TabSizeMode.Fixed;
            set => base.SizeMode = TabSizeMode.Fixed;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color BackColor
        {
            get => Colors.GreyBackground;
            set => base.BackColor = Colors.GreyBackground;
        }

        #endregion

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (e.Control is TabPage page)
            {
                page.UseVisualStyleBackColor = false;
                page.TextChanged += (_, _) => AutoSizeTabs();
            }
            AutoSizeTabs();
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);
            AutoSizeTabs();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AutoSizeTabs();
        }

        private void AutoSizeTabs()
        {
            if (TabCount == 0 || !IsHandleCreated) return;

            int maxW = 80; // minimum
            using (var g = CreateGraphics())
            {
                for (int i = 0; i < TabCount; i++)
                {
                    var sz = g.MeasureString(TabPages[i].Text, Font);
                    int w = (int)sz.Width + 32; // 16px padding each side
                    if (w > maxW) maxW = w;
                }
            }
            if (ItemSize.Width != maxW)
                ItemSize = new Size(maxW, ItemSize.Height);
            Invalidate();
        }

        #region State

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int prev = _hoveredTab;
            _hoveredTab = -1;
            for (int i = 0; i < TabCount; i++)
            {
                if (GetTabRect(i).Contains(e.Location))
                {
                    _hoveredTab = i;
                    break;
                }
            }
            if (_hoveredTab != prev) Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredTab = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_hoveredTab >= 0) Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            Invalidate();
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }

        #endregion

        #region Paint

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var tabHeight = ItemSize.Height;

            // --- Fill the entire tab header row background ---
            var headerRect = new Rectangle(0, 0, Width, tabHeight);
            using (var brush = new SolidBrush(Colors.GreyBackground))
                g.FillRectangle(brush, headerRect);

            // --- Draw each tab ---
            for (int i = 0; i < TabCount; i++)
            {
                var tabRect = GetTabRect(i);
                bool active = i == SelectedIndex;
                bool hovered = i == _hoveredTab && !active;

                Color fill, text, border;
                if (active)
                {
                    fill = Colors.LighterBackground;
                    text = Colors.LightText;
                    border = Colors.LightestBackground;
                }
                else if (hovered)
                {
                    fill = Colors.LightBackground;
                    text = Colors.LightText;
                    border = Colors.GreyHighlight;
                }
                else
                {
                    fill = Colors.MediumBackground;
                    text = Colors.LightText;
                    border = Colors.LightBorder;
                }

                using (var brush = new SolidBrush(fill))
                    g.FillRectangle(brush, new Rectangle(tabRect.Left, tabRect.Top, tabRect.Width - 1, tabRect.Height - 1));

                using (var pen = new Pen(border))
                    g.DrawRectangle(pen, tabRect.Left, tabRect.Top, tabRect.Width - 1, tabRect.Height - 1);

                // Tab text
                var textRect = new Rectangle(tabRect.Left + 8, tabRect.Top,
                    tabRect.Width - 16, tabRect.Height);
                using (var brush = new SolidBrush(text))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter,
                        FormatFlags = StringFormatFlags.NoWrap
                    };
                    g.DrawString(TabPages[i].Text, Font, brush, textRect, sf);
                }
            }

            // --- Content area background ---
            var bodyRect = new Rectangle(0, tabHeight, Width, Height - tabHeight);
            using (var brush = new SolidBrush(Colors.GreyBackground))
                g.FillRectangle(brush, bodyRect);

            // Content border — matches DataGridView outline color
            using (var pen = new Pen(Colors.LightBorder))
                g.DrawRectangle(pen, bodyRect.Left, bodyRect.Top, bodyRect.Width - 1, bodyRect.Height - 1);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Absorbed — handled in OnPaint
        }

        #endregion

    }
}
