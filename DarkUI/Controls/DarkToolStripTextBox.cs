using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A themed ToolStripTextBox. Colours are applied once the control is
    /// hosted on a ToolStrip (OnOwnerChanged), never in the constructor.
    /// </summary>
    public class DarkToolStripTextBox : ToolStripTextBox
    {
        private bool _disposed;

        public DarkToolStripTextBox()
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        public DarkToolStripTextBox(string name) : base(name)
        {
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void OnOwnerChanged(EventArgs e)
        {
            base.OnOwnerChanged(e);
            // Owner becomes non-null once the item is added to a ToolStrip.
            // Before that, the inner TextBox is null — defer colour setup.
            if (Owner != null)
                UpdateColors();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                ThemeManager.ThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e) => UpdateColors();

        private void UpdateColors()
        {
            if (Owner == null) return; // not hosted yet — nothing to colour

            BackColor = Colors.GreyBackground;
            ForeColor = Colors.LightText;

            if (TextBox is { } tb)
            {
                tb.BackColor = Colors.GreyBackground;
                tb.ForeColor = Colors.LightText;
                ApplyEditSubclass(tb);
            }

            Invalidate();
        }

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

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color ForeColor
        {
            get => Colors.LightText;
            set => base.ForeColor = Colors.LightText;
        }

        // ── Disabled-state theming for the hosted edit ──────────────────
        // A disabled native EDIT paints its text with system gray — subclass
        // it and overlay the text with the theme's DisabledText instead.

        private delegate IntPtr EditSubclassProcDelegate(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam,
            UIntPtr uIdSubclass, IntPtr dwRefData);

        [DllImport("comctl32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SetWindowSubclass(IntPtr hWnd, EditSubclassProcDelegate pfnSubclass,
            UIntPtr uIdSubclass, IntPtr dwRefData);

        [DllImport("comctl32.dll")]
        private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam,
            UIntPtr uIdSubclass, IntPtr dwRefData);

        private const int WM_PAINT = 0x000F;

        private readonly EditSubclassProcDelegate _editSubclassProc = EditSubclassProcImpl;
        private IntPtr _subclassedEdit;

        private void ApplyEditSubclass(TextBox tb)
        {
            if (tb.Handle == _subclassedEdit) return;
            if (SetWindowSubclass(tb.Handle, _editSubclassProc, (UIntPtr)3, IntPtr.Zero))
                _subclassedEdit = tb.Handle;
        }

        private static IntPtr EditSubclassProcImpl(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam,
            UIntPtr uIdSubclass, IntPtr dwRefData)
        {
            IntPtr result = DefSubclassProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData);

            if (uMsg == WM_PAINT)
            {
                var tb = Control.FromHandle(hWnd) as TextBox;
                if (tb != null && !tb.Enabled && tb.Text.Length > 0)
                {
                    using (var g = Graphics.FromHwnd(tb.Handle))
                    {
                        var rect = new Rectangle(0, 0, tb.ClientSize.Width, tb.ClientSize.Height);
                        TextRenderer.DrawText(g, tb.Text, tb.Font, rect, Colors.DisabledText,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                    }
                }
            }
            return result;
        }
    }
}
