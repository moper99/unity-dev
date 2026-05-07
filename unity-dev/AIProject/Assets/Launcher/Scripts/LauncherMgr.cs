using System;
using FairyGUI;
using UnityEngine;

namespace Launcher
{
    /// <summary>
    /// 热更界面加载管理器（FairyGUI 版本）。
    /// </summary>
    /// <remarks>
    /// 保持原有的静态 API 接口，内部委托给 LauncherFairyUIMgr。
    /// </remarks>
    public static class LauncherMgr
    {
        /// <summary>
        /// 初始化 Launcher 管理器。
        /// </summary>
        public static void Initialize()
        {
            // 注册 FairyGUI 组件扩展
            LauncherUI.LauncherUIBinder.BindAll();

            LauncherFairyUIMgr.Instance.Initialize();
            Debug.Log("======== 初始化 LauncherMgr 完成 ========");
        }

        /// <summary>
        /// 释放 Launcher 管理器。
        /// </summary>
        public static void Release()
        {
            LauncherFairyUIMgr.Instance.Release();
            Debug.Log("======== 释放 LauncherMgr 完成 ========");
        }

        /// <summary>
        /// 更新所有窗口（每帧调用）。
        /// </summary>
        public static void Update()
        {
            LauncherFairyUIMgr.Instance.OnUpdate();
        }

        #region UI 调用（新版 API）

        /// <summary>
        /// 显示提示框。
        /// </summary>
        /// <param name="desc">描述文本。</param>
        /// <param name="onConfirm">确认回调。</param>
        /// <param name="onCancel">取消回调。</param>
        /// <param name="onUpdate">更新回调。</param>
        public static void ShowMessageBox(string desc, Action onConfirm = null,
            Action onCancel = null, Action onUpdate = null)
        {
            var ui = LauncherFairyUIMgr.Instance.ShowUI<LoadTipsUIWindow>(
                LoadTipsUIWindow.PackageNameConst,
                LoadTipsUIWindow.ComponentNameConst,
                desc);
            ui?.SetAllCallback(onConfirm, onUpdate, onCancel);
        }

        /// <summary>
        /// 刷新更新进度。
        /// </summary>
        /// <param name="progress">进度值（0-1）。</param>
        public static void RefreshProgress(float progress)
        {
            var ui = LauncherFairyUIMgr.Instance.GetUI<LoadUpdateUIWindow>();
            if (ui == null)
            {
                ui = LauncherFairyUIMgr.Instance.ShowUI<LoadUpdateUIWindow>(
                    LoadUpdateUIWindow.PackageNameConst,
                    LoadUpdateUIWindow.ComponentNameConst);
            }
            ui?.RefreshProgress(progress);
        }

        #endregion

        #region 向后兼容 API（旧版接口）

        /// <summary>
        /// 显示 UI（向后兼容旧版 API）。
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        /// <param name="param">参数。</param>
        public static void ShowUI<T>(object param = null) where T : LauncherFairyUIBase, new()
        {
            string packageName;
            string componentName;

            // 根据类型确定包名和组件名（支持继承关系）
            if (typeof(LoadUpdateUIWindow).IsAssignableFrom(typeof(T)))
            {
                packageName = LoadUpdateUIWindow.PackageNameConst;
                componentName = LoadUpdateUIWindow.ComponentNameConst;
            }
            else if (typeof(LoadTipsUIWindow).IsAssignableFrom(typeof(T)))
            {
                packageName = LoadTipsUIWindow.PackageNameConst;
                componentName = LoadTipsUIWindow.ComponentNameConst;
            }
            else
            {
                Debug.LogError($"[LauncherMgr] Unknown UI type: {typeof(T).Name}");
                return;
            }

            LauncherFairyUIMgr.Instance.ShowUI<T>(packageName, componentName, param);
        }

        /// <summary>
        /// 关闭 UI（向后兼容旧版 API）。
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        public static void CloseUI<T>() where T : LauncherFairyUIBase
        {
            LauncherFairyUIMgr.Instance.CloseUI<T>();
        }

        /// <summary>
        /// 隐藏所有 UI（向后兼容旧版 API）。
        /// </summary>
        public static void HideAllUI()
        {
            LauncherFairyUIMgr.Instance.CloseAll();
        }

        /// <summary>
        /// 获取活跃的 UI（向后兼容旧版 API）。
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        /// <returns>窗口实例。</returns>
        public static T GetActiveUI<T>() where T : LauncherFairyUIBase
        {
            return LauncherFairyUIMgr.Instance.GetUI<T>();
        }

        #endregion
    }
}
