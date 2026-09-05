using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DarkUI.Forms
{
    public class DarkForm : Form
    {
        #region DWM Title Bar

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // Win10 1809+ supports dark/light title bars; arbitrary caption/text
        // colors require Win11 22H2+.
        private static bool IsDarkModeSupported => Environment.OSVersion.Version.Build >= 17763;
        private static bool IsCaptionColorSupported => Environment.OSVersion.Version.Build >= 22621;

        // COLORREF is 0x00BBGGRR (BGR byte order — NOT Color.ToArgb()).
        private static int ToColorRef(Color c) => c.R | (c.G << 8) | (c.B << 16);

        #endregion
        #region Field Region

        private bool _flatBorder;
        private bool _automaticDockLayout = true;
        private bool _normalizingDockOrder;

        #endregion

        #region Property Region

        [Category("Appearance")]
        [Description("Determines whether a single pixel border should be rendered around the form.")]
        [DefaultValue(false)]
        public bool FlatBorder
        {
            get { return _flatBorder; }
            set
            {
                _flatBorder = value;
                Invalidate();
            }
        }

        [Category("Layout")]
        [Description("Keeps one direct Dock=Fill child behind top, bottom, left, and right docked controls, regardless of the order controls were added.")]
        [DefaultValue(true)]
        public bool AutomaticDockLayout
        {
            get => _automaticDockLayout;
            set
            {
                if (_automaticDockLayout == value) return;
                _automaticDockLayout = value;
                PerformLayout();
            }
        }

        #endregion

        #region Constructor Region

        public DarkForm()
        {
            BackColor = Colors.GreyBackground;
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            ApplyThemeColors();
            if (!DesignMode && IsHandleCreated && IsDarkModeSupported)
                ApplyDarkTitleBar();
        }

        private void ApplyThemeColors()
        {
            BackColor = Colors.GreyBackground;
            Invalidate(true);
        }

        // ── Designer freeze guard ─────────────────────────────────────
        // Theme-owned colors must not be serialized by the designer — a
        // frozen value in Designer.cs would stop the form from following
        // ThemeManager (VS re-emits BackColor into designer.cs on every
        // edit of any property).
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public sealed override Color BackColor
        {
            get => Colors.GreyBackground;
            set => base.BackColor = Colors.GreyBackground;
        }

        #endregion

        #region Paint Region

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            if (!_flatBorder)
                return;

            var g = e.Graphics;

            using (var p = new Pen(Colors.DarkBorder))
            {
                var modRect = new Rectangle(ClientRectangle.Location, new Size(ClientRectangle.Width - 1, ClientRectangle.Height - 1));
                g.DrawRectangle(p, modRect);
            }
        }

        #endregion

        #region Event Handler Region

        protected override void OnLayout(LayoutEventArgs levent)
        {
            NormalizeDockedFillControl();
            base.OnLayout(levent);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (!DesignMode) ApplyThemeColors();

            if (!IsDarkModeSupported)
                return;

            ApplyDarkTitleBar();
        }

        private void ApplyDarkTitleBar()
        {
            // Dark/light mode — supported since Win10 1809
            var useDarkMode = 1;
            DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));

            // Per-window caption/text colors follow the active theme —
            // Win11 22H2+ only; older systems keep the dark-mode default.
            if (!IsCaptionColorSupported)
                return;

            var captionColor = ToColorRef(Colors.DarkBackground);
            DwmSetWindowAttribute(Handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

            var textColor = ToColorRef(Colors.LightText);
            DwmSetWindowAttribute(Handle, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
        }

        private void NormalizeDockedFillControl()
        {
            if (!_automaticDockLayout || _normalizingDockOrder)
                return;

            Control fillControl = null;
            foreach (Control control in Controls)
            {
                if (control.Dock != DockStyle.Fill)
                    continue;

                // Multiple Fill controls deliberately overlap in WinForms;
                // leave that advanced layout untouched.
                if (fillControl != null)
                    return;

                fillControl = control;
            }

            // WinForms lays out docked children back-to-front. Index zero is
            // processed last, so the Fill control receives only the middle
            // area remaining after top/bottom/left/right docked siblings.
            if (fillControl != null && Controls.GetChildIndex(fillControl) != 0)
            {
                try
                {
                    _normalizingDockOrder = true;
                    Controls.SetChildIndex(fillControl, 0);
                }
                finally
                {
                    _normalizingDockOrder = false;
                }
            }
        }

        /// <summary>
        /// Icon type whose sound plays when this form opens modally. Defaults
        /// to None (silent) — only notification dialogs set this, so plain
        /// forms (settings, etc.) no longer beep on every open.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MessageBoxIcon NotificationIcon { get; set; } = MessageBoxIcon.None;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // For modal dialogs (shown via ShowDialog), play the matching
            // notification sound and bring to front so the user notices
            // even when another window is focused.
            if (Modal)
            {
                try { PlayNotificationSound(); } catch { }
                try { Activate(); } catch { }
            }
        }

        private void PlayNotificationSound()
        {
            // Same mapping as DarkToast: Info → Asterisk, Warning → Exclamation,
            // Error → Hand. Question and None stay silent.
            switch (NotificationIcon)
            {
                case MessageBoxIcon.Warning:
                    SystemSounds.Exclamation.Play();
                    break;
                case MessageBoxIcon.Error:
                    SystemSounds.Hand.Play();
                    break;
                case MessageBoxIcon.Information:
                    SystemSounds.Asterisk.Play();
                    break;
            }
        }

        #endregion
    }
}
