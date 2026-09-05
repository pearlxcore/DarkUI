using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A themed combo box for status strips and tool strips. Hosts a real
    /// DarkComboBox (UserPaint — paints itself inside the host, unlike
    /// ProgressBar which swallows OnPaint). The inner DarkComboBox subscribes
    /// to ThemeManager itself (handle-safe), so the host needs no theme hook;
    /// nothing is subscribed in this constructor for the same reason as
    /// DarkToolStripProgressBar.
    /// Note: the host is 20px tall, so the hosting strip should be at least
    /// 28px high (DarkStatusStrip default content height is 24 - 8 = 16px).
    /// </summary>
    public class DarkToolStripComboBox : ToolStripControlHost
    {
        public DarkToolStripComboBox()
            : base(new DarkComboBox())
        {
            Size = new Size(140, 20);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DarkComboBox ComboBox
        {
            get { return (DarkComboBox)Control; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ComboBox.ObjectCollection Items
        {
            get { return ComboBox.Items; }
        }

        [DefaultValue(-1)]
        public int SelectedIndex
        {
            get { return ComboBox.SelectedIndex; }
            set { ComboBox.SelectedIndex = value; }
        }

        // Mirrors the built-in ToolStripComboBox.Text.
        public override string Text
        {
            get { return ComboBox.Text; }
            set { ComboBox.Text = value; }
        }
    }
}
