using DarkUI.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A themed flow-layout container for DarkChip tags. Wraps chips onto
    /// multiple rows as they are added. Typically populated from the checked
    /// items of a DarkCheckedComboBox / DarkCheckedListBox.
    /// </summary>
    public class DarkChipsPanel : FlowLayoutPanel
    {
        public DarkChipsPanel()
        {
            BackColor = Colors.GreyBackground;
            AutoScroll = false;
            FlowDirection = FlowDirection.LeftToRight;
            WrapContents = true;
            Padding = new Padding(2);
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        public DarkChipsPanel(IContainer container) : this()
        {
            container.Add(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object sender, EventArgs e) => BackColor = Colors.GreyBackground;

        /// <summary>
        /// Theme-driven fill, set from Colors.GreyBackground in the
        /// constructor and on ThemeChanged. Hidden from the designer so a
        /// serialized BackColor can never pin the panel to one theme's grey
        /// (VS re-emits the value into designer.cs on every edit). The
        /// setter clamps to the theme color so even a pre-existing frozen
        /// value in designer.cs is a no-op at runtime.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Color BackColor
        {
            get => Colors.GreyBackground;
            set => base.BackColor = Colors.GreyBackground;
        }

        /// <summary>
        /// Creates a chip, wires its remove handler, and adds it to the
        /// panel. Returns the chip.
        /// </summary>
        public DarkChip AddChip(string text, EventHandler removeClicked)
        {
            var chip = new DarkChip { Text = text };
            if (removeClicked != null)
                chip.RemoveClicked += removeClicked;
            Controls.Add(chip);
            return chip;
        }
    }
}
