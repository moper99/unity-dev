/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace LauncherUI
{
    public partial class ProgressBar1 : GProgressBar
    {
        public GImage n0;
        public GImage bar;
        public const string URL = "ui://agvfrfcch9co3";

        public static ProgressBar1 CreateInstance()
        {
            return (ProgressBar1)UIPackage.CreateObject("LauncherUI", "ProgressBar1");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            n0 = (GImage)GetChild("n0");
            bar = (GImage)GetChild("bar");
        }
    }
}