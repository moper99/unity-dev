using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Launcher
{
    /// <summary>
    /// Launcher FairyGUI 窗口管理器。
    /// </summary>
    /// <remarks>
    /// 精简版 FairyUIModule，不依赖 TEngine，适用于热更新前的 Launcher 阶段。
    /// </remarks>
    public sealed class LauncherFairyUIMgr
    {
        #region 单例

        private static LauncherFairyUIMgr _instance;

        /// <summary>
        /// 获取单例实例。
        /// </summary>
        public static LauncherFairyUIMgr Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LauncherFairyUIMgr();
                }
                return _instance;
            }
        }

        private LauncherFairyUIMgr()
        {
        }

        #endregion

        #region 字段

        private readonly Dictionary<string, LauncherFairyUIBase> _windows = new Dictionary<string, LauncherFairyUIBase>();
        private readonly List<LauncherFairyUIBase> _windowStack = new List<LauncherFairyUIBase>();
        private bool _isInitialized = false;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化管理器。
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            Debug.Log("[LauncherFairyUIMgr] Initialized");
        }

        /// <summary>
        /// 释放管理器。
        /// </summary>
        public void Release()
        {
            CloseAll();
            LauncherFairyGUIPackageLoader.RemoveAllPackages();
            _isInitialized = false;
            Debug.Log("[LauncherFairyUIMgr] Released");
        }

        #endregion

        #region 窗口管理

        /// <summary>
        /// 显示窗口（同步）。
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        /// <param name="packageName">FairyGUI 包名称。</param>
        /// <param name="componentName">FairyGUI 组件名称。</param>
        /// <param name="userDatas">用户数据。</param>
        /// <returns>窗口实例。</returns>
        public T ShowUI<T>(string packageName, string componentName, params object[] userDatas) where T : LauncherFairyUIBase, new()
        {
            return ShowUIImp<T>(packageName, componentName, false, userDatas);
        }

        /// <summary>
        /// 显示窗口（异步）。
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        /// <param name="packageName">FairyGUI 包名称。</param>
        /// <param name="componentName">FairyGUI 组件名称。</param>
        /// <param name="userDatas">用户数据。</param>
        /// <returns>窗口实例。</returns>
        public void ShowUIAsync<T>(string packageName, string componentName, params object[] userDatas) where T : LauncherFairyUIBase, new()
        {
            ShowUIImp<T>(packageName, componentName, true, userDatas);
        }

        /// <summary>
        /// 关闭窗口。
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        public void CloseUI<T>() where T : LauncherFairyUIBase
        {
            CloseUI(typeof(T));
        }

        /// <summary>
        /// 关闭窗口（通过类型）。
        /// </summary>
        /// <param name="type">窗口类型。</param>
        public void CloseUI(Type type)
        {
            string windowName = type.FullName;
            if (_windows.TryGetValue(windowName, out var window))
            {
                window.Destroy();
                _windows.Remove(windowName);
                _windowStack.Remove(window);
            }
        }

        /// <summary>
        /// 隐藏窗口。
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        public void HideUI<T>() where T : LauncherFairyUIBase
        {
            HideUI(typeof(T));
        }

        /// <summary>
        /// 隐藏窗口（通过类型）。
        /// </summary>
        /// <param name="type">窗口类型。</param>
        public void HideUI(Type type)
        {
            string windowName = type.FullName;
            if (_windows.TryGetValue(windowName, out var window))
            {
                window.Hide();
            }
        }

        /// <summary>
        /// 关闭所有窗口。
        /// </summary>
        public void CloseAll()
        {
            foreach (var window in _windowStack)
            {
                window.Destroy();
            }
            _windowStack.Clear();
            _windows.Clear();
        }

        /// <summary>
        /// 检查窗口是否存在。
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        /// <returns>是否存在。</returns>
        public bool HasWindow<T>() where T : LauncherFairyUIBase
        {
            return HasWindow(typeof(T));
        }

        /// <summary>
        /// 检查窗口是否存在（通过类型）。
        /// </summary>
        /// <param name="type">窗口类型。</param>
        /// <returns>是否存在。</returns>
        public bool HasWindow(Type type)
        {
            return _windows.ContainsKey(type.FullName);
        }

        /// <summary>
        /// 获取窗口实例。
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        /// <returns>窗口实例，不存在返回 null。</returns>
        public T GetUI<T>() where T : LauncherFairyUIBase
        {
            string windowName = typeof(T).FullName;
            if (_windows.TryGetValue(windowName, out var window))
            {
                return window as T;
            }
            return null;
        }

        /// <summary>
        /// 获取所有窗口数量。
        /// </summary>
        public int WindowCount => _windowStack.Count;

        #endregion

        #region 更新

        /// <summary>
        /// 更新所有窗口（每帧调用）。
        /// </summary>
        public void OnUpdate()
        {
            if (_windowStack == null || _windowStack.Count == 0)
            {
                return;
            }

            int count = _windowStack.Count;
            for (int i = 0; i < _windowStack.Count; i++)
            {
                if (_windowStack.Count != count)
                {
                    break;
                }

                var window = _windowStack[i];
                window.InternalUpdate();
            }
        }

        #endregion

        #region 私有方法

        private T ShowUIImp<T>(string packageName, string componentName, bool isAsync, params object[] userDatas) where T : LauncherFairyUIBase, new()
        {
            Type type = typeof(T);
            string windowName = type.FullName;

            // 窗口已存在，重新显示
            if (_windows.TryGetValue(windowName, out var existingWindow))
            {
                existingWindow.Show();
                return existingWindow as T;
            }

            // 确保 FairyGUI 包已加载
            if (!LauncherFairyGUIPackageLoader.IsPackageLoaded(packageName))
            {
                if (isAsync)
                {
                    // 异步加载包
                    ShowUIAsyncImp<T>(packageName, componentName, userDatas).Forget();
                    return null;
                }
                else
                {
                    // 同步加载包
                    LauncherFairyGUIPackageLoader.AddPackage(packageName);
                }
            }

            // 创建 FairyGUI 对象
            var gObject = LauncherFairyGUIPackageLoader.CreateObject(packageName, componentName);
            if (gObject == null)
            {
                Debug.LogError($"[LauncherFairyUIMgr] Failed to create FairyGUI object: {packageName}/{componentName}");
                return null;
            }

            // 添加到 FairyGUI GRoot（使用 FairyGUI 自己的 UI 系统）
            FairyGUI.GRoot.inst.AddChild(gObject);

            // 创建窗口实例
            var window = new T();
            window._userDatas = userDatas;
            window.Init(gObject);

            // 添加到管理
            _windows[windowName] = window;
            _windowStack.Add(window);

            // 显示窗口
            window.Show();

            return window;
        }

        private async UniTaskVoid ShowUIAsyncImp<T>(string packageName, string componentName, object[] userDatas) where T : LauncherFairyUIBase, new()
        {
            try
            {
                // 异步加载 FairyGUI 包
                await LauncherFairyGUIPackageLoader.AddPackageAsync(packageName);

                Type type = typeof(T);
                string windowName = type.FullName;

                // 检查窗口是否已创建（可能在异步加载期间被创建）
                if (_windows.ContainsKey(windowName))
                {
                    return;
                }

                // 创建 FairyGUI 对象
                var gObject = LauncherFairyGUIPackageLoader.CreateObject(packageName, componentName);
                if (gObject == null)
                {
                    Debug.LogError($"[LauncherFairyUIMgr] Failed to create FairyGUI object async: {packageName}/{componentName}");
                    return;
                }

                // 添加到 FairyGUI GRoot（使用 FairyGUI 自己的 UI 系统）
                FairyGUI.GRoot.inst.AddChild(gObject);

                // 创建窗口实例
                var window = new T();
                window._userDatas = userDatas;
                window.Init(gObject);

                // 添加到管理
                _windows[windowName] = window;
                _windowStack.Add(window);

                // 显示窗口
                window.Show();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LauncherFairyUIMgr] Failed to show UI async: {e.Message}");
            }
        }

        #endregion
    }
}
