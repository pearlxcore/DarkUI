using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    internal static class FitToContentsDesignerHelper
    {
        internal static void Fit(
            Control container,
            IComponentChangeService changeService,
            IDesignerHost host,
            string transactionName)
        {
            var children = new List<Control>();
            var childBounds = Rectangle.Empty;

            foreach (Control child in container.Controls)
            {
                if (!child.Visible || child.Dock != DockStyle.None)
                    continue;

                children.Add(child);
                childBounds = childBounds.IsEmpty ? child.Bounds : Rectangle.Union(childBounds, child.Bounds);
            }

            if (children.Count == 0)
                return;

            using (var transaction = host?.CreateTransaction(transactionName))
            {
                var contentBounds = container is DarkSectionPanel sectionPanel
                    ? sectionPanel.GetContentBounds()
                    : container.DisplayRectangle;
                var offset = new Size(contentBounds.Left - childBounds.Left, contentBounds.Top - childBounds.Top);

                foreach (var child in children)
                {
                    var boundsProperty = TypeDescriptor.GetProperties(child)["Bounds"];
                    changeService?.OnComponentChanging(child, boundsProperty);
                    child.Location = new Point(child.Left + offset.Width, child.Top + offset.Height);
                    changeService?.OnComponentChanged(child, boundsProperty, null, child.Bounds);
                }

                var rightInset = Math.Max(0, container.ClientSize.Width - contentBounds.Right);
                var bottomInset = Math.Max(0, container.ClientSize.Height - contentBounds.Bottom);
                var requiredSize = new Size(
                    childBounds.Width + contentBounds.Left + rightInset,
                    childBounds.Height + contentBounds.Top + bottomInset);

                var sizeProperty = TypeDescriptor.GetProperties(container)["Size"];
                changeService?.OnComponentChanging(container, sizeProperty);
                container.Size = requiredSize;
                changeService?.OnComponentChanged(container, sizeProperty, null, requiredSize);
                transaction?.Commit();
            }
        }
    }
}
