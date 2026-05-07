using FairyGUI;
using FairyGUI.Utils;

namespace GameLogic
{
    public partial class CircleProgress : GProgressBar
    {
        public const string URL = "ui://Basics/CircleProgress";

        public static CircleProgress CreateInstance()
        {
            return (CircleProgress)UIPackage.CreateObject("Basics", "CircleProgress");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);


            OnInit();
        }

        partial void OnInit();
    }
}