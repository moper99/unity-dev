using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FairyGUI;
using TEngine;
using UnityEngine;
using YooAsset;

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
        /// 是否需要每帧更新。
        /// </summary>
        public virtual bool NeedUpdate => false;
        

        /// <summary>
        /// 是否支持对象池复用。
        /// </summary>
        public virtual bool UseObjectPool => false;

        /// <summary>
        /// 是否启用背景模糊（默认false，子类可重写为true则启用）。
        /// </summary>
        public virtual bool BgBlur => false;

        /// <summary>
        /// 是否为弹窗（自动根据组件名判断，以PopPanel结尾）。
        /// 弹窗会自动启用点击背景关闭功能。
        /// </summary>
        public bool IsPopPanel { get; private set; }

        /// <summary>
        /// 是否居中对齐（默认true，子类可重写）。
        /// </summary>
        public virtual bool Center => true;

        /// <summary>
        /// 是否正在播放动画。
        /// </summary>
        public bool IsPlayingTransition { get; protected set; }

        /// <summary>
        /// 是否播放页面动画（默认true，子类可重写为false则跳过动画）。
        /// </summary>
        protected bool _playAnimation = true;

        /// <summary>
        /// 点击场景时的回调。
        /// </summary>
        public Action ClickSceneCallback;

        /// <summary>
        /// 点击比自身层级低的 UI 时的回调。
        /// </summary>
        public Action ClickLowerUICallback;

        /// <summary>
        /// 独立父节点（每个窗口一个）。
        /// </summary>
        internal CompPanelParent? _panelParent;

        /// <summary>
        /// 窗口辅助工具（协程/定时器/延迟调用等）。
        /// </summary>
        internal UIPanelHelper? _helper;

        /// <summary>
        /// 获取窗口辅助工具（供子类使用）。
        /// </summary>
        protected UIPanelHelper Helper => _helper ??= new UIPanelHelper();

        /// <summary>
        /// 重置窗口状态（从对象池取出时调用）。
        /// </summary>
        internal void ResetState()
        {
            IsVisible = false;
            _userDatas = null;
            IsPlayingTransition = false;
            TransitionCallback = null;
            _transition = null;
            _transitionBg = null;
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

        /// <summary>
        /// 显示动画。
        /// </summary>
        private Transition? _transShow;

        /// <summary>
        /// 隐藏动画。
        /// </summary>
        private Transition? _transHide;

        /// <summary>
        /// 背景显示动画。
        /// </summary>
        private Transition? _transBgShow;

        /// <summary>
        /// 背景隐藏动画。
        /// </summary>
        private Transition? _transBgHide;

        /// <summary>
        /// 当前正在播放的动画。
        /// </summary>
        private Transition? _transition;

        /// <summary>
        /// 当前正在播放的背景动画。
        /// </summary>
        private Transition? _transitionBg;

        /// <summary>
        /// 动画播放完成回调。
        /// </summary>
        protected Action? TransitionCallback;

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
            finally
            {
                FairyUIModule.Instance.MarkWindowLoaded(this.GetType());
            }
        }

        /// <summary>
        /// 创建窗口对象。
        /// </summary>
        private void CreateWindow()
        {
            // 1. 检测是否为弹窗（必须在CreatePanelParent之前）
            IsPopPanel = ComponentName.EndsWith("PopPanel", StringComparison.Ordinal);

            // 2. 创建内容面板（必须在CreatePanelParent之前，以便Center逻辑）
            if (_contentPane == null)
            {
                var obj = FairyUIPackageLoader.CreateObject(PackageName, ComponentName);
                _contentPane = obj as FairyGUI.GComponent;

                if (_contentPane == null)
                {
                    throw new Exception($"Failed to create component: {PackageName}/{ComponentName}, got {obj?.GetType().FullName ?? "null"}");
                }
            }

            // 3. 创建父节点（此时IsPopPanel和_contentPane都已就绪）
            if (_panelParent == null)
            {
                CreatePanelParent();
            }

            // 4. 确保内容面板被添加到父节点
            if (_panelParent != null && _contentPane != null)
            {
                bool isChild = false;
                for (int i = 0; i < _panelParent.numChildren; i++)
                {
                    if (_panelParent.GetChildAt(i) == _contentPane)
                    {
                        isChild = true;
                        break;
                    }
                }
                if (!isChild)
                {
                    _panelParent.AddChild(_contentPane);
                }
            }

            GObject = _contentPane;

            if (FullScreen && _contentPane != null)
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

            // 弹窗自动启用点击背景关闭
            if (IsPopPanel)
            {
                _panelParent.BtnBackGround.visible = true;
                _panelParent.BtnBackGround.onClick.Add(OnBackgroundClick);
            }
            else
            {
                _panelParent.BtnBackGround.visible = false;
            }

            // 居中对齐（此时_contentPane已存在）
            if (Center && _contentPane != null)
            {
                _contentPane.SetPivot(0.5f, 0.5f);
                _contentPane.Center(IsPopPanel);
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

                // 初始化动画引用
                if (_contentPane != null)
                {
                    _transShow = _contentPane.GetTransition("transShow");
                    _transHide = _contentPane.GetTransition("transHide");
                }
                if (_panelParent != null)
                {
                    _transBgShow = _panelParent.GetTransition("transShow");
                    _transBgHide = _panelParent.GetTransition("transHide");
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

            IsVisible = true;

            if (NeedUpdate)
            {
                FairyUIModule.Instance.RegisterWindowUpdate(this);
            }

            // 注册触摸捕获
            if (ClickSceneCallback != null || ClickLowerUICallback != null)
            {
                Stage.inst.onTouchBegin.AddCapture(HandleTouchBegin);
                Stage.inst.onTouchEnd.AddCapture(HandleTouchEnd);
            }

            OnShowBegin();
            OnShow();
            PlayShowAnimation();
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

            OnHideBegin();

            IsVisible = false;

            if (NeedUpdate)
            {
                FairyUIModule.Instance.UnregisterWindowUpdate(this);
            }

            // 移除触摸捕获
            if (ClickSceneCallback != null || ClickLowerUICallback != null)
            {
                Stage.inst.onTouchBegin.RemoveCapture(HandleTouchBegin);
                Stage.inst.onTouchEnd.RemoveCapture(HandleTouchEnd);
            }

            _helper?.Clear();
            OnHide();
            PlayHideAnimation();
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
        }

        /// <summary>
        /// 显示开始（在OnShow之前调用）。
        /// </summary>
        protected virtual void OnShowBegin()
        {
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
        /// 显示结束（在动画完成后调用）。
        /// </summary>
        protected virtual void OnShowEnd()
        {
        }

        /// <summary>
        /// 隐藏开始（在OnHide之前调用）。
        /// </summary>
        protected virtual void OnHideBegin()
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
        /// 隐藏结束（在隐藏完成后调用）。
        /// </summary>
        protected virtual void OnHideEnd()
        {
        }

        /// <summary>
        /// 响应屏幕尺寸变化（由 FairyUIModule 驱动）。
        /// </summary>
        public virtual void OnScreenChanged()
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
        /// 播放动画（内部通用方法）。
        /// </summary>
        /// <param name="isShow">true=显示动画，false=隐藏动画。</param>
        private void PlayAnimation(bool isShow)
        {
            StopTransition();
            if (_playAnimation)
            {
                IsPlayingTransition = true;
                TransitionCallback = () =>
                {
                    IsPlayingTransition = false;
                    if (isShow)
                    {
                        OnShowComplete();
                        OnShowEnd();
                    }
                    else
                    {
                        OnHideComplete();
                        OnHideEnd();
                    }
                };
                if (isShow)
                    OnShowTransition();
                else
                    OnHideTransition();
            }
            else
            {
                if (isShow)
                {
                    OnShowComplete();
                    OnShowEnd();
                }
                else
                {
                    OnHideComplete();
                    OnHideEnd();
                }
            }
        }

        /// <summary>
        /// 播放显示动画（内部调用）。
        /// </summary>
        private void PlayShowAnimation() => PlayAnimation(true);

        /// <summary>
        /// 播放隐藏动画（内部调用）。
        /// </summary>
        private void PlayHideAnimation() => PlayAnimation(false);

        /// <summary>
        /// 播放显示动画（子类可重写）。
        /// </summary>
        protected virtual void OnShowTransition()
        {
            if (_transShow != null)
            {
                if (_transBgShow != null)
                {
                    _transitionBg = _transBgShow;
                    _transBgShow.Play();
                }
                _transition = _transShow;
                _transShow.Play(() =>
                {
                    TransitionCallback?.Invoke();
                });
            }
            else
            {
                TransitionCallback?.Invoke();
            }
        }

        /// <summary>
        /// 播放隐藏动画（子类可重写）。
        /// </summary>
        protected virtual void OnHideTransition()
        {
            if (_transHide != null)
            {
                if (_transBgHide != null)
                {
                    _transitionBg = _transBgHide;
                    _transBgHide.Play();
                }
                _transition = _transHide;
                _transHide.Play(() =>
                {
                    TransitionCallback?.Invoke();
                });
            }
            else
            {
                TransitionCallback?.Invoke();
            }
        }

        /// <summary>
        /// 停止当前正在播放的动画。
        /// </summary>
        public void StopTransition()
        {
            if (IsPlayingTransition)
            {
                if (_transition != null)
                {
                    _transition.Stop();
                }
                if (_transitionBg != null)
                {
                    _transitionBg.Stop();
                }
                TransitionCallback = null;
                IsPlayingTransition = false;
            }
        }

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
        /// 粒子特效资源句柄字典（用于资源管理）。
        /// </summary>
        private readonly Dictionary<GGraph, AssetHandle> _particleHandles = new Dictionary<GGraph, AssetHandle>();

        /// <summary>
        /// 粒子特效GoWrapper列表（跟随界面生命周期）。
        /// </summary>
        private readonly List<FairyGUI.GoWrapper> _particleWrappers = new List<FairyGUI.GoWrapper>();

        /// <summary>
        /// 添加粒子特效到UI（跟随界面生命周期）。
        /// 使用GoWrapper包裹ParticleSystem的GameObject。
        /// </summary>
        /// <param name="particlePrefab">粒子特效预制体。</param>
        /// <param name="holder">GGraph占位组件。</param>
        /// <param name="localPosition">局部位置。</param>
        /// <param name="cloneMaterial">是否克隆材质（默认true，避免材质共享问题）。</param>
        /// <returns>GoWrapper实例。</returns>
        protected FairyGUI.GoWrapper? AddParticleEffect(GameObject particlePrefab, GGraph holder, Vector2 localPosition = default, bool cloneMaterial = true)
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
        /// 异步加载粒子特效到UI（支持资源管理和重播）。
        /// </summary>
        /// <param name="path">YooAsset资源路径。</param>
        /// <param name="holder">GGraph占位组件。</param>
        /// <param name="timeEndCallBack">粒子播放完成回调。</param>
        /// <param name="loadCallBack">资源加载完成回调。</param>
        /// <returns>资源操作句柄。</returns>
        protected AssetHandle LoadParticleEffect(string path, GGraph holder, Action timeEndCallBack = null, Action loadCallBack = null)
        {
            // 如果该holder已有粒子，先清理
            if (_particleHandles.TryGetValue(holder, out var oldHandle))
            {
                oldHandle?.Dispose();
                var wrapperOld = holder.displayObject as GoWrapper;
                wrapperOld?.Dispose();
                _particleHandles.Remove(holder);
            }

            var handle = UIParticleUtil.LoadParticle(path, holder, timeEndCallBack, loadCallBack);
            _particleHandles[holder] = handle;
            return handle;
        }

        /// <summary>
        /// 重播已有粒子特效。
        /// </summary>
        /// <param name="holder">GGraph占位组件。</param>
        /// <param name="timeEndCallBack">粒子播放完成回调。</param>
        protected void ReplayParticleEffect(GGraph holder, Action timeEndCallBack = null)
        {
            UIParticleUtil.ReplayParticle(holder, timeEndCallBack);
        }

        /// <summary>
        /// 清理所有粒子特效。
        /// </summary>
        private void CleanupParticleEffects()
        {
            // 释放资源句柄
            foreach (var kvp in _particleHandles)
            {
                kvp.Value?.Dispose();
            }
            _particleHandles.Clear();

            // 销毁GoWrapper
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
        /// <param name="holder">GGraph占位组件。</param>
        /// <param name="width">显示宽度。</param>
        /// <param name="height">显示高度。</param>
        /// <param name="cloneMaterial">是否克隆材质（默认true）。</param>
        /// <returns>GoWrapper实例。</returns>
        protected FairyGUI.GoWrapper? Add3DModel(GameObject gameObject, GGraph holder, int width, int height, bool cloneMaterial = true)
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

            OnUpdate();
            return true;
        }

        /// <summary>
        /// 处理触摸开始事件。
        /// </summary>
        private void HandleTouchBegin(EventContext context)
        {
            if (ClickSceneCallback != null)
            {
                if (Stage.inst.touchTarget == null)
                {
                    ClickSceneCallback();
                }
            }
        }

        /// <summary>
        /// 处理触摸结束事件。
        /// </summary>
        private void HandleTouchEnd(EventContext context)
        {
            if (ClickLowerUICallback != null)
            {
                if (Stage.inst.touchTarget == null) return;

                var target = Stage.inst.touchTarget.gOwner;
                var otherWindow = FairyUIModule.Instance.GetPanelByChild(target);
                if (otherWindow != null && otherWindow.IsVisible && otherWindow != this)
                {
                    var selfLayer = (int)this.WindowLayer;
                    var otherLayer = (int)otherWindow.WindowLayer;
                    
                    if (selfLayer > otherLayer)
                    {
                        ClickLowerUICallback();
                        return;
                    }

                    if (selfLayer == otherLayer)
                    {
                        // 同层级，比较在栈中的索引（索引越大，显示在越前面）
                        int selfIdx = FairyUIModule.Instance.GetWindowIndex(this);
                        int otherIdx = FairyUIModule.Instance.GetWindowIndex(otherWindow);
                        if (selfIdx > otherIdx)
                        {
                            ClickLowerUICallback();
                        }
                    }
                }
            }
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
            // 弹窗默认点击背景关闭
            if (IsPopPanel)
            {
                CloseSelf();
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

        #region 便捷方法

        /// <summary>
        /// 关闭自身窗口。
        /// </summary>
        protected void CloseSelf()
        {
            FairyUIModule.Instance.CloseUI(GetType());
        }

        /// <summary>
        /// 关闭自身窗口（支持立即关闭）。
        /// </summary>
        /// <param name="hideImmediately">是否立即关闭（跳过动画）。</param>
        protected void CloseSelf(bool hideImmediately)
        {
            FairyUIModule.Instance.CloseUI(GetType(), hideImmediately);
        }

        /// <summary>
        /// 将窗口置顶到最前面。
        /// </summary>
        public void BringToFront()
        {
            FairyUIModule.Instance.BringPanelToFront(this);
        }

        /// <summary>
        /// 检查指定对象是否属于当前窗口。
        /// </summary>
        /// <param name="obj">要检查的GObject。</param>
        /// <returns>是否属于当前窗口。</returns>
        public bool IsChildOfView(GObject obj)
        {
            if (obj == null || _panelParent == null) return false;
            return obj.displayObject.cachedTransform != null &&
                   obj.displayObject.cachedTransform.IsChildOf(_panelParent.displayObject.cachedTransform);
        }

        #endregion
    }
}
