using System;
using FairyGUI;
using LauncherUI;
using UnityEngine;

namespace Launcher
{
    /// <summary>
    /// 提示框界面业务逻辑。
    /// </summary>
    /// <remarks>
    /// 使用 FairyGUI 自动生成的 LoadTipsUI 组件类。
    /// </remarks>
    public class LoadTipsUIWindow : LauncherFairyUIBase
    {
        #region FairyGUI 组件

        private LauncherUI.LoadTipsUI _panel;

        #endregion

        #region 常量

        /// <summary>
        /// FairyGUI 包名称。
        /// </summary>
        public const string PackageNameConst = "LauncherUI";

        /// <summary>
        /// FairyGUI 组件名称。
        /// </summary>
        public const string ComponentNameConst = "LoadTipsUI";

        #endregion

        #region 回调

        /// <summary>
        /// 确认按钮点击回调。
        /// </summary>
        public Action OnConfirmClick { get; set; }

        /// <summary>
        /// 更新按钮点击回调。
        /// </summary>
        public Action OnUpdateClick { get; set; }

        /// <summary>
        /// 取消按钮点击回调。
        /// </summary>
        public Action OnCancelClick { get; set; }

        #endregion

        #region 生命周期

        protected override void RegisterEvent()
        {
            base.RegisterEvent();

            _panel = GObject as LauncherUI.LoadTipsUI;
            if (_panel != null)
            {
                _panel.btnConfirm?.onClick.Add(OnClickConfirmBtn);
                _panel.btnUpdate?.onClick.Add(OnClickUpdateBtn);
                _panel.btnCancel?.onClick.Add(OnClickCancelBtn);
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            if (_panel != null)
            {
                // 默认隐藏所有按钮
                _panel.btnConfirm.visible = false;
                _panel.btnUpdate.visible = false;
                _panel.btnCancel.visible = false;
            }
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();

            if (_panel != null && UserData != null)
            {
                _panel.txtDesc.text = UserData.ToString();
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置所有按钮回调。
        /// </summary>
        /// <param name="onConfirm">确认回调。</param>
        /// <param name="onUpdate">更新回调。</param>
        /// <param name="onCancel">取消回调。</param>
        public void SetAllCallback(Action onConfirm, Action onUpdate, Action onCancel)
        {
            if (_panel == null) return;

            // 默认隐藏所有按钮
            _panel.btnConfirm.visible = false;
            _panel.btnUpdate.visible = false;
            _panel.btnCancel.visible = false;

            if (onConfirm != null)
            {
                OnConfirmClick = onConfirm;
                _panel.btnConfirm.visible = true;
            }

            if (onUpdate != null)
            {
                OnUpdateClick = onUpdate;
                _panel.btnUpdate.visible = true;
            }

            if (onCancel != null)
            {
                OnCancelClick = onCancel;
                _panel.btnCancel.visible = true;
            }
        }

        #endregion

        #region 事件处理

        private void OnClickUpdateBtn()
        {
            OnUpdateClick?.Invoke();
            CloseUI();
        }

        private void OnClickCancelBtn()
        {
            OnCancelClick?.Invoke();
            CloseUI();
        }

        private void OnClickConfirmBtn()
        {
            OnConfirmClick?.Invoke();
            CloseUI();
        }

        /// <summary>
        /// 关闭 UI。
        /// </summary>
        private void CloseUI()
        {
            LauncherFairyUIMgr.Instance.CloseUI<LoadTipsUIWindow>();
        }

        #endregion
    }
}
