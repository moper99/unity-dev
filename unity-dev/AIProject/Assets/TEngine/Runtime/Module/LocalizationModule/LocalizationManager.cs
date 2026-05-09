using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using YooAsset;
using TEngine.Localization;
using FairyGUI;

namespace TEngine
{
    /// <summary>
    /// 极简本地化管理模块，负责JSON资源加载与I2 Localization同步。
    /// </summary>
    [DisallowMultipleComponent]
    public class LocalizationManager : MonoBehaviour, ILocalizationModule
    {
        private LanguageSource _languageSource;
        private LanguageSourceData SourceData
        {
            get
            {
                if (_languageSource == null)
                {
                    _languageSource = gameObject.AddComponent<LanguageSource>();
                }
                return _languageSource.SourceData;
            }
        }

        private string LangPrefKey => Constant.Setting.Language;
        private Dictionary<string, string> _strMap = new Dictionary<string, string>();
        private Language _currentLanguage = Language.Unspecified;
        private IResourceModule _resourceModule;

        public Language Language
        {
            get => _currentLanguage;
            set => SetLanguage(value);
        }

        public Language SystemLanguage => LocalizationUtils.SystemLanguage;

        private void Awake()
        {
            _resourceModule = ModuleSystem.GetModule<IResourceModule>();
            
            // 创建并注册桥接模块
            var module = new LocalizationModule();
            module.Bind(this);
            ModuleSystem.RegisterModule<ILocalizationModule>(module);
        }

        private async void Start()
        {
            // 初始化语言设置
            Language lang = GetSaveLanguage();
            if (lang == Language.Unspecified)
            {
                lang = SystemLanguage;
            }

            await LoadLanguageAsync(lang);
        }

        public string GetString(string strID)
        {
            if (_strMap.TryGetValue(strID, out var value))
            {
                return value;
            }
            Log.Error($"[Localization] 找不到国际化字符串: {strID} (Language: {_currentLanguage})");
            return strID;
        }

        public bool SetLanguage(Language language)
        {
            if (language == Language.Unspecified) return false;
            if (_currentLanguage == language) return true;

            _currentLanguage = language;
            PlayerPrefs.SetString(LangPrefKey, language.ToString());
            PlayerPrefs.Save();

            LoadLanguageAsync(language).Forget();
            return true;
        }

        public bool SetLanguage(string languageStr)
        {
            return SetLanguage(LocalizationUtils.GetLanguage(languageStr));
        }

        private async UniTask LoadLanguageAsync(Language lang)
        {
            _currentLanguage = lang;
            _strMap.Clear();

            // 1. 加载通用文本表
            await LoadJsonTableAsync(lang.GetFileName());
            // 2. 加载配置文本表
            await LoadJsonTableAsync(lang.GetConfigFileName());
            // 3. 加载 UI 文本表 (JSON 形式，用于代码访问)
            await LoadJsonTableAsync($"ui_string_{lang.GetLangShortString()}");

            // 4. 同步到 I2 Localization (驱动场景中 I2 目标刷新)
            SyncToI2(lang);

            // 5. 同步到 FairyGUI 原生本地化系统 (驱动 FairyGUI UI 刷新)
            await SyncFairyGUIStrings(lang);

            Log.Info($"[Localization] 语言加载完成: {lang}");
        }

        /// <summary>
        /// 使用 FairyGUI 原生方式设置 UI 字符串源。
        /// </summary>
        private async UniTask SyncFairyGUIStrings(Language lang)
        {
            try
            {
                string assetName = $"Localization_{lang.GetLangShortString()}";
                using var handle = YooAssets.LoadAssetAsync<TextAsset>(assetName);
                await handle;

                if (handle.AssetObject == null)
                {
                    Log.Warning($"[Localization] 无法加载 FairyGUI 语言 XML 资源: {assetName}");
                    return;
                }

                var textAsset = (TextAsset)handle.AssetObject;
                FairyGUI.Utils.XML xml = new FairyGUI.Utils.XML(textAsset.text);
                FairyGUI.UIPackage.SetStringsSource(xml);
                
                Log.Info($"[Localization] FairyGUI 原生字符串源已更新: {assetName}");
            }
            catch (System.Exception e)
            {
                Log.Error($"[Localization] 同步 FairyGUI 字符串出错: {e.Message}");
            }
        }

        private async UniTask LoadJsonTableAsync(string assetName)
        {
            try
            {
                using var handle = YooAssets.LoadAssetAsync<TextAsset>(assetName);
                await handle;
                
                if (handle.AssetObject == null)
                {
                    Log.Warning($"[Localization] 无法加载语言资源文件: {assetName}");
                    return;
                }

                var textAsset = (TextAsset)handle.AssetObject;
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);
                if (data != null)
                {
                    foreach (var kv in data)
                    {
                        _strMap[kv.Key] = kv.Value;
                    }
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[Localization] 解析语言文件 {assetName} 出错: {e.Message}");
            }
        }

        private void SyncToI2(Language lang)
        {
            string langStr = LocalizationUtils.GetLanguageStr(lang);
            int langIdx = SourceData.GetLanguageIndex(langStr);
            
            if (langIdx < 0)
            {
                Log.Warning($"[Localization] I2 Localization 中不存在语言: {langStr}");
                return;
            }

            // 将内存字典同步到 I2 Localization 系统
            foreach (var kv in _strMap)
            {
                var termData = SourceData.GetTermData(kv.Key);
                if (termData == null)
                {
                    termData = SourceData.AddTerm(kv.Key);
                }
                termData.SetTranslation(langIdx, kv.Value, null);
            }

            // 触发全局 UI 刷新
            Localization.LocalizationManager.LocalizeAll();
        }

        private Language GetSaveLanguage()
        {
            string langStr = PlayerPrefs.GetString(LangPrefKey, string.Empty);
            if (string.IsNullOrEmpty(langStr)) return Language.Unspecified;
            return LocalizationUtils.GetLanguage(langStr);
        }
    }
}
