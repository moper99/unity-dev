using FairyGUI;
using FairyGUI.Utils;

namespace GameLogic
{
    public partial class CompMicroPhone : GComponent
    {
        public Controller CtrlSelfMicro;
        public const string URL = "ui://BattleMainUI/CompMicroPhone";

        public static CompMicroPhone CreateInstance()
        {
            return (CompMicroPhone)UIPackage.CreateObject("BattleMainUI", "CompMicroPhone");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            CtrlSelfMicro = GetController("ctrlSelfMicro");

            OnInit();
        }

        partial void OnInit();
    }
}