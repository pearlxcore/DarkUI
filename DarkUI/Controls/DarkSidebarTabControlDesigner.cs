using System;
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
    /// Designer for DarkSidebarTabControl — design-surface switching,
    /// sidebar resizing, and page layout commands.
    /// </summary>
    public class DarkSidebarTabControlDesigner : ParentControlDesigner
    {
        private DesignerVerbCollection _verbs;
        private BehaviorService _behaviorService;
        private Adorner _sidebarAdorner;
        private DesignerTransaction _resizeTransaction;
        private int _resizeOriginalWidth;

        private DarkSidebarTabControl SidebarControl => (DarkSidebarTabControl)Component;

        public override DesignerVerbCollection Verbs
        {
            get
            {
                if (_verbs == null)
                {
                    _verbs = new DesignerVerbCollection
                    {
                        CreateVerb("Arrange Controls by Margin", OnArrangeSelectedPageSectionPanels)
                    };
                }
                return _verbs;
            }
        }

        public override void Initialize(IComponent component)
        {
            base.Initialize(component);

            var sel = (ISelectionService)GetService(typeof(ISelectionService));
            if (sel != null) sel.SelectionChanged += OnSelectionChanged;

            // The nested navigation list receives sidebar input and changes the
            // control's selected page. Mirror that into ISelectionService.
            SidebarControl.SelectedIndexChanged += OnControlPageChanged;
            SidebarControl.SidebarResizeStarted += OnSidebarResizeStarted;
            SidebarControl.SidebarResizeCompleted += OnSidebarResizeCompleted;

            _behaviorService = (BehaviorService)GetService(typeof(BehaviorService));
            if (_behaviorService != null)
            {
                _sidebarAdorner = new Adorner();
                _sidebarAdorner.Glyphs.Add(new SidebarGlyph(this, _behaviorService));
                _behaviorService.Adorners.Add(_sidebarAdorner);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var sel = (ISelectionService)GetService(typeof(ISelectionService));
                if (sel != null) sel.SelectionChanged -= OnSelectionChanged;
                SidebarControl.SelectedIndexChanged -= OnControlPageChanged;
                SidebarControl.SidebarResizeStarted -= OnSidebarResizeStarted;
                SidebarControl.SidebarResizeCompleted -= OnSidebarResizeCompleted;
                _resizeTransaction?.Cancel();
                _resizeTransaction = null;
                if (_behaviorService != null && _sidebarAdorner != null)
                    _behaviorService.Adorners.Remove(_sidebarAdorner);
                _sidebarAdorner = null;
                _behaviorService = null;
            }
            base.Dispose(disposing);
        }

        private void OnControlPageChanged(object sender, EventArgs e)
        {
            // The control switched pages — make that page the primary
            // design-time selection (skip when it already is, and when no
            // page is current). Recursion is impossible: re-entering the
            // control's setter is an early-return no-op.
            var page = SidebarControl.SelectedPage;
            if (page == null) return;
            var sel = (ISelectionService)GetService(typeof(ISelectionService));
            if (sel != null && !ReferenceEquals(sel.PrimarySelection, page))
                sel.SetSelectedComponents(new object[] { page });
        }

        private void SelectSidebarControl()
        {
            var selection = (ISelectionService)GetService(typeof(ISelectionService));
            if (selection != null && !ReferenceEquals(selection.PrimarySelection, SidebarControl))
                selection.SetSelectedComponents(new object[] { SidebarControl });
        }

        private void OnSidebarResizeStarted(object sender, EventArgs e)
        {
            _resizeTransaction?.Cancel();
            var host = (IDesignerHost)GetService(typeof(IDesignerHost));
            _resizeTransaction = host?.CreateTransaction("Resize Sidebar");
            _resizeOriginalWidth = SidebarControl.SidebarWidth;

            PropertyDescriptor property = TypeDescriptor
                .GetProperties(SidebarControl)[nameof(DarkSidebarTabControl.SidebarWidth)];
            var change = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            change?.OnComponentChanging(SidebarControl, property);
        }

        private void OnSidebarResizeCompleted(object sender, EventArgs e)
        {
            if (SidebarControl.SidebarWidth == _resizeOriginalWidth)
            {
                _resizeTransaction?.Cancel();
                _resizeTransaction = null;
                return;
            }

            PropertyDescriptor property = TypeDescriptor
                .GetProperties(SidebarControl)[nameof(DarkSidebarTabControl.SidebarWidth)];
            var change = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            change?.OnComponentChanged(
                SidebarControl,
                property,
                _resizeOriginalWidth,
                SidebarControl.SidebarWidth);

            _resizeTransaction?.Commit();
            _resizeTransaction = null;
        }

        private void OnArrangeSelectedPageSectionPanels(object sender, EventArgs e)
        {
            var page = SidebarControl.SelectedPage;
            if (page == null)
                return;

            SectionPanelArrangementDesignerHelper.Arrange(
                page,
                (IComponentChangeService)GetService(typeof(IComponentChangeService)),
                (IDesignerHost)GetService(typeof(IDesignerHost)),
                "Arrange sidebar page section panels");
        }

        private sealed class SidebarGlyph : Glyph
        {
            private readonly DarkSidebarTabControlDesigner _designer;
            private readonly BehaviorService _behaviorService;

            public SidebarGlyph(DarkSidebarTabControlDesigner designer, BehaviorService behaviorService)
                : base(new SidebarBehavior(designer, behaviorService))
            {
                _designer = designer;
                _behaviorService = behaviorService;
            }

            public override Rectangle Bounds
            {
                get
                {
                    Rectangle controlBounds = _behaviorService.ControlRectInAdornerWindow(_designer.SidebarControl);
                    controlBounds.Width = Math.Min(
                        Math.Max(0, _designer.SidebarControl.SidebarWidth),
                        controlBounds.Width);
                    return controlBounds;
                }
            }

            public override Cursor GetHitTest(Point point)
            {
                // BehaviorService asks every glyph to hit-test in adorner-window
                // coordinates; it does not guarantee that point is already
                // constrained to Bounds. Returning a cursor unconditionally
                // makes this glyph capture the page surface as well, preventing
                // selection and toolbox drops. Only the sidebar is interactive.
                Rectangle bounds = Bounds;
                if (!bounds.Contains(point))
                    return null;

                int gripLeft = bounds.Right - SidebarBehavior.ResizeGripWidth;
                return point.X >= gripLeft ? Cursors.SizeWE : Cursors.Hand;
            }

            public override void Paint(PaintEventArgs pe)
            {
                // Input-only glyph: the real sidebar remains visible.
            }
        }

        private sealed class SidebarBehavior : Behavior
        {
            internal const int ResizeGripWidth = 6;

            private readonly DarkSidebarTabControlDesigner _designer;
            private readonly BehaviorService _behaviorService;
            private bool _resizing;
            private int _startScreenX;
            private int _startWidth;

            public SidebarBehavior(
                DarkSidebarTabControlDesigner designer,
                BehaviorService behaviorService)
            {
                _designer = designer;
                _behaviorService = behaviorService;
            }

            public override bool OnMouseDown(Glyph glyph, MouseButtons button, Point mouseLoc)
            {
                if (button != MouseButtons.Left)
                    return false;

                // Behavior mouse coordinates are in the adorner window. Use
                // the glyph's bounds in that same coordinate space so DPI
                // scaling in the out-of-process designer cannot move the hit
                // outside the six-pixel grip.
                if (mouseLoc.X >= glyph.Bounds.Right - ResizeGripWidth)
                {
                    _resizing = true;
                    _startScreenX = Control.MousePosition.X;
                    _startWidth = _designer.SidebarControl.SidebarWidth;
                    _designer.OnSidebarResizeStarted(this, EventArgs.Empty);
                    _behaviorService.PushCaptureBehavior(this);
                    return true;
                }

                int index = _designer.SidebarControl.GetSidebarIndexAtScreen(Control.MousePosition);
                if (index >= 0)
                    _designer.SidebarControl.SelectedIndex = index;

                // Any sidebar click selects the composite control. A click on
                // a menu row also switches the visible page first. This gives
                // a dock-filled control an accessible selection surface while
                // clicks in the page area still select/edit that page.
                _designer.SelectSidebarControl();
                return true;
            }

            public override bool OnMouseMove(Glyph glyph, MouseButtons button, Point mouseLoc)
            {
                if (!_resizing)
                    return false;

                int maximum = Math.Max(60, _designer.SidebarControl.ClientSize.Width - 60);
                _designer.SidebarControl.SidebarWidth = Math.Clamp(
                    _startWidth + Control.MousePosition.X - _startScreenX,
                    60,
                    maximum);
                _behaviorService.Invalidate();
                return true;
            }

            public override bool OnMouseUp(Glyph glyph, MouseButtons button, Point mouseLoc)
            {
                if (!_resizing)
                    return false;

                _resizing = false;
                _behaviorService.PopBehavior(this);
                _designer.OnSidebarResizeCompleted(this, EventArgs.Empty);
                return true;
            }
        }

        // If the OOP designer resolves a toolbox drop to the composite control
        // instead of the visible page designer, make the selected page the
        // parent. Direct page hits and composite hits therefore agree.
        protected override Control GetParentForComponent(IComponent component)
        {
            return SidebarControl.SelectedPage ?? base.GetParentForComponent(component);
        }

        private void OnAddPage(object sender, EventArgs e)
        {
            var host = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (host == null) return;
            var sel = (ISelectionService)GetService(typeof(ISelectionService));

            using (var t = host.CreateTransaction("Add Page"))
            {
                var page = (SidebarPage)host.CreateComponent(typeof(SidebarPage), UniqueName(host, "tabPage"));
                page.Text = $"Page {SidebarControl.Pages.Count + 1}";

                var change = (IComponentChangeService)GetService(typeof(IComponentChangeService));
                change?.OnComponentChanging(SidebarControl, null);
                SidebarControl.Pages.Add(page);
                change?.OnComponentChanged(SidebarControl, null, null, null);

                // Select the new page so the user lands on its design surface.
                sel?.SetSelectedComponents(new object[] { page });
                t.Commit();
            }
        }

        private void OnRemovePage(object sender, EventArgs e)
        {
            var sel = (ISelectionService)GetService(typeof(ISelectionService));
            var page = sel?.PrimarySelection as SidebarPage;
            if (page == null || !SidebarControl.Pages.Contains(page)) return;

            var host = (IDesignerHost)GetService(typeof(IDesignerHost));
            if (host == null) return;

            using (var t = host.CreateTransaction("Remove Page"))
            {
                var change = (IComponentChangeService)GetService(typeof(IComponentChangeService));
                change?.OnComponentChanging(SidebarControl, null);
                SidebarControl.Pages.Remove(page);
                change?.OnComponentChanged(SidebarControl, null, null, null);

                sel.SetSelectedComponents(new object[] { SidebarControl });
                host.DestroyComponent(page);
                t.Commit();
            }
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            var sel = (ISelectionService)GetService(typeof(ISelectionService));
            if (sel?.PrimarySelection is SidebarPage page && SidebarControl.Pages.Contains(page))
                SidebarControl.SelectedPage = page; // show the page as the design surface
        }

        private static string UniqueName(IDesignerHost host, string prefix)
        {
            var container = host.Container;
            int n = 1;
            while (container.Components[prefix + n] != null) n++;
            return prefix + n;
        }
    }

    /// <summary>
    /// Designer for a SidebarPage — a normal panel design surface whose
    /// context menu also carries the owner control's Add Page / Remove Page
    /// verbs, matching how right-clicking a TabPage surfaces a TabControl's
    /// Add Tab / Remove Tab verbs.
    /// </summary>
    public class DarkSidebarPageDesigner : ParentControlDesigner
    {
        private DesignerVerbCollection _verbs;

        public override DesignerVerbCollection Verbs
        {
            get
            {
                if (_verbs != null)
                    return _verbs;

                _verbs = new DesignerVerbCollection();
                // Visual Studio's context menu displays only two verbs for
                // this child design surface. Keep the page-specific layout
                // actions here; Add/Remove Page stay on the owner control.
                _verbs.Add(CreateVerb("Fit to Contents", OnFitToContents));
                _verbs.Add(CreateVerb("Arrange Controls by Margin", OnArrangeSectionPanels));
                return _verbs;
            }
        }

        private void OnFitToContents(object sender, EventArgs e)
        {
            FitToContentsDesignerHelper.Fit(
                (SidebarPage)Component,
                (IComponentChangeService)GetService(typeof(IComponentChangeService)),
                (IDesignerHost)GetService(typeof(IDesignerHost)),
                "Fit sidebar page to contents");
        }

        private void OnArrangeSectionPanels(object sender, EventArgs e)
        {
            SectionPanelArrangementDesignerHelper.Arrange(
                (SidebarPage)Component,
                (IComponentChangeService)GetService(typeof(IComponentChangeService)),
                (IDesignerHost)GetService(typeof(IDesignerHost)),
                "Arrange sidebar page section panels");
        }
    }

}
