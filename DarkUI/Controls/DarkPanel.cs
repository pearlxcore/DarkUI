using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// Selects which theme token supplies a <see cref="DarkPanel"/> background.
    /// </summary>
    public enum DarkPanelSurface
    {
        GreyBackground,
        HeaderBackground,
        MediumBackground,
        DarkBackground,
        LightBackground
    }

    /// <summary>
    /// A container whose background is owned by <see cref="ThemeManager"/>.
    /// Unlike a plain <see cref="Panel"/>, an explicit <see cref="BackColor"/>
    /// assigned by the designer is ignored (and not serialized), so the panel
    /// always follows the active theme.
    /// </summary>
    public class DarkPanel : Panel
    {
        private DarkPanelSurface _surface = DarkPanelSurface.GreyBackground;

        public DarkPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);
            base.BackColor = ResolveSurface();
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ThemeManager.ThemeChanged -= OnThemeChanged;

            base.Dispose(disposing);
        }

        [Category("Appearance")]
        [Description("Which theme background token this panel uses.")]
        [DefaultValue(DarkPanelSurface.GreyBackground)]
        public DarkPanelSurface Surface
        {
            get => _surface;
            set
            {
                if (_surface == value)
                    return;
                _surface = value;
                base.BackColor = ResolveSurface();
                Invalidate(true);
            }
        }

        // Theme-owned: the value is derived, never stored from the designer.
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Color BackColor
        {
            get => ResolveSurface();
            set => base.BackColor = ResolveSurface();
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            base.BackColor = ResolveSurface();
            Invalidate(true);
        }

        private Color ResolveSurface() => DarkPanelSurfaceColors.Resolve(_surface);
    }

    /// <summary>
    /// Flow-layout companion to <see cref="DarkPanel"/> with a theme-owned
    /// background that follows the active theme.
    /// </summary>
    public class DarkFlowLayoutPanel : FlowLayoutPanel
    {
        private DarkPanelSurface _surface = DarkPanelSurface.GreyBackground;

        public DarkFlowLayoutPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);
            base.BackColor = ResolveSurface();
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ThemeManager.ThemeChanged -= OnThemeChanged;

            base.Dispose(disposing);
        }

        [Category("Appearance")]
        [Description("Which theme background token this panel uses.")]
        [DefaultValue(DarkPanelSurface.GreyBackground)]
        public DarkPanelSurface Surface
        {
            get => _surface;
            set
            {
                if (_surface == value)
                    return;
                _surface = value;
                base.BackColor = ResolveSurface();
                Invalidate(true);
            }
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Color BackColor
        {
            get => ResolveSurface();
            set => base.BackColor = ResolveSurface();
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            base.BackColor = ResolveSurface();
            Invalidate(true);
        }

        private Color ResolveSurface() => DarkPanelSurfaceColors.Resolve(_surface);
    }

    internal static class DarkPanelSurfaceColors
    {
        internal static Color Resolve(DarkPanelSurface surface)
        {
            switch (surface)
            {
                case DarkPanelSurface.HeaderBackground: return Colors.HeaderBackground;
                case DarkPanelSurface.MediumBackground: return Colors.MediumBackground;
                case DarkPanelSurface.DarkBackground: return Colors.DarkBackground;
                case DarkPanelSurface.LightBackground: return Colors.LightBackground;
                default: return Colors.GreyBackground;
            }
        }
    }
}

