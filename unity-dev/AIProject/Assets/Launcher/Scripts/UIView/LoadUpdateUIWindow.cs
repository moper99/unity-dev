using FairyGUI;
using LauncherUI;
using UnityEngine;

namespace Launcher
{
    /// <summary>
    /// UI 更新界面业务逻辑。
    /// </summary>
    /// <remarks>
    /// 使用 FairyGUI 自动生成的 LauncherUI 组件类。
    /// </remarks>
    public class LoadUpdateUIWindow : LauncherFairyUIBase
    {
        #region FairyGUI 组件

        private LauncherUI.LauncherUI _panel;

        #endregion

        #region 常量

        /// <summary>
        /// FairyGUI 包名称。
        /// </summary>
        public const string PackageNameConst = "LauncherUI";

        /// <summary>
        /// FairyGUI 组件名称。
        /// </summary>
        public const string ComponentNameConst = "LauncherUI";

        #endregion

        #region 生命周期

        protected override void OnCreate()
        {
            base.OnCreate();

            _panel = GObject as LauncherUI.LauncherUI;
            if (_panel != null)
            {
                RefreshProgress(0f);
            }
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();

            if (_panel != null && UserData != null)
            {
                // 更新描述文本（如果存在）
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 刷新进度条。
        /// </summary>
        /// <param name="progress">进度值（0-1）。</param>
        public void RefreshProgress(float progress)
        {
            if (_panel?.progressBar != null)
            {
                _panel.progressBar.value = progress * 100;
                _panel.progressBar.visible = true;
            }
        }

        /// <summary>
        /// 刷新版本号。
        /// </summary>
        /// <param name="version">版本号文本。</param>
        public void RefreshVersion(string version)
        {
            if (_panel?.txtVersion != null)
            {
                _panel.txtVersion.text = version;
            }
        }

        /// <summary>
        /// 刷新 App ID。
        /// </summary>
        /// <param name="appid">App ID 文本。</param>
        public void RefreshAppid(string appid)
        {
            if (_panel?.txtLabelAppid != null)
            {
                _panel.txtLabelAppid.text = appid;
            }
        }

        #endregion
    }
}
