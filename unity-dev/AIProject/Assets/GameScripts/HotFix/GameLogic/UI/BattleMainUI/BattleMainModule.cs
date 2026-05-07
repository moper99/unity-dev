using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 战斗主界面模块。
    /// </summary>
    public class BattleMainModule : Singleton<BattleMainModule>
    {
        
        protected override void OnRelease()
        {
            
        }
        
        public override void Active()
        {
            // 注册 BattleMainUI Binder
            BattleMainUIBinder.BindAll();
        }
    }
}
