using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 本地化静态代理类，兼容旧版业务代码调用。
    /// </summary>
    public static class I18N
    {
        public static string CurLangString => LocalizationUtils.GetLanguageStr(GameModule.Localization.Language);

        public static UnityEngine.SystemLanguage CurLanguage => GameModule.Localization.Language.ToSystemLanguage();

        public static Language GetSaveLanguage()
        {
            return GameModule.Localization.Language;
        }

        public static void SetSaveLanguage(Language lang)
        {
            GameModule.Localization.SetLanguage(lang);
        }

        public static string GetString(string strID)
        {
            return GameModule.Localization.GetString(strID);
        }
    }
}
