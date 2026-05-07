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
    }
}
