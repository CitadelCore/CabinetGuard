using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Tower.CabinetGuard.Integrations
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    class BrowserMethodHandler
    {
        CabinetView view;
        public BrowserMethodHandler(CabinetView view)
        {
            this.view = view;
        }

        public void RefreshAllData()
        {
            view.RefreshAllData();
        }

        public void CallPSMethod(string commandLine)
        {

        }
    }
}
