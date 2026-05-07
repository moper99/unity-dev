using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FairyGUI;
using TEngine;
using UnityEngine;

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
        /// 层级容器字典（每个UILayer对应一个GComponent容器）。
        /// </summary>
        private readonly Dictionary<UILayer, GComponent> _layerRoots = new Dictionary<UILayer, GComponent>();

        /// <summary>
        /// 模块初始化。
        /// </summary>
        protected override void OnInit()
        {
            if (_isInitialized)
            {
                return;
            }

            InitLayerContainers();
            _isInitialized = true;
            Log.Info("FairyUIModule initialized.");
        }

        /// <summary>
        /// 初始化层级容器。
        /// </summary>
        private void InitLayerContainers()
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var container = new GComponent();
                container.name = $"ViewRoot{layer}";
                GRoot.inst.AddChild(container);
                container.MakeFullScreen();
                _layerRoots[layer] = container;
            }
            Log.Info("UI layer containers initialized.");
        }

        /// <summary>
        /// 获取层级根容器。
        /// </summary>
        /// <param name="layer">UI层级。</param>
        /// <returns>层级根容器。</returns>
        public GComponent GetLayerRoot(UILayer layer)
        {
            if (_layerRoots.TryGetValue(layer, out var root))
            {
                return root;
            }
            Log.Warning($"Layer root not found: {layer}, returning Normal layer.");
            return _layerRoots[UILayer.Normal];
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

            // 清理层级容器
            foreach (var kvp in _layerRoots)
            {
                kvp.Value.Dispose();
            }
            _layerRoots.Clear();

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

        /// <summary>
        /// 显示窗口（通过名称，支持反射调用）。
        /// </summary>
        /// <param name="panelName">面板类型全名。</param>
        /// <param name="userDatas">用户数据。</param>
        public void ShowUI(string panelName, params object[] userDatas)
        {
            Type type = Type.GetType(panelName);
            if (type == null)
            {
                Log.Error($"ShowUI: Type not found: {panelName}");
                return;
            }
            ShowUIImp(type, false, userDatas);
        }

        /// <summary>
        /// 检查窗口是否存在（通过名称）。
        /// </summary>
        /// <param name="panelName">面板类型全名。</param>
        /// <returns>是否存在。</returns>
        public bool HasWindow(string panelName)
        {
            foreach (var kvp in _windows)
            {
                if (kvp.Key.FullName == panelName || kvp.Key.Name == panelName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取栈顶窗口。
        /// </summary>
        /// <returns>栈顶窗口。</returns>
        public FairyUIWindow GetTopWindow()
        {
            if (_windowStack.Count > 0)
            {
                return _windowStack[_windowStack.Count - 1];
            }
            return null;
        }

        /// <summary>
        /// 获取栈中指定位置的窗口。
        /// </summary>
        /// <param name="index">索引（从0开始）。</param>
        /// <returns>窗口实例。</returns>
        public FairyUIWindow GetStackWindow(int index)
        {
            if (index >= 0 && index < _windowStack.Count)
            {
                return _windowStack[index];
            }
            return null;
        }

        /// <summary>
        /// 检查窗口是否可见。
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        /// <returns>是否可见。</returns>
        public bool IsWindowVisible<T>() where T : FairyUIWindow
        {
            var window = GetUI<T>();
            return window != null && window.IsVisible;
        }

        /// <summary>
        /// 关闭窗口（通过类型，支持立即关闭）。
        /// </summary>
        /// <param name="type">窗口类型。</param>
        /// <param name="immediate">是否立即关闭。</param>
        public void CloseUI(Type type, bool immediate)
        {
            if (_windows.TryGetValue(type, out var window))
            {
                UnregisterWindowUpdate(window);
                if (immediate)
                {
                    window.Destroy();
                    _windows.Remove(type);
                    _windowStack.Remove(window);
                }
                else
                {
                    if (window.Close())
                    {
                        _windows.Remove(type);
                        _windowStack.Remove(window);
                    }
                }
            }
        }

        /// <summary>
        /// 按层级关闭所有窗口。
        /// </summary>
        /// <param name="layer">UI层级。</param>
        public void CloseAllPanels(UILayer layer)
        {
            for (int i = _windowStack.Count - 1; i >= 0; i--)
            {
                var window = _windowStack[i];
                if ((UILayer)window.WindowLayer == layer)
                {
                    UnregisterWindowUpdate(window);
                    window.Close();
                    window.Destroy();
                    _windows.Remove(window.GetType());
                    _windowStack.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 窗口置顶。
        /// </summary>
        /// <typeparam name="T">窗口类型。</typeparam>
        public void BringPanel2Front<T>() where T : FairyUIWindow
        {
            var window = GetUI<T>();
            if (window != null && window.IsVisible)
            {
                BringPanelToFront(window);
            }
        }

        /// <summary>
        /// 窗口置顶。
        /// </summary>
        /// <param name="window">窗口实例。</param>
        public void BringPanelToFront(FairyUIWindow window)
        {
            if (window == null || window._panelParent == null) return;

            // 将面板移到层级容器的最前面
            var layerRoot = GetLayerRoot((UILayer)window.WindowLayer);
            var parentIndex = layerRoot.numChildren - 1;
            layerRoot.SetChildIndex(window._panelParent, parentIndex);
            window._panelParent.displayObject.cachedTransform.SetAsLastSibling();
        }

        #endregion

        #region 临时隐藏面板

        private readonly List<FairyUIWindow> _tempHideList = new List<FairyUIWindow>();

        /// <summary>
        /// 临时隐藏除指定窗口外的所有窗口。
        /// </summary>
        /// <typeparam name="T">排除的窗口类型。</typeparam>
        public void TempHideAllPanelsExcept<T>() where T : FairyUIWindow
        {
            if (_tempHideList.Count > 0)
            {
                ShowAllTempHidePanels();
            }
            _tempHideList.Clear();

            for (int i = 0; i < _windowStack.Count; i++)
            {
                var window = _windowStack[i];
                if (window is T) continue;
                if (IsHighLayer(window)) continue;

                if (window.IsVisible && window._panelParent != null)
                {
                    window._panelParent.visible = false;
                    _tempHideList.Add(window);
                }
            }
        }

        /// <summary>
        /// 临时隐藏除指定两个窗口外的所有窗口。
        /// </summary>
        public void TempHideAllPanelsExcept<T1, T2>() where T1 : FairyUIWindow where T2 : FairyUIWindow
        {
            if (_tempHideList.Count > 0)
            {
                ShowAllTempHidePanels();
            }
            _tempHideList.Clear();

            for (int i = 0; i < _windowStack.Count; i++)
            {
                var window = _windowStack[i];
                if (window is T1 || window is T2) continue;
                if (IsHighLayer(window)) continue;

                if (window.IsVisible && window._panelParent != null)
                {
                    window._panelParent.visible = false;
                    _tempHideList.Add(window);
                }
            }
        }

        /// <summary>
        /// 临时隐藏除指定类型数组外的所有窗口。
        /// </summary>
        /// <param name="excludeTypes">排除的窗口类型数组。</param>
        public void TempHideAllPanelsExcept(params Type[] excludeTypes)
        {
            if (excludeTypes == null) return;

            if (_tempHideList.Count > 0)
            {
                ShowAllTempHidePanels();
            }
            _tempHideList.Clear();

            for (int i = 0; i < _windowStack.Count; i++)
            {
                var window = _windowStack[i];
                bool isExclude = false;
                for (int j = 0; j < excludeTypes.Length; j++)
                {
                    if (excludeTypes[j].IsInstanceOfType(window))
                    {
                        isExclude = true;
                        break;
                    }
                }

                if (isExclude) continue;
                if (IsHighLayer(window)) continue;

                if (window.IsVisible && window._panelParent != null)
                {
                    window._panelParent.visible = false;
                    _tempHideList.Add(window);
                }
            }
        }

        /// <summary>
        /// 临时隐藏所有窗口。
        /// </summary>
        public void TempHideAllPanelsExcept()
        {
            if (_tempHideList.Count > 0)
            {
                ShowAllTempHidePanels();
            }
            _tempHideList.Clear();

            for (int i = 0; i < _windowStack.Count; i++)
            {
                var window = _windowStack[i];
                if (IsHighLayer(window)) continue;

                if (window.IsVisible && window._panelParent != null)
                {
                    window._panelParent.visible = false;
                    _tempHideList.Add(window);
                }
            }
        }

        /// <summary>
        /// 恢复所有临时隐藏的窗口。
        /// </summary>
        public void ShowAllTempHidePanels()
        {
            for (int i = 0; i < _tempHideList.Count; i++)
            {
                var window = _tempHideList[i];
                if (window._panelParent != null && !window._panelParent.isDisposed)
                {
                    window._panelParent.visible = true;
                }
            }
            _tempHideList.Clear();
        }

        /// <summary>
        /// 检查是否为高层级窗口（不应被临时隐藏）。
        /// </summary>
        /// <param name="window">窗口实例。</param>
        /// <returns>是否为高层级。</returns>
        private bool IsHighLayer(FairyUIWindow window)
        {
            var layer = (UILayer)window.WindowLayer;
            return layer == UILayer.Effect || layer == UILayer.High ||
                   layer == UILayer.Top || layer == UILayer.TopEffect;
        }

        #endregion

        #region 面板池动态调整

        /// <summary>
        /// 设置面板池容量。
        /// </summary>
        /// <param name="capacity">容量。</param>
        public void SetPoolCapacity(int capacity)
        {
            if (_windowPool != null)
            {
                // TEngine 对象池不支持动态调整容量，这里记录配置
                Log.Debug($"FairyUIModule.SetPoolCapacity: {capacity}");
            }
        }

        /// <summary>
        /// 根据画质等级动态设置面板池大小。
        /// </summary>
        public void DynamicSetPanelPoolSize()
        {
            // 根据项目实际情况实现
            // 可以根据 QualitySettings.GetQualityLevel() 来判断
            int qualityLevel = QualitySettings.GetQualityLevel();
            int poolSize;

            switch (qualityLevel)
            {
                case 0: // Performant
                    poolSize = 2;
                    break;
                case 1: // Balanced
                    poolSize = 10;
                    break;
                default: // HighFidelity
                    poolSize = 15;
                    break;
            }

            SetPoolCapacity(poolSize);
        }

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
                var window = obj.Target as FairyUIWindow;
                if (window != null)
                {
                    // 从对象池恢复：重新创建内容
                    RestoreWindowContent(window);
                }
                return window;
            }
            return Activator.CreateInstance(type) as FairyUIWindow;
        }

        /// <summary>
        /// 恢复窗口内容（从对象池取出时调用）。
        /// </summary>
        private void RestoreWindowContent(FairyUIWindow window)
        {
            if (window._panelParent != null && window._contentPane == null)
            {
                // 重新创建内容
                var obj = FairyUIPackageLoader.CreateObject(window.PackageName, window.ComponentName);
                var contentPane = obj as FairyGUI.GComponent;

                if (contentPane != null)
                {
                    window._panelParent.AddChild(contentPane);
                    window._contentPane = contentPane;
                    window.GObject = contentPane;
                }
            }
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
                if (_window != null)
                {
                    // 销毁父节点（会同时销毁所有子节点）
                    if (_window._panelParent != null)
                    {
                        _window._panelParent.Dispose();
                        _window._panelParent = null;
                    }
                    _window._contentPane = null;
                    _window.GObject = null;
                }
                _window = null;
            }
        }

        #endregion
    }
}
