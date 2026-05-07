using FairyGUI;
using TEngine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// LoginUIPanel 窗口业务逻辑。
    /// </summary>
    public partial class LoginUIPanel
    {
        /// <inheritdoc/>
        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            
            GameEvent.AddEventListener(ILoginUI_Event.ShowLoginUI,ShowLoginUI);
            
            BtnEnterGame.onClick.Add(OnClickEnterGame);
        }

        /// <inheritdoc/>
        protected override void UnregisterEvent()
        {
            base.UnregisterEvent();
            GameEvent.RemoveEventListener(ILoginUI_Event.ShowLoginUI,ShowLoginUI);
            
            BtnEnterGame.onClick.Remove(OnClickEnterGame);
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            Log.Info("LoginUIPanel OnCreate");
        }

        /// <inheritdoc/>
        protected override void OnRefresh()
        {
            base.OnRefresh();
            Log.Info("LoginUIPanel OnRefresh");
        }

        /// <inheritdoc/>
        protected override void OnShow()
        {
            base.OnShow();
            Log.Info("LoginUIPanel OnShow");
            // 初始化登录界面状态
            CtrlLoading.selectedIndex = 0;
            BtnEnterGame.enabled = true;
        }

        /// <inheritdoc/>
        protected override void OnHide()
        {
            base.OnHide();
            Log.Info("LoginUIPanel OnHide");
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Log.Info("LoginUIPanel OnDestroy");
        }

        /// <summary>
        /// 点击进入游戏按钮。
        /// </summary>
        private void OnClickEnterGame()
        {
            Log.Info("点击进入游戏");
            // 切换到加载状态
            CtrlLoading.selectedIndex = 1;
            // 禁用按钮防止重复点击
            BtnEnterGame.enabled = false;
            // 执行登录
            DoLogin().Forget();
        }

        /// <summary>
        /// 执行登录流程。
        /// </summary>
        private async UniTaskVoid DoLogin()
        {
            //test
            GameEvent.Send(ILoginUI_Event.ShowLoginUI);
            
            var success = await LoginModule.Instance.LoginAsync();
            if (success)
            {
                // 登录成功，通知GameApp跳转场景
                LoginModule.Instance.OnLoginSuccess();
                
            }
            else
            {
                // 登录失败，恢复界面状态
                CtrlLoading.selectedIndex = 0;
                BtnEnterGame.enabled = true;
                Log.Error("登录失败");
            }
        }
        
        private void ShowLoginUI()
        {
            Debug.Log("ShowLoginUI-----------------");
        }
    }
}
