using FairyGUI;

namespace GameLogic
{
    public class BasicsBinder
    {
        public static void BindAll()
        {
            UIObjectFactory.SetPackageItemExtension(BtnPanelMask.URL, typeof(BtnPanelMask));
            UIObjectFactory.SetPackageItemExtension(CircleProgress.URL, typeof(CircleProgress));
            UIObjectFactory.SetPackageItemExtension(CompPanelParent.URL, typeof(CompPanelParent));
            UIObjectFactory.SetPackageItemExtension(Demo_Loader.URL, typeof(Demo_Loader));
            UIObjectFactory.SetPackageItemExtension(Demo_MovieClip.URL, typeof(Demo_MovieClip));
        }
    }
}