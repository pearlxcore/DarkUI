using DarkUI.Config;
using DarkUI.Renderers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ThemeV2TestApp;

internal abstract class ThemeV2AdditionalControl : Control
{
    protected ThemeV2AdditionalControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true);
        ThemeManager.ThemeChanged += OnThemeChanged;
    }
    protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged; base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    protected void PaintSurface(PaintEventArgs e, Color fill, Color border) => ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, ThemeV2Painting.ParentColor(this), fill, border, ThemeManager.Active);
}

internal sealed class ThemeV2SegmentedSelector : ThemeV2AdditionalControl
{
    public List<string> Items { get; } = new();
    [DefaultValue(0)] public int SelectedIndex { get; set; }
    public ThemeV2SegmentedSelector() { Cursor = Cursors.Hand; TabStop = true; AccessibleRole = AccessibleRole.Grouping; }
    protected override void OnMouseUp(MouseEventArgs e) { if (Items.Count > 0) { SelectedIndex = Math.Clamp(e.X * Items.Count / Math.Max(1, Width), 0, Items.Count - 1); Invalidate(); } base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e) { PaintSurface(e, Colors.LightBackground, Focused ? Colors.ActiveControl : Colors.LightBorder); if (Items.Count == 0) return; var w = Width / Items.Count; for (var i = 0; i < Items.Count; i++) { var r = new Rectangle(i * w + 2, 2, i == Items.Count - 1 ? Width - (i * w) - 4 : w - 4, Height - 4); if (i == SelectedIndex) ThemeSurfaceRenderer.DrawSurface(e.Graphics, r, Colors.LightBackground, Colors.BlueSelection, Colors.BlueHighlight, ThemeManager.Active); TextRenderer.DrawText(e.Graphics, Items[i], Font, r, i == SelectedIndex ? Colors.SelectionText : Colors.LightText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); } }
}

internal sealed class ThemeV2DropdownField : ThemeV2AdditionalControl
{
    public List<string> Items { get; } = new();
    [DefaultValue(0)] public int SelectedIndex { get; set; }
    public ThemeV2DropdownField() { Cursor = Cursors.Hand; TabStop = true; AccessibleRole = AccessibleRole.ComboBox; }
    protected override void OnMouseUp(MouseEventArgs e) { if (Items.Count > 0) { SelectedIndex = (SelectedIndex + 1) % Items.Count; Invalidate(); } base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (Items.Count > 0 && e.KeyCode is Keys.Space or Keys.Enter or Keys.Down) { SelectedIndex = (SelectedIndex + 1) % Items.Count; Invalidate(); e.Handled = true; } base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e) { PaintSurface(e, Colors.LightBackground, Focused ? Colors.ActiveControl : Colors.LightBorder); TextRenderer.DrawText(e.Graphics, Items.Count == 0 ? "Select" : Items[Math.Clamp(SelectedIndex, 0, Items.Count - 1)], Font, new Rectangle(12, 0, Width - 36, Height), Colors.LightText, TextFormatFlags.VerticalCenter); using var b = new SolidBrush(Colors.LightText); e.Graphics.FillPolygon(b, new[] { new Point(Width - 20, Height / 2 - 2), new Point(Width - 12, Height / 2 - 2), new Point(Width - 16, Height / 2 + 3) }); }
}

internal sealed class ThemeV2PasswordField : UserControl
{
    private readonly TextBox _box = new() { BorderStyle = BorderStyle.None, UseSystemPasswordChar = true };
    [DefaultValue("")] public string Placeholder { get => _box.PlaceholderText; set => _box.PlaceholderText = value ?? string.Empty; }
    public ThemeV2PasswordField() { Padding = new Padding(12, 10, 12, 8); SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true); _box.Dock = DockStyle.Fill; Controls.Add(_box); ThemeManager.ThemeChanged += OnThemeChanged; ApplyTheme(); }
    protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged; base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTheme();
    private void ApplyTheme() { BackColor = Colors.MediumBackground; _box.BackColor = Colors.MediumBackground; _box.ForeColor = Colors.LightText; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e) => ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, ThemeV2Painting.ParentColor(this), Colors.MediumBackground, _box.Focused ? Colors.ActiveControl : Colors.LightBorder, ThemeManager.Active);
}

internal sealed class ThemeV2MultilineField : UserControl
{
    private readonly TextBox _box = new() { BorderStyle = BorderStyle.None, Multiline = true, ScrollBars = ScrollBars.Vertical };
    [DefaultValue("")] public string Placeholder { get => _box.PlaceholderText; set => _box.PlaceholderText = value ?? string.Empty; }
    public ThemeV2MultilineField() { Padding = new Padding(12, 10, 12, 8); SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true); _box.Dock = DockStyle.Fill; Controls.Add(_box); ThemeManager.ThemeChanged += OnThemeChanged; ApplyTheme(); }
    protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged; base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTheme();
    private void ApplyTheme() { BackColor = Colors.MediumBackground; _box.BackColor = Colors.MediumBackground; _box.ForeColor = Colors.LightText; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e) => ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, ThemeV2Painting.ParentColor(this), Colors.MediumBackground, _box.Focused ? Colors.ActiveControl : Colors.LightBorder, ThemeManager.Active);
}

internal sealed class ThemeV2DateField : ThemeV2AdditionalControl
{
    private DateTime _value;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public DateTime Value { get => _value; set { _value = value; Invalidate(); } }
    public ThemeV2DateField() { Cursor = Cursors.Hand; TabStop = true; AccessibleRole = AccessibleRole.Text; }
    protected override void OnMouseUp(MouseEventArgs e) { Value = Value == default ? DateTime.Today : Value.AddDays(1); base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e) { PaintSurface(e, Colors.LightBackground, Focused ? Colors.ActiveControl : Colors.LightBorder); TextRenderer.DrawText(e.Graphics, (Value == default ? DateTime.Today : Value).ToString("ddd, MMM d"), Font, new Rectangle(12, 0, Width - 12, Height), Colors.LightText, TextFormatFlags.VerticalCenter); }
}

internal sealed class ThemeV2Pagination : ThemeV2AdditionalControl
{
    [DefaultValue(1)] public int PageCount { get; set; } = 1;
    [DefaultValue(1)] public int Page { get; set; } = 1;
    public ThemeV2Pagination() { Cursor = Cursors.Hand; TabStop = true; AccessibleRole = AccessibleRole.PushButton; }
    protected override void OnMouseUp(MouseEventArgs e) { Page = e.X < Width / 2 ? Math.Max(1, Page - 1) : Math.Min(PageCount, Page + 1); Invalidate(); base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e) { PaintSurface(e, Colors.LightBackground, Focused ? Colors.ActiveControl : Colors.LightBorder); TextRenderer.DrawText(e.Graphics, $"‹   {Page} / {PageCount}   ›", Font, ClientRectangle, Colors.LightText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
}

internal sealed class ThemeV2Rating : ThemeV2AdditionalControl
{
    [DefaultValue(0)] public int Value { get; set; }
    public ThemeV2Rating() { Cursor = Cursors.Hand; TabStop = true; AccessibleRole = AccessibleRole.Slider; }
    protected override void OnMouseUp(MouseEventArgs e) { Value = Math.Clamp((e.X * 5 / Math.Max(1, Width)) + 1, 1, 5); Invalidate(); base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e) { e.Graphics.Clear(ThemeV2Painting.ParentColor(this)); var star = "★"; for (var i = 0; i < 5; i++) TextRenderer.DrawText(e.Graphics, star, Font, new Point(i * 27, 8), i < Value ? Colors.BlueHighlight : Colors.DisabledText); }
}

internal sealed class ThemeV2ColorSwatch : ThemeV2AdditionalControl
{
    private readonly Color[] _colors = { Color.FromArgb(111, 129, 241), Color.FromArgb(62, 178, 142), Color.FromArgb(231, 112, 92), Color.FromArgb(232, 182, 67) };
    private int _index;
    public ThemeV2ColorSwatch() { Cursor = Cursors.Hand; TabStop = true; AccessibleRole = AccessibleRole.PushButton; AccessibleName = "Color swatch"; }
    protected override void OnMouseUp(MouseEventArgs e) { _index = (_index + 1) % _colors.Length; Invalidate(); base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e) { PaintSurface(e, _colors[_index], Focused ? Colors.ActiveControl : Colors.LightBorder); }
}

internal sealed class ThemeV2SplitActionButton : ThemeV2AdditionalControl
{
    public event EventHandler? PrimaryClick;
    public event EventHandler? MenuClick;
    public ThemeV2SplitActionButton() { Cursor = Cursors.Hand; TabStop = true; AccessibleRole = AccessibleRole.SplitButton; }
    protected override void OnMouseUp(MouseEventArgs e) { if (e.X < Width - 30) PrimaryClick?.Invoke(this, EventArgs.Empty); else MenuClick?.Invoke(this, EventArgs.Empty); base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e) { PaintSurface(e, Colors.BlueSelection, Focused ? Colors.ActiveControl : Colors.BlueHighlight); TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(10, 0, Width - 40, Height), Colors.SelectionText, TextFormatFlags.VerticalCenter); TextRenderer.DrawText(e.Graphics, "⌄", Font, new Rectangle(Width - 30, 0, 30, Height), Colors.SelectionText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
}

internal sealed class ThemeV2ToastBanner : ThemeV2AdditionalControl
{
    public ThemeV2ToastBanner() { AccessibleRole = AccessibleRole.StaticText; TabStop = false; }
    protected override void OnPaint(PaintEventArgs e) { PaintSurface(e, Colors.DarkBlueBackground, Colors.DarkBlueBorder); using var dot = new SolidBrush(Colors.StatusSuccess); e.Graphics.FillEllipse(dot, 12, (Height - 8) / 2, 8, 8); TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(30, 0, Width - 38, Height), Colors.LightText, TextFormatFlags.VerticalCenter); }
}
