using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SupplyChain.UI
{
    public class UIStyle
    {
        public static GUIStyle passableStyle;
        public static GUIStyle impassableStyle;
        public static GUIStyle activeStyle;

        public static GUIStyle passableLabelStyle;
        public static GUIStyle impassableLabelStyle;
        public static GUIStyle activeLabelStyle;

        public static UIStyle instance;

        public static void init()
        {
            if(instance == null)
            {
                instance = new UIStyle();

                passableStyle = new GUIStyle("button");
                passableStyle.normal.textColor = Color.green;

                activeStyle = new GUIStyle("button");
                activeStyle.normal.textColor = Color.yellow;

                impassableStyle = new GUIStyle("button");
                impassableStyle.normal.textColor = Color.red;

                passableLabelStyle = new GUIStyle("label");
                passableLabelStyle.normal.textColor = Color.green;

                activeLabelStyle = new GUIStyle("label");
                activeLabelStyle.normal.textColor = Color.yellow;

                impassableLabelStyle = new GUIStyle("label");
                impassableLabelStyle.normal.textColor = Color.red;
            }
        }

        /*
         * small form = "DD:HH:MM:SS"
         * large form = "dd days, hh hours, mm minutes, ss seconds"
         */
        public static string formatTimespan(double ts, bool smallForm = false)
        {
            string ret = "";

            int t = (int)Math.Round(ts);

            int days = 0;

            if (GameSettings.KERBIN_TIME)
            {
                days = t / (int)Math.Round(FlightGlobals.Bodies[1].solarDayLength);
                t %= (int)Math.Round(FlightGlobals.Bodies[1].solarDayLength);
            }
            else
            {
                days = t / 86400;
                t %= 86400;
            }

            int hours = t / 3600;
            t %= 3600;

            int minutes = t / 60;
            t %= 60;

            if (smallForm)
            {
                if (days > 0)
                    ret += days.ToString("D2");

                if (hours > 0)
                    ret += ((ret.Length > 0) ? ":" : "") + hours.ToString("D2") + "h";

                if (minutes > 0)
                    ret += ((ret.Length > 0) ? ":" : "") + minutes.ToString("D2") + "m";

                if (t > 0)
                    ret += ((ret.Length > 0) ? ":" : "") + t.ToString("D2") + "s";
            }
            else
            {
                if (days > 0)
                    ret += days.ToString() + " days";

                if (hours > 0)
                    ret += ((ret.Length > 0) ? ", " : "") + hours.ToString() + " hours";

                if (minutes > 0)
                    ret += ((ret.Length > 0) ? ", " : "") + minutes.ToString() + " minutes";

                if (t > 0)
                    ret += ((ret.Length > 0) ? ", " : "") + t.ToString() + " seconds";
            }

            return ret;
        }

    }
}
