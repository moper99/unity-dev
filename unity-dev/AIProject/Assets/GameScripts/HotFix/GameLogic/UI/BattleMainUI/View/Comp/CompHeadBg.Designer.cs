using FairyGUI;
using FairyGUI.Utils;

namespace GameLogic
{
    public partial class CompHeadBg : GComponent
    {
        public const string URL = "ui://BattleMainUI/CompHeadBg";

        public static CompHeadBg CreateInstance()
        {
            return (CompHeadBg)UIPackage.CreateObject("BattleMainUI", "CompHeadBg");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);


            OnInit();
        }

        partial void OnInit();
    }
}