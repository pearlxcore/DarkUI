using DarkUI.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A themed split container that supports 2 or more panels. Panels are
    /// separated by draggable splitter strips that follow the active theme.
    /// Dragging shows a lightweight preview in actual pixel space; on release
    /// the position is converted back into requested-size ratios and the
    /// panels resize exactly once.
    /// </summary>
    [Designer(typeof(DarkSplitContainerDesigner))]
    public class DarkSplitContainer : UserControl
    {
        public enum DarkSplitOrientation { Vertical, Horizontal }

        private readonly List<Control> _panels = new();
        private readonly List<SplitterStrip> _splitters = new();
        private readonly List<int> _sizes = new();   // requested sizes (user intent, px)
        private DarkSplitOrientation _orientation = DarkSplitOrientation.Vertical;
        private int _splitterWidth = 5;
        private const int MinPanelSize = 40;
        private bool _disposed;

        public DarkSplitContainer()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);
            BackColor = Colors.LightBorder;
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        // ── public API ────────────────────────────────────────────────

        [DefaultValue(DarkSplitOrientation.Vertical)]
        public DarkSplitOrientation Orientation
        {
            get => _orientation;
            set
            {
                if (_orientation == value) return;
                _orientation = value;
                foreach (var s in _splitters)
                    s.UpdateCursor();
                PerformLayout();
                Invalidate();
            }
        }

        [DefaultValue(5)]
        public int SplitterWidth
        {
            get => _splitterWidth;
            set
            {
                _splitterWidth = Math.Max(3, value);
                PerformLayout();
                Invalidate();
            }
        }

        [Browsable(false)]
        public int PanelCount => _panels.Count;

        [Browsable(false)]
        public IReadOnlyList<Control> Panels => _panels;

        public Control GetPanel(int index) => _panels[index];

        /// <summary>
        /// Sets the requested (proportional) size of a panel. Sizes are
        /// relative weights scaled to the available space — e.g. 100/400
        /// gives a 1:4 split, and a 1:4 outer split whose second pane
        /// holds a 100/100 inner container yields 1:2:2 across three panes.
        /// </summary>
        public void SetPanelSize(int index, int size)
        {
            if (index < 0 || index >= _panels.Count) return;
            _sizes[index] = Math.Max(MinPanelSize, size);
            PerformLayout();
            Invalidate();
        }

        [Category("Layout")]
        [Description("Requested sizes of the panels along the primary axis (pixels).")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int[] PanelSizes
        {
            get => _sizes.ToArray();
            set
            {
                if (value == null || value.Length == 0) return;
                _sizes.Clear();
                for (int i = 0; i < value.Length; i++)
                    _sizes.Add(Math.Max(1, value[i]));
                while (_sizes.Count < _panels.Count) _sizes.Add(100);
                PerformLayout();
                Invalidate();
            }
        }

        public void AddPanel(Control content) => InsertPanel(_panels.Count, content);

        public void InsertPanel(int index, Control content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            index = Math.Max(0, Math.Min(index, _panels.Count));

            content.Dock = DockStyle.None;
            _panels.Insert(index, content);
            _sizes.Insert(index, 100);

            SyncSplitters();

            Controls.Add(content);
            PerformLayout();
            Invalidate();
        }

        public void RemovePanel(int index)
        {
            if (index < 0 || index >= _panels.Count) return;
            var c = _panels[index];
            _panels.RemoveAt(index);
            _sizes.RemoveAt(index);
            Controls.Remove(c);

            SyncSplitters();

            PerformLayout();
            Invalidate();
        }

        public void RemovePanel(Control content) => RemovePanel(_panels.IndexOf(content));

        public void ClearPanels()
        {
            while (_panels.Count > 0) RemovePanel(_panels.Count - 1);
        }

        // ── designer support ──────────────────────────────────────────
        // In the designer, panes are added by simply dropping a control
        // (Panel, or any control) onto the container — it is adopted as a
        // pane automatically. Deleting it in the designer removes the pane.

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (e.Control is SplitterStrip || e.Control is SplitterPreview) return;
            // Adopt any non-splitter child as a pane — at design time AND
            // runtime. The runtime path matters: InitializeComponent wires
            // panes via Controls.Add, and without adoption the container
            // has zero panels and no draggable splitters.
            if (_panels.Contains(e.Control)) return;

            _panels.Add(e.Control);
            _sizes.Add(100);
            SyncSplitters();
            PerformLayout();
            Invalidate();
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);
            if (e.Control is SplitterStrip || e.Control is SplitterPreview) return;

            int idx = _panels.IndexOf(e.Control);
            if (idx < 0) return;
            _panels.RemoveAt(idx);
            _sizes.RemoveAt(idx);
            SyncSplitters();
            PerformLayout();
            Invalidate();
        }

        // ── plumbing ──────────────────────────────────────────────────

        private void SyncSplitters()
        {
            while (_splitters.Count < _panels.Count - 1)
            {
                var s = new SplitterStrip(this);
                _splitters.Add(s);
                Controls.Add(s);
            }
            while (_splitters.Count > _panels.Count - 1)
            {
                var s = _splitters[_splitters.Count - 1];
                _splitters.RemoveAt(_splitters.Count - 1);
                Controls.Remove(s);
                s.Dispose();
            }
            for (int i = 0; i < _splitters.Count; i++)
            {
                _splitters[i].PanelIndex = i;
                _splitters[i].UpdateCursor();
            }
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

        private void OnThemeChanged(object sender, EventArgs e)
        {
            BackColor = Colors.LightBorder;
            // Invalidate(true) repaints children too — covers the preview
            // if a theme switch happens mid-drag.
            Invalidate(true);
        }

        // ── Designer freeze guard ─────────────────────────────────────
        // The wrapper's BackColor is the themed BORDER — clamp it so a
        // serialized value in Designer.cs can never pin it to a stale
        // color or stop it following ThemeManager.
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Color BackColor
        {
            get => Colors.LightBorder;
            set => base.BackColor = Colors.LightBorder;
        }

        private int GetPanelPrimarySize(int index)
        {
            var c = _panels[index];
            return _orientation == DarkSplitOrientation.Vertical ? c.Width : c.Height;
        }

        // ── layout ────────────────────────────────────────────────────
        // _sizes holds the user's requested sizes and is NEVER modified
        // here — shrinking the container scales the layout proportionally
        // so the ratio (and the user's intent) survives regrowing.

        private bool _layoutting;

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_layoutting || _panels.Count == 0) return;
            _layoutting = true;
            try
            {
                bool vertical = _orientation == DarkSplitOrientation.Vertical;

                // Panes are owned by the split layout at both runtime and
                // design time. Preserving arbitrary designer bounds leaves
                // uncovered grey space and lets a pane overlap its siblings.
                // Use the same proportional layout everywhere instead.
                LayoutPanels(vertical);
            }
            finally { _layoutting = false; }
        }

        private void LayoutPanels(bool vertical)
        {
            int n = _panels.Count;
            int total = (vertical ? ClientSize.Width : ClientSize.Height)
                        - _splitterWidth * (n - 1);
            int avail = Math.Max(1, total);

            int sum = 0;
            foreach (var s in _sizes) sum += s;
            double scale = sum > 0 ? (double)avail / sum : 1.0;

            bool canEnforceMin = avail >= n * MinPanelSize;

            var actual = new int[n];
            int used = 0;
            for (int i = 0; i < n; i++)
            {
                int sz = (int)(_sizes[i] * scale);
                if (canEnforceMin) sz = Math.Max(MinPanelSize, sz);
                actual[i] = sz;
                used += sz;
            }

            // If min-clamping overflowed the available space, rescale
            // proportionally without enforcing the minimum — the
            // container is simply too small.
            if (used > avail)
            {
                double s2 = (double)avail / used;
                used = 0;
                for (int i = 0; i < n; i++)
                {
                    actual[i] = Math.Max(1, (int)(actual[i] * s2));
                    used += actual[i];
                }
            }

            // Absorb leftover space in the last panel.
            int leftover = avail - used;
            if (leftover > 0) actual[n - 1] += leftover;

            int pos = 0;
            int cross = vertical ? ClientSize.Height : ClientSize.Width;
            for (int i = 0; i < n; i++)
            {
                _panels[i].Bounds = vertical
                    ? new Rectangle(pos, 0, actual[i], cross)
                    : new Rectangle(0, pos, cross, actual[i]);
                pos += actual[i];

                if (i < _splitters.Count)
                {
                    _splitters[i].Bounds = vertical
                        ? new Rectangle(pos, 0, _splitterWidth, cross)
                        : new Rectangle(0, pos, cross, _splitterWidth);
                    pos += _splitterWidth;
                }
            }
        }

        // Places splitters between the panels' ACTUAL bounds — used by the
        // design-mode adopt path where panels are positioned by the designer.
        private void PositionSplitters(bool vertical)
        {
            int pos = 0;
            int cross = vertical ? ClientSize.Height : ClientSize.Width;
            for (int i = 0; i < _panels.Count; i++)
            {
                pos += vertical ? _panels[i].Width : _panels[i].Height;
                if (i < _splitters.Count)
                {
                    _splitters[i].Bounds = vertical
                        ? new Rectangle(pos, 0, _splitterWidth, cross)
                        : new Rectangle(0, pos, cross, _splitterWidth);
                    pos += _splitterWidth;
                }
            }
        }

        // ── drag state (two coordinate spaces) ────────────────────────
        // Requested space: _sizes (user intent, may be scaled by layout).
        // Actual space:    panel Bounds in pixels (what the user sees).
        // The mouse delta and the preview live in ACTUAL space; on commit
        // the desired actual position is converted back into the requested
        // pair via its ratio, so the splitter lands exactly on the preview.

        private readonly SplitterPreview _preview = new SplitterPreview();
        private bool _previewShown;
        private int _dragPanelIndex = -1;
        private Point _dragStartOwner;          // owner coords at MouseDown
        private int _startActualFirst, _startActualSecond, _actualPairSize;
        private int _startRequestedFirst, _startRequestedSecond, _requestedPairSize;
        private int _boundaryPos;               // actual committed boundary at MouseDown
        private int _currentDragDelta;          // latest clamped delta (actual px)

        private void ShowPreview()
        {
            if (_previewShown) return;
            Controls.Add(_preview);
            _preview.BringToFront();
            _previewShown = true;
        }

        private void HidePreview()
        {
            if (!_previewShown) return;
            Controls.Remove(_preview);
            _previewShown = false;
        }

        private void MovePreview(int delta)
        {
            bool vertical = _orientation == DarkSplitOrientation.Vertical;
            int cross = vertical ? ClientSize.Height : ClientSize.Width;
            _preview.Bounds = vertical
                ? new Rectangle(_boundaryPos + delta, 0, 2, cross)
                : new Rectangle(0, _boundaryPos + delta, cross, 2);
        }

        // Clamp in ACTUAL pixels — MinPanelSize refers to the visible size.
        private int ClampDelta(int delta)
        {
            int minDelta = MinPanelSize - _startActualFirst;
            int maxDelta = _startActualSecond - MinPanelSize;
            return Math.Max(minDelta, Math.Min(maxDelta, delta));
        }

        private void CommitDrag(int delta)
        {
            if (_dragPanelIndex < 0 || _dragPanelIndex + 1 >= _sizes.Count) return;

            // Desired actual first-panel size after the drag.
            int desiredActualFirst = _startActualFirst + delta;

            // Convert back into the requested pair, preserving the
            // requested pair total. The ratio is scale-invariant, so the
            // committed position survives any layout scaling factor.
            double ratio = _actualPairSize > 0
                ? (double)desiredActualFirst / _actualPairSize
                : 0.5;
            int newRequestedFirst = (int)Math.Round(_requestedPairSize * ratio);
            newRequestedFirst = Math.Max(1, Math.Min(_requestedPairSize - 1, newRequestedFirst));

            _sizes[_dragPanelIndex] = newRequestedFirst;
            _sizes[_dragPanelIndex + 1] = _requestedPairSize - newRequestedFirst;
        }

        private void CancelDrag()
        {
            _dragPanelIndex = -1;
            HidePreview();
        }

        // Called by the splitter strip during a drag.
        private void DragMouseMove(int delta)
        {
            _currentDragDelta = ClampDelta(delta);
            MovePreview(_currentDragDelta);
        }

        private void DragMouseUp(int delta)
        {
            CommitDrag(delta);
            CancelDrag();
            PerformLayout();
            Invalidate();
        }

        private void DragCaptureLost()
        {
            if (_dragPanelIndex < 0) return;
            CancelDrag();
            PerformLayout();
            Invalidate();
        }

        internal int GetBoundaryPos(int panelIndex)
        {
            // The splitter's own position is the exact committed boundary
            // in the current actual layout.
            var s = _splitters[panelIndex];
            return _orientation == DarkSplitOrientation.Vertical ? s.Left : s.Top;
        }

        // ── splitter strip ────────────────────────────────────────────

        private sealed class SplitterStrip : Control
        {
            private readonly DarkSplitContainer _owner;
            private bool _hover;
            private bool _pressed;

            public int PanelIndex;

            public SplitterStrip(DarkSplitContainer owner)
            {
                _owner = owner;
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer, true);
                UpdateCursor();
            }

            public void UpdateCursor()
            {
                Cursor = _owner._orientation == DarkSplitOrientation.Vertical
                    ? Cursors.SizeWE : Cursors.SizeNS;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                bool vertical = _owner._orientation == DarkSplitOrientation.Vertical;

                using (var b = new SolidBrush(_hover || _pressed ? Colors.GreySelection : Colors.MediumBackground))
                    g.FillRectangle(b, ClientRectangle);

                using (var p = new Pen(_hover || _pressed ? Colors.BlueHighlight : Colors.DarkBorder))
                {
                    if (vertical)
                        g.DrawLine(p, Width / 2, 2, Width / 2, Height - 2);
                    else
                        g.DrawLine(p, 2, Height / 2, Width - 2, Height / 2);
                }
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                _hover = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hover = false;
                if (!_pressed) Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button != MouseButtons.Left) return;
                if (PanelIndex < 0 || PanelIndex + 1 >= _owner._sizes.Count) return;

                _pressed = true;
                Capture = true;

                // Capture the drag origin in OWNER coordinates — the
                // splitter's own coordinate space is invalid once its
                // bounds move.
                _owner._dragStartOwner = _owner.PointToClient(Cursor.Position);
                _owner._dragPanelIndex = PanelIndex;
                _owner._currentDragDelta = 0;   // never inherit a previous drag

                // Actual (visible) panel dimensions — used to clamp and
                // position the preview in real pixels.
                _owner._startActualFirst = _owner.GetPanelPrimarySize(PanelIndex);
                _owner._startActualSecond = _owner.GetPanelPrimarySize(PanelIndex + 1);
                _owner._actualPairSize = _owner._startActualFirst + _owner._startActualSecond;

                // Requested (user-intent) pair values — used to commit.
                _owner._startRequestedFirst = _owner._sizes[PanelIndex];
                _owner._startRequestedSecond = _owner._sizes[PanelIndex + 1];
                _owner._requestedPairSize = _owner._startRequestedFirst + _owner._startRequestedSecond;

                _owner._boundaryPos = _owner.GetBoundaryPos(PanelIndex);
                _owner.ShowPreview();
                _owner.MovePreview(0);
                Invalidate();
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (!_pressed) return;

                var p = _owner.PointToClient(Cursor.Position);
                bool vertical = _owner._orientation == DarkSplitOrientation.Vertical;
                int delta = (vertical ? p.X : p.Y)
                            - (vertical ? _owner._dragStartOwner.X : _owner._dragStartOwner.Y);

                _owner.DragMouseMove(delta);
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (!_pressed) return;

                // Clear _pressed BEFORE releasing capture: releasing
                // capture fires OnMouseCaptureChanged, which must see
                // _pressed == false so it does not cancel the commit.
                _pressed = false;
                Capture = false;
                _owner.DragMouseUp(_owner._currentDragDelta);
            }

            protected override void OnMouseCaptureChanged(EventArgs e)
            {
                base.OnMouseCaptureChanged(e);
                if (!_pressed) return;
                // Capture was lost unexpectedly (alt-tab etc.) — cancel.
                _pressed = false;
                _owner.DragCaptureLost();
            }
        }

        // ── preview strip ─────────────────────────────────────────────

        private sealed class SplitterPreview : Control
        {
            public SplitterPreview()
            {
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer, true);
                TabStop = false;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                using var b = new SolidBrush(Colors.BlueHighlight);
                e.Graphics.FillRectangle(b, ClientRectangle);
            }
        }
    }
}
