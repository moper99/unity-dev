using System;
using System.Collections.Generic;
using UnityEngine;

namespace Launcher
{
    /// <summary>
    /// Launcher FairyGUI 窗口基类。
    /// </summary>
    /// <remarks>
    /// 精简版 FairyUIBase，不依赖 TEngine，适用于热更新前的 Launcher 阶段。
    /// </remarks>
    public abstract class LauncherFairyUIBase
    {
        #region 类型定义

        /// <summary>
        /// UI 类型枚举。
        /// </summary>
        public enum UIType
        {
            None,
            Window,
            Widget,
        }

        #endregion

        #region 属性

        /// <summary>
        /// FairyGUI 显示对象。
        /// </summary>
        public virtual FairyGUI.GObject GObject { get; protected set; }

        /// <summary>
        /// 获取底层的 GComponent。
        /// </summary>
        protected FairyGUI.GComponent Panel => GObject as FairyGUI.GComponent;

        /// <summary>
        /// UI 类型。
        /// </summary>
        public virtual UIType Type => UIType.None;

        /// <summary>
        /// 资源是否准备完毕。
        /// </summary>
        public bool IsPrepare { get; protected set; }

        /// <summary>
        /// 用户数据。
        /// </summary>
        public object UserData
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
        /// 用户数据集。
        /// </summary>
        public object[] UserDatas => _userDatas;

        #endregion

        #region 字段

        /// <summary>
        /// 用户数据。
        /// </summary>
        public object[] _userDatas;

        /// <summary>
        /// 子组件列表。
        /// </summary>
        internal readonly List<LauncherFairyUIBase> ListChild = new List<LauncherFairyUIBase>();

        /// <summary>
        /// 父 UI 节点。
        /// </summary>
        protected LauncherFairyUIBase _parent;

        /// <summary>
        /// 是否持有 Update 行为。
        /// </summary>
        protected bool _hasOverrideUpdate = true;

        #endregion

        #region 生命周期

        /// <summary>
        /// 绑定 UI 成员元素。
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
        /// 窗口销毁。
        /// </summary>
        protected virtual void OnDestroy()
        {
        }

        /// <summary>
        /// 窗口显示。
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// 窗口隐藏。
        /// </summary>
        protected virtual void OnHide()
        {
        }

        #endregion

        #region 组件查找

        /// <summary>
        /// 查找子组件。
        /// </summary>
        /// <param name="name">子组件名称。</param>
        /// <returns>FairyGUI 对象。</returns>
        public FairyGUI.GObject GetChild(string name)
        {
            return Panel?.GetChild(name);
        }

        /// <summary>
        /// 查找子组件并转换类型。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="name">子组件名称。</param>
        /// <returns>FairyGUI 对象。</returns>
        public T GetChild<T>(string name) where T : FairyGUI.GObject
        {
            return GetChild(name) as T;
        }

        /// <summary>
        /// 查找子组件（兼容旧 API）。
        /// </summary>
        /// <param name="path">路径。</param>
        /// <returns>FairyGUI 对象。</returns>
        public FairyGUI.GObject FindChild(string path)
        {
            return GetChild(path);
        }

        /// <summary>
        /// 查找子组件并转换类型（兼容旧 API）。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="path">路径。</param>
        /// <returns>FairyGUI 对象。</returns>
        public T FindChild<T>(string path) where T : FairyGUI.GObject
        {
            return FindChild(path) as T;
        }

        #endregion

        #region 控制器

        /// <summary>
        /// 获取控制器。
        /// </summary>
        /// <param name="name">控制器名称。</param>
        /// <returns>控制器对象。</returns>
        protected FairyGUI.Controller GetController(string name)
        {
            return Panel?.GetController(name);
        }

        /// <summary>
        /// 获取控制器（通过路径）。
        /// </summary>
        /// <param name="path">组件路径。</param>
        /// <param name="name">控制器名称。</param>
        /// <returns>控制器对象。</returns>
        protected FairyGUI.Controller GetController(string path, string name)
        {
            var child = GetChild(path) as FairyGUI.GComponent;
            return child?.GetController(name);
        }

        #endregion

        #region 过渡动画

        /// <summary>
        /// 获取过渡动画。
        /// </summary>
        /// <param name="name">过渡动画名称。</param>
        /// <returns>过渡动画对象。</returns>
        protected FairyGUI.Transition GetTransition(string name)
        {
            return Panel?.GetTransition(name);
        }

        /// <summary>
        /// 获取过渡动画（通过路径）。
        /// </summary>
        /// <param name="path">组件路径。</param>
        /// <param name="name">过渡动画名称。</param>
        /// <returns>过渡动画对象。</returns>
        protected FairyGUI.Transition GetTransition(string path, string name)
        {
            var child = GetChild(path) as FairyGUI.GComponent;
            return child?.GetTransition(name);
        }

        #endregion

        #region 事件

        /// <summary>
        /// 添加点击事件。
        /// </summary>
        /// <param name="callback">回调。</param>
        protected void AddClickEvent(Action callback)
        {
            GObject?.onClick.Add(() => callback?.Invoke());
        }

        /// <summary>
        /// 添加点击事件（带发送者参数）。
        /// </summary>
        /// <param name="callback">回调。</param>
        protected void AddClickEvent(Action<FairyGUI.EventContext> callback)
        {
            GObject?.onClick.Add((context) => callback?.Invoke(context));
        }

        /// <summary>
        /// 清理事件。
        /// </summary>
        protected void RemoveAllEvents()
        {
            // FairyGUI 会自动清理，无需手动处理
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 初始化窗口（由 LauncherFairyUIMgr 调用）。
        /// </summary>
        /// <param name="gObject">FairyGUI 对象。</param>
        internal void Init(FairyGUI.GObject gObject)
        {
            GObject = gObject;
            BindMemberProperty();
            RegisterEvent();
            OnCreate();
            OnRefresh();
            IsPrepare = true;
        }

        /// <summary>
        /// 显示窗口。
        /// </summary>
        internal void Show()
        {
            if (GObject != null)
            {
                GObject.visible = true;
                OnShow();
            }
        }

        /// <summary>
        /// 隐藏窗口。
        /// </summary>
        internal void Hide()
        {
            if (GObject != null)
            {
                GObject.visible = false;
                OnHide();
            }
        }

        /// <summary>
        /// 销毁窗口。
        /// </summary>
        internal void Destroy()
        {
            // 递归销毁子组件
            for (int i = ListChild.Count - 1; i >= 0; i--)
            {
                var child = ListChild[i];
                child.Destroy();
            }
            ListChild.Clear();

            if (_parent != null)
            {
                _parent.ListChild.Remove(this);
            }

            RemoveAllEvents();
            OnDestroy();

            if (GObject != null)
            {
                GObject.Dispose();
                GObject = null;
            }
        }

        /// <summary>
        /// 内部更新。
        /// </summary>
        /// <returns>是否继续更新。</returns>
        internal bool InternalUpdate()
        {
            if (!IsPrepare || GObject == null || !GObject.visible)
            {
                return false;
            }

            // 更新子组件
            for (int i = 0; i < ListChild.Count; i++)
            {
                var child = ListChild[i];
                if (child != null)
                {
                    child.InternalUpdate();
                }
            }

            // 更新自身
            _hasOverrideUpdate = true;
            OnUpdate();
            return _hasOverrideUpdate;
        }

        #endregion
    }
}
