using DarkUI.Config;
using DarkUI.Forms;
using DarkUI.Renderers;
using DarkUI.Animation;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ThemeV2TestApp;

/// <summary>Interactive preview for the theme geometry and surface tokens.</summary>
public sealed class ThemeV2LabForm : DarkForm
{
    private readonly ThemeV2Picker _themePicker = new() { Width = 210 };
    private readonly Label _themeDetails = new() { AutoSize = true };
    private readonly List<Theme> _themes = new();

    public ThemeV2LabForm()
    {
        Text = "DarkUI — Theme V2 Lab";
        Size = new Size(1180, 780);
        MinimumSize = new Size(900, 620);
        Padding = new Padding(20);

        _themes.AddRange(new[] { CreateSoftTheme(), CreateGlassTheme(), CreateBrutalistTheme(), ThemeManager.BuiltIn.Default });
        // Construct every child against the selected theme. Previously, containers
        // captured Default (Charcoal) before the picker applied Soft Modern.
        ThemeManager.Apply(_themes[0]);
        _themePicker.Items.AddRange(_themes.ConvertAll(theme => theme.Name));
        _themePicker.SelectedIndexChanged += (_, _) => ThemeManager.Apply(_themes[_themePicker.SelectedIndex]);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Colors.GreyBackground,
            Padding = new Padding(0),
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 214));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new ThemeV2Surface { Dock = DockStyle.Fill, Elevated = true, Tone = ThemeV2SurfaceTone.Header, Padding = new Padding(22, 14, 22, 14) };
        var title = new Label { Text = "Theme V2 Lab", AutoSize = true, Font = new Font("Segoe UI Semibold", 18f), Location = new Point(20, 11) };
        _themeDetails.Location = new Point(23, 43);
        header.Controls.Add(title);
        header.Controls.Add(_themeDetails);
        _themePicker.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _themePicker.Location = new Point(header.Width - _themePicker.Width - 22, 21);
        header.Resize += (_, _) => _themePicker.Left = header.Width - _themePicker.Width - 22;
        header.Controls.Add(_themePicker);

        var nav = BuildNavigation();
        var content = BuildContent();
        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 2);
        root.Controls.Add(nav, 0, 1);
        root.Controls.Add(content, 1, 1);
        Controls.Add(root);

        ThemeManager.ThemeChanged += (_, _) =>
        {
            root.BackColor = Colors.GreyBackground;
            _themeDetails.Text = $"{ThemeManager.Active.SurfaceStyle} · radius {ThemeManager.Active.CornerRadius}px · spacing {ThemeManager.Active.ContentSpacing}px";
            ApplyPickerTheme();
        };

        _themePicker.SelectedIndex = 0;
        ThemeManager.Refresh();
    }

    private void ApplyPickerTheme()
    {
        _themePicker.ApplyTheme();
    }

    private static Control BuildNavigation()
    {
        var panel = new ThemeV2Surface { Dock = DockStyle.Fill, Margin = new Padding(0, 16, 16, 0), Padding = new Padding(12), Elevated = true };
        var heading = new Label { Text = "WORKSPACE", AutoSize = true, Location = new Point(16, 18), Font = new Font("Segoe UI Semibold", 8.5f) };
        panel.Controls.Add(heading);

        var nav = new ThemeV2NavigationPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, Location = new Point(12, 48), Height = 280 };
        foreach (var item in new[] { "Overview", "Components", "Data", "Automations", "Settings" })
            nav.Controls.Add(new ThemeV2NavItem(item, item == "Overview"));
        void LayoutNavigation()
        {
            // The table-cell margin reduces the actual sidebar client width. Keeping
            // this child inside the padded client rectangle prevents it from covering
            // the outer surface's right border and creating a false second edge.
            nav.Width = Math.Max(1, panel.ClientSize.Width - panel.Padding.Left - panel.Padding.Right);
            foreach (Control item in nav.Controls)
                item.Width = nav.ClientSize.Width;
        }
        panel.ClientSizeChanged += (_, _) => LayoutNavigation();
        LayoutNavigation();
        panel.Controls.Add(nav);
        return panel;
    }

    private static Control BuildContent()
    {
        var scroll = new ThemeV2Page { Dock = DockStyle.Fill, AutoScroll = true, Margin = new Padding(0, 16, 0, 0) };
        var canvas = new ThemeV2FlowPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 0, 12, 20) };
        scroll.Controls.Add(canvas);

        var intro = new ThemeV2Surface { Width = 880, Height = 116, Elevated = true, Padding = new Padding(24, 20, 24, 18), Margin = new Padding(0, 0, 0, 16) };
        intro.Controls.Add(new Label { Text = "A theme can change geometry, not just color.", AutoSize = true, Font = new Font("Segoe UI Semibold", 16f), Location = new Point(24, 20) });
        intro.Controls.Add(new Label { Text = "Switch the preset above to preview surface treatment, radius, spacing, elevation, and control shape.", AutoSize = true, Location = new Point(25, 55) });
        canvas.Controls.Add(intro);

        var metrics = new FlowLayoutPanel { Width = 880, Height = 108, Margin = new Padding(0, 0, 0, 16), WrapContents = false };
        metrics.Controls.Add(new ThemeV2Metric("1,284", "Active projects", "↑ 12% this month"));
        metrics.Controls.Add(new ThemeV2Metric("96.8%", "Automation health", "All systems operational"));
        metrics.Controls.Add(new ThemeV2Metric("18 min", "Time saved today", "Across your workspace"));
        canvas.Controls.Add(metrics);

        var row = new FlowLayoutPanel { Width = 880, Height = 250, Margin = new Padding(0, 0, 0, 16), WrapContents = false };
        row.Controls.Add(BuildControlsCard());
        row.Controls.Add(BuildActivityCard());
        canvas.Controls.Add(row);

        canvas.Controls.Add(BuildControlGallery());
        canvas.Controls.Add(BuildMoreControlsCard());
        canvas.Controls.Add(BuildExtendedControlsCard());
        canvas.Controls.Add(BuildNewControlTypesCard());
        canvas.Controls.Add(BuildAdvancedControlsCard());
        return scroll;
    }

    private static Control BuildControlsCard()
    {
        var card = new ThemeV2Surface { Width = 520, Height = 250, Elevated = true, Padding = new Padding(24) };
        card.Controls.Add(new Label { Text = "Component playground", AutoSize = true, Font = new Font("Segoe UI Semibold", 12f), Location = new Point(24, 22) });

        var field = new ThemeV2TextField { Placeholder = "Search your workspace", Location = new Point(24, 62), Width = 470 };
        var primary = new ThemeV2Button { Text = "Create project", Primary = true, Location = new Point(24, 122), Size = new Size(150, 40) };
        var secondary = new ThemeV2Button { Text = "Preview", Location = new Point(184, 122), Size = new Size(110, 40) };
        var toggle = new ThemeV2Toggle { Text = "Enable notifications", Location = new Point(24, 180), Size = new Size(230, 32) };
        card.Controls.AddRange(new Control[] { field, primary, secondary, toggle });
        return card;
    }

    private static Control BuildActivityCard()
    {
        var card = new ThemeV2Surface { Width = 344, Height = 250, Elevated = true, Padding = new Padding(24) };
        card.Controls.Add(new Label { Text = "Recent activity", AutoSize = true, Font = new Font("Segoe UI Semibold", 12f), Location = new Point(24, 22) });
        var entries = new[] { ("Release candidate created", "Just now"), ("Design review completed", "24 min ago"), ("Analytics synced", "1 hour ago") };
        var top = 68;
        foreach (var (title, time) in entries)
        {
            var dot = new ThemeV2Dot { Location = new Point(25, top + 4), Size = new Size(10, 10) };
            var text = new Label { Text = title, AutoSize = true, Location = new Point(48, top) };
            var stamp = new Label { Text = time, AutoSize = true, Location = new Point(48, top + 22) };
            card.Controls.AddRange(new Control[] { dot, text, stamp });
            top += 62;
        }
        return card;
    }

    private static Control BuildControlGallery()
    {
        var card = new ThemeV2Surface { Width = 880, Height = 246, Elevated = true, Padding = new Padding(24) };
        card.Controls.Add(new Label { Text = "Additional controls", AutoSize = true, Font = new Font("Segoe UI Semibold", 12f), Location = new Point(24, 22) });
        card.Controls.Add(new ThemeV2Checkbox { Text = "Include archived projects", Location = new Point(24, 64), Size = new Size(230, 32) });
        card.Controls.Add(new ThemeV2Slider { Location = new Point(300, 62), Size = new Size(230, 38), Value = 64 });
        card.Controls.Add(new ThemeV2Switch { Text = "Sync enabled", Location = new Point(24, 110), Size = new Size(180, 34), Checked = true });
        card.Controls.Add(new ThemeV2Stepper { Label = "Retries", Location = new Point(300, 106), Size = new Size(160, 38), Value = 3 });
        card.Controls.Add(new ThemeV2Progress { Location = new Point(24, 174), Size = new Size(500, 28), Value = 72, Label = "Storage used" });
        return card;
    }

    private static Control BuildMoreControlsCard()
    {
        var card = new ThemeV2Surface { Width = 880, Height = 206, Elevated = true, Padding = new Padding(24) };
        card.Controls.Add(new Label { Text = "More controls", AutoSize = true, Font = new Font("Segoe UI Semibold", 12f), Location = new Point(24, 22) });
        card.Controls.Add(new ThemeV2Radio { Text = "Weekly summary", Location = new Point(24, 62), Size = new Size(180, 30), Checked = true });
        card.Controls.Add(new ThemeV2Radio { Text = "Real-time alerts", Location = new Point(24, 96), Size = new Size(180, 30) });
        card.Controls.Add(new ThemeV2Chip { Text = "Design", Location = new Point(250, 62), Size = new Size(82, 30), Selected = true });
        card.Controls.Add(new ThemeV2Chip { Text = "Research", Location = new Point(340, 62), Size = new Size(94, 30) });
        card.Controls.Add(new ThemeV2IconButton { Glyph = "+", ToolTipText = "Add item", Location = new Point(470, 58), Size = new Size(38, 38) });
        card.Controls.Add(new ThemeV2IconButton { Glyph = "×", ToolTipText = "Remove item", Location = new Point(516, 58), Size = new Size(38, 38) });
        card.Controls.Add(new ThemeV2Divider { Location = new Point(250, 112), Size = new Size(304, 1) });
        card.Controls.Add(new ThemeV2Notice { Text = "Changes are saved automatically", Location = new Point(250, 132), Size = new Size(430, 38) });
        return card;
    }

    private static Control BuildExtendedControlsCard()
    {
        var card = new ThemeV2Surface { Width = 880, Height = 238, Elevated = true, Padding = new Padding(24) };
        card.Controls.Add(new Label { Text = "Extended controls", AutoSize = true, Font = new Font("Segoe UI Semibold", 12f), Location = new Point(24, 22) });

        // Ten additional, independently usable control instances for interaction testing.
        card.Controls.Add(new ThemeV2Button { Text = "Save changes", Primary = true, Location = new Point(24, 60), Size = new Size(130, 38) });
        card.Controls.Add(new ThemeV2Button { Text = "Discard", Location = new Point(164, 60), Size = new Size(100, 38) });
        card.Controls.Add(new ThemeV2Toggle { Text = "Auto-save", Location = new Point(280, 60), Size = new Size(126, 32) });
        card.Controls.Add(new ThemeV2Switch { Text = "Live mode", Location = new Point(430, 60), Size = new Size(140, 34) });
        card.Controls.Add(new ThemeV2Slider { Location = new Point(600, 60), Size = new Size(190, 38), Value = 38 });

        card.Controls.Add(new ThemeV2Checkbox { Text = "Share with team", Location = new Point(24, 116), Size = new Size(180, 30) });
        card.Controls.Add(new ThemeV2Radio { Text = "Standard access", Location = new Point(224, 116), Size = new Size(160, 30), Checked = true });
        card.Controls.Add(new ThemeV2Chip { Text = "Priority", Location = new Point(404, 116), Size = new Size(86, 30) });
        card.Controls.Add(new ThemeV2Stepper { Label = "Seats", Location = new Point(520, 112), Size = new Size(150, 38), Value = 4 });
        card.Controls.Add(new ThemeV2Progress { Location = new Point(24, 174), Size = new Size(420, 28), Value = 46, Label = "Migration" });
        return card;
    }

    private static Control BuildNewControlTypesCard()
    {
        var card = new ThemeV2Surface { Width = 880, Height = 318, Elevated = true, Padding = new Padding(24) };
        card.Controls.Add(new Label { Text = "New control types", AutoSize = true, Font = new Font("Segoe UI Semibold", 12f), Location = new Point(24, 22) });
        card.Controls.Add(new ThemeV2SegmentedSelector { Items = { "Day", "Week", "Month" }, Location = new Point(24, 60), Size = new Size(238, 34) });
        card.Controls.Add(new ThemeV2DropdownField { Items = { "Standard", "Compact", "Detailed" }, Location = new Point(286, 60), Size = new Size(190, 34) });
        card.Controls.Add(new ThemeV2DateField { Location = new Point(500, 60), Size = new Size(170, 34), Value = DateTime.Today });
        card.Controls.Add(new ThemeV2PasswordField { Location = new Point(24, 116), Size = new Size(238, 40), Placeholder = "Password" });
        card.Controls.Add(new ThemeV2MultilineField { Location = new Point(286, 116), Size = new Size(384, 64), Placeholder = "Add a note…" });
        card.Controls.Add(new ThemeV2Pagination { Location = new Point(24, 202), Size = new Size(190, 34), PageCount = 5 });
        card.Controls.Add(new ThemeV2Rating { Location = new Point(240, 202), Size = new Size(150, 34), Value = 3 });
        card.Controls.Add(new ThemeV2ColorSwatch { Location = new Point(414, 200), Size = new Size(44, 38) });
        card.Controls.Add(new ThemeV2SplitActionButton { Text = "Export", Location = new Point(482, 200), Size = new Size(142, 38) });
        card.Controls.Add(new ThemeV2ToastBanner { Text = "Draft saved", Location = new Point(24, 260), Size = new Size(646, 34) });
        return card;
    }

    private static Control BuildAdvancedControlsCard()
    {
        var card = new ThemeV2Surface { Width = 880, Height = 490, Elevated = true, Padding = new Padding(24) };
        card.Controls.Add(new Label { Text = "Advanced controls", AutoSize = true, Font = new Font("Segoe UI Semibold", 12f), Location = new Point(24, 22) });
        var tabs = new ThemeV2TabControl { Location = new Point(24, 54), Size = new Size(832, 412) };

        var overview = new ThemeV2TabPage("Overview");
        overview.Controls.Add(new ThemeV2DataGrid { Location = new Point(14, 14), Size = new Size(390, 190) });
        overview.Controls.Add(new ThemeV2List { Location = new Point(418, 14), Size = new Size(184, 190) });
        overview.Controls.Add(new ThemeV2Tree { Location = new Point(616, 14), Size = new Size(202, 190) });

        var activity = new ThemeV2TabPage("Activity");
        activity.Controls.Add(new ThemeV2RichTextEditor { Location = new Point(14, 14), Size = new Size(360, 120), Placeholder = "Write a project update…" });
        activity.Controls.Add(new ThemeV2PictureBox { Location = new Point(390, 14), Size = new Size(180, 120), Caption = "Image preview" });
        activity.Controls.Add(new ThemeV2ComboBox { Location = new Point(586, 14), Size = new Size(200, 34), Items = { "All projects", "Active", "Archived" } });

        var files = new ThemeV2TabPage("Files");
        files.Controls.Add(new ThemeV2Toolbar { Location = new Point(14, 14), Size = new Size(804, 32), Items = { "New", "Open", "Save", "Share" } });
        files.Controls.Add(new ThemeV2Toolbar { Location = new Point(14, 166), Size = new Size(804, 24), Compact = true, Items = { "Ready", "3 updates", "Synced just now" } });
        files.Controls.Add(new ThemeV2Pagination { Location = new Point(14, 70), Size = new Size(190, 34), PageCount = 5 });
        files.Controls.Add(new ThemeV2Notice { Location = new Point(220, 68), Size = new Size(360, 38), Text = "Files are synced" });

        tabs.AddPage(overview); // 3 controls
        tabs.AddPage(activity); // 3 controls
        tabs.AddPage(files);    // 4 controls
        card.Controls.Add(tabs);
        return card;
    }

    private static Theme CreateSoftTheme() => new("Soft Modern")
    {
        SurfaceStyle = ThemeSurfaceStyle.Soft, CornerRadius = 12, BorderThickness = 1, ControlHeight = 40, ContentSpacing = 12, Elevation = 4,
        GreyBackground = Color.FromArgb(23, 25, 32), HeaderBackground = Color.FromArgb(31, 34, 43), MediumBackground = Color.FromArgb(36, 40, 50),
        LightBackground = Color.FromArgb(48, 53, 66), LighterBackground = Color.FromArgb(64, 70, 85), LightestBackground = Color.FromArgb(164, 174, 194),
        DarkBackground = Color.FromArgb(17, 19, 25), BlueBackground = Color.FromArgb(50, 65, 102), DarkBlueBackground = Color.FromArgb(34, 43, 66),
        LightBorder = Color.FromArgb(66, 72, 88), DarkBorder = Color.FromArgb(25, 29, 38), DarkBlueBorder = Color.FromArgb(49, 62, 91), LightBlueBorder = Color.FromArgb(95, 121, 182),
        LightText = Color.FromArgb(236, 239, 247), DisabledText = Color.FromArgb(145, 153, 171), BlueHighlight = Color.FromArgb(133, 151, 255), BlueSelection = Color.FromArgb(111, 129, 241),
        GreyHighlight = Color.FromArgb(94, 102, 122), GreySelection = Color.FromArgb(60, 67, 83), DarkGreySelection = Color.FromArgb(46, 51, 64), ActiveControl = Color.FromArgb(174, 186, 255),
        MenuItemToggledOnFill = Color.FromArgb(61, 56, 94), MenuItemToggledOnBorder = Color.FromArgb(160, 145, 255)
    };

    private static Theme CreateGlassTheme() => new("Aurora Glass")
    {
        SurfaceStyle = ThemeSurfaceStyle.Glass, CornerRadius = 16, BorderThickness = 1, ControlHeight = 42, ContentSpacing = 14, Elevation = 5,
        GreyBackground = Color.FromArgb(16, 29, 42), HeaderBackground = Color.FromArgb(28, 53, 73), MediumBackground = Color.FromArgb(34, 65, 86),
        LightBackground = Color.FromArgb(47, 83, 107), LighterBackground = Color.FromArgb(72, 115, 139), LightestBackground = Color.FromArgb(179, 211, 224),
        DarkBackground = Color.FromArgb(9, 20, 31), BlueBackground = Color.FromArgb(47, 96, 138), DarkBlueBackground = Color.FromArgb(27, 57, 83),
        LightBorder = Color.FromArgb(99, 154, 179), DarkBorder = Color.FromArgb(18, 47, 67), DarkBlueBorder = Color.FromArgb(34, 79, 110), LightBlueBorder = Color.FromArgb(117, 209, 225),
        LightText = Color.FromArgb(236, 250, 255), DisabledText = Color.FromArgb(151, 185, 197), BlueHighlight = Color.FromArgb(111, 225, 222), BlueSelection = Color.FromArgb(61, 166, 191),
        GreyHighlight = Color.FromArgb(92, 139, 158), GreySelection = Color.FromArgb(52, 103, 125), DarkGreySelection = Color.FromArgb(32, 71, 92), ActiveControl = Color.FromArgb(173, 244, 239),
        MenuItemToggledOnFill = Color.FromArgb(36, 87, 100), MenuItemToggledOnBorder = Color.FromArgb(121, 237, 224)
    };

    private static Theme CreateBrutalistTheme() => new("Neo Brutalist")
    {
        SurfaceStyle = ThemeSurfaceStyle.NeoBrutalist, CornerRadius = 2, BorderThickness = 3, ControlHeight = 42, ContentSpacing = 10, Elevation = 0,
        GreyBackground = Color.FromArgb(245, 238, 219), HeaderBackground = Color.FromArgb(255, 248, 228), MediumBackground = Color.FromArgb(255, 255, 255),
        LightBackground = Color.FromArgb(255, 247, 217), LighterBackground = Color.FromArgb(255, 224, 112), LightestBackground = Color.FromArgb(55, 45, 35),
        DarkBackground = Color.FromArgb(29, 27, 24), BlueBackground = Color.FromArgb(255, 217, 0), DarkBlueBackground = Color.FromArgb(255, 190, 0),
        LightBorder = Color.FromArgb(29, 27, 24), DarkBorder = Color.FromArgb(29, 27, 24), DarkBlueBorder = Color.FromArgb(29, 27, 24), LightBlueBorder = Color.FromArgb(29, 27, 24),
        LightText = Color.FromArgb(29, 27, 24), DisabledText = Color.FromArgb(117, 105, 88), BlueHighlight = Color.FromArgb(231, 74, 59), BlueSelection = Color.FromArgb(255, 217, 0),
        GreyHighlight = Color.FromArgb(225, 216, 198), GreySelection = Color.FromArgb(231, 74, 59), DarkGreySelection = Color.FromArgb(201, 192, 174), ActiveControl = Color.FromArgb(231, 74, 59),
        MenuItemToggledOnFill = Color.FromArgb(255, 217, 0), MenuItemToggledOnBorder = Color.FromArgb(29, 27, 24)
    };
}

internal enum ThemeV2SurfaceTone { Medium, Header, Light }

internal static class ThemeV2Painting
{
    public static Color ParentColor(Control control) => control.Parent?.BackColor ?? Colors.GreyBackground;
}

internal class ThemeV2Surface : Panel
{
    [DefaultValue(false)]
    public bool Elevated { get; set; }
    [DefaultValue(ThemeV2SurfaceTone.Medium)]
    public ThemeV2SurfaceTone Tone { get; set; }

    public ThemeV2Surface()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
        ThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }

    private Color SurfaceColor => Tone switch
    {
        ThemeV2SurfaceTone.Header => Colors.HeaderBackground,
        ThemeV2SurfaceTone.Light => Colors.LightBackground,
        _ => Colors.MediumBackground
    };

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTheme();
    private void ApplyTheme() { BackColor = SurfaceColor; ForeColor = Colors.LightText; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e) => ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, ThemeV2Painting.ParentColor(this), SurfaceColor, Colors.LightBorder, ThemeManager.Active, Elevated);
}

internal sealed class ThemeV2Page : Panel
{
    public ThemeV2Page()
    {
        DoubleBuffered = true;
        ThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTheme();
    private void ApplyTheme() { BackColor = Colors.GreyBackground; ForeColor = Colors.LightText; Invalidate(true); }
}

internal sealed class ThemeV2FlowPanel : FlowLayoutPanel
{
    public ThemeV2FlowPanel()
    {
        DoubleBuffered = true;
        ThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTheme();
    private void ApplyTheme() { BackColor = Colors.GreyBackground; ForeColor = Colors.LightText; Invalidate(true); }
}

/// <summary>
/// A layout-only child of the sidebar. It deliberately draws no border or opaque
/// background, so the surrounding ThemeV2Surface is the only visible sidebar edge.
/// </summary>
internal sealed class ThemeV2NavigationPanel : FlowLayoutPanel
{
    public ThemeV2NavigationPanel()
    {
        BorderStyle = BorderStyle.None;
        AutoScroll = false;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e) { }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(ThemeV2Painting.ParentColor(this));
        base.OnPaint(e);
    }
}

internal sealed class ThemeV2Metric : ThemeV2Surface
{
    private readonly string _value; private readonly string _label; private readonly string _note;
    public ThemeV2Metric(string value, string label, string note) { _value = value; _label = label; _note = note; Size = new Size(270, 108); Margin = new Padding(0, 0, 12, 0); Elevated = true; Font = new Font("Segoe UI Semibold", 18f); }
    protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); TextRenderer.DrawText(e.Graphics, _value, Font, new Point(20, 18), Colors.LightText); TextRenderer.DrawText(e.Graphics, _label, SystemFonts.MessageBoxFont, new Point(21, 51), Colors.DisabledText); TextRenderer.DrawText(e.Graphics, _note, SystemFonts.MessageBoxFont, new Point(21, 73), Colors.BlueHighlight); }
}

internal sealed class ThemeV2NavItem : Control
{
    private readonly bool _selected;
    private readonly ThemeMotion _hover;
    public ThemeV2NavItem(string text, bool selected) { Text = text; _selected = selected; Size = new Size(180, 40); Margin = new Padding(0, 0, 0, 4); Cursor = Cursors.Hand; SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true); _hover = new ThemeMotion(this, Invalidate); ThemeManager.ThemeChanged += OnThemeChanged; }
    protected override void Dispose(bool disposing) { if (disposing) { ThemeManager.ThemeChanged -= OnThemeChanged; _hover.Dispose(); } base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    protected override void OnMouseEnter(EventArgs e) { if (!_selected) _hover.AnimateTo(1, 115); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { if (!_selected) _hover.AnimateTo(0, 145); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var canvas = ThemeV2Painting.ParentColor(this);
        if (_selected)
        {
            ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, canvas, Colors.BlueSelection, Colors.BlueHighlight, ThemeManager.Active);
        }
        else if (_hover.Value > 0.001)
        {
            var fill = ThemeMotion.Blend(canvas, Colors.LightBackground, 0.75 * _hover.Value);
            var border = ThemeMotion.Blend(canvas, Colors.LightBorder, _hover.Value);
            ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, canvas, fill, border, ThemeManager.Active);
        }
        else e.Graphics.Clear(canvas);

        var textBounds = new Rectangle(14 + (int)Math.Round(2 * _hover.Value), 0, Width - 20, Height);
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, _selected ? Colors.SelectionText : Colors.LightText, TextFormatFlags.VerticalCenter);
    }
}

internal sealed class ThemeV2TextField : UserControl
{
    private readonly TextBox _textBox = new() { BorderStyle = BorderStyle.None };
    private string _placeholder = string.Empty;
    [DefaultValue("")]
    public string Placeholder { get => _placeholder; set { _placeholder = value ?? string.Empty; _textBox.PlaceholderText = _placeholder; } }
    public ThemeV2TextField() { Height = 40; Padding = new Padding(14, 10, 14, 8); SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true); _textBox.Dock = DockStyle.Fill; _textBox.GotFocus += (_, _) => Invalidate(); _textBox.LostFocus += (_, _) => Invalidate(); Controls.Add(_textBox); ThemeManager.ThemeChanged += (_, _) => ApplyTheme(); ApplyTheme(); }
    private void ApplyTheme() { BackColor = Colors.MediumBackground; _textBox.BackColor = Colors.MediumBackground; _textBox.ForeColor = Colors.LightText; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e) => ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, ThemeV2Painting.ParentColor(this), Colors.MediumBackground, _textBox.Focused ? Colors.BlueHighlight : Colors.LightBorder, ThemeManager.Active);
}

internal sealed class ThemeV2Button : Control
{
    [DefaultValue(false)]
    public bool Primary { get; set; }
    private readonly ThemeMotion _hover;
    private readonly ThemeMotion _press;
    public ThemeV2Button() { Cursor = Cursors.Hand; TabStop = true; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true); _hover = new ThemeMotion(this, Invalidate); _press = new ThemeMotion(this, Invalidate); ThemeManager.ThemeChanged += OnThemeChanged; }
    protected override void Dispose(bool disposing) { if (disposing) { ThemeManager.ThemeChanged -= OnThemeChanged; _hover.Dispose(); _press.Dispose(); } base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    protected override void OnMouseEnter(EventArgs e) { _hover.AnimateTo(1, 140); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover.AnimateTo(0, 140); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) _press.AnimateTo(1, 70); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _press.AnimateTo(0, 100); if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location)) OnClick(EventArgs.Empty); base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode is Keys.Space or Keys.Enter) { OnClick(EventArgs.Empty); e.Handled = true; } base.OnKeyDown(e); }
    protected override void OnEnabledChanged(EventArgs e) { Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var baseFill = Primary ? Colors.BlueSelection : Colors.LightBackground;
        var hoverStrength = Primary ? 0.08 : 0.16;
        var fill = ThemeMotion.Blend(baseFill, Colors.BlueHighlight, _hover.Value * hoverStrength);
        fill = ThemeMotion.Blend(fill, Colors.DarkBackground, _press.Value * 0.16);
        var border = Focused
            ? Colors.ActiveControl
            : ThemeMotion.Blend(Primary ? Colors.BlueHighlight : Colors.LightBorder, Colors.BlueHighlight, _hover.Value);
        if (!Enabled) { fill = Colors.MediumBackground; border = Colors.DarkBorder; }

        var pressedInset = (int)Math.Round(_press.Value);
        var surfaceBounds = pressedInset == 0 ? ClientRectangle : Rectangle.Inflate(ClientRectangle, -pressedInset, -pressedInset);
        ThemeSurfaceRenderer.DrawSurface(e.Graphics, surfaceBounds, ThemeV2Painting.ParentColor(this), fill, border, ThemeManager.Active, Primary);
        var textBounds = surfaceBounds;
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, Enabled ? (Primary ? Colors.SelectionText : Colors.LightText) : Colors.DisabledText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

internal sealed class ThemeV2Toggle : Control
{
    public bool Checked { get; private set; }
    private readonly ThemeMotion _hover;
    private readonly ThemeMotion _press;
    public ThemeV2Toggle() { Cursor = Cursors.Hand; TabStop = true; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true); _hover = new ThemeMotion(this, Invalidate); _press = new ThemeMotion(this, Invalidate); ThemeManager.ThemeChanged += OnThemeChanged; }
    protected override void Dispose(bool disposing) { if (disposing) { ThemeManager.ThemeChanged -= OnThemeChanged; _hover.Dispose(); _press.Dispose(); } base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    protected override void OnClick(EventArgs e) { Checked = !Checked; Invalidate(); base.OnClick(e); }
    protected override void OnMouseEnter(EventArgs e) { if (Enabled) _hover.AnimateTo(1, 130); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover.AnimateTo(0, 150); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (Enabled && e.Button == MouseButtons.Left) _press.AnimateTo(1, 75); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _press.AnimateTo(0, 100); if (Enabled && e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location)) OnClick(EventArgs.Empty); base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode is Keys.Space or Keys.Enter) { OnClick(EventArgs.Empty); e.Handled = true; } base.OnKeyDown(e); }
    protected override void OnEnabledChanged(EventArgs e) { Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnPaint(PaintEventArgs e) { var fill = ThemeMotion.Blend(Checked ? Colors.BlueSelection : Colors.LightBackground, Colors.BlueHighlight, _hover.Value * .12); fill = ThemeMotion.Blend(fill, Colors.DarkBackground, _press.Value * .16); var border = Focused ? Colors.ActiveControl : ThemeMotion.Blend(Colors.LightBorder, Colors.BlueHighlight, _hover.Value); if (!Enabled) { fill = Colors.MediumBackground; border = Colors.DarkBorder; } var inset = (int)Math.Round(_press.Value); var bounds = inset == 0 ? ClientRectangle : Rectangle.Inflate(ClientRectangle, -inset, -inset); ThemeSurfaceRenderer.DrawSurface(e.Graphics, bounds, ThemeV2Painting.ParentColor(this), fill, border, ThemeManager.Active); TextRenderer.DrawText(e.Graphics, Text, Font, bounds, Enabled ? (Checked ? Colors.SelectionText : Colors.LightText) : Colors.DisabledText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
}

internal sealed class ThemeV2Radio : Control
{
    private bool _checked;
    private readonly ThemeMotion _selection;
    private readonly ThemeMotion _hover;
    [DefaultValue(false)]
    public bool Checked { get => _checked; set { if (_checked == value) return; _checked = value; _selection.AnimateTo(value ? 1 : 0, 170); if (value) UncheckSiblings(); } }
    public ThemeV2Radio() { Cursor = Cursors.Hand; TabStop = true; AccessibleRole = AccessibleRole.RadioButton; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true); _selection = new ThemeMotion(this, Invalidate); _hover = new ThemeMotion(this, Invalidate); ThemeManager.ThemeChanged += OnThemeChanged; }
    protected override void Dispose(bool disposing) { if (disposing) { ThemeManager.ThemeChanged -= OnThemeChanged; _selection.Dispose(); _hover.Dispose(); } base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    protected override void OnMouseEnter(EventArgs e) { if (Enabled) _hover.AnimateTo(1, 120); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover.AnimateTo(0, 120); base.OnMouseLeave(e); }
    protected override void OnMouseUp(MouseEventArgs e) { if (Enabled && e.Button == MouseButtons.Left) Checked = true; base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (Enabled && e.KeyCode is Keys.Space or Keys.Enter) { Checked = true; e.Handled = true; } if (Enabled && e.KeyCode is Keys.Up or Keys.Left or Keys.Down or Keys.Right) MoveFocus(e.KeyCode is Keys.Down or Keys.Right ? 1 : -1); base.OnKeyDown(e); }
    private void UncheckSiblings() { if (Parent == null) return; foreach (Control control in Parent.Controls) if (control is ThemeV2Radio radio && radio != this) radio.Checked = false; }
    private void MoveFocus(int direction) { if (Parent == null) return; var radios = Parent.Controls.OfType<ThemeV2Radio>().ToList(); var index = radios.IndexOf(this); if (index < 0) return; var next = radios[(index + direction + radios.Count) % radios.Count]; next.Focus(); next.Checked = true; }
    protected override void OnPaint(PaintEventArgs e) { e.Graphics.Clear(ThemeV2Painting.ParentColor(this)); var circle = new Rectangle(0, (Height - 20) / 2, 20, 20); var borderColor = ThemeMotion.Blend(Colors.LightBorder, Colors.BlueHighlight, Math.Max(Math.Max(_selection.Value, _hover.Value), Focused ? 1 : 0)); using var border = new Pen(borderColor, 1.5f); using var surface = new SolidBrush(Colors.LightBackground); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.FillEllipse(surface, circle); e.Graphics.DrawEllipse(border, circle); var dotSize = (int)Math.Round(10 * _selection.Value); if (dotSize > 0) { using var fill = new SolidBrush(Colors.BlueSelection); e.Graphics.FillEllipse(fill, new Rectangle(circle.X + ((20 - dotSize) / 2), circle.Y + ((20 - dotSize) / 2), dotSize, dotSize)); } var textColor = Enabled ? Colors.LightText : Colors.DisabledText; TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(30, 0, Width - 30, Height), textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis); }
}

internal sealed class ThemeV2Chip : Control
{
    private bool _selected;
    private readonly ThemeMotion _selection;
    private readonly ThemeMotion _hover;
    private readonly ThemeMotion _press;
    [DefaultValue(false)]
    public bool Selected { get => _selected; set { if (_selected == value) return; _selected = value; _selection.AnimateTo(value ? 1 : 0, 170); } }
    public ThemeV2Chip() { Cursor = Cursors.Hand; TabStop = true; AccessibleRole = AccessibleRole.CheckButton; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true); _selection = new ThemeMotion(this, Invalidate); _hover = new ThemeMotion(this, Invalidate); _press = new ThemeMotion(this, Invalidate); ThemeManager.ThemeChanged += OnThemeChanged; }
    protected override void Dispose(bool disposing) { if (disposing) { ThemeManager.ThemeChanged -= OnThemeChanged; _selection.Dispose(); _hover.Dispose(); _press.Dispose(); } base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    protected override void OnMouseEnter(EventArgs e) { if (Enabled) _hover.AnimateTo(1, 120); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover.AnimateTo(0, 120); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (Enabled && e.Button == MouseButtons.Left) _press.AnimateTo(1, 75); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _press.AnimateTo(0, 100); if (Enabled && e.Button == MouseButtons.Left) Selected = !Selected; base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (Enabled && e.KeyCode is Keys.Space or Keys.Enter) { Selected = !Selected; e.Handled = true; } base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e) { var fill = ThemeMotion.Blend(Colors.LightBackground, Colors.BlueSelection, _selection.Value); fill = ThemeMotion.Blend(fill, Colors.BlueHighlight, _hover.Value * .12); var border = ThemeMotion.Blend(Colors.LightBorder, Colors.BlueHighlight, Math.Max(Math.Max(_selection.Value, _hover.Value), Focused ? 1 : 0)); ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, ThemeV2Painting.ParentColor(this), fill, border, ThemeManager.Active); var bounds = ClientRectangle; bounds.Offset(0, (int)Math.Round(_press.Value)); TextRenderer.DrawText(e.Graphics, Text, Font, bounds, ThemeMotion.Blend(Colors.LightText, Colors.SelectionText, _selection.Value), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
}

internal sealed class ThemeV2IconButton : Control
{
    private readonly ToolTip _toolTip = new();
    private string _glyph = string.Empty;
    private readonly ThemeMotion _hover;
    private readonly ThemeMotion _press;
    [DefaultValue("")]
    public string Glyph { get => _glyph; set { _glyph = value ?? string.Empty; Invalidate(); } }
    [DefaultValue("")]
    public string ToolTipText { get => _toolTip.GetToolTip(this) ?? string.Empty; set { var text = value ?? string.Empty; _toolTip.SetToolTip(this, text); AccessibleName = text; AccessibleDescription = text; } }
    public ThemeV2IconButton() { Cursor = Cursors.Hand; TabStop = true; AccessibleRole = AccessibleRole.PushButton; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true); _hover = new ThemeMotion(this, Invalidate); _press = new ThemeMotion(this, Invalidate); ThemeManager.ThemeChanged += OnThemeChanged; }
    protected override void Dispose(bool disposing) { if (disposing) { ThemeManager.ThemeChanged -= OnThemeChanged; _toolTip.Dispose(); _hover.Dispose(); _press.Dispose(); } base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    protected override void OnEnabledChanged(EventArgs e) { Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnMouseEnter(EventArgs e) { if (Enabled) _hover.AnimateTo(1, 120); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover.AnimateTo(0, 120); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (Enabled && e.Button == MouseButtons.Left) _press.AnimateTo(1, 75); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _press.AnimateTo(0, 100); if (Enabled && e.Button == MouseButtons.Left) OnClick(EventArgs.Empty); base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (Enabled && e.KeyCode is Keys.Space or Keys.Enter) { OnClick(EventArgs.Empty); e.Handled = true; } base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e) { var fill = ThemeMotion.Blend(Colors.LightBackground, Colors.BlueHighlight, _hover.Value * .16); var border = Focused || _hover.Value > 0 ? Colors.BlueHighlight : Colors.LightBorder; if (!Enabled) { fill = Colors.MediumBackground; border = Colors.DarkBorder; } ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, ThemeV2Painting.ParentColor(this), fill, border, ThemeManager.Active); var bounds = ClientRectangle; bounds.Offset(0, (int)Math.Round(_press.Value)); TextRenderer.DrawText(e.Graphics, Glyph, Font, bounds, Enabled ? Colors.LightText : Colors.DisabledText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
}

internal enum ThemeV2DividerOrientation { Horizontal, Vertical }

internal sealed class ThemeV2Divider : Control
{
    [DefaultValue(ThemeV2DividerOrientation.Horizontal)]
    public ThemeV2DividerOrientation Orientation { get; set; }
    public ThemeV2Divider() { TabStop = false; Cursor = Cursors.Default; AccessibleRole = AccessibleRole.Separator; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true); ThemeManager.ThemeChanged += OnThemeChanged; }
    protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged; base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    protected override bool IsInputKey(Keys keyData) => false;
    protected override void OnPaint(PaintEventArgs e) { e.Graphics.Clear(ThemeV2Painting.ParentColor(this)); using var pen = new Pen(Colors.LightBorder); if (Orientation == ThemeV2DividerOrientation.Horizontal) e.Graphics.DrawLine(pen, 0, Height / 2, Width - 1, Height / 2); else e.Graphics.DrawLine(pen, Width / 2, 0, Width / 2, Height - 1); }
}

internal enum ThemeV2NoticeSeverity { Info, Success, Warning, Error }

internal sealed class ThemeV2Notice : Control
{
    private string _title = string.Empty;
    private string _actionText = string.Empty;
    private bool _dismissible;
    private ThemeV2NoticeSeverity _severity;
    [DefaultValue(ThemeV2NoticeSeverity.Info)]
    public ThemeV2NoticeSeverity Severity { get => _severity; set { if (_severity == value) return; _severity = value; Invalidate(); } }
    [DefaultValue("")]
    public string Title { get => _title; set { _title = value ?? string.Empty; Invalidate(); } }
    [DefaultValue("")]
    public string Message { get => Text; set { Text = value ?? string.Empty; Invalidate(); } }
    [DefaultValue("")]
    public string ActionText { get => _actionText; set { _actionText = value ?? string.Empty; UpdateInteractionState(); Invalidate(); } }
    [DefaultValue(false)]
    public bool Dismissible { get => _dismissible; set { _dismissible = value; UpdateInteractionState(); Invalidate(); } }
    public event EventHandler? ActionClicked;
    public ThemeV2Notice() { TabStop = false; Cursor = Cursors.Default; AccessibleRole = AccessibleRole.StaticText; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true); ThemeManager.ThemeChanged += OnThemeChanged; }
    protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged; base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    private void UpdateInteractionState() { TabStop = _dismissible || !string.IsNullOrEmpty(_actionText); AccessibleRole = TabStop ? AccessibleRole.PushButton : AccessibleRole.StaticText; AccessibleName = string.IsNullOrEmpty(_title) ? Text : _title; AccessibleDescription = Text; }
    private Rectangle ActionBounds => string.IsNullOrEmpty(_actionText) ? Rectangle.Empty : new Rectangle(Width - (_dismissible ? 120 : 90), 0, 82, Height);
    private Rectangle DismissBounds => _dismissible ? new Rectangle(Width - 28, 0, 28, Height) : Rectangle.Empty;
    protected override void OnMouseMove(MouseEventArgs e) { Cursor = ActionBounds.Contains(e.Location) || DismissBounds.Contains(e.Location) ? Cursors.Hand : Cursors.Default; base.OnMouseMove(e); }
    protected override void OnMouseLeave(EventArgs e) { Cursor = Cursors.Default; base.OnMouseLeave(e); }
    protected override void OnMouseUp(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { if (DismissBounds.Contains(e.Location)) Visible = false; else if (ActionBounds.Contains(e.Location)) ActionClicked?.Invoke(this, EventArgs.Empty); } base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode is Keys.Space or Keys.Enter) { if (Dismissible) Visible = false; else ActionClicked?.Invoke(this, EventArgs.Empty); e.Handled = true; } base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e) { var accent = Severity switch { ThemeV2NoticeSeverity.Success => Colors.StatusSuccess, ThemeV2NoticeSeverity.Warning => Colors.StatusWarning, ThemeV2NoticeSeverity.Error => Colors.StatusError, _ => Colors.BlueHighlight }; var glyph = Severity switch { ThemeV2NoticeSeverity.Success => "✓", ThemeV2NoticeSeverity.Warning => "!", ThemeV2NoticeSeverity.Error => "×", _ => "i" }; ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, ThemeV2Painting.ParentColor(this), Colors.DarkBlueBackground, Colors.DarkBlueBorder, ThemeManager.Active); using var dot = new SolidBrush(accent); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.FillEllipse(dot, 12, (Height - 14) / 2, 14, 14); TextRenderer.DrawText(e.Graphics, glyph, Font, new Rectangle(12, 0, 14, Height), Colors.SelectionText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); var messageLeft = 36; var right = string.IsNullOrEmpty(_actionText) ? (_dismissible ? 30 : 8) : (_dismissible ? 120 : 90); var message = string.IsNullOrEmpty(_title) ? Text : $"{_title}  {Text}"; TextRenderer.DrawText(e.Graphics, message, Font, new Rectangle(messageLeft, 0, Math.Max(1, Width - messageLeft - right), Height), Colors.LightText, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis); if (!string.IsNullOrEmpty(_actionText)) TextRenderer.DrawText(e.Graphics, _actionText, Font, ActionBounds, accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); if (_dismissible) TextRenderer.DrawText(e.Graphics, "×", Font, DismissBounds, Colors.LightText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
}

internal sealed class ThemeV2Switch : Control
{
    private bool _checked;
    private readonly ThemeMotion _position;
    private readonly ThemeMotion _press;
    [DefaultValue(false)]
    public bool Checked { get => _checked; set { if (_checked == value) return; _checked = value; _position.AnimateTo(value ? 1 : 0, 180); } }

    public ThemeV2Switch()
    {
        Cursor = Cursors.Hand;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true);
        _position = new ThemeMotion(this, Invalidate);
        _press = new ThemeMotion(this, Invalidate);
        ThemeManager.ThemeChanged += OnThemeChanged;
    }

    protected override void Dispose(bool disposing) { if (disposing) { ThemeManager.ThemeChanged -= OnThemeChanged; _position.Dispose(); _press.Dispose(); } base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) _press.AnimateTo(1, 80); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _press.AnimateTo(0, 110); if (e.Button == MouseButtons.Left) Checked = !Checked; base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode is Keys.Space or Keys.Enter) { Checked = !Checked; e.Handled = true; } base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var canvas = ThemeV2Painting.ParentColor(this);
        ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, canvas, canvas, canvas, ThemeManager.Active);
        var track = new Rectangle(0, (Height - 24) / 2, 44, 24);
        var position = _position.Value;
        ThemeSurfaceRenderer.DrawSurface(e.Graphics, track, canvas, ThemeMotion.Blend(Colors.LightBackground, Colors.BlueSelection, position), Focused ? Colors.BlueHighlight : Colors.LightBorder, ThemeManager.Active);
        var thumbX = track.Left + 4 + (int)Math.Round((track.Width - 24) * position);
        var diameter = 16 + (int)Math.Round(2 * _press.Value);
        var thumbY = track.Top + ((track.Height - diameter) / 2);
        using var brush = new SolidBrush(Checked ? Colors.SelectionText : Colors.DisabledText);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillEllipse(brush, thumbX - ((diameter - 16) / 2), thumbY, diameter, diameter);
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(56, 0, Width - 56, Height), Colors.LightText, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class ThemeV2Stepper : Control
{
    private int _value;
    private string _label = string.Empty;
    private readonly ThemeMotion _leftHover;
    private readonly ThemeMotion _rightHover;
    private readonly ThemeMotion _valuePulse;
    [DefaultValue(0)]
    public int Value { get => _value; set { var next = Math.Clamp(value, 0, 99); if (_value == next) return; _value = next; Invalidate(); } }
    [DefaultValue("")]
    public string Label { get => _label; set { _label = value ?? string.Empty; Invalidate(); } }

    public ThemeV2Stepper()
    {
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true);
        _leftHover = new ThemeMotion(this, Invalidate);
        _rightHover = new ThemeMotion(this, Invalidate);
        _valuePulse = new ThemeMotion(this, Invalidate);
        ThemeManager.ThemeChanged += OnThemeChanged;
    }

    protected override void Dispose(bool disposing) { if (disposing) { ThemeManager.ThemeChanged -= OnThemeChanged; _leftHover.Dispose(); _rightHover.Dispose(); _valuePulse.Dispose(); } base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    protected override void OnMouseMove(MouseEventArgs e)
    {
        var overLeft = e.X < 34;
        var overRight = e.X > Width - 34;
        Cursor = overLeft || overRight ? Cursors.Hand : Cursors.Default;
        _leftHover.AnimateTo(overLeft ? 1 : 0, 110);
        _rightHover.AnimateTo(overRight ? 1 : 0, 110);
        base.OnMouseMove(e);
    }
    protected override void OnMouseLeave(EventArgs e) { Cursor = Cursors.Default; _leftHover.AnimateTo(0, 110); _rightHover.AnimateTo(0, 110); base.OnMouseLeave(e); }
    protected override void OnMouseUp(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { if (e.X < 34) ChangeValue(-1); else if (e.X > Width - 34) ChangeValue(1); } base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) { ChangeValue(-1); e.Handled = true; } if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) { ChangeValue(1); e.Handled = true; } base.OnKeyDown(e); }
    private void ChangeValue(int delta) { Value += delta; _valuePulse.SnapTo(1); _valuePulse.AnimateTo(0, 130); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var canvas = ThemeV2Painting.ParentColor(this);
        ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, canvas, Colors.LightBackground, Focused ? Colors.BlueHighlight : Colors.LightBorder, ThemeManager.Active);
        var leftColor = ThemeMotion.Blend(Colors.LightText, Colors.BlueHighlight, _leftHover.Value);
        var rightColor = ThemeMotion.Blend(Colors.LightText, Colors.BlueHighlight, _rightHover.Value);
        var valueColor = ThemeMotion.Blend(Colors.LightText, Colors.BlueHighlight, _valuePulse.Value * 0.7);
        TextRenderer.DrawText(e.Graphics, "−", Font, new Rectangle(0, 0, 34, Height), leftColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        var valueBounds = new Rectangle(34, -(int)Math.Round(_valuePulse.Value * 2), Width - 68, Height);
        TextRenderer.DrawText(e.Graphics, $"{Label}  {Value}", Font, valueBounds, valueColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(e.Graphics, "+", Font, new Rectangle(Width - 34, 0, 34, Height), rightColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

internal sealed class ThemeV2Checkbox : Control
{
    private bool _hovered;
    private bool _checked;
    [DefaultValue(false)]
    public bool Checked { get => _checked; set { if (_checked == value) return; _checked = value; Invalidate(); } }

    public ThemeV2Checkbox()
    {
        Cursor = Cursors.Hand;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true);
        ThemeManager.ThemeChanged += OnThemeChanged;
    }

    protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged; base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseUp(MouseEventArgs e) { if (e.Button == MouseButtons.Left) Checked = !Checked; base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode is Keys.Space or Keys.Enter) { Checked = !Checked; e.Handled = true; } base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var canvas = ThemeV2Painting.ParentColor(this);
        var box = new Rectangle(0, Math.Max(0, (Height - 22) / 2), 22, 22);
        ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, canvas, canvas, canvas, ThemeManager.Active);
        ThemeSurfaceRenderer.DrawSurface(e.Graphics, box, canvas, Checked ? Colors.BlueSelection : Colors.LightBackground, _hovered || Focused ? Colors.BlueHighlight : Colors.LightBorder, ThemeManager.Active);
        if (Checked)
        {
            using var pen = new Pen(Colors.SelectionText, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawLines(pen, new[] { new Point(6, box.Top + 11), new Point(10, box.Top + 15), new Point(17, box.Top + 7) });
        }
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(32, 0, Width - 32, Height), Colors.LightText, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class ThemeV2Slider : Control
{
    private int _value;
    [DefaultValue(0)]
    public int Value { get => _value; set { var next = Math.Clamp(value, 0, 100); if (_value == next) return; _value = next; Invalidate(); } }

    public ThemeV2Slider()
    {
        Cursor = Cursors.Hand;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true);
        ThemeManager.ThemeChanged += OnThemeChanged;
    }

    protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged; base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    protected override void OnMouseDown(MouseEventArgs e) { Focus(); SetValueFromX(e.X); base.OnMouseDown(e); }
    protected override void OnMouseMove(MouseEventArgs e) { if (e.Button == MouseButtons.Left) SetValueFromX(e.X); base.OnMouseMove(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Left) { Value -= 1; e.Handled = true; } if (e.KeyCode == Keys.Right) { Value += 1; e.Handled = true; } base.OnKeyDown(e); }
    private void SetValueFromX(int x) => Value = (int)Math.Round(100d * Math.Clamp(x - 10, 0, Math.Max(1, Width - 20)) / Math.Max(1, Width - 20));
    protected override void OnPaint(PaintEventArgs e)
    {
        var canvas = ThemeV2Painting.ParentColor(this);
        var track = new Rectangle(0, (Height - 8) / 2, Width, 8);
        ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, canvas, canvas, canvas, ThemeManager.Active);
        ThemeSurfaceRenderer.DrawSurface(e.Graphics, track, canvas, Colors.LightBackground, Colors.LightBorder, ThemeManager.Active);
        var thumbX = 10 + (int)Math.Round((Width - 20) * (Value / 100d));
        using var brush = new SolidBrush(Colors.BlueSelection);
        using var border = new Pen(Focused ? Colors.BlueHighlight : Colors.LightBorder);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillEllipse(brush, thumbX - 8, Height / 2 - 8, 16, 16);
        e.Graphics.DrawEllipse(border, thumbX - 8, Height / 2 - 8, 16, 16);
    }
}

internal sealed class ThemeV2Progress : Control
{
    private int _value;
    private string _label = string.Empty;
    [DefaultValue(0)]
    public int Value { get => _value; set { _value = Math.Clamp(value, 0, 100); Invalidate(); } }
    [DefaultValue("")]
    public string Label { get => _label; set { _label = value ?? string.Empty; Invalidate(); } }

    public ThemeV2Progress() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true); ThemeManager.ThemeChanged += OnThemeChanged; }
    protected override void Dispose(bool disposing) { if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged; base.Dispose(disposing); }
    private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();
    protected override void OnPaint(PaintEventArgs e)
    {
        var canvas = ThemeV2Painting.ParentColor(this);
        var track = new Rectangle(0, 14, Width, 10);
        ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, canvas, canvas, canvas, ThemeManager.Active);
        ThemeSurfaceRenderer.DrawSurface(e.Graphics, track, canvas, Colors.LightBackground, Colors.LightBorder, ThemeManager.Active);
        var fillWidth = Math.Max(0, (int)Math.Round(track.Width * (Value / 100d)));
        if (fillWidth > 0)
        {
            var fill = new Rectangle(track.X, track.Y, fillWidth, track.Height);
            using var path = ThemeSurfaceRenderer.CreatePath(fill, Math.Min(ThemeManager.Active.CornerRadius, fill.Height / 2));
            using var brush = new SolidBrush(Colors.BlueSelection);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(brush, path);
        }
        TextRenderer.DrawText(e.Graphics, $"{Label} · {Value}%", Font, new Rectangle(0, 0, Width, 14), Colors.DisabledText, TextFormatFlags.VerticalCenter);
    }
}

internal sealed class ThemeV2Dot : Control
{
    public ThemeV2Dot() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true); ThemeManager.ThemeChanged += (_, _) => Invalidate(); }
    protected override void OnPaint(PaintEventArgs e) { e.Graphics.Clear(ThemeV2Painting.ParentColor(this)); using var brush = new SolidBrush(Colors.BlueHighlight); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.FillEllipse(brush, ClientRectangle); }
}

/// <summary>Painted theme selector; its closed state never falls back to native ComboBox chrome.</summary>
internal sealed class ThemeV2Picker : Control
{
    private readonly List<string> _items = new();
    private int _selectedIndex = -1;
    private bool _hovered;

    public List<string> Items => _items;
    public event EventHandler? SelectedIndexChanged;
    [DefaultValue(-1)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < -1 || value >= _items.Count || value == _selectedIndex) return;
            _selectedIndex = value;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ThemeV2Picker()
    {
        Height = 32;
        Cursor = Cursors.Hand;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
        ThemeManager.ThemeChanged += OnThemeChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) ThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }

    public void ApplyTheme() => Invalidate();
    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTheme();
    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseUp(MouseEventArgs e) { if (e.Button == MouseButtons.Left) ShowMenu(); base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter or Keys.F4 or Keys.Down) { ShowMenu(); e.Handled = true; }
        base.OnKeyDown(e);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        ThemeSurfaceRenderer.DrawSurface(e.Graphics, ClientRectangle, ThemeV2Painting.ParentColor(this), Colors.LightBackground,
            Focused || _hovered ? Colors.BlueHighlight : Colors.LightBorder, ThemeManager.Active);
        var text = _selectedIndex >= 0 ? _items[_selectedIndex] : "Select a theme";
        TextRenderer.DrawText(e.Graphics, text, Font, new Rectangle(12, 0, Width - 38, Height), Colors.LightText, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        using var brush = new SolidBrush(Colors.LightText);
        var centerX = Width - 17;
        e.Graphics.FillPolygon(brush, new[] { new Point(centerX - 4, Height / 2 - 2), new Point(centerX + 4, Height / 2 - 2), new Point(centerX, Height / 2 + 3) });
    }

    private void ShowMenu()
    {
        if (_items.Count == 0) return;
        var menu = new ContextMenuStrip { ShowImageMargin = false, ShowCheckMargin = false, Renderer = new ThemeV2PickerMenuRenderer() };
        foreach (var (name, index) in _items.Select((name, index) => (name, index)))
        {
            var item = new ToolStripMenuItem(name) { Checked = index == SelectedIndex, ForeColor = Colors.LightText };
            // ToolStripDropDown dismisses itself after the click handler returns.
            // Disposing it here races that dismissal and causes ObjectDisposedException.
            item.Click += (_, _) => SelectedIndex = index;
            menu.Items.Add(item);
        }
        // Closed is raised during ToolStrip's own item-dismissal sequence. Dispose on
        // the next UI message so WinForms has finished that sequence before handles go away.
        menu.Closed += (_, _) =>
        {
            if (!IsDisposed && IsHandleCreated)
                BeginInvoke((MethodInvoker)menu.Dispose);
        };
        menu.Show(this, new Point(0, Height));
    }
}

internal sealed class ThemeV2PickerMenuRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) => e.Graphics.Clear(Colors.LightBackground);
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        e.Graphics.Clear(e.Item.Selected ? Colors.BlueSelection : Colors.LightBackground);
    }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Selected ? Colors.SelectionText : Colors.LightText;
        base.OnRenderItemText(e);
    }
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Colors.LightBorder);
        e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
    }
}
