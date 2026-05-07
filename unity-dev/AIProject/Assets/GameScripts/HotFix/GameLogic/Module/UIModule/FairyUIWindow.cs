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
    /// UI层级枚举（对齐参考项目 PanelDepthType）。
    /// 层级从低到高：Scene → Main → Normal → Effect → High → Top → TopEffect
    /// </summary>
    public enum UILayer : int
    {
        /// <summary>
        /// 场景附属UI（最低层级）。
        /// </summary>
        Scene = 0,

        /// <summary>
        /// 场景主页面。
        /// </summary>
        Main = 1,

        /// <summary>
        /// 常规窗口（默认层级）。
        /// </summary>
        Normal = 2,

        /// <summary>
        /// 特效层。
        /// </summary>
        Effect = 3,

        /// <summary>
        /// 引导/遮罩层。
        /// </summary>
        High = 4,

        /// <summary>
        /// 加载/云层。
        /// </summary>
        Top = 5,

        /// <summary>
        /// 点击特效层（最高层级）。
        /// </summary>
        TopEffect = 6,
    }

    /// <summary>
    /// FairyGUI窗口基类（合并 FairyUIBase + FairyUIWindow）。
    /// </summary>
    public abstract class FairyUIWindow
    {

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
        /// 窗口层级（默认 Normal，子类可重写）。
        /// </summary>
        public virtual int WindowLayer => (int)UILayer.Normal;

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
        /// 背景点击是否关闭窗口（默认false，子类可重写为true则点击关闭）。
        /// </summary>
        public virtual bool BackgroundClickClose => false;

        /// <summary>
        /// 是否启用背景模糊（默认false，子类可重写为true则启用）。
        /// </summary>
        public virtual bool BgBlur => false;

        /// <summary>
        /// 独立父节点（每个窗口一个）。
        /// </summary>
        internal CompPanelParent? _panelParent;

        /// <summary>
        /// 窗口辅助工具（协程/定时器/延迟调用等）。
        /// </summary>
        internal UIPanelHelper? _helper;

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
            // 1. 创建父节点
            CreatePanelParent();

            // 2. 创建内容
            Log.Debug($"CreateWindow: {PackageName}/{ComponentName}");
            var obj = FairyUIPackageLoader.CreateObject(PackageName, ComponentName);
            Log.Debug($"CreateObject result: {obj?.GetType().FullName ?? "null"}");
            _contentPane = obj as FairyGUI.GComponent;

            if (_contentPane == null)
            {
                throw new Exception($"Failed to create component: {PackageName}/{ComponentName}, got {obj?.GetType().FullName ?? "null"}");
            }

            // 3. 将内容添加到父节点
            if (_panelParent != null)
            {
                _panelParent.AddChild(_contentPane);
            }

            GObject = _contentPane;

            if (FullScreen)
            {
                _contentPane.MakeFullScreen();
            }
        }

        /// <summary>
        /// 创建独立的父节点。
        /// </summary>
        private void CreatePanelParent()
        {
            // 确保Basic包已加载
            if (!FairyUIPackageLoader.IsPackageLoaded("Basics"))
            {
                FairyUIPackageLoader.AddPackage("Basics");
            }

            // 创建父节点
            _panelParent = CompPanelParent.CreateInstance();

            // 获取层级容器并添加到对应层级
            var layerRoot = FairyUIModule.Instance.GetLayerRoot((UILayer)WindowLayer);
            layerRoot.AddChild(_panelParent);
            _panelParent.MakeFullScreen();

            // 背景遮罩始终显示
            if (BackgroundClickClose)
            {
                // 注册背景点击事件
                _panelParent.BtnBackGround.onClick.Add(OnBackgroundClick);
            }

            // 适配安全区域
            AdaptSafeArea();

            // 初始化辅助工具
            _helper = new UIPanelHelper();
        }

        /// <summary>
        /// 适配安全区域（刘海屏等）。
        /// </summary>
        private void AdaptSafeArea()
        {
            if (_panelParent == null) return;

            var safeArea = Screen.safeArea;
            var screenH = Screen.height;

            float topOffset = (screenH - safeArea.y - safeArea.height) / screenH;
            float bottomOffset = safeArea.y / screenH;

            if (_panelParent.GraphSafeTop != null)
            {
                _panelParent.GraphSafeTop.height = topOffset * 100;
            }

            if (_panelParent.GraphSafeBottom != null)
            {
                _panelParent.GraphSafeBottom.height = bottomOffset * 100;
            }
        }

        /// <summary>
        /// 背景点击事件处理。
        /// </summary>
        private void OnBackgroundClick()
        {
            FairyUIModule.Instance.CloseUI(this.GetType());
        }

        /// <summary>
        /// 加载完成回调。
        /// </summary>
        private void OnLoadComplete()
        {
            if (!_isCreated)
            {
                _isCreated = true;
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
            if (_panelParent == null)
            {
                Log.Error($"Window not loaded: {WindowName}");
                return;
            }

            // 显示父节点（背景遮罩始终显示）
            _panelParent.visible = true;

            IsVisible = true;
            OnShow();
        }

        /// <summary>
        /// 隐藏窗口。
        /// </summary>
        public void Hide()
        {
            if (_panelParent == null)
            {
                return;
            }

            IsVisible = false;
            _helper?.Clear();
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
            if (_panelParent != null)
            {
                _loadCts?.Cancel();
                FairyUIModule.Instance.UnregisterWindowUpdate(this);
                UnregisterEvent();

                // 清理辅助工具
                _helper?.Clear();
                _helper = null;

                // 清理粒子特效
                CleanupParticleEffects();

                OnDestroy();

                if (UseObjectPool)
                {
                    FairyUIModule.Instance.RecycleToPool(this);
                }
                else
                {
                    // 销毁父节点（会同时销毁所有子节点）
                    _panelParent.Dispose();
                    _panelParent = null;
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

        /// <summary>
        /// 通过深路径查找子组件（如 "a.b.c"）。
        /// </summary>
        /// <param name="path">路径（用点号分隔）。</param>
        /// <returns>FairyGUI对象。</returns>
        public FairyGUI.GObject? GetChildByPath(string path)
        {
            if (string.IsNullOrEmpty(path) || Panel == null) return null;

            string[] parts = path.Split('.');
            FairyGUI.GObject current = Panel;

            for (int i = 0; i < parts.Length; i++)
            {
                if (current == null) return null;
                current = current.asCom.GetChild(parts[i]);
            }

            return current;
        }

        /// <summary>
        /// 通过深路径查找子组件并转换类型。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="path">路径（用点号分隔）。</param>
        /// <returns>FairyGUI对象。</returns>
        public T? GetChildByPath<T>(string path) where T : FairyGUI.GObject
        {
            return GetChildByPath(path) as T;
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

        #region 3D模型/粒子特效

        /// <summary>
        /// 粒子特效GoWrapper列表（跟随界面生命周期）。
        /// </summary>
        private readonly List<FairyGUI.GoWrapper> _particleWrappers = new List<FairyGUI.GoWrapper>();

        /// <summary>
        /// 添加粒子特效到UI（跟随界面生命周期）。
        /// 使用GoWrapper包裹ParticleSystem的GameObject。
        /// </summary>
        /// <param name="particlePrefab">粒子特效预制体。</param>
        /// <param name="localPosition">局部位置。</param>
        /// <param name="cloneMaterial">是否克隆材质（默认true，避免材质共享问题）。</param>
        /// <returns>GoWrapper实例。</returns>
        protected FairyGUI.GoWrapper? AddParticleEffect(GameObject particlePrefab, GGraph holder,Vector2 localPosition = default, bool cloneMaterial = true)
        {
            if (particlePrefab == null)
            {
                Log.Warning("AddParticleEffect: particlePrefab is null");
                return null;
            }

            if (Panel == null)
            {
                Log.Warning("AddParticleEffect: Panel is null");
                return null;
            }

            var go = UnityEngine.Object.Instantiate(particlePrefab);

            // 使用GoWrapper包裹粒子特效
            var wrapper = new FairyGUI.GoWrapper();
            wrapper.SetWrapTarget(go, cloneMaterial);
            holder.SetNativeObject(wrapper);

            if (localPosition != default)
            {
                wrapper.SetXY(localPosition.x, localPosition.y);
            }

            _particleWrappers.Add(wrapper);
            return wrapper;
        }

        /// <summary>
        /// 清理所有粒子特效。
        /// </summary>
        private void CleanupParticleEffects()
        {
            foreach (var wrapper in _particleWrappers)
            {
                if (wrapper != null)
                {
                    // GoWrapper.Dispose()会销毁wrapTarget
                    wrapper.Dispose();
                }
            }
            _particleWrappers.Clear();
        }

        /// <summary>
        /// 添加3D模型到UI（业务代码管理生命周期）。
        /// 使用GoWrapper包裹3D模型的GameObject。
        /// </summary>
        /// <param name="gameObject">3D模型GameObject。</param>
        /// <param name="width">显示宽度。</param>
        /// <param name="height">显示高度。</param>
        /// <param name="cloneMaterial">是否克隆材质（默认true）。</param>
        /// <returns>GoWrapper实例。</returns>
        protected FairyGUI.GoWrapper? Add3DModel(GameObject gameObject,GGraph holder, int width, int height, bool cloneMaterial = true)
        {
            if (gameObject == null)
            {
                Log.Warning("Add3DModel: gameObject is null");
                return null;
            }

            if (Panel == null)
            {
                Log.Warning("Add3DModel: Panel is null");
                return null;
            }

            // 使用GoWrapper包裹3D模型
            var wrapper = new FairyGUI.GoWrapper();
            wrapper.SetWrapTarget(gameObject, cloneMaterial);
            wrapper.SetSize(width, height);
            holder.SetNativeObject(wrapper);

            return wrapper;
        }

        /// <summary>
        /// 移除3D模型GoWrapper（不销毁GameObject，由业务代码管理）。
        /// </summary>
        /// <param name="wrapper">GoWrapper实例。</param>
        protected void RemoveGoWrapper(FairyGUI.GoWrapper? wrapper)
        {
            if (wrapper != null)
            {
                // 解除包裹关系，但不销毁GameObject
                wrapper.wrapTarget = null;
                wrapper.Dispose();
            }
        }

        #endregion

        #region 内部更新

        /// <summary>
        /// 内部更新（由 FairyUIModule 驱动）。
        /// </summary>
        internal bool InternalUpdate()
        {
            if (!IsLoadDone || _panelParent == null || !IsVisible)
            {
                return false;
            }

            _hasOverrideUpdate = true;
            OnUpdate();
            return _hasOverrideUpdate;
        }

        #endregion

        #region 排序层级

        /// <summary>
        /// 设置窗口排序层级（手动调整同层级内的显示顺序）。
        /// </summary>
        /// <param name="order">排序值（越大越靠前）。</param>
        public void SetSortingOrder(int order)
        {
            if (_panelParent != null)
            {
                _panelParent.sortingOrder = order;
            }
        }

        /// <summary>
        /// 获取窗口排序层级。
        /// </summary>
        /// <returns>排序值。</returns>
        public int GetSortingOrder()
        {
            return _panelParent?.sortingOrder ?? 0;
        }

        #endregion

        #region 返回键/应用生命周期

        /// <summary>
        /// Android返回键处理（子类可重写）。
        /// </summary>
        protected virtual void OnBackButton()
        {
            // 默认点击背景关闭
            if (BackgroundClickClose)
            {
                FairyUIModule.Instance.CloseUI(GetType());
            }
        }

        /// <summary>
        /// 应用进入后台回调（子类可重写）。
        /// </summary>
        /// <param name="pause">是否暂停。</param>
        protected virtual void OnApplicationPause(bool pause)
        {
        }

        /// <summary>
        /// 处理Android返回键（由外部调用）。
        /// </summary>
        public void HandleBackButton()
        {
            OnBackButton();
        }

        /// <summary>
        /// 处理应用前后台切换（由外部调用）。
        /// </summary>
        /// <param name="pause">是否暂停。</param>
        public void HandleApplicationPause(bool pause)
        {
            OnApplicationPause(pause);
        }

        #endregion
    }
}
