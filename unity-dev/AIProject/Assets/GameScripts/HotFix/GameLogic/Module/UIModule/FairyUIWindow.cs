using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// UI层级枚举。
    /// </summary>
    public enum UILayer : int
    {
        Bottom = 0,
        UI = 1,
        Top = 2,
        Tips = 3,
        System = 4,
    }

    /// <summary>
    /// FairyGUI窗口基类（合并 FairyUIBase + FairyUIWindow）。
    /// </summary>
    public abstract class FairyUIWindow
    {
        #region 依赖注入

        /// <summary>
        /// 依赖注入回调。
        /// </summary>
        public static Action<FairyUIWindow>? Injector;

        /// <summary>
        /// 依赖注入。
        /// </summary>
        protected void Inject()
        {
            Injector?.Invoke(this);
        }

        #endregion

        #region 用户数据

        /// <summary>
        /// 用户数据。
        /// </summary>
        protected object[]? _userDatas;

        /// <summary>
        /// 获取第一个用户数据。
        /// </summary>
        public object? UserData
        {
            get
            {
                if (_userDatas != null && _userDatas.Length >= 1)
                {
                    return _userDatas[0];
                }
                return null;
            }
        }

        /// <summary>
        /// 获取全部用户数据。
        /// </summary>
        public object[]? UserDatas => _userDatas;

        #endregion

        #region FairyGUI 对象

        /// <summary>
        /// FairyGUI显示对象。
        /// </summary>
        public virtual FairyGUI.GObject? GObject { get; internal set; }

        /// <summary>
        /// 获取底层的GComponent。
        /// </summary>
        protected FairyGUI.GComponent? Panel => GObject as FairyGUI.GComponent;

        #endregion

        #region 窗口属性

        /// <summary>
        /// 包名称（必须实现）。
        /// </summary>
        public abstract string PackageName { get; }

        /// <summary>
        /// 组件名称（必须实现）。
        /// </summary>
        public abstract string ComponentName { get; }

        /// <summary>
        /// 窗口层级。
        /// </summary>
        public virtual int WindowLayer => (int)UILayer.UI;

        /// <summary>
        /// 是否为全屏窗口。
        /// </summary>
        public virtual bool FullScreen => false;

        /// <summary>
        /// 窗口名称。
        /// </summary>
        public string WindowName { get; set; } = string.Empty;

        /// <summary>
        /// 是否可见。
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// 是否已加载完成。
        /// </summary>
        internal bool IsLoadDone = false;

        /// <summary>
        /// 是否持有Update行为。
        /// </summary>
        protected bool _hasOverrideUpdate = true;

        /// <summary>
        /// 是否支持对象池复用。
        /// </summary>
        public virtual bool UseObjectPool => false;

        /// <summary>
        /// 重置窗口状态（从对象池取出时调用）。
        /// </summary>
        internal void ResetState()
        {
            IsVisible = false;
            _userDatas = null;
        }

        #endregion

        #region FairyGUI 内部对象

        /// <summary>
        /// FairyGUI窗口对象。
        /// </summary>
        internal FairyGUI.Window? _window;

        /// <summary>
        /// 窗口内容面板。
        /// </summary>
        internal FairyGUI.GComponent? _contentPane;

        /// <summary>
        /// 是否已创建。
        /// </summary>
        private bool _isCreated = false;

        /// <summary>
        /// 异步加载取消令牌。
        /// </summary>
        private CancellationTokenSource? _loadCts;

        #endregion

        #region 加载

        /// <summary>
        /// 加载窗口资源。
        /// </summary>
        /// <param name="isAsync">是否异步加载。</param>
        /// <param name="userDatas">用户数据。</param>
        public void Load(bool isAsync, params object[] userDatas)
        {
            _userDatas = userDatas;

            if (isAsync)
            {
                _loadCts = new CancellationTokenSource();
                LoadAsync(_loadCts.Token).Forget();
            }
            else
            {
                LoadSync();
            }
        }

        /// <summary>
        /// 加载窗口资源（异步等待版本）。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <param name="userDatas">用户数据。</param>
        public async UniTask LoadAsync(CancellationToken ct, params object[] userDatas)
        {
            _userDatas = userDatas;
            _loadCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            await LoadAsync(_loadCts.Token);
        }

        /// <summary>
        /// 同步加载窗口。
        /// </summary>
        private void LoadSync()
        {
            try
            {
                if (!FairyUIPackageLoader.IsPackageLoaded(PackageName))
                {
                    FairyUIPackageLoader.AddPackage(PackageName);
                }

                CreateWindow();

                IsLoadDone = true;
                OnLoadComplete();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load window: {WindowName}, Error: {e.Message}\n{e.StackTrace}");
                FairyUIModule.Instance.OnWindowLoadFailed(this);
                throw;
            }
        }

        /// <summary>
        /// 异步加载窗口。
        /// </summary>
        private async UniTask LoadAsync(CancellationToken ct)
        {
            try
            {
                if (!FairyUIPackageLoader.IsPackageLoaded(PackageName))
                {
                    await FairyUIPackageLoader.AddPackageAsync(PackageName, ct);
                }

                ct.ThrowIfCancellationRequested();
                CreateWindow();

                IsLoadDone = true;
                OnLoadComplete();

                Show();
            }
            catch (OperationCanceledException)
            {
                Log.Debug($"Window load cancelled: {WindowName}");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load window: {WindowName}, Error: {e.Message}\n{e.StackTrace}");
                FairyUIModule.Instance.OnWindowLoadFailed(this);
            }
        }

        /// <summary>
        /// 创建窗口对象。
        /// </summary>
        private void CreateWindow()
        {
            _window = new FairyGUI.Window();
            Log.Debug($"CreateWindow: {PackageName}/{ComponentName}");
            var obj = FairyUIPackageLoader.CreateObject(PackageName, ComponentName);
            Log.Debug($"CreateObject result: {obj?.GetType().FullName ?? "null"}");
            _contentPane = obj as FairyGUI.GComponent;

            if (_contentPane == null)
            {
                throw new Exception($"Failed to create component: {PackageName}/{ComponentName}, got {obj?.GetType().FullName ?? "null"}");
            }

            _window.contentPane = _contentPane;
            GObject = _contentPane;

            if (FullScreen)
            {
                _contentPane.MakeFullScreen();
            }
        }

        /// <summary>
        /// 加载完成回调。
        /// </summary>
        private void OnLoadComplete()
        {
            if (!_isCreated)
            {
                _isCreated = true;
                Inject();
                BindMemberProperty();
                RegisterEvent();
                OnCreate();

                // 检测是否有 OnUpdate override，有则注册帧更新
                _hasOverrideUpdate = true;
                OnUpdate();
                if (_hasOverrideUpdate)
                {
                    FairyUIModule.Instance.RegisterWindowUpdate(this);
                }
            }

            OnRefresh();
        }

        #endregion

        #region 显示/隐藏/关闭

        /// <summary>
        /// 显示窗口。
        /// </summary>
        public void Show()
        {
            if (_window == null)
            {
                Log.Error($"Window not loaded: {WindowName}");
                return;
            }

            _window.Show();
            IsVisible = true;
            OnShow();
        }

        /// <summary>
        /// 隐藏窗口。
        /// </summary>
        public void Hide()
        {
            if (_window == null)
            {
                return;
            }

            _window.Hide();
            IsVisible = false;
            OnHide();
        }

        /// <summary>
        /// 关闭窗口（由 FairyUIModule 调用）。
        /// </summary>
        internal bool Close()
        {
            if (!OnBeforeClose())
            {
                return false;
            }
            Destroy();
            return true;
        }

        /// <summary>
        /// 销毁窗口。
        /// </summary>
        internal void Destroy()
        {
            if (_window != null)
            {
                _loadCts?.Cancel();
                FairyUIModule.Instance.UnregisterWindowUpdate(this);
                UnregisterEvent();
                OnDestroy();

                if (UseObjectPool)
                {
                    FairyUIModule.Instance.RecycleToPool(this);
                }
                else
                {
                    _window.Dispose();
                    _window = null;
                    _contentPane = null;
                    GObject = null;
                }
            }
        }

        #endregion

        #region 生命周期虚方法

        /// <summary>
        /// 绑定UI成员元素。
        /// </summary>
        protected virtual void BindMemberProperty()
        {
        }

        /// <summary>
        /// 注册事件。
        /// </summary>
        protected virtual void RegisterEvent()
        {
        }

        /// <summary>
        /// 注销事件（与 RegisterEvent 对称）。
        /// </summary>
        protected virtual void UnregisterEvent()
        {
        }

        /// <summary>
        /// 窗口创建。
        /// </summary>
        protected virtual void OnCreate()
        {
        }

        /// <summary>
        /// 窗口刷新。
        /// </summary>
        protected virtual void OnRefresh()
        {
        }

        /// <summary>
        /// 窗口更新。
        /// </summary>
        protected virtual void OnUpdate()
        {
            _hasOverrideUpdate = false;
        }

        /// <summary>
        /// 窗口显示。
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// 显示动画播放完毕后调用。
        /// </summary>
        protected virtual void OnShowComplete()
        {
        }

        /// <summary>
        /// 窗口隐藏。
        /// </summary>
        protected virtual void OnHide()
        {
        }

        /// <summary>
        /// 隐藏动画播放完毕后调用。
        /// </summary>
        protected virtual void OnHideComplete()
        {
        }

        /// <summary>
        /// 关闭前回调。返回 false 可阻止关闭。
        /// </summary>
        protected virtual bool OnBeforeClose()
        {
            return true;
        }

        /// <summary>
        /// 窗口销毁。
        /// </summary>
        protected virtual void OnDestroy()
        {
        }

        /// <summary>
        /// 当触发窗口的层级排序。
        /// </summary>
        protected virtual void OnSortDepth()
        {
        }

        /// <summary>
        /// 当因为全屏遮挡或者窗口可见性触发窗口的显隐。
        /// </summary>
        protected virtual void OnSetVisible(bool visible)
        {
        }

        #endregion

        #region 动画辅助

        /// <summary>
        /// 播放显示动画，动画完毕后自动回调 OnShowComplete。
        /// </summary>
        /// <param name="trans">FairyGUI过渡动画。</param>
        /// <param name="onComplete">额外完成回调。</param>
        protected void PlayShowTransition(FairyGUI.Transition? trans, Action? onComplete = null)
        {
            if (trans == null)
            {
                onComplete?.Invoke();
                OnShowComplete();
                return;
            }
            trans.Play(() =>
            {
                onComplete?.Invoke();
                OnShowComplete();
            });
        }

        /// <summary>
        /// 播放隐藏动画，动画完毕后自动回调 OnHideComplete。
        /// </summary>
        /// <param name="trans">FairyGUI过渡动画。</param>
        /// <param name="onComplete">额外完成回调。</param>
        protected void PlayHideTransition(FairyGUI.Transition? trans, Action? onComplete = null)
        {
            if (trans == null)
            {
                onComplete?.Invoke();
                OnHideComplete();
                return;
            }
            trans.Play(() =>
            {
                onComplete?.Invoke();
                OnHideComplete();
            });
        }

        #endregion

        #region 查找子组件

        /// <summary>
        /// 查找子组件。
        /// </summary>
        /// <param name="name">子组件名称。</param>
        /// <returns>FairyGUI对象。</returns>
        public FairyGUI.GObject? GetChild(string name)
        {
            return Panel?.GetChild(name);
        }

        /// <summary>
        /// 查找子组件并转换类型。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="name">子组件名称。</param>
        /// <returns>FairyGUI对象。</returns>
        public T? GetChild<T>(string name) where T : FairyGUI.GObject
        {
            return GetChild(name) as T;
        }

        /// <summary>
        /// 查找子组件（兼容旧API）。
        /// </summary>
        /// <param name="path">路径。</param>
        /// <returns>FairyGUI对象。</returns>
        public FairyGUI.GObject? FindChild(string path)
        {
            return GetChild(path);
        }

        /// <summary>
        /// 查找子组件并转换类型（兼容旧API）。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="path">路径。</param>
        /// <returns>FairyGUI对象。</returns>
        public T? FindChild<T>(string path) where T : FairyGUI.GObject
        {
            return FindChild(path) as T;
        }

        #endregion

        #region 获取控制器

        /// <summary>
        /// 获取控制器。
        /// </summary>
        /// <param name="name">控制器名称。</param>
        /// <returns>控制器对象。</returns>
        protected FairyGUI.Controller? GetController(string name)
        {
            return Panel?.GetController(name);
        }

        /// <summary>
        /// 获取控制器（通过路径）。
        /// </summary>
        /// <param name="path">组件路径。</param>
        /// <param name="name">控制器名称。</param>
        /// <returns>控制器对象。</returns>
        protected FairyGUI.Controller? GetController(string path, string name)
        {
            var child = GetChild(path) as FairyGUI.GComponent;
            return child?.GetController(name);
        }

        #endregion

        #region 获取过渡动画

        /// <summary>
        /// 获取过渡动画。
        /// </summary>
        /// <param name="name">过渡动画名称。</param>
        /// <returns>过渡动画对象。</returns>
        protected FairyGUI.Transition? GetTransition(string name)
        {
            return Panel?.GetTransition(name);
        }

        /// <summary>
        /// 获取过渡动画（通过路径）。
        /// </summary>
        /// <param name="path">组件路径。</param>
        /// <param name="name">过渡动画名称。</param>
        /// <returns>过渡动画对象。</returns>
        protected FairyGUI.Transition? GetTransition(string path, string name)
        {
            var child = GetChild(path) as FairyGUI.GComponent;
            return child?.GetTransition(name);
        }

        #endregion

        #region 内部更新

        /// <summary>
        /// 内部更新（由 FairyUIModule 驱动）。
        /// </summary>
        internal bool InternalUpdate()
        {
            if (!IsLoadDone || _window == null || !_window.isShowing)
            {
                return false;
            }

            _hasOverrideUpdate = true;
            OnUpdate();
            return _hasOverrideUpdate;
        }

        #endregion
    }
}
