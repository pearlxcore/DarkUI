using DarkUI.Controls;

#nullable disable

namespace DarkUI.SidebarDesignerTestApp;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        themePanel = new Panel();
        themeSelector = new ComboBox();
        themeLabel = new Label();
        darkSidebarTabControl = new DarkSidebarTabControl();
        generalPage = new SidebarPage();
        pathsPage = new SidebarPage();
        advancedPage = new SidebarPage();
        themePanel.SuspendLayout();
        darkSidebarTabControl.SuspendLayout();
        SuspendLayout();
        // 
        // themePanel
        // 
        themePanel.Controls.Add(themeSelector);
        themePanel.Controls.Add(themeLabel);
        themePanel.Dock = DockStyle.Top;
        themePanel.Location = new Point(0, 0);
        themePanel.Name = "themePanel";
        themePanel.Size = new Size(884, 42);
        themePanel.TabIndex = 0;
        // 
        // themeSelector
        // 
        themeSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        themeSelector.FormattingEnabled = true;
        themeSelector.Location = new Point(70, 9);
        themeSelector.Name = "themeSelector";
        themeSelector.Size = new Size(220, 23);
        themeSelector.TabIndex = 1;
        themeSelector.SelectedIndexChanged += themeSelector_SelectedIndexChanged;
        // 
        // themeLabel
        // 
        themeLabel.AutoSize = true;
        themeLabel.Location = new Point(12, 12);
        themeLabel.Name = "themeLabel";
        themeLabel.Size = new Size(47, 15);
        themeLabel.TabIndex = 0;
        themeLabel.Text = "Theme:";
        // 
        // darkSidebarTabControl
        // 
        darkSidebarTabControl.Dock = DockStyle.Fill;
        darkSidebarTabControl.Location = new Point(0, 42);
        darkSidebarTabControl.Name = "darkSidebarTabControl";
        darkSidebarTabControl.Pages.AddRange(new SidebarPage[] { generalPage, pathsPage, advancedPage });
        darkSidebarTabControl.SelectedIndex = 0;
        darkSidebarTabControl.Size = new Size(884, 519);
        darkSidebarTabControl.TabIndex = 1;
        // 
        // generalPage
        // 
        generalPage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        generalPage.Location = new Point(180, 0);
        generalPage.Name = "generalPage";
        generalPage.Size = new Size(704, 519);
        generalPage.TabIndex = 0;
        generalPage.Text = "General";
        // 
        // pathsPage
        // 
        pathsPage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pathsPage.Location = new Point(180, 0);
        pathsPage.Name = "pathsPage";
        pathsPage.Size = new Size(704, 519);
        pathsPage.TabIndex = 1;
        pathsPage.Text = "Paths";
        // 
        // advancedPage
        // 
        advancedPage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        advancedPage.Location = new Point(180, 0);
        advancedPage.Name = "advancedPage";
        advancedPage.Size = new Size(704, 519);
        advancedPage.TabIndex = 2;
        advancedPage.Text = "Advanced";
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(884, 561);
        Controls.Add(darkSidebarTabControl);
        Controls.Add(themePanel);
        MinimumSize = new Size(720, 440);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "DarkSidebarTabControl Designer Test";
        themePanel.ResumeLayout(false);
        themePanel.PerformLayout();
        darkSidebarTabControl.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private Panel themePanel;
    private ComboBox themeSelector;
    private Label themeLabel;
    private DarkSidebarTabControl darkSidebarTabControl;
    private SidebarPage generalPage;
    private SidebarPage pathsPage;
    private SidebarPage advancedPage;
}
