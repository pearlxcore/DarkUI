using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using Microsoft.DotNet.DesignTools.Designers;

namespace DarkUI.Controls
{
    public class DarkGroupBoxDesigner : ParentControlDesigner
    {
        private DesignerVerbCollection _verbs;

        public override DesignerVerbCollection Verbs
        {
            get
            {
                if (_verbs == null)
                    _verbs = new DesignerVerbCollection { CreateVerb("Fit to Contents", OnFitToContents) };

                return _verbs;
            }
        }

        private void OnFitToContents(object sender, EventArgs e)
        {
            FitToContentsDesignerHelper.Fit(
                (DarkGroupBox)Component,
                (IComponentChangeService)GetService(typeof(IComponentChangeService)),
                (IDesignerHost)GetService(typeof(IDesignerHost)),
                "Fit group box to contents");
        }
    }
}
