using FairyGUI;
using FairyGUI.Utils;

namespace GameLogic
{
    public partial class Demo_MovieClip : GComponent
    {
        public const string URL = "ui://Basics/Demo_MovieClip";

        public static Demo_MovieClip CreateInstance()
        {
            return (Demo_MovieClip)UIPackage.CreateObject("Basics", "Demo_MovieClip");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);


            OnInit();
        }

        partial void OnInit();
    }
}