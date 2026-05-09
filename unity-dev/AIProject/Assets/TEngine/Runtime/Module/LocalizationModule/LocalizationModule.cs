using Cysharp.Threading.Tasks;

namespace TEngine
{
    /// <summary>
    /// 本地化模块桥接类，用于适配 ModuleSystem 的 Module 基类要求。
    /// </summary>
    public class LocalizationModule : Module, ILocalizationModule
    {
        private LocalizationManager _manager;

        public void Bind(LocalizationManager manager)
        {
            _manager = manager;
        }

        public override void OnInit() { }
        public override void Shutdown() { }

        public Language Language
        {
            get => _manager.Language;
            set => _manager.SetLanguage(value);
        }

        public Language SystemLanguage => _manager.SystemLanguage;

        public string GetString(string strID) => _manager.GetString(strID);

        public bool SetLanguage(Language language) => _manager.SetLanguage(language);

        public bool SetLanguage(string language) => _manager.SetLanguage(language);
    }
}
