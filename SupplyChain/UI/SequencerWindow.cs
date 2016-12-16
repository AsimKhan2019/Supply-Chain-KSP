using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace SupplyChain.UI
{
    public class SequencerWindow : SupplyBaseWindow
    {
        public SequencerWindow()
        {
            this.toolbarIconURL = "SupplyChain/Icons/SupplyLinkIcon";
            this.windowName = "Supply Chains";
            this.windowPos = new Rect(0, 0, 600, 600);
            this.activeScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.FLIGHT;
        }

        public override void onUpdate()
        {
            throw new NotImplementedException();
        }

        public override void windowInternals(int id)
        {
            throw new NotImplementedException();
        }
    }
}
