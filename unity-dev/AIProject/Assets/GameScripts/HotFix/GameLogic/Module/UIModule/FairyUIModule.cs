using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// FairyGUI UI管理模块。
    /// </summary>
    public sealed class FairyUIModule : Singleton<FairyUIModule>, IUpdate
    {
        private readonly Dictionary<Type, FairyUIWindow> _windows = new Dictionary<Type, FairyUIWindow>();
        private readonly List<FairyUIWindow> _windowStack = new List<FairyUIWindow>();
        private readonly List<FairyUIWindow> _updateWindows = new List<FairyUIWindow>();
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private IObjectPool<UIWindowObject> _windowPool;
        private bool _isInitialized = false;

        /// <summary>
        /// 模块初始化。
        /// </summary>
        protected override void OnInit()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            Log.Info("FairyUIModule initialized.");
        }

        /// <summary>
        /// 模块释放。
        /// </summary>
        protected override void OnRelease()
        {
            _cts.Cancel();
            _cts.Dispose();
            CloseAll();
            FairyUIPackageLoader.RemoveAllPackages();
            _isInitialized = false;
            Log.Info("FairyUIModule released.");
        }

        #region 公开API

        /// <summary>
        /// 显示窗口（同步）。
        /// </summary>
        public T ShowUI<T>(params object[] userDatas) where T : FairyUIWindow, new()
        {
            return ShowUIImp<T>(false, userDatas);
        }

        /// <summary>
        /// 显示窗口（异步 fire-and-forget）。
        /// </summary>
        public void ShowUIAsync<T>(params object[] userDatas) where T : FairyUIWindow, new()
        {
            ShowUIImp<T>(true, userDatas);
        }

        /// <summary>
        /// 异步显示窗口（等待加载完成）。
        /// </summary>
        public async UniTask<T> ShowUIAsyncAwait<T>(params object[] userDatas) where T : FairyUIWindow, new()
        {
            return await ShowUIAwaitImp<T>(userDatas);
        }

        /// <summary>
        /// 显示窗口（通过类型）。
        /// </summary>
        public void ShowUI(Type type, params object[] userDatas)
        {
            ShowUIImp(type, false, userDatas);
        }

        /// <summary>
        /// 异步显示窗口（通过类型）。
        /// </summary>
        public void ShowUIAsync(Type type, params object[] userDatas)
        {
            ShowUIImp(type, true, userDatas);
        }

        /// <summary>
        /// 关闭窗口。
        /// </summary>
        public void CloseUI<T>() where T : FairyUIWindow
        {
            CloseUI(typeof(T));
        }

        /// <summary>
        /// 关闭窗口（通过类型）。
        /// </summary>
        public void CloseUI(Type type)
        {
            if (_windows.TryGetValue(type, out var window))
            {
                UnregisterWindowUpdate(window);
                if (window.Close())
                {
                    _windows.Remove(type);
                    _windowStack.Remove(window);
                }
            }
        }

        /// <summary>
        /// 隐藏窗口。
        /// </summary>
        public void HideUI<T>() where T : FairyUIWindow
        {
            HideUI(typeof(T));
        }

        /// <summary>
        /// 隐藏窗口（通过类型）。
        /// </summary>
        public void HideUI(Type type)
        {
            if (_windows.TryGetValue(type, out var window))
            {
                window.Hide();
            }
        }

        /// <summary>
        /// 关闭所有窗口。
        /// </summary>
        public void CloseAll()
        {
            _updateWindows.Clear();
            for (int i = _windowStack.Count - 1; i >= 0; i--)
            {
                _windowStack[i].Close();
                _windowStack[i].Destroy();
            }
            _windowStack.Clear();
            _windows.Clear();
        }

        /// <summary>
        /// 检查窗口是否存在。
        /// </summary>
        public bool HasWindow<T>() where T : FairyUIWindow
        {
            return HasWindow(typeof(T));
        }

        /// <summary>
        /// 检查窗口是否存在（通过类型）。
        /// </summary>
        public bool HasWindow(Type type)
        {
            return _windows.ContainsKey(type);
        }

        /// <summary>
        /// 获取窗口实例。
        /// </summary>
        public T GetUI<T>() where T : FairyUIWindow
        {
            if (_windows.TryGetValue(typeof(T), out var window))
            {
                return window as T;
            }
            return null;
        }

        /// <summary>
        /// 切换窗口显示/隐藏。
        /// </summary>
        public void ToggleUI<T>() where T : FairyUIWindow, new()
        {
            if (HasWindow<T>())
            {
                var window = GetUI<T>();
                if (window.IsVisible)
                    HideUI<T>();
                else
                    ShowUI<T>();
            }
            else
            {
                ShowUI<T>();
            }
        }

        /// <summary>
        /// 显示UI并执行回调。
        /// </summary>
        public void ShowUI<T>(Action<T> onShow) where T : FairyUIWindow, new()
        {
            var window = ShowUI<T>();
            onShow?.Invoke(window);
        }

        /// <summary>
        /// 获取所有窗口数量。
        /// </summary>
        public int WindowCount => _windowStack.Count;

        #endregion

        #region 内部窗口Update管理

        /// <summary>
        /// 窗口加载失败时清理。
        /// </summary>
        internal void OnWindowLoadFailed(FairyUIWindow window)
        {
            Type type = window.GetType();
            UnregisterWindowUpdate(window);
            _windows.Remove(type);
            _windowStack.Remove(window);
        }

        /// <summary>
        /// 注册需要每帧更新的窗口。
        /// </summary>
        internal void RegisterWindowUpdate(FairyUIWindow window)
        {
            if (!_updateWindows.Contains(window))
                _updateWindows.Add(window);
        }

        /// <summary>
        /// 注销窗口更新。
        /// </summary>
        internal void UnregisterWindowUpdate(FairyUIWindow window)
        {
            _updateWindows.Remove(window);
        }

        #endregion

        #region 私有方法

        private T ShowUIImp<T>(bool isAsync, params object[] userDatas) where T : FairyUIWindow, new()
        {
            Type type = typeof(T);
            return ShowUIImp(type, isAsync, userDatas) as T;
        }

        private FairyUIWindow ShowUIImp(Type type, bool isAsync, params object[] userDatas)
        {
            if (_windows.TryGetValue(type, out var existingWindow))
            {
                existingWindow.Show();
                return existingWindow;
            }

            var window = GetFromPoolOrCreate(type);
            if (window == null)
            {
                Log.Error($"Failed to create window: {type.FullName}");
                return null;
            }

            window.WindowName = type.FullName;
            _windows[type] = window;
            _windowStack.Add(window);

            window.Load(isAsync, userDatas);

            if (!isAsync && window.IsLoadDone)
            {
                window.Show();
            }

            return window;
        }

        private async UniTask<T> ShowUIAwaitImp<T>(params object[] userDatas) where T : FairyUIWindow, new()
        {
            Type type = typeof(T);

            if (_windows.TryGetValue(type, out var existingWindow))
            {
                existingWindow.Show();
                return existingWindow as T;
            }

            var window = GetFromPoolOrCreate(type);
            if (window == null)
            {
                Log.Error($"Failed to create window: {type.FullName}");
                return null;
            }

            window.WindowName = type.FullName;
            _windows[type] = window;
            _windowStack.Add(window);

            await window.LoadAsync(_cts.Token, userDatas);

            if (window.IsLoadDone)
            {
                window.Show();
            }

            return window as T;
        }

        #endregion

        #region IUpdate

        public void OnUpdate()
        {
            for (int i = 0; i < _updateWindows.Count; i++)
            {
                _updateWindows[i].InternalUpdate();
            }
        }

        #endregion

        #region 对象池

        /// <summary>
        /// 回收窗口到对象池。
        /// </summary>
        internal void RecycleToPool(FairyUIWindow window)
        {
            window.ResetState();
            var obj = UIWindowObject.Create(window);
            if (_windowPool == null)
            {
                _windowPool = ModuleSystem.GetModule<IObjectPoolModule>()
                    .CreateSingleSpawnObjectPool<UIWindowObject>("UIWindowPool", capacity: 10, expireTime: 300f);
            }
            _windowPool.Register(obj, false);
        }

        /// <summary>
        /// 从对象池获取或创建窗口。
        /// </summary>
        private FairyUIWindow GetFromPoolOrCreate(Type type)
        {
            string poolName = type.FullName;
            if (_windowPool != null && _windowPool.CanSpawn(poolName))
            {
                var obj = _windowPool.Spawn(poolName);
                return obj.Target as FairyUIWindow;
            }
            return Activator.CreateInstance(type) as FairyUIWindow;
        }

        /// <summary>
        /// UI窗口对象池包装。
        /// </summary>
        internal class UIWindowObject : ObjectBase
        {
            private FairyUIWindow _window;

            public static UIWindowObject Create(FairyUIWindow window)
            {
                var obj = new UIWindowObject();
                obj.Initialize(window.GetType().FullName, window);
                obj._window = window;
                return obj;
            }

            protected override void OnSpawn()
            {
            }

            protected override void OnUnspawn()
            {
                _window.Hide();
                _window.ResetState();
            }

            protected override void Release(bool isShutdown)
            {
                if (_window?._window != null)
                {
                    _window._window.Dispose();
                    _window._window = null;
                    _window._contentPane = null;
                    _window.GObject = null;
                }
                _window = null;
            }
        }

        #endregion
    }
}
