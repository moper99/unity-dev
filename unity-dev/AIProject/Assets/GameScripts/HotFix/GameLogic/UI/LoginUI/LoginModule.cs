using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 登录模块。
    /// </summary>
    public class LoginModule : Singleton<LoginModule>
    {
        /// <summary>
        /// 是否已登录。
        /// </summary>
        public bool IsLoggedIn { get; private set; }

        /// <summary>
        /// 模拟登录。
        /// </summary>
        /// <returns>登录是否成功。</returns>
        public async UniTask<bool> LoginAsync()
        {
            Log.Info("[LoginModule] 开始登录...");
            
            // 模拟登录请求延迟
            await UniTask.Delay(1500);
            
            IsLoggedIn = true;
            Log.Info("[LoginModule] 登录成功");
            return true;
        }

        /// <summary>
        /// 重置登录状态。
        /// </summary>
        public void ResetLogin()
        {
            IsLoggedIn = false;
        }

        public override void Active()
        {
            // 注册 LoginUI Binder
            //LoginUIBinder.BindAll();
        }

        protected override void OnRelease()
        {
            IsLoggedIn = false;
            Log.Info("LoginModule released.");
        }
        
        /// <summary>
        /// 登录成功回调。
        /// </summary>
        public void OnLoginSuccess()
        {
            Log.Info("[GameApp] 登录成功，开始跳转场景");
            LoginSuccessAsync().Forget();
        }

        /// <summary>
        /// 登录成功后异步跳转场景。
        /// </summary>
        private async UniTaskVoid LoginSuccessAsync()
        {
            // 关闭登录界面
            GameModule.UI.CloseUI<LoginUIPanel>();
        
            // 加载战斗场景
            Log.Info("[GameApp] 加载战斗场景...");
            await GameModule.Scene.LoadSceneAsync("Assets/AssetRaw/Scenes/Battle/battle.unity");
        
            // 显示主界面
            Log.Info("[GameApp] 场景加载完成，显示主界面");
            GameModule.UI.ShowUIAsync<BattleMainUIPanel>();
        }
    }
}
