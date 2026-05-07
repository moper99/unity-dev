using FairyGUI;
using FairyGUI.Utils;

namespace GameLogic
{
    public partial class CompPanelParent : GComponent
    {
        public BtnPanelMask BtnBackGround;
        public GGraph GraphContent;
        public GGraph GraphSafeBottom;
        public GGraph GraphSafeTop;
        public Transition TransHide;
        public Transition TransShow;
        public const string URL = "ui://Basics/CompPanelParent";

        public static CompPanelParent CreateInstance()
        {
            return (CompPanelParent)UIPackage.CreateObject("Basics", "CompPanelParent");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            BtnBackGround = (BtnPanelMask)GetChild("btnBackGround");
            GraphContent = (GGraph)GetChild("graphContent");
            GraphSafeBottom = (GGraph)GetChild("graphSafeBottom");
            GraphSafeTop = (GGraph)GetChild("graphSafeTop");
            TransHide = GetTransition("transHide");
            TransShow = GetTransition("transShow");

            OnInit();
        }

        partial void OnInit();
    }
}