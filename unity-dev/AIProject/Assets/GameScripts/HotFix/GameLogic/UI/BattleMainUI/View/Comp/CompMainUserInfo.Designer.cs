using FairyGUI;
using FairyGUI.Utils;

namespace GameLogic
{
    public partial class CompMainUserInfo : GComponent
    {
        public CompHeadBg CompHeadBg;
        public CompMicroPhone CompMicroPhone;
        public Controller CtrlSelfMicro;
        public GTextField TxtUserName;
        public GTextField Txtlevel;
        public const string URL = "ui://BattleMainUI/CompMainUserInfo";

        public static CompMainUserInfo CreateInstance()
        {
            return (CompMainUserInfo)UIPackage.CreateObject("BattleMainUI", "CompMainUserInfo");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            CompHeadBg = (CompHeadBg)GetChild("compHeadBg");
            CompMicroPhone = (CompMicroPhone)GetChild("compMicroPhone");
            CtrlSelfMicro = GetController("ctrlSelfMicro");
            TxtUserName = (GTextField)GetChild("txtUserName");
            Txtlevel = (GTextField)GetChild("txtlevel");

            OnInit();
        }

        partial void OnInit();
    }
}