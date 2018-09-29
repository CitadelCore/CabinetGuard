using Microsoft.VirtualManager.UI.AddIns;
using System;
using System.AddIn;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.SystemCenter.VirtualMachineManager.UIAddIns.ContextTypes;
using Microsoft.VirtualManager.Utils;
using Microsoft.SystemCenter.VirtualMachineManager;
using Newtonsoft.Json;

namespace Tower.CabinetGuard.Integrations
{
    [AddIn("Cabinet View Add-in")]
    class CabinetView : ViewAddInBase
    {
        public WebBrowser webBrowser;
        private AddInContextType currentContext;
        private ContextObject currentObject;

        public override FrameworkElement CreateViewControl()
        {
            webBrowser = new WebBrowser
            {
                AllowDrop = false,
                ObjectForScripting = new BrowserMethodHandler(this),
            };

            return webBrowser;
        }

        public override void SetCurrentScope(AddInContextType scopeType, ContextObject scopeObject)
        {
            currentContext = scopeType;
            currentObject = scopeObject;
            webBrowser.Navigate(new Uri(String.Format("https://ccs.tower.local/api/scvmm?scopeType={0}&scopeName={1}", scopeType.ToString(), scopeObject != null ? scopeObject.Name : String.Empty)));
        }

        public void RefreshAllData()
        {
            if (currentContext == AddInContextType.HostGroup && currentObject.ObjectType == CarmineObjectType.VMHostGroup)
            {
                RefreshPageHosts();
            }
        }

        private void RefreshPageHosts()
        {
            // Read the host group objects
            HostGroupContext context = (currentObject as HostGroupContext);
            IList<Host> hosts = new List<Host>(context.HostIds.Count());

            // Retrieve host objects
            foreach (Guid guid in context.HostIds)
            {
                Host host = QueryContract(guid.ToString()) as Host;
                hosts.Add(host);
            }

            // Serialize list and send it to CCS's GUI
            webBrowser.InvokeScript("loadHosts", JsonConvert.SerializeObject(hosts));
        }
    }
}
