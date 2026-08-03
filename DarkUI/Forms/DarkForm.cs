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
        #region DWM Dark Title Bar

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private static bool IsDarkModeSupported => Environment.OSVersion.Version.Build >= 17763;

        #endregion
        #region Field Region

        private bool _flatBorder;

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

        #endregion

        #region Constructor Region

        public DarkForm()
        {
            BackColor = Colors.GreyBackground;
        }

        public sealed override Color BackColor
        {
            get { return base.BackColor; }
            set { base.BackColor = value; }
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

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (!IsDarkModeSupported)
                return;

            ApplyDarkTitleBar();
        }

        private void ApplyDarkTitleBar()
        {
            var useDarkMode = 1;
            DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));

            var captionColor = 0x00303030;
            DwmSetWindowAttribute(Handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // For modal dialogs (shown via ShowDialog), play a sound and
            // bring to front so the user notices even when another window
            // is focused.
            if (Modal)
            {
                try { SystemSounds.Asterisk.Play(); } catch { }
                try { Activate(); } catch { }
            }
        }

        #endregion
    }
}
