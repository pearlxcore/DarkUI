using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using Microsoft.DotNet.DesignTools.Designers;

namespace DarkUI.Controls
{
    public class DarkSectionPanelDesigner : ParentControlDesigner
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
                        CreateVerb("Fit to Contents", OnFitToContents),
                        CreateVerb("Remove Padding", OnRemovePadding),
                        CreateVerb("Reset Padding", OnResetPadding),
                        CreateVerb("Remove Margin", OnRemoveMargin),
                        CreateVerb("Reset Margin", OnResetMargin)
                    };
                }

                return _verbs;
            }
        }

        public override void Initialize(IComponent component)
        {
            base.Initialize(component);
        }

        private void OnFitToContents(object sender, EventArgs e)
        {
            FitToContentsDesignerHelper.Fit(
                (DarkSectionPanel)Component,
                (IComponentChangeService)GetService(typeof(IComponentChangeService)),
                (IDesignerHost)GetService(typeof(IDesignerHost)),
                "Fit section panel to contents");
        }

        private void OnRemovePadding(object sender, EventArgs e)
        {
            SetLayoutProperty(nameof(DarkSectionPanel.Padding), System.Windows.Forms.Padding.Empty, "Remove section panel padding");
        }

        private void OnResetPadding(object sender, EventArgs e)
        {
            SetLayoutProperty(nameof(DarkSectionPanel.Padding), System.Windows.Forms.Padding.Empty, "Reset section panel padding");
        }

        private void OnRemoveMargin(object sender, EventArgs e)
        {
            SetLayoutProperty(nameof(DarkSectionPanel.Margin), System.Windows.Forms.Padding.Empty, "Remove section panel margin");
        }

        private void OnResetMargin(object sender, EventArgs e)
        {
            SetLayoutProperty(nameof(DarkSectionPanel.Margin), new System.Windows.Forms.Padding(6), "Reset section panel margin");
        }

        private void SetLayoutProperty(string propertyName, object value, string transactionName)
        {
            var property = TypeDescriptor.GetProperties(Component)[propertyName];
            var changeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));
            var host = (IDesignerHost)GetService(typeof(IDesignerHost));
            var previousValue = property.GetValue(Component);

            using (var transaction = host?.CreateTransaction(transactionName))
            {
                changeService?.OnComponentChanging(Component, property);
                property.SetValue(Component, value);
                changeService?.OnComponentChanged(Component, property, previousValue, value);
                transaction?.Commit();
            }
        }
    }
}
