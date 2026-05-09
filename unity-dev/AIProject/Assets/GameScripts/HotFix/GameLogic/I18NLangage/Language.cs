using TEngine;
using UnityEngine;

namespace GameLogic
{
    public static class LanguageExtend
    {
        /// <summary>
        /// 获取本地语言配置缓存
        /// </summary>
        /// <returns></returns>
        public static SystemLanguage GetSystemLanguage(this Language lang)
        {
            if (lang == Language.Japanese)
            {
                return SystemLanguage.Japanese;
            }
            return SystemLanguage.Chinese;
        }

        /// <summary>
        /// Language枚举转Lang字符串
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        public static string GetLangString(this Language language)
        {
            if (language == Language.Japanese)
            {
                return "ja";
            }
            return "zh";
        }

        public static string GetFileName(this Language lang)
        {
            return $"string_{lang.GetLangString()}";
        }

        public static string GetAOTFileName(this Language lang)
        {
            return $"aot_string_{lang.GetLangString()}.json";
        }

        public static string GetConfigFileName(this Language lang)
        {
            return $"config_string_{lang.GetLangString()}";
        }

        public static string GetPrivacyDetailFileName(this Language lang)
        {
            if (lang == Language.Japanese)
            {
                // return "https://vopi.jp/agreement/privacyjp.html";
                return "privacyjp";
            }
            else
            {
                // return "https://vopi.jp/agreement/privacycn.html";
                return "privacycn";
            }
        }

        public static string GetAgreementDetailFileName(this Language lang)
        {
            if (lang == Language.Japanese)
            {
                // return "https://www.starryislands.com/static/lz-fishing-privacy/automaticRenewalAgreement.html";
                // return "https://www.starryislands.com/static/lz-fishing-privacy/paidService.html";
                // return "https://www.starryislands.com/static/lz-fishing-privacy/userAgreement.html";
                // return "https://www.starryislands.com/static/lz-fishing-privacy/userPrivacyPolicy.html";
                // return "https://www.starryislands.com/agreement/userjp.html";
                return "userjp";
            }
            else
            {
                // return "https://vopi.jp/agreement/usercn.html";
                return "usercn";
            }
        }
    }
}