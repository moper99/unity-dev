/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace LauncherUI
{
    public partial class LoadTipsUI : GComponent
    {
        public GImage n0;
        public Button1 btnConfirm;
        public Button2 btnUpdate;
        public Button3 btnCancel;
        public GTextField txtDesc;
        public const string URL = "ui://agvfrfcch9co4";

        public static LoadTipsUI CreateInstance()
        {
            return (LoadTipsUI)UIPackage.CreateObject("LauncherUI", "LoadTipsUI");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            n0 = (GImage)GetChild("n0");
            btnConfirm = (Button1)GetChild("btnConfirm");
            btnUpdate = (Button2)GetChild("btnUpdate");
            btnCancel = (Button3)GetChild("btnCancel");
            txtDesc = (GTextField)GetChild("txtDesc");
        }
    }
}