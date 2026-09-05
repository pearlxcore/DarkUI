using System.Windows.Forms;

namespace DarkUI.Renderers
{
    /// <summary>
    /// Renderer for DarkStatusStrip. Extends DarkToolStripRenderer so hover,
    /// pressed, checked, separator and arrow rendering match the rest of
    /// DarkUI, but keeps natural (text-sized) button sizing instead of the
    /// toolbar's 24x24 icon sizing.
    /// </summary>
    public class DarkStatusStripRenderer : DarkToolStripRenderer
    {
        protected override void InitializeItem(ToolStripItem item)
        {
            base.InitializeItem(item);

            // DarkToolStripRenderer forces ToolStripButton to AutoSize=false,
            // Size=(24,24) for icon toolbars. Status strips keep natural
            // AutoSize so text buttons size to their caption.
            if (item is ToolStripButton button)
                button.AutoSize = true;
        }
    }
}
