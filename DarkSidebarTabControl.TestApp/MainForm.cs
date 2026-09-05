using DarkUI.Config;

namespace DarkUI.SidebarDesignerTestApp;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();

        foreach (Theme theme in ThemeManager.Presets)
            themeSelector.Items.Add(theme.Name);

        ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
        SyncThemeSelector();
        ApplyThemeColors();
    }

    private void themeSelector_SelectedIndexChanged(object? sender, EventArgs e)
    {
        int index = themeSelector.SelectedIndex;
        if (index >= 0 && index < ThemeManager.Presets.Count &&
            !ReferenceEquals(ThemeManager.Active, ThemeManager.Presets[index]))
        {
            ThemeManager.Apply(ThemeManager.Presets[index]);
        }
    }

    private void ThemeManager_ThemeChanged(object? sender, EventArgs e)
    {
        SyncThemeSelector();
        ApplyThemeColors();
    }

    private void SyncThemeSelector()
    {
        for (int i = 0; i < ThemeManager.Presets.Count; i++)
        {
            if (ReferenceEquals(ThemeManager.Presets[i], ThemeManager.Active))
            {
                themeSelector.SelectedIndex = i;
                break;
            }
        }
    }

    private void ApplyThemeColors()
    {
        BackColor = Colors.GreyBackground;
        ForeColor = Colors.LightText;
        themePanel.BackColor = Colors.MediumBackground;
        themeLabel.ForeColor = Colors.LightText;
        themeSelector.BackColor = Colors.GreyBackground;
        themeSelector.ForeColor = Colors.LightText;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
        base.OnFormClosed(e);
    }
}
