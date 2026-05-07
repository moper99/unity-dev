using FairyGUI;

namespace GameLogic
{
    public class BattleMainUIBinder
    {
        public static void BindAll()
        {
            UIObjectFactory.SetPackageItemExtension(CompHeadBg.URL, typeof(CompHeadBg));
            UIObjectFactory.SetPackageItemExtension(CompMainUserInfo.URL, typeof(CompMainUserInfo));
            UIObjectFactory.SetPackageItemExtension(CompMicroPhone.URL, typeof(CompMicroPhone));
        }
    }
}