using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Microsoft.DotNet.DesignTools.Designers;

namespace DarkUI.Controls
{
    /// <summary>
    /// A neutral content host created by the DarkSplitContainer designer.
    /// It is a normal Panel at runtime, with an extra designer verb for
    /// extending the owning split container.
    /// </summary>
    [Designer(typeof(DarkSplitPaneDesigner))]
    public class DarkSplitPane : Panel
    {
    }

    public class DarkSplitPaneDesigner : ParentControlDesigner
    {
        private DesignerVerbCollection _verbs;

        public override DesignerVerbCollection Verbs
        {
            get
            {
                if (_verbs == null)
                {
                    _verbs = new DesignerVerbCollection
                    {
                        CreateVerb("Add Split Pane", OnAddSplitPane),
                        CreateVerb("Make All Panes Equal", OnMakeAllPanesEqual)
                    };
                }

                return _verbs;
            }
        }

        private void OnAddSplitPane(object sender, EventArgs e)
        {
            GetOwnerDesigner()?.AddSplitPane();
        }

        private void OnMakeAllPanesEqual(object sender, EventArgs e)
        {
            var owner = GetOwnerDesigner();
            owner?.MakeAllPanesEqual();
        }

        private DarkSplitContainerDesigner GetOwnerDesigner()
        {
            if (Control.Parent is not DarkSplitContainer owner)
                return null;

            var host = (IDesignerHost)GetService(typeof(IDesignerHost));
            return host?.GetDesigner(owner) as DarkSplitContainerDesigner;
        }
    }
}
