using System.Collections.Generic;
using UnityEngine;

namespace TEngine
{
    /// <summary>
    /// 统一的本地化辅助工具类。
    /// </summary>
    public static class LocalizationUtils
    {
#if UNITY_EDITOR
        public const string I2GlobalSourcesEditorPath = "Assets/Editor/I2Localization/I2Languages.asset";
#endif
        public const string I2ResAssetNamePrefix = "I2_";

        /// <summary>
        /// 获取系统语言。
        /// </summary>
        public static Language SystemLanguage
        {
            get
            {
                switch (Application.systemLanguage)
                {
                    case UnityEngine.SystemLanguage.Afrikaans: return Language.Afrikaans;
                    case UnityEngine.SystemLanguage.Arabic: return Language.Arabic;
                    case UnityEngine.SystemLanguage.Basque: return Language.Basque;
                    case UnityEngine.SystemLanguage.Belarusian: return Language.Belarusian;
                    case UnityEngine.SystemLanguage.Bulgarian: return Language.Bulgarian;
                    case UnityEngine.SystemLanguage.Catalan: return Language.Catalan;
                    case UnityEngine.SystemLanguage.Chinese: return Language.ChineseSimplified;
                    case UnityEngine.SystemLanguage.ChineseSimplified: return Language.ChineseSimplified;
                    case UnityEngine.SystemLanguage.ChineseTraditional: return Language.ChineseTraditional;
                    case UnityEngine.SystemLanguage.Czech: return Language.Czech;
                    case UnityEngine.SystemLanguage.Danish: return Language.Danish;
                    case UnityEngine.SystemLanguage.Dutch: return Language.Dutch;
                    case UnityEngine.SystemLanguage.English: return Language.English;
                    case UnityEngine.SystemLanguage.Estonian: return Language.Estonian;
                    case UnityEngine.SystemLanguage.Faroese: return Language.Faroese;
                    case UnityEngine.SystemLanguage.Finnish: return Language.Finnish;
                    case UnityEngine.SystemLanguage.French: return Language.French;
                    case UnityEngine.SystemLanguage.German: return Language.German;
                    case UnityEngine.SystemLanguage.Greek: return Language.Greek;
                    case UnityEngine.SystemLanguage.Hebrew: return Language.Hebrew;
                    case UnityEngine.SystemLanguage.Hungarian: return Language.Hungarian;
                    case UnityEngine.SystemLanguage.Icelandic: return Language.Icelandic;
                    case UnityEngine.SystemLanguage.Indonesian: return Language.Indonesian;
                    case UnityEngine.SystemLanguage.Italian: return Language.Italian;
                    case UnityEngine.SystemLanguage.Japanese: return Language.Japanese;
                    case UnityEngine.SystemLanguage.Korean: return Language.Korean;
                    case UnityEngine.SystemLanguage.Latvian: return Language.Latvian;
                    case UnityEngine.SystemLanguage.Lithuanian: return Language.Lithuanian;
                    case UnityEngine.SystemLanguage.Norwegian: return Language.Norwegian;
                    case UnityEngine.SystemLanguage.Polish: return Language.Polish;
                    case UnityEngine.SystemLanguage.Portuguese: return Language.PortuguesePortugal;
                    case UnityEngine.SystemLanguage.Romanian: return Language.Romanian;
                    case UnityEngine.SystemLanguage.Russian: return Language.Russian;
                    case UnityEngine.SystemLanguage.SerboCroatian: return Language.SerboCroatian;
                    case UnityEngine.SystemLanguage.Slovak: return Language.Slovak;
                    case UnityEngine.SystemLanguage.Slovenian: return Language.Slovenian;
                    case UnityEngine.SystemLanguage.Spanish: return Language.Spanish;
                    case UnityEngine.SystemLanguage.Swedish: return Language.Swedish;
                    case UnityEngine.SystemLanguage.Thai: return Language.Thai;
                    case UnityEngine.SystemLanguage.Turkish: return Language.Turkish;
                    case UnityEngine.SystemLanguage.Ukrainian: return Language.Ukrainian;
                    case UnityEngine.SystemLanguage.Unknown: return Language.Unspecified;
                    case UnityEngine.SystemLanguage.Vietnamese: return Language.Vietnamese;
                    default: return Language.Unspecified;
                }
            }
        }

        private static readonly Dictionary<Language, string> _languageMap = new Dictionary<Language, string>();
        private static readonly Dictionary<string, Language> _languageStrMap = new Dictionary<string, Language>();

        static LocalizationUtils()
        {
            RegisterLanguageMap(Language.English);
            RegisterLanguageMap(Language.ChineseSimplified, "Chinese");
            RegisterLanguageMap(Language.ChineseTraditional);
            RegisterLanguageMap(Language.Japanese);
            RegisterLanguageMap(Language.Korean);
        }

        private static void RegisterLanguageMap(Language language, string str = "")
        {
            if (string.IsNullOrEmpty(str))
            {
                str = language.ToString();
            }
            _languageMap[language] = str;
            _languageStrMap[str] = language;
        }

        public static Language GetLanguage(string str)
        {
            if (string.IsNullOrEmpty(str)) return Language.Unspecified;
            if (_languageStrMap.TryGetValue(str, out var language)) return language;
            return Language.English;
        }

        public static string GetLanguageStr(Language language)
        {
            if (_languageMap.TryGetValue(language, out var ret)) return ret;
            return "English";
        }

        /// <summary>
        /// 获取语言的简写字符串 (如 zh, ja)。
        /// </summary>
        public static string GetLangShortString(this Language language)
        {
            return language == Language.Japanese ? "ja" : "zh";
        }

        public static string GetFileName(this Language lang) => $"string_{lang.GetLangShortString()}";
        public static string GetAOTFileName(this Language lang) => $"aot_string_{lang.GetLangShortString()}.json";
        public static string GetConfigFileName(this Language lang) => $"config_string_{lang.GetLangShortString()}";

        public static string GetPrivacyDetailFileName(this Language lang)
        {
            return lang == Language.Japanese ? "privacyjp" : "privacycn";
        }

        public static string GetAgreementDetailFileName(this Language lang)
        {
            return lang == Language.Japanese ? "userjp" : "usercn";
        }

        /// <summary>
        /// 将项目语言枚举转换为 Unity 系统语言枚举。
        /// </summary>
        public static UnityEngine.SystemLanguage ToSystemLanguage(this Language lang)
        {
            if (lang == Language.Japanese) return UnityEngine.SystemLanguage.Japanese;
            if (lang == Language.English) return UnityEngine.SystemLanguage.English;
            if (lang == Language.Korean) return UnityEngine.SystemLanguage.Korean;
            return UnityEngine.SystemLanguage.Chinese;
        }
    }
}
