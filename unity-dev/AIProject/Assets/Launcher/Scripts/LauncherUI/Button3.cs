/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace LauncherUI
{
    public partial class Button3 : GButton
    {
        public Controller button;
        public GImage n0;
        public GImage n1;
        public GImage n2;
        public GTextField n3;
        public const string URL = "ui://agvfrfcch9cob";

        public static Button3 CreateInstance()
        {
            return (Button3)UIPackage.CreateObject("LauncherUI", "Button3");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            button = GetController("button");
            n0 = (GImage)GetChild("n0");
            n1 = (GImage)GetChild("n1");
            n2 = (GImage)GetChild("n2");
            n3 = (GTextField)GetChild("n3");
        }
    }
}