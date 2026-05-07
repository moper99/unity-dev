using System.Collections.Generic;
using System.Reflection;
using GameLogic;
using Cysharp.Threading.Tasks;
#if ENABLE_OBFUZ
using Obfuz;
#endif
using TEngine;
#pragma warning disable CS0436


/// <summary>
/// 游戏App。
/// </summary>
#if ENABLE_OBFUZ
[ObfuzIgnore(ObfuzScope.TypeName | ObfuzScope.MethodName)]
#endif
public partial class GameApp
{
    private static List<Assembly> _hotfixAssembly;

    /// <summary>
    /// 热更域App主入口。
    /// </summary>
    /// <param name="objects"></param>
    public static void Entrance(object[] objects)
    {
        GameEventHelper.Init();
        _hotfixAssembly = (List<Assembly>)objects[0];
        Log.Warning("======= 看到此条日志代表你成功运行了热更新代码 =======");
        Log.Warning("======= Entrance GameApp =======");
        Utility.Unity.AddDestroyListener(Release);
        Log.Warning("======= StartGameLogic =======");
        StartGameLogic();
    }
    
    private static void StartGameLogic()
    {
        // 初始化模块（触发 Active，注册 Binder）
        InitUIModuleActive();

        // 显示登录界面
        GameModule.UI.ShowUIAsync<LoginUIPanel>();
    }
    
    /// <summary>
    /// 初始化UI各模块。
    /// </summary>
    private static void InitUIModuleActive()
    {
        FairyUIPackageLoader.AddPackage("Basics");
        BasicsBinder.BindAll();
        LoginModule.Instance.Active();
        BattleMainModule.Instance.Active();
    }

    private static void Release()
    {
        Log.Info("======= Release GameApp =======");
        SingletonSystem.Release();
    }
}
