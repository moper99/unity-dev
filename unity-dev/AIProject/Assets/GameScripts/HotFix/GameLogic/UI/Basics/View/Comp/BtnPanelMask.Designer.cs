using FairyGUI;
using FairyGUI.Utils;

namespace GameLogic
{
    public partial class BtnPanelMask : GButton
    {
        public Controller CtrlBlack;
        public GGraph Graph;
        public const string URL = "ui://Basics/BtnPanelMask";

        public static BtnPanelMask CreateInstance()
        {
            return (BtnPanelMask)UIPackage.CreateObject("Basics", "BtnPanelMask");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            CtrlBlack = GetController("ctrlBlack");
            Graph = (GGraph)GetChild("graph");

            OnInit();
        }

        partial void OnInit();
    }
}