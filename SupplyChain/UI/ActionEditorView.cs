using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SupplyChain.UI
{
    /* Base class for editing views for particular action types.
     * 
     * Each view provides a button for accessing the view from higher-level menus,
     * and an actual detailed editor (intended to take up the whole window space.)
     */
    public abstract class ActionEditorView
    {
        /* Returns true when button pressed (like GUI.Button). 
         * Should be at most two lines wide.
         */
        public abstract bool drawSelectorButton();

        /* Returns true when view closed */
        public abstract bool drawEditorWindow();

        /* Called regularly to refresh world state (i.e. once every in-game second or so)
         * also called on warp state change, etc.
         */
        public abstract void onUpdate();
        
        /* Factory method for getting specific ActionEditorView subclass objects. */
        public static ActionEditorView getActionEditorView(SupplyChainAction action)
        {
            if(action.GetType() == typeof(SupplyLink))
            {
                return new SupplyLinkEditor((SupplyLink)action);
            } else if(action.GetType() == typeof(ResourceTransferAction))
            {
                return new ResourceTransferEditor((ResourceTransferAction)action);
            }

            return null;
        }
    }
}
