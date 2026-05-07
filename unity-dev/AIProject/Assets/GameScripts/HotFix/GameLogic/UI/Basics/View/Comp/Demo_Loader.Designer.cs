using FairyGUI;
using FairyGUI.Utils;

namespace GameLogic
{
    public partial class Demo_Loader : GComponent
    {
        public const string URL = "ui://Basics/Demo_Loader";

        public static Demo_Loader CreateInstance()
        {
            return (Demo_Loader)UIPackage.CreateObject("Basics", "Demo_Loader");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);


            OnInit();
        }

        partial void OnInit();
    }
}