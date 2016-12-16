using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SupplyChain.UI
{
    /* Base class for all detail views for particular action types.
     * 
     * Each view provides a button (for accessing the view from higher-level menus)
     * and an actual detailed view (intended to take up the whole window space.)
     */
    public abstract class ActionStatusView
    {
        public SupplyChainAction action;

        public abstract bool drawSelectorButton();
        public abstract bool drawStatusWindow();
        public abstract void onUpdate();

        public static ActionStatusView getActionDetailsView(SupplyChainAction action)
        {
            if (action.GetType() == typeof(SupplyLink))
            {
                return new SupplyLinkStatus((SupplyLink)action);
            }
            else if (action.GetType() == typeof(ResourceTransferAction))
            {
                return new ResourceTransferStatus((ResourceTransferAction)action);
            }

            return null;
        }
    }
}
