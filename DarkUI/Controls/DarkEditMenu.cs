using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// Builds the default themed edit context menu (Undo/Cut/Copy/Paste/Delete/
    /// Select All) for text-based controls. Uses DarkContextMenu so it follows
    /// the active theme. The app can replace it by assigning ContextMenuStrip.
    /// </summary>
    internal static class DarkEditMenu
    {
        internal static DarkContextMenu Create(TextBoxBase box)
        {
            var menu = new DarkContextMenu();

            var undo = new ToolStripMenuItem("Undo", null, (s, e) => box.Undo()) { ShortcutKeys = Keys.Control | Keys.Z };
            var cut = new ToolStripMenuItem("Cut", null, (s, e) => box.Cut()) { ShortcutKeys = Keys.Control | Keys.X };
            var copy = new ToolStripMenuItem("Copy", null, (s, e) => box.Copy()) { ShortcutKeys = Keys.Control | Keys.C };
            var paste = new ToolStripMenuItem("Paste", null, (s, e) => box.Paste()) { ShortcutKeys = Keys.Control | Keys.V };
            var delete = new ToolStripMenuItem("Delete", null, (s, e) => box.SelectedText = "") { ShortcutKeys = Keys.Delete };
            var selectAll = new ToolStripMenuItem("Select All", null, (s, e) => box.SelectAll()) { ShortcutKeys = Keys.Control | Keys.A };

            menu.Items.Add(undo);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(cut);
            menu.Items.Add(copy);
            menu.Items.Add(paste);
            menu.Items.Add(delete);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(selectAll);

            // Enable/disable commands based on the control state on every open.
            menu.Opening += (s, e) =>
            {
                bool hasSel = box.SelectionLength > 0;
                bool writable = !box.ReadOnly;
                undo.Enabled = box.CanUndo;
                cut.Enabled = hasSel && writable;
                copy.Enabled = hasSel;
                paste.Enabled = writable && Clipboard.ContainsText();
                delete.Enabled = hasSel && writable;
                selectAll.Enabled = box.TextLength > 0;
            };

            return menu;
        }
    }
}
