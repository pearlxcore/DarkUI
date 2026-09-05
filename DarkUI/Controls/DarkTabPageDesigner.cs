using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Microsoft.DotNet.DesignTools.Designers;

namespace DarkUI.Controls
{
    /// <summary>
    /// Adds DarkUI's layout command to the standard TabPage design surface.
    /// The command is associated with individual TabPages by DarkTabControl
    /// only while they are hosted in the Visual Studio designer.
    /// </summary>
    public class DarkTabPageDesigner : ParentControlDesigner
    {
        private DesignerVerbCollection _verbs;

        public override DesignerVerbCollection Verbs
        {
            get
            {
                if (_verbs != null)
                    return _verbs;

                _verbs = new DesignerVerbCollection();
                if (Component is TabPage page && page.Parent != null)
                {
                    var host = (IDesignerHost)GetService(typeof(IDesignerHost));
                    if (host?.GetDesigner(page.Parent) is Microsoft.DotNet.DesignTools.Designers.ComponentDesigner ownerDesigner)
                    {
                        foreach (DesignerVerb verb in ownerDesigner.Verbs)
                            _verbs.Add(verb);
                    }
                }

                _verbs.Add(CreateVerb("Fit to Contents", OnFitToContents));
                _verbs.Add(CreateVerb("Arrange Controls by Margin", OnArrangeSectionPanels));
                return _verbs;
            }
        }

        private void OnFitToContents(object sender, EventArgs e)
        {
            FitToContentsDesignerHelper.Fit(
                (TabPage)Component,
                (IComponentChangeService)GetService(typeof(IComponentChangeService)),
                (IDesignerHost)GetService(typeof(IDesignerHost)),
                "Fit tab page to contents");
        }

        private void OnArrangeSectionPanels(object sender, EventArgs e)
        {
            SectionPanelArrangementDesignerHelper.Arrange(
                (TabPage)Component,
                (IComponentChangeService)GetService(typeof(IComponentChangeService)),
                (IDesignerHost)GetService(typeof(IDesignerHost)),
                "Arrange tab page controls by margin",
                preserveColumns: true);
        }
    }
}
