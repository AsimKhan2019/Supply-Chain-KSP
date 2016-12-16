using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace SupplyChain.UI
{
    /* Base class for windows. */
    public abstract class SupplyBaseWindow
    {
        public string windowName;
        protected string toolbarIconURL;

        protected Texture tex = null;
        protected bool windowActive = false;
        protected ApplicationLauncherButton button = null;
        protected Rect windowPos;
        protected ApplicationLauncher.AppScenes activeScenes;

        public static SupplyBaseWindow instance;
        public static int nextWindowID = 1;

        protected int windowID = 0;

        public void initWindow()
        {
            if (button == null)
            {
                if(tex == null)
                    tex = GameDatabase.Instance.GetTexture(toolbarIconURL, false);

                button = ApplicationLauncher.Instance.AddModApplication(
                    () => { this.windowActive = true; },    // On toggle active.
                    () => { this.windowActive = false; },   // On toggle inactive.
                    null, null, null, null,
                    activeScenes,
                    tex // Texture.
                );

                

            }

            if (this.windowID == 0)
            {
                this.windowID = SupplyBaseWindow.nextWindowID++;
            }

            GameEvents.OnFlightGlobalsReady.Add(
                (bool data) =>
                {
                    this.onUpdate();
                }
            );

            GameEvents.onTimeWarpRateChanged.Add(this.onUpdate);
        }

        public abstract void onUpdate();
        public abstract void windowInternals(int id);
        
        public void drawWindow()
        {
            if(this.windowActive)
            {
                this.windowPos = GUI.Window(this.windowID, this.windowPos,
                    (int id) =>
                    {
                        this.windowInternals(id);
                        GUI.DragWindow();
                    },
                    this.windowName
                );
            }
        }

        public static void updateAllWindows()
        {
            SupplyPointWindow.instance.onUpdate();
            SupplyStatusWindow.instance.onUpdate();
            OneshotEditorWindow.instance.onUpdate();
        }

        public static void initAllWindows()
        {
            SupplyPointWindow.instance.initWindow();
            SupplyStatusWindow.instance.initWindow();
            OneshotEditorWindow.instance.initWindow();
        }
    }
}
