using DarkUI.Config;
using DarkUI.Renderers;
using DarkUI.Animation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ThemeV2TestApp;

/// <summary>Shared native popup host for Theme V2 menus and dropdowns.</summary>
internal static class ThemeV2Popup
{
    public static void Show(Control anchor, IReadOnlyList<string> items, int selectedIndex, Action<int> selected)
    {
        var menu = new ContextMenuStrip { ShowImageMargin = false, ShowCheckMargin = true, Renderer = new ThemeV2PickerMenuRenderer(), AutoSize = true };
        for (var index = 0; index < items.Count; index++)
        {
            var captured = index;
            var item = new ToolStripMenuItem(items[index]) { Checked = index == selectedIndex, ForeColor = Colors.LightText };
            item.Click += (_, _) => selected(captured);
            menu.Items.Add(item);
        }
        menu.Closed += (_, _) => { if (!anchor.IsDisposed && anchor.IsHandleCreated) anchor.BeginInvoke((MethodInvoker)menu.Dispose); };
        var point = anchor.PointToScreen(new Point(0, anchor.Height));
        var workArea = Screen.FromControl(anchor).WorkingArea;
        var preferred = menu.GetPreferredSize(Size.Empty);
        var width = Math.Min(workArea.Width, Math.Max(anchor.Width, preferred.Width));
        point.X = Math.Min(point.X, workArea.Right - width);
        point.Y = point.Y + preferred.Height > workArea.Bottom ? anchor.PointToScreen(Point.Empty).Y - preferred.Height : point.Y;
        menu.Show(point);
    }
}

internal sealed class ThemeV2Toolbar : ThemeV2AdditionalControl
{
    public List<string> Items { get; } = new();
    [DefaultValue(false)] public bool Compact { get; set; }
    public event EventHandler? ItemClicked;
    private int _hoveredIndex = -1;
    private int _pressedIndex = -1;
    public ThemeV2Toolbar() { TabStop = true; AccessibleRole = AccessibleRole.ToolBar; }
    private int ItemWidth => Compact ? 110 : 82;
    private int HitTest(int x) => Math.Clamp((x - 12) / ItemWidth, 0, Items.Count - 1);
    protected override void OnMouseMove(MouseEventArgs e) { var index = e.X < 12 || Items.Count == 0 ? -1 : HitTest(e.X); if (index != _hoveredIndex) { _hoveredIndex = index; Invalidate(); } Cursor = index >= 0 ? Cursors.Hand : Cursors.Default; base.OnMouseMove(e); }
    protected override void OnMouseLeave(EventArgs e) { _hoveredIndex = -1; Cursor = Cursors.Default; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressedIndex = _hoveredIndex; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { var pressed = _pressedIndex; _pressedIndex = -1; Invalidate(); if (e.Button == MouseButtons.Left && pressed >= 0 && pressed == _hoveredIndex) { if (Items[pressed] == "Share") ThemeV2Popup.Show(this, new[] { "Copy link", "Invite people", "Export" }, -1, _ => ItemClicked?.Invoke(this, EventArgs.Empty)); else ItemClicked?.Invoke(this, EventArgs.Empty); } base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e) { PaintSurface(e, Colors.LightBackground, Colors.LightBorder); var x = 12; for (var i = 0; i < Items.Count; i++) { var r = new Rectangle(x, 2, ItemWidth - 6, Height - 4); if (i == _hoveredIndex || i == _pressedIndex) ThemeSurfaceRenderer.DrawSurface(e.Graphics, r, Colors.LightBackground, i == _pressedIndex ? Colors.DarkBlueBackground : Colors.GreySelection, Colors.BlueHighlight, ThemeManager.Active); var text = r; if (i == _pressedIndex) text.Offset(0, 1); TextRenderer.DrawText(e.Graphics, Items[i] + (Items[i] == "Share" ? "  ⌄" : string.Empty), Font, text, Colors.LightText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); x += ItemWidth; } }
}

internal sealed class ThemeV2Tabs : ThemeV2AdditionalControl
{
    public List<string> Items { get; } = new();
    private int _selectedIndex;
    [DefaultValue(0)] public int SelectedIndex { get => _selectedIndex; set { if (_selectedIndex == value) return; _selectedIndex = value; SelectedIndexChanged?.Invoke(this, EventArgs.Empty); Invalidate(); } }
    public event EventHandler? SelectedIndexChanged;
    private int _hoveredIndex = -1;
    private int _fromIndex;
    private readonly ThemeMotion _indicator;
    public ThemeV2Tabs() { Cursor = Cursors.Hand; TabStop = true; AccessibleRole = AccessibleRole.PageTabList; _indicator = new ThemeMotion(this, Invalidate); }
    protected override void Dispose(bool disposing) { if (disposing) _indicator.Dispose(); base.Dispose(disposing); }
    private int HitTest(int x) => Items.Count == 0 ? -1 : Math.Clamp(x * Items.Count / Math.Max(1, Width), 0, Items.Count - 1);
    protected override void OnMouseMove(MouseEventArgs e) { _hoveredIndex = HitTest(e.X); Invalidate(); base.OnMouseMove(e); }
    protected override void OnMouseLeave(EventArgs e) { _hoveredIndex = -1; Invalidate(); base.OnMouseLeave(e); }
    private void SelectTab(int next) { next = Math.Clamp(next, 0, Math.Max(0, Items.Count - 1)); if (next == SelectedIndex) return; _fromIndex = SelectedIndex; _selectedIndex = next; _indicator.SnapTo(0); _indicator.AnimateTo(1, 160); SelectedIndexChanged?.Invoke(this, EventArgs.Empty); }
    protected override void OnMouseUp(MouseEventArgs e) { if (Items.Count > 0) SelectTab(HitTest(e.X)); base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (Items.Count > 0 && e.KeyCode is Keys.Left or Keys.Right) { SelectTab((SelectedIndex + (e.KeyCode == Keys.Right ? 1 : -1) + Items.Count) % Items.Count); e.Handled = true; } base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e) { e.Graphics.Clear(ThemeV2Painting.ParentColor(this)); if (Items.Count == 0) return; var w = Width / Items.Count; for (var i = 0; i < Items.Count; i++) { var r = new Rectangle(i * w, 0, i == Items.Count - 1 ? Width - i * w : w, Height); if (i == _hoveredIndex && i != SelectedIndex) { using var h = new SolidBrush(Colors.GreySelection); e.Graphics.FillRectangle(h, r); } TextRenderer.DrawText(e.Graphics, Items[i], Font, r, i == SelectedIndex ? Colors.LightText : Colors.DisabledText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); } var from = _fromIndex * w + 10; var to = SelectedIndex * w + 10; var x = (int)Math.Round(from + ((to - from) * _indicator.Value)); using var b = new SolidBrush(Colors.BlueHighlight); e.Graphics.FillRectangle(b, x, Height - 3, w - 20, 3); }
}

internal sealed class ThemeV2TabPage : Panel
{
    public string Title { get; }
    public ThemeV2TabPage(string title)
    {
        Title = title;
        Padding = new Padding(14);
        DoubleBuffered = true;
        ThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }
    protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged; base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTheme();
    private void ApplyTheme() { BackColor = Colors.MediumBackground; ForeColor = Colors.LightText; Invalidate(true); }
}

internal sealed class ThemeV2TabControl : UserControl
{
    private readonly ThemeV2Tabs _tabs = new() { Dock = DockStyle.Top, Height = 38 };
    private readonly Panel _pageHost = new() { Dock = DockStyle.Fill };
    public List<ThemeV2TabPage> TabPages { get; } = new();
    public ThemeV2TabControl()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Controls.Add(_pageHost);
        Controls.Add(_tabs);
        _tabs.SelectedIndexChanged += (_, _) => ShowSelectedPage();
    }
    public void AddPage(ThemeV2TabPage page)
    {
        TabPages.Add(page);
        _tabs.Items.Add(page.Title);
        page.Dock = DockStyle.Fill;
        _pageHost.Controls.Add(page);
        ShowSelectedPage();
    }
    private void ShowSelectedPage()
    {
        for (var i = 0; i < TabPages.Count; i++)
            TabPages[i].Visible = i == _tabs.SelectedIndex;
    }
}

internal sealed class ThemeV2DataGrid : ThemeV2AdditionalControl
{
    private readonly (string Name, string State, string Updated)[] _rows = { ("Website refresh", "Active", "Just now"), ("Mobile release", "Review", "14 min"), ("Research notes", "Draft", "1 hour") };
    private int _selectedRow;
    private int _hoveredRow = -1;
    private readonly ThemeMotion[] _rowHover;
    public ThemeV2DataGrid()
    {
        AccessibleRole = AccessibleRole.Table;
        TabStop = true;
        Cursor = Cursors.Default;
        _rowHover = new[] { new ThemeMotion(this, Invalidate), new ThemeMotion(this, Invalidate), new ThemeMotion(this, Invalidate) };
    }
    protected override void Dispose(bool disposing) { if (disposing) foreach (var motion in _rowHover) motion.Dispose(); base.Dispose(disposing); }
    private int HitRow(int y) => y < 38 ? -1 : Math.Clamp((y - 38) / 45, 0, _rows.Length - 1);
    private void SetHoveredRow(int row) { if (_hoveredRow == row) return; _hoveredRow = row; for (var i = 0; i < _rowHover.Length; i++) _rowHover[i].AnimateTo(i == row ? 1 : 0, i == row ? 115 : 130); }
    protected override void OnMouseMove(MouseEventArgs e) { SetHoveredRow(HitRow(e.Y)); base.OnMouseMove(e); }
    protected override void OnMouseLeave(EventArgs e) { SetHoveredRow(-1); base.OnMouseLeave(e); }
    protected override void OnMouseUp(MouseEventArgs e) { if (e.Y >= 38) { Focus(); _selectedRow = Math.Clamp((e.Y - 38) / 45, 0, _rows.Length - 1); Invalidate(); } base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Up) _selectedRow = Math.Max(0, _selectedRow - 1); if (e.KeyCode == Keys.Down) _selectedRow = Math.Min(_rows.Length - 1, _selectedRow + 1); if (e.KeyCode == Keys.Home) _selectedRow = 0; if (e.KeyCode == Keys.End) _selectedRow = _rows.Length - 1; Invalidate(); base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var border = Focused ? Colors.ActiveControl : Colors.LightBorder;
        PaintSurface(e, Colors.MediumBackground, border);
        var state = e.Graphics.Save();
        try
        {
            var scale = e.Graphics.DpiX / 96f;
            var stroke = Math.Max(1f, ThemeManager.Active.BorderThickness * scale);
            var radius = Math.Max(0, (ThemeManager.Active.CornerRadius * scale) - (stroke / 2f));
            var bounds = new RectangleF(stroke, stroke, Width - (stroke * 2f), 30);
            using var path = ThemeSurfaceRenderer.CreateTopRoundedPath(bounds, radius);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            using var fill = new SolidBrush(Colors.LightBackground);
            e.Graphics.FillPath(fill, path);
        }
        finally { e.Graphics.Restore(state); }
        ThemeSurfaceRenderer.DrawBorder(e.Graphics, ClientRectangle, border, ThemeManager.Active);
        TextRenderer.DrawText(e.Graphics, "Project", Font, new Point(12, 9), Colors.DisabledText);
        TextRenderer.DrawText(e.Graphics, "State", Font, new Point(170, 9), Colors.DisabledText);
        TextRenderer.DrawText(e.Graphics, "Updated", Font, new Point(274, 9), Colors.DisabledText);
        PaintRows(e);
    }

    private void PaintRows(PaintEventArgs e)
    {
        for (var i = 0; i < _rows.Length; i++)
        {
            var y = 38 + i * 45;
            var rowRect = new Rectangle(2, y - 3, Width - 4, 34);
            var hover = _rowHover[i].Value;
            if (i == _selectedRow) { using var selected = new SolidBrush(ThemeMotion.Blend(Colors.DarkBlueBackground, Colors.BlueBackground, hover * .25)); e.Graphics.FillRectangle(selected, rowRect); using var indicator = new SolidBrush(Colors.BlueHighlight); e.Graphics.FillRectangle(indicator, rowRect.X, rowRect.Y, 3, rowRect.Height); }
            else if (hover > 0.001) { using var hovered = new SolidBrush(ThemeMotion.Blend(Colors.MediumBackground, Colors.LightBackground, hover * .45)); e.Graphics.FillRectangle(hovered, rowRect); }
            using var line = new Pen(Colors.DarkBorder);
            e.Graphics.DrawLine(line, 1, y + 34, Width - 2, y + 34);
            TextRenderer.DrawText(e.Graphics, _rows[i].Name, Font, new Point(12, y), Colors.LightText);
            TextRenderer.DrawText(e.Graphics, _rows[i].State, Font, new Point(170, y), i == _selectedRow ? Colors.ActiveControl : Colors.BlueHighlight);
            TextRenderer.DrawText(e.Graphics, _rows[i].Updated, Font, new Point(274, y), Colors.DisabledText);
        }
    }
}

internal sealed class ThemeV2List : ThemeV2AdditionalControl
{
    private readonly string[] _items = { "Release notes", "Design system", "Customer feedback", "Roadmap" };
    [DefaultValue(0)] public int SelectedIndex { get; set; }
    public ThemeV2List() { Cursor = Cursors.Hand; TabStop = true; AccessibleRole = AccessibleRole.List; }
    protected override void OnMouseUp(MouseEventArgs e) { SelectedIndex = Math.Clamp((e.Y - 12) / 40, 0, _items.Length - 1); Invalidate(); base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e) { PaintSurface(e, Colors.MediumBackground, Colors.LightBorder); for (var i = 0; i < _items.Length; i++) { var r = new Rectangle(8, 10 + i * 40, Width - 16, 32); if (i == SelectedIndex) ThemeSurfaceRenderer.DrawSurface(e.Graphics, r, Colors.MediumBackground, Colors.BlueSelection, Colors.BlueHighlight, ThemeManager.Active); TextRenderer.DrawText(e.Graphics, _items[i], Font, new Rectangle(18, r.Y, r.Width - 20, r.Height), i == SelectedIndex ? Colors.SelectionText : Colors.LightText, TextFormatFlags.VerticalCenter); } }
}

internal sealed class ThemeV2Tree : ThemeV2AdditionalControl
{
    private bool _projectsExpanded = true;
    private int _selectedRow = 1;
    private bool _isOverProjectsDisclosure;
    private readonly ThemeMotion _projectsChevron;
    private readonly ThemeMotion _disclosureHover;

    public ThemeV2Tree()
    {
        Cursor = Cursors.Default;
        TabStop = true;
        AccessibleRole = AccessibleRole.Outline;
        _projectsChevron = new ThemeMotion(this, Invalidate);
        _projectsChevron.SnapTo(1);
        _disclosureHover = new ThemeMotion(this, Invalidate);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _projectsChevron.Dispose();
            _disclosureHover.Dispose();
        }

        base.Dispose(disposing);
    }

    private int RowCount => _projectsExpanded ? 5 : 3;

    private static Rectangle ProjectsDisclosureBounds => new(24, 43, 24, 24);

    private void SetProjectsExpanded(bool expanded)
    {
        if (_projectsExpanded == expanded) return;

        _projectsExpanded = expanded;
        _projectsChevron.AnimateTo(expanded ? 1 : 0, 140);
        if (!expanded) _selectedRow = Math.Min(_selectedRow, 2);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var overDisclosure = ProjectsDisclosureBounds.Contains(e.Location);
        if (overDisclosure != _isOverProjectsDisclosure)
        {
            _isOverProjectsDisclosure = overDisclosure;
            _disclosureHover.AnimateTo(overDisclosure ? 1 : 0, 120);
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _isOverProjectsDisclosure = false;
        _disclosureHover.AnimateTo(0, 120);
        base.OnMouseLeave(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        var row = Math.Clamp((e.Y - 12) / 31, 0, RowCount - 1);
        _selectedRow = row;
        Focus();
        if (e.Button == MouseButtons.Left && row == 1 && ProjectsDisclosureBounds.Contains(e.Location))
            SetProjectsExpanded(!_projectsExpanded);
        else
            Invalidate();

        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Up) _selectedRow = Math.Max(0, _selectedRow - 1);
        if (e.KeyCode == Keys.Down) _selectedRow = Math.Min(RowCount - 1, _selectedRow + 1);
        if (_selectedRow == 1 && e.KeyCode == Keys.Left) SetProjectsExpanded(false);
        if (_selectedRow == 1 && e.KeyCode == Keys.Right) SetProjectsExpanded(true);
        Invalidate();
        base.OnKeyDown(e);
    }

    private static void DrawDisclosureCaret(Graphics graphics, PointF center, float angle, Color color, float scale)
    {
        var state = graphics.Save();
        try
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            graphics.TranslateTransform(center.X, center.Y);
            graphics.RotateTransform(angle);
            using var pen = new Pen(color, Math.Max(1f, 1.1f * scale))
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round,
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round
            };
            // The visible mark stays understated: a short, open caret that is wider than it is tall.
            // Rotation turns the same canonical geometry from collapsed (>) to expanded (v).
            var halfWidth = 4.5f * scale;
            var halfHeight = 3.25f * scale;
            graphics.DrawLines(pen, new[]
            {
                new PointF(-halfWidth, -halfHeight),
                new PointF(halfWidth, 0),
                new PointF(-halfWidth, halfHeight)
            });
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        PaintSurface(e, Colors.MediumBackground, Focused ? Colors.ActiveControl : Colors.LightBorder);
        var scale = e.Graphics.DpiX / 96f;
        var rows = _projectsExpanded
            ? new[] { ("Workspace", 10, true, 1d), ("Projects", 30, true, _projectsChevron.Value), ("Website refresh", 50, false, 0d), ("Mobile release", 50, false, 0d), ("Archive", 30, true, 0d) }
            : new[] { ("Workspace", 10, true, 1d), ("Projects", 30, true, _projectsChevron.Value), ("Archive", 30, true, 0d) };

        var y = 12;
        for (var i = 0; i < rows.Length; i++)
        {
            if (i == _selectedRow)
            {
                using var selection = new SolidBrush(Colors.DarkBlueBackground);
                e.Graphics.FillRectangle(selection, 6, y - 2, Width - 12, 26);
            }

            var row = rows[i];
            var disclosureCenter = new PointF(row.Item2 + 6, y + 8);
            if (row.Item3)
            {
                var chevronColor = i == 1
                    ? ThemeMotion.Blend(Colors.DisabledText, Colors.LightText, _disclosureHover.Value * .7)
                    : Colors.DisabledText;
                DrawDisclosureCaret(e.Graphics, disclosureCenter, (float)(90 * row.Item4), chevronColor, scale);
            }
            else
            {
                using var bullet = new SolidBrush(Colors.DisabledText);
                e.Graphics.FillEllipse(bullet, disclosureCenter.X - 2, disclosureCenter.Y - 2, 4, 4);
            }

            TextRenderer.DrawText(e.Graphics, row.Item1, Font, new Point(row.Item2 + 18, y), Colors.LightText);
            y += 31;
        }
    }
}

internal sealed class ThemeV2RichTextEditor : UserControl
{
    private readonly RichTextBox _box = new() { BorderStyle = BorderStyle.None, DetectUrls = true };
    [DefaultValue("")] public string Placeholder { get => _box.Text; set => _box.Text = value ?? string.Empty; }
    public ThemeV2RichTextEditor() { Padding = new Padding(10); SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true); _box.Dock = DockStyle.Fill; Controls.Add(_box); ThemeManager.ThemeChanged += OnThemeChanged; ApplyTheme(); }
    protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged; base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTheme();
    private void ApplyTheme() { BackColor = Colors.MediumBackground; _box.BackColor = Colors.MediumBackground; _box.ForeColor = Colors.LightText; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e) => ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, ThemeV2Painting.ParentColor(this), Colors.MediumBackground, _box.Focused ? Colors.ActiveControl : Colors.LightBorder, ThemeManager.Active);
}

internal sealed class ThemeV2PictureBox : ThemeV2AdditionalControl
{
    [DefaultValue("")] public string Caption { get; set; } = string.Empty;
    public ThemeV2PictureBox() { AccessibleRole = AccessibleRole.Graphic; }
    protected override void OnPaint(PaintEventArgs e) { PaintSurface(e, Colors.LightBackground, Colors.LightBorder); using var b = new SolidBrush(Colors.GreySelection); e.Graphics.FillRectangle(b, 12, 12, Width - 24, Height - 34); TextRenderer.DrawText(e.Graphics, "▧", new Font(Font.FontFamily, 18), new Rectangle(0, 12, Width, 30), Colors.DisabledText, TextFormatFlags.HorizontalCenter); TextRenderer.DrawText(e.Graphics, Caption, Font, new Rectangle(8, Height - 20, Width - 16, 18), Colors.DisabledText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
}

internal sealed class ThemeV2ComboBox : ThemeV2AdditionalControl
{
    public List<string> Items { get; } = new();
    private int _selectedIndex;
    [DefaultValue(0)] public int SelectedIndex { get => _selectedIndex; set { var next = Math.Clamp(value, 0, Math.Max(0, Items.Count - 1)); if (_selectedIndex == next) return; _selectedIndex = next; SelectedIndexChanged?.Invoke(this, EventArgs.Empty); Invalidate(); } }
    public string? SelectedItem => Items.Count == 0 ? null : Items[SelectedIndex];
    public event EventHandler? SelectedIndexChanged;
    public ThemeV2ComboBox() { Cursor = Cursors.Hand; TabStop = true; AccessibleRole = AccessibleRole.ComboBox; }
    private void OpenPopup() { if (Items.Count > 0) ThemeV2Popup.Show(this, Items, SelectedIndex, index => SelectedIndex = index); }
    protected override void OnMouseUp(MouseEventArgs e) { if (e.Button == MouseButtons.Left) OpenPopup(); base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode is Keys.F4 or Keys.Enter || (e.Alt && e.KeyCode == Keys.Down)) { OpenPopup(); e.Handled = true; } else if (e.KeyCode == Keys.Down) { SelectedIndex++; e.Handled = true; } else if (e.KeyCode == Keys.Up) { SelectedIndex--; e.Handled = true; } base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e) { PaintSurface(e, Colors.LightBackground, Focused ? Colors.ActiveControl : Colors.LightBorder); var text = Items.Count == 0 ? "Select" : Items[Math.Clamp(SelectedIndex, 0, Items.Count - 1)]; TextRenderer.DrawText(e.Graphics, text, Font, new Rectangle(12, 0, Width - 36, Height), Colors.LightText, TextFormatFlags.VerticalCenter); TextRenderer.DrawText(e.Graphics, "⌄", Font, new Rectangle(Width - 28, 0, 24, Height), Colors.LightText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
}
