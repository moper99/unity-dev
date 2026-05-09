using Cysharp.Threading.Tasks;

namespace TEngine
{
    public interface ILocalizationModule
    {
        /// <summary>
        /// 获取或设置本地化语言。
        /// </summary>
        public Language Language { get; set; }

        /// <summary>
        /// 获取系统语言。
        /// </summary>
        public Language SystemLanguage { get; }

        /// <summary>
        /// 根据ID获取翻译文本。
        /// </summary>
        public string GetString(string strID);

        /// <summary>
        /// 设置当前语言。
        /// </summary>
        public bool SetLanguage(Language language);

        /// <summary>
        /// 设置当前语言。
        /// </summary>
        public bool SetLanguage(string language);
    }
}
