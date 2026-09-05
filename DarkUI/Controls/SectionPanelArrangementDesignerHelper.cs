using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    internal static class SectionPanelArrangementDesignerHelper
    {
        internal static void Arrange(
            Control page,
            IComponentChangeService changeService,
            IDesignerHost host,
            string transactionName,
            bool preserveColumns = false)
        {
            var controls = page.Controls
                .Cast<Control>()
                .Where(control => control.Visible && control.Dock == DockStyle.None)
                .OrderBy(control => control.Top)
                .ThenBy(control => control.Left)
                .ToList();

            if (controls.Count == 0)
                return;

            if (preserveColumns)
            {
                ArrangeWithinExistingColumns(page, controls, changeService, host, transactionName);
                return;
            }

            // Preserve the layout the user designed: controls whose original
            // vertical bounds overlap belong to the same row. This lets a
            // two-column page stay two columns instead of becoming a stack.
            // Every direct, non-docked control is a layout block: section
            // panels, group boxes, nested tabs, grids, and normal panels.
            var rows = new List<List<Control>>();
            var rowBottom = int.MinValue;
            foreach (var control in controls)
            {
                if (rows.Count == 0 || control.Top >= rowBottom)
                {
                    rows.Add(new List<Control>());
                    rowBottom = control.Bottom;
                }
                else
                {
                    rowBottom = System.Math.Max(rowBottom, control.Bottom);
                }

                rows[rows.Count - 1].Add(control);
            }

            using (var transaction = host?.CreateTransaction(transactionName))
            {
                var nextTop = page.DisplayRectangle.Top;
                foreach (var row in rows)
                {
                    var nextLeft = page.DisplayRectangle.Left;
                    var nextRowTop = nextTop;

                    foreach (var control in row.OrderBy(control => control.Left))
                    {
                        var boundsProperty = TypeDescriptor.GetProperties(control)["Bounds"];
                        var location = new Point(
                            nextLeft + control.Margin.Left,
                            nextTop + control.Margin.Top);

                        changeService?.OnComponentChanging(control, boundsProperty);
                        control.Location = location;
                        changeService?.OnComponentChanged(control, boundsProperty, null, control.Bounds);

                        nextLeft = control.Right + control.Margin.Right;
                        nextRowTop = System.Math.Max(nextRowTop, control.Bottom + control.Margin.Bottom);
                    }

                    nextTop = nextRowTop;
                }

                transaction?.Commit();
            }
        }

        private static void ArrangeWithinExistingColumns(
            Control page,
            List<Control> controls,
            IComponentChangeService changeService,
            IDesignerHost host,
            string transactionName)
        {
            // A dashboard often has a wide, tall control on the left (such
            // as a nested tab control) and a vertical stack of panels on the
            // right. Those rectangles overlap vertically, so a row-based
            // algorithm sees one giant row. Group by existing horizontal
            // lanes instead, preserving each control's X coordinate and
            // applying margins only to the vertical spacing in that lane.
            var columns = new List<List<Control>>();
            var columnRightEdges = new List<int>();
            foreach (var control in controls.OrderBy(control => control.Left))
            {
                int columnIndex = -1;
                for (int i = 0; i < columns.Count; i++)
                {
                    if (control.Left < columnRightEdges[i])
                    {
                        columnIndex = i;
                        break;
                    }
                }

                if (columnIndex < 0)
                {
                    columns.Add(new List<Control>());
                    columnRightEdges.Add(control.Right);
                    columnIndex = columns.Count - 1;
                }
                else
                {
                    columnRightEdges[columnIndex] = System.Math.Max(columnRightEdges[columnIndex], control.Right);
                }

                columns[columnIndex].Add(control);
            }

            using (var transaction = host?.CreateTransaction(transactionName))
            {
                foreach (var column in columns)
                {
                    int nextTop = page.DisplayRectangle.Top;
                    foreach (var control in column.OrderBy(control => control.Top))
                    {
                        var boundsProperty = TypeDescriptor.GetProperties(control)["Bounds"];
                        var location = new Point(control.Left, nextTop + control.Margin.Top);

                        changeService?.OnComponentChanging(control, boundsProperty);
                        control.Location = location;
                        changeService?.OnComponentChanged(control, boundsProperty, null, control.Bounds);

                        nextTop = control.Bottom + control.Margin.Bottom;
                    }
                }

                transaction?.Commit();
            }
        }
    }
}
