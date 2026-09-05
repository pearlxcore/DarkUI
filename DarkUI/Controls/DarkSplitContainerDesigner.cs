using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.DotNet.DesignTools.Designers;
using Microsoft.DotNet.DesignTools.Designers.Behaviors;

namespace DarkUI.Controls
{
    /// <summary>
    /// Provides a small design-time hit area for selecting a split container
    /// whose panes otherwise cover its entire client surface.
    /// </summary>
    public class DarkSplitContainerDesigner : ParentControlDesigner
    {
        private BehaviorService _behaviorService;
        private Adorner _selectionAdorner;
        private DesignerVerbCollection _verbs;

        private DarkSplitContainer SplitContainer => (DarkSplitContainer)Component;

        public override DesignerVerbCollection Verbs
        {
            get
            {
                if (_verbs == null)
                {
                    _verbs = new DesignerVerbCollection
                    {
                        CreateVerb("Add Container", OnAddPane),
                        CreateVerb("Make Panes Equal", OnMakePanesEqual)
                    };
                }

                return _verbs;
            }
        }

        public override void Initialize(IComponent component)
        {
            base.Initialize(component);

            _behaviorService = (BehaviorService)GetService(typeof(BehaviorService));
            if (_behaviorService == null)
                return;

            _selectionAdorner = new Adorner();
            _selectionAdorner.Glyphs.Add(new SplitContainerSelectionGlyph(this, _behaviorService));
            _behaviorService.Adorners.Add(_selectionAdorner);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _behaviorService != null && _selectionAdorner != null)
                _behaviorService.Adorners.Remove(_selectionAdorner);

            _selectionAdorner = null;
            _behaviorService = null;
            base.Dispose(disposing);
        }

        // A direct child of the split container is a pane. Once that pane is
        // selected, toolbox drops belong inside it so normal Panel docking
        // (Top/Bottom/Fill, etc.) works as expected instead of creating a
        // further split pane.
        protected override Control GetParentForComponent(IComponent component)
        {
            var selection = (ISelectionService)GetService(typeof(ISelectionService));
            if (selection?.PrimarySelection is Control selectedPane
                && ReferenceEquals(selectedPane.Parent, SplitContainer)
                && IsDirectPane(selectedPane))
            {
                return selectedPane;
            }

            return base.GetParentForComponent(component);
        }

        private bool IsDirectPane(Control control)
        {
            foreach (Control pane in SplitContainer.Panels)
            {
                if (ReferenceEquals(pane, control))
                    return true;
            }

            return false;
        }

        private void OnAddPane(object sender, EventArgs e)
        {
            AddSplitPane();
        }

        internal void AddSplitPane()
        {
            var host = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (host == null)
                return;

            using (var transaction = host.CreateTransaction("Add split container pane"))
            {
                var pane = (DarkSplitPane)host.CreateComponent(typeof(DarkSplitPane), UniqueName(host, "splitPane"));
                var change = (IComponentChangeService)GetService(typeof(IComponentChangeService));
                change?.OnComponentChanging(SplitContainer, null);
                SplitContainer.Controls.Add(pane);
                change?.OnComponentChanged(SplitContainer, null, null, null);

                MakePanesEqual(change);
                ((ISelectionService)GetService(typeof(ISelectionService)))?.SetSelectedComponents(new object[] { pane });
                transaction.Commit();
            }
        }

        private void OnMakePanesEqual(object sender, EventArgs e)
        {
            MakeAllPanesEqual();
        }

        internal void MakeAllPanesEqual()
        {
            var host = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (host == null || SplitContainer.Panels.Count == 0)
                return;

            using (var transaction = host.CreateTransaction("Make split panes equal"))
            {
                MakePanesEqual((IComponentChangeService)GetService(typeof(IComponentChangeService)));
                transaction.Commit();
            }
        }

        private void MakePanesEqual(IComponentChangeService change)
        {
            int count = SplitContainer.Panels.Count;
            if (count == 0)
                return;

            bool vertical = SplitContainer.Orientation == DarkSplitContainer.DarkSplitOrientation.Vertical;
            int primary = vertical ? SplitContainer.ClientSize.Width : SplitContainer.ClientSize.Height;
            int cross = vertical ? SplitContainer.ClientSize.Height : SplitContainer.ClientSize.Width;
            int available = Math.Max(1, primary - SplitContainer.SplitterWidth * (count - 1));
            int each = Math.Max(1, available / count);
            int position = 0;

            foreach (Control pane in SplitContainer.Panels)
            {
                int size = pane == SplitContainer.Panels[SplitContainer.Panels.Count - 1]
                    ? Math.Max(1, available - position)
                    : each;
                Rectangle oldBounds = pane.Bounds;
                Rectangle newBounds = vertical
                    ? new Rectangle(position, 0, size, cross)
                    : new Rectangle(0, position, cross, size);

                change?.OnComponentChanging(pane, null);
                pane.Bounds = newBounds;
                change?.OnComponentChanged(pane, null, oldBounds, newBounds);
                position += size + SplitContainer.SplitterWidth;
            }

            SplitContainer.PerformLayout();
            SplitContainer.Invalidate();
        }

        private static string UniqueName(IDesignerHost host, string prefix)
        {
            int n = 1;
            while (host.Container.Components[prefix + n] != null)
                n++;
            return prefix + n;
        }

        private sealed class SplitContainerSelectionGlyph : Glyph
        {
            private const int HitBorderWidth = 4;
            private readonly DarkSplitContainerDesigner _designer;
            private readonly BehaviorService _behaviorService;

            public SplitContainerSelectionGlyph(
                DarkSplitContainerDesigner designer,
                BehaviorService behaviorService)
                : base(new SplitContainerSelectionBehavior(designer))
            {
                _designer = designer;
                _behaviorService = behaviorService;
            }

            public override Rectangle Bounds => _behaviorService.ControlRectInAdornerWindow((Control)_designer.Component);

            public override Cursor GetHitTest(Point point)
            {
                return GetSelectionTarget(point) != null ? Cursors.SizeAll : null;
            }

            public IComponent GetSelectionTarget(Point point)
            {
                // The outer four pixels select the split container itself.
                if (IsOnBorder(Bounds, point))
                    return _designer.Component;

                // Each direct child is a pane. Its four-pixel frame is a
                // design-time-only selection target, leaving its centre free
                // for selecting and editing the pane's own child controls.
                foreach (Control pane in _designer.SplitContainer.Panels)
                {
                    Rectangle paneBounds = _behaviorService.ControlRectInAdornerWindow(pane);
                    if (IsOnBorder(paneBounds, point))
                        return pane;
                }

                return null;
            }

            private static bool IsOnBorder(Rectangle bounds, Point point)
            {
                if (!bounds.Contains(point))
                    return false;

                Rectangle inner = bounds;
                inner.Inflate(-HitBorderWidth, -HitBorderWidth);
                return !inner.Contains(point);
            }

            public override void Paint(PaintEventArgs pe)
            {
                // Input-only: selection handles are supplied by the designer.
            }
        }

        private sealed class SplitContainerSelectionBehavior : Behavior
        {
            private readonly DarkSplitContainerDesigner _designer;

            public SplitContainerSelectionBehavior(DarkSplitContainerDesigner designer)
            {
                _designer = designer;
            }

            public override bool OnMouseDown(Glyph glyph, MouseButtons button, Point mouseLoc)
            {
                if (button != MouseButtons.Left)
                    return false;

                var selectionGlyph = glyph as SplitContainerSelectionGlyph;
                var target = selectionGlyph?.GetSelectionTarget(mouseLoc);
                if (target == null)
                    return false;

                var selection = (ISelectionService)_designer.GetService(typeof(ISelectionService));
                if (selection != null && !ReferenceEquals(selection.PrimarySelection, target))
                    selection.SetSelectedComponents(new[] { target });

                return true;
            }
        }
    }
}
