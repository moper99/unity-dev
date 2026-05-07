/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace LauncherUI
{
    public partial class LauncherUI : GComponent
    {
        public ProgressBar1 progressBar;
        public GTextField txtVersion;
        public GTextField txtLabelAppid;
        public const string URL = "ui://agvfrfcch9co2";

        public static LauncherUI CreateInstance()
        {
            return (LauncherUI)UIPackage.CreateObject("LauncherUI", "LauncherUI");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            progressBar = (ProgressBar1)GetChild("progressBar");
            txtVersion = (GTextField)GetChild("txtVersion");
            txtLabelAppid = (GTextField)GetChild("txtLabelAppid");
        }
    }
}