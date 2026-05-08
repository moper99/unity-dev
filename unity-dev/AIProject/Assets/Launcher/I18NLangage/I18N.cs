using System.Collections.Generic;
using Newtonsoft.Json;
using TEngine;
using UnityEngine;
using YooAsset;

namespace Launcher
{
    public static class I18N
    {
        static Language _lang = Language.Japanese;
        public static string CurLangString => _lang.GetLangString();

        public static SystemLanguage CurLanguage => _lang.GetSystemLanguage();

        private static Dictionary<string, string> _strMap;
        private static Dictionary<string, string> _aotStrMap;

        public static Language GetSaveLanguage()
        {
            var langInt = PlayerPrefs.GetInt("lang", 1);;
            return (Language)langInt;
        }

        public static void SetSaveLanguage(Language lang)
        {
            PlayerPrefs.SetInt("lang", (int)lang);
            _lang = lang;
        }

        public static string GetString(string strID)
        {
            if (!_strMap.ContainsKey(strID))
            {
                Log.Error($"国际化字符串:{strID} 找不到{_lang}语言下的文本。");
                return strID;
            }
            return _strMap[strID];
        }

        public static string GetAOTString(string strID)
        {
            if (!_aotStrMap.ContainsKey(strID))
            {
                Log.Error($"国际化字符串:{strID} => 找不到{_lang}语言下的文本。");
                return "";
            }
            return _aotStrMap[strID];
        }

        public static void InitStrings()
        {
            var la = GetSaveLanguage();
            _lang = la;
            using var handle = YooAssets.LoadAssetSync<TextAsset>(la.GetFileName());
            var textAsset = (TextAsset)handle.AssetObject;
            _strMap = File2Dict(textAsset);
            using var handle2 = YooAssets.LoadAssetSync<TextAsset>(la.GetConfigFileName());
            var textAsset2 = (TextAsset)handle2.AssetObject;
            var map2 = File2Dict(textAsset2);
            foreach (var pa in map2)
            {
                _strMap[pa.Key] = pa.Value;
            }
            Log.Error($"i18n string init");
        }

        public static void InitAOTStrings()
        {
            var la = GetSaveLanguage();
            _lang = la;
            var fileName = la.GetAOTFileName();
            using var handle = new AotResHandler(fileName);
            var asset = handle.LoadAssetSync<TextAsset>(fileName);
            _aotStrMap = File2Dict(asset);
            Log.Error($"aot i18n string init");
        }

        private static Dictionary<string, string> File2Dict(TextAsset textAsset)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);
        }
    }
}