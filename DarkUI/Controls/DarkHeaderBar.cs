using DarkUI.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DarkUI.Controls
{
    /// <summary>
    /// A themed header bar for a form — app title on the left, a live
    /// theme selector combo on the right (mirrors the TestApp's header).
    /// Docks to the top by default.
    /// </summary>
    [DefaultProperty(nameof(Title))]
    public class DarkHeaderBar : Control
    {
        private readonly DarkLabel _titleLabel;
        private readonly DarkLabel _themeTypeLabel;
        private readonly DarkComboBox _themeTypeCombo;
        private readonly DarkLabel _themeLabel;
        private readonly DarkComboBox _themeCombo;
        private bool _showThemeSelector = true;
        private int _themeSelectorWidth = 400;
        private HorizontalAlignment _themeSelectorAlignment = HorizontalAlignment.Right;
        private IReadOnlyList<Theme> _visibleThemes = Array.Empty<Theme>();
        private bool _updatingThemeSelector;
        private Control _dockParent;
        private bool _normalizingParentDockOrder;

        [DefaultValue("")]
        [Category("Appearance")]
        [Description("Title shown on the left side of the bar.")]
        public string Title
        {
            get => _titleLabel.Text;
            set
            {
                _titleLabel.Text = value;
                LayoutChildren();
            }
        }

        [DefaultValue(true)]
        [Category("Behavior")]
        [Description("Shows the theme selector combo on the right side of the bar.")]
        public bool ShowThemeSelector
        {
            get => _showThemeSelector;
            set
            {
                _showThemeSelector = value;
                LayoutChildren();
            }
        }

        [DefaultValue(400)]
        [Category("Layout")]
        [Description("Width of the theme selector combo.")]
        public int ThemeSelectorWidth
        {
            get => _themeSelectorWidth;
            set
            {
                _themeSelectorWidth = Math.Max(80, value);
                LayoutChildren();
            }
        }

        [DefaultValue(HorizontalAlignment.Right)]
        [Category("Layout")]
        [Description("Horizontal position of the Type and Theme selectors within the header bar.")]
        public HorizontalAlignment ThemeSelectorAlignment
        {
            get => _themeSelectorAlignment;
            set
            {
                if (value is not HorizontalAlignment.Left and not HorizontalAlignment.Right)
                    value = HorizontalAlignment.Right;
                if (_themeSelectorAlignment == value) return;
                _themeSelectorAlignment = value;
                LayoutChildren();
            }
        }

        // Size a fresh toolbox drop creates — the bar stretches anyway
        // once docked to the form.
        protected override Size DefaultSize => new Size(400, 40);

        public DarkHeaderBar()
        {
            Height = 40;
            Dock = DockStyle.Top;
            DockChanged += (_, _) => NormalizeParentDockLayout();
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Colors.GreyBackground;

            _titleLabel = new DarkLabel { AutoSize = true, Location = new Point(12, 11) };
            _themeTypeLabel = new DarkLabel { Text = "Type:", AutoSize = true };
            _themeTypeCombo = new DarkComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Size = new Size(125, 24) };
            _themeLabel = new DarkLabel { Text = "Theme:", AutoSize = true };
            _themeCombo = new DarkComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Size = new Size(160, 24) };
            _themeTypeCombo.Items.AddRange(ThemeManager.ThemeCategories.Cast<object>().ToArray());
            if (!DesignMode)
            {
                _themeTypeCombo.SelectedIndexChanged += (s, e) =>
                    PopulateThemesForSelectedCategory(applyFirstTheme: true);
                _themeCombo.SelectedIndexChanged += (s, e) =>
                {
                    if (_updatingThemeSelector) return;
                    int idx = _themeCombo.SelectedIndex;
                    if (idx >= 0 && idx < _visibleThemes.Count)
                        ThemeManager.Apply(_visibleThemes[idx]);
                };

                SelectActiveTheme();
            }

            Controls.Add(_themeCombo);
            Controls.Add(_themeLabel);
            Controls.Add(_themeTypeCombo);
            Controls.Add(_themeTypeLabel);
            Controls.Add(_titleLabel);

            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.ThemeChanged -= OnThemeChanged;
                DetachDockParent();
            }
            base.Dispose(disposing);
        }

        protected override void OnParentChanged(EventArgs e)
        {
            DetachDockParent();
            base.OnParentChanged(e);

            if (Parent != null)
            {
                _dockParent = Parent;
                _dockParent.ControlAdded += OnDockParentControlsChanged;
                _dockParent.ControlRemoved += OnDockParentControlsChanged;
                _dockParent.Layout += OnDockParentLayout;
                NormalizeParentDockLayout();
            }
        }

        private void DetachDockParent()
        {
            if (_dockParent == null)
                return;

            _dockParent.ControlAdded -= OnDockParentControlsChanged;
            _dockParent.ControlRemoved -= OnDockParentControlsChanged;
            _dockParent.Layout -= OnDockParentLayout;
            _dockParent = null;
        }

        private void OnDockParentControlsChanged(object sender, ControlEventArgs e) => NormalizeParentDockLayout();

        private void OnDockParentLayout(object sender, LayoutEventArgs e) => NormalizeParentDockLayout();

        private void NormalizeParentDockLayout()
        {
            if (_dockParent == null || Dock != DockStyle.Top || _normalizingParentDockOrder)
                return;

            Control fillControl = null;
            bool hasMultipleFillControls = false;
            foreach (Control child in _dockParent.Controls)
            {
                if (child.Dock != DockStyle.Fill)
                    continue;

                // Multiple Fill children intentionally overlap in WinForms.
                if (fillControl != null)
                {
                    hasMultipleFillControls = true;
                    break;
                }

                fillControl = child;
            }

            try
            {
                _normalizingParentDockOrder = true;

                // WinForms docks children in reverse z-order. Keep a single
                // Fill child at index zero so edge-docked controls reserve
                // their space before the Fill child receives the remainder.
                if (!hasMultipleFillControls && fillControl != null &&
                    _dockParent.Controls.GetChildIndex(fillControl) != 0)
                {
                    _dockParent.Controls.SetChildIndex(fillControl, 0);
                }

                // A top-docked ToolStrip is the form's command row and must be
                // processed before the header. Its z-order index therefore has
                // to be greater than the header's. This also works when both
                // controls live on a DarkTabPage or another container.
                int headerIndex = _dockParent.Controls.GetChildIndex(this);
                int firstToolStripIndex = int.MaxValue;

                foreach (Control child in _dockParent.Controls)
                {
                    if (child is ToolStrip && child.Dock == DockStyle.Top)
                    {
                        firstToolStripIndex = Math.Min(
                            firstToolStripIndex,
                            _dockParent.Controls.GetChildIndex(child));
                    }
                }

                if (firstToolStripIndex != int.MaxValue && headerIndex > firstToolStripIndex)
                    _dockParent.Controls.SetChildIndex(this, firstToolStripIndex);
            }
            finally
            {
                _normalizingParentDockOrder = false;
            }
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (DesignMode) return;
            ApplyThemeColors();
            // Follow themes applied outside the selector (e.g. from code).
            SelectActiveTheme();
        }

        private void ApplyThemeColors()
        {
            base.BackColor = Colors.GreyBackground;
            Invalidate(true);
        }

        private void LayoutChildren()
        {
            if (_themeCombo == null) return; // ctor not finished

            _titleLabel.Location = new Point(12, Math.Max(0, (Height - _titleLabel.Height) / 2));

            bool show = _showThemeSelector;
            _themeCombo.Visible = show;
            _themeLabel.Visible = show;
            _themeTypeCombo.Visible = show;
            _themeTypeLabel.Visible = show;
            if (!show) return;

            int typeWidth = Math.Min(125, Math.Max(90, _themeSelectorWidth / 3));
            int themeWidth = Math.Max(110, _themeSelectorWidth - typeWidth - _themeTypeLabel.Width - _themeLabel.Width - 18);
            _themeTypeCombo.Width = typeWidth;
            _themeCombo.Width = themeWidth;
            int comboY = Math.Max(0, (Height - _themeCombo.Height) / 2);
            int labelY = Math.Max(0, (Height - _themeLabel.Height) / 2);

            if (_themeSelectorAlignment == HorizontalAlignment.Left)
            {
                int left = _titleLabel.Right + 24;
                _themeTypeLabel.Location = new Point(left, labelY);
                _themeTypeCombo.Location = new Point(_themeTypeLabel.Right + 6, comboY);
                _themeLabel.Location = new Point(_themeTypeCombo.Right + 12, labelY);
                _themeCombo.Location = new Point(_themeLabel.Right + 6, comboY);
            }
            else
            {
                _themeCombo.Location = new Point(Math.Max(0, Width - themeWidth - 12), comboY);
                _themeLabel.Location = new Point(_themeCombo.Left - _themeLabel.Width - 6, labelY);
                _themeTypeCombo.Location = new Point(_themeLabel.Left - typeWidth - 12, comboY);
                _themeTypeLabel.Location = new Point(_themeTypeCombo.Left - _themeTypeLabel.Width - 6, labelY);
            }
        }

        private void PopulateThemesForSelectedCategory(bool applyFirstTheme = false)
        {
            if (_updatingThemeSelector) return;

            _updatingThemeSelector = true;
            try
            {
                string category = _themeTypeCombo.SelectedItem as string ?? ThemeManager.AllThemesCategory;
                _visibleThemes = ThemeManager.GetThemes(category);
                _themeCombo.Items.Clear();
                _themeCombo.Items.AddRange(_visibleThemes.Select(theme => (object)theme.Name).ToArray());
                _themeCombo.SelectedIndex = _visibleThemes.Count > 0 ? 0 : -1;
            }
            finally
            {
                _updatingThemeSelector = false;
            }

            // A category change is a valid selection on its own. Apply the
            // first visible theme now instead of waiting for a second click in
            // the Theme combo box.
            if (applyFirstTheme && _visibleThemes.Count > 0)
                ThemeManager.Apply(_visibleThemes[0]);
        }

        private void SelectActiveTheme()
        {
            if (_themeTypeCombo == null || DesignMode) return;

            _updatingThemeSelector = true;
            try
            {
                string category = ThemeManager.GetThemeCategory(ThemeManager.Active);
                int categoryIndex = _themeTypeCombo.Items.IndexOf(category);
                _themeTypeCombo.SelectedIndex = categoryIndex >= 0 ? categoryIndex : 0;
            }
            finally
            {
                _updatingThemeSelector = false;
            }

            PopulateThemesForSelectedCategory();
            int themeIndex = _visibleThemes
                .Select((theme, index) => new { theme, index })
                .Where(item => ReferenceEquals(item.theme, ThemeManager.Active))
                .Select(item => item.index)
                .DefaultIfEmpty(-1)
                .First();
            if (themeIndex >= 0)
                _themeCombo.SelectedIndex = themeIndex;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChildren();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Bottom border line, like the TestApp header.
            using (var p = new Pen(Colors.DarkBorder))
                e.Graphics.DrawLine(p, 0, Height - 1, Width, Height - 1);
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
    }
}
