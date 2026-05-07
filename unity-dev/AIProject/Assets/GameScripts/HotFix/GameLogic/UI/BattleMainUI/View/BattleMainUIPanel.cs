using FairyGUI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// BattleMainUIPanel 窗口业务逻辑。
    /// </summary>
    public partial class BattleMainUIPanel
    {
        /// <inheritdoc/>
        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            // TODO: 注册战斗界面事件
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            Log.Info("BattleMainUIPanel OnCreate");
        }

        /// <inheritdoc/>
        protected override void OnRefresh()
        {
            base.OnRefresh();
            Log.Info("BattleMainUIPanel OnRefresh");
            // 刷新玩家信息
            RefreshUserInfo();
        }

        /// <inheritdoc/>
        protected override void OnShow()
        {
            base.OnShow();
            Log.Info("BattleMainUIPanel OnShow");
            PlayShowTransition(TransShow);
        }

        /// <inheritdoc/>
        protected override void OnHide()
        {
            base.OnHide();
            Log.Info("BattleMainUIPanel OnHide");
            PlayHideTransition(TransHide);
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Log.Info("BattleMainUIPanel OnDestroy");
        }

        /// <summary>
        /// 刷新玩家信息。
        /// </summary>
        private void RefreshUserInfo()
        {
            // TODO: 从数据层获取玩家信息并刷新UI
            Log.Info("RefreshUserInfo");
        }
    }
}
