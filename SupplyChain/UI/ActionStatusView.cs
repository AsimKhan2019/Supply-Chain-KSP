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
        public abstract bool drawButton();
        public abstract bool drawEditorWindow();

        public static ActionStatusView getActionDetailsView(SupplyChainAction action)
        {
            return null;
        }
    }
}
