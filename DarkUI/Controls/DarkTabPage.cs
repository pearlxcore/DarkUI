using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DarkUI.Config;

namespace DarkUI.Controls
{
    /// <summary>
    /// DarkUI's editable tab-page surface. It supports the standard WinForms
    /// TabPage API, page-level designer commands, and order-independent docking
    /// for one direct Dock=Fill child.
    /// </summary>
    [Designer(typeof(DarkTabPageDesigner))]
    public class DarkTabPage : TabPage
    {
        private bool _normalizingDockOrder;

        public DarkTabPage()
        {
            Padding = Padding.Empty;
            UseVisualStyleBackColor = false;
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
            BackColor = Colors.GreyBackground;
            Invalidate();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            NormalizeDockedFillControl();
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);
            NormalizeDockedFillControl();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            NormalizeDockedFillControl();
            base.OnLayout(levent);
        }

        private void NormalizeDockedFillControl()
        {
            if (_normalizingDockOrder)
                return;

            Control fillControl = null;
            foreach (Control control in Controls)
            {
                if (control.Dock != DockStyle.Fill)
                    continue;

                // Multiple Fill children deliberately overlap in WinForms.
                if (fillControl != null)
                    return;

                fillControl = control;
            }

            if (fillControl == null || Controls.GetChildIndex(fillControl) == 0)
                return;

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
}
