using FairyGUI;

namespace GameLogic
{
    public class BasicsBinder
    {
        public static void BindAll()
        {
            UIObjectFactory.SetPackageItemExtension(CircleProgress.URL, typeof(CircleProgress));
            UIObjectFactory.SetPackageItemExtension(Demo_Loader.URL, typeof(Demo_Loader));
            UIObjectFactory.SetPackageItemExtension(Demo_MovieClip.URL, typeof(Demo_MovieClip));
        }
    }
}