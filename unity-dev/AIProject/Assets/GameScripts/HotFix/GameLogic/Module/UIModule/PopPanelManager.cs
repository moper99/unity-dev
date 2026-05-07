using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 弹窗队列管理器。
    /// 解决多个弹窗同时触发时的排队和优先级问题。
    /// </summary>
    public class PopPanelManager : Singleton<PopPanelManager>
    {
        #region 内部数据结构

        /// <summary>
        /// 等待中的弹窗信息。
        /// </summary>
        class WaitingInfo
        {
            public string PanelName;
            public PanelData PanelData;
            public Func<object, bool> PreconditionFunc;
            public object PreFuncParam;
        }

        /// <summary>
        /// 面板数据。
        /// </summary>
        public class PanelData
        {
            public object[] UserDatas;
        }

        #endregion

        #region 私有字段

        private List<WaitingInfo> _waitingList;
        private List<string> _mutuallyExclusivePanelList;
        private List<string> _guideWhiteList;
        private CancellationTokenSource _cts;
        private bool _isChecking = false;

        /// <summary>
        /// 面板优先级字典（数值越小优先级越高）。
        /// </summary>
        private readonly Dictionary<string, int> _panelSortOrderDict = new Dictionary<string, int>();

        #endregion

        #region 初始化/释放

        public override void Active()
        {
            _waitingList = new List<WaitingInfo>();
            _mutuallyExclusivePanelList = new List<string>();
            _guideWhiteList = new List<string>();
            _cts = new CancellationTokenSource();

            // 初始化互斥弹窗列表（可由外部配置）
            InitMutuallyExclusivePanels();

            // 初始化引导白名单（可由外部配置）
            InitGuideWhiteList();

            // 初始化优先级（可由外部配置）
            InitPanelSortOrder();
        }

        protected override void OnRelease()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _waitingList?.Clear();
            _mutuallyExclusivePanelList?.Clear();
            _guideWhiteList?.Clear();
            _panelSortOrderDict?.Clear();
            _isChecking = false;
        }

        /// <summary>
        /// 初始化互斥弹窗列表（子类可重写或外部配置）。
        /// </summary>
        protected virtual void InitMutuallyExclusivePanels()
        {
            // 默认为空，由外部添加
        }

        /// <summary>
        /// 初始化引导白名单（子类可重写或外部配置）。
        /// </summary>
        protected virtual void InitGuideWhiteList()
        {
            // 默认为空，由外部添加
        }

        /// <summary>
        /// 初始化面板优先级（子类可重写或外部配置）。
        /// </summary>
        protected virtual void InitPanelSortOrder()
        {
            // 默认为空，由外部添加
        }

        #endregion

        #region 公开API

        /// <summary>
        /// 添加弹窗到等待队列。
        /// </summary>
        /// <param name="panelName">面板名称。</param>
        /// <param name="param">面板参数。</param>
        /// <param name="preconditionFunc">前置条件函数。</param>
        /// <param name="preFuncParam">前置条件函数参数。</param>
        public void Add(string panelName, PanelData param = null, Func<object, bool> preconditionFunc = null, object preFuncParam = null)
        {
            if (string.IsNullOrEmpty(panelName))
            {
                Log.Warning("PopPanelManager.Add: panelName is null or empty.");
                return;
            }

            // 检查是否在引导白名单中
            if (!CanPopInNewbieGuiding(panelName))
            {
                Log.Debug($"PopPanelManager.Add: {panelName} blocked by guide whitelist.");
                return;
            }

            // 检查是否已在等待队列中
            for (int i = 0; i < _waitingList.Count; i++)
            {
                if (_waitingList[i].PanelName == panelName)
                {
                    Log.Debug($"PopPanelManager.Add: {panelName} already in waiting list.");
                    return;
                }
            }

            var info = new WaitingInfo
            {
                PanelName = panelName,
                PanelData = param,
                PreconditionFunc = preconditionFunc,
                PreFuncParam = preFuncParam
            };
            _waitingList.Add(info);

            // 启动检查协程
            if (!_isChecking)
            {
                _isChecking = true;
                StartCheckCoroutine();
            }

            Log.Debug($"PopPanelManager.Add: {panelName} added to waiting list.");
        }

        /// <summary>
        /// 从等待队列移除弹窗。
        /// </summary>
        /// <param name="panelName">面板名称。</param>
        public void Remove(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return;

            for (int i = _waitingList.Count - 1; i >= 0; i--)
            {
                if (_waitingList[i].PanelName == panelName)
                {
                    _waitingList.RemoveAt(i);
                    Log.Debug($"PopPanelManager.Remove: {panelName} removed from waiting list.");
                }
            }
        }

        /// <summary>
        /// 检查引导过程中是否有弹窗打开。
        /// </summary>
        /// <returns>是否有引导弹窗打开。</returns>
        public bool HasGuidePopPanelOpen()
        {
            for (int i = 0; i < _guideWhiteList.Count; i++)
            {
                if (FairyUIModule.Instance.HasWindow(_guideWhiteList[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查是否有等待中的面板。
        /// </summary>
        /// <returns>是否有等待中的面板。</returns>
        public bool HaveWaitingPanel()
        {
            return _waitingList.Count > 0;
        }

        /// <summary>
        /// 检查指定面板是否在等待队列中。
        /// </summary>
        /// <param name="panelName">面板名称。</param>
        /// <returns>是否在等待队列中。</returns>
        public bool HasWaitingPanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return false;

            for (int i = 0; i < _waitingList.Count; i++)
            {
                if (_waitingList[i].PanelName == panelName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查指定面板数组中是否有在等待队列中的。
        /// </summary>
        /// <param name="panelNames">面板名称数组。</param>
        /// <returns>是否有在等待队列中的。</returns>
        public bool HasWaitingPanel(string[] panelNames)
        {
            if (panelNames == null) return false;

            for (int i = 0; i < panelNames.Length; i++)
            {
                if (HasWaitingPanel(panelNames[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 添加互斥弹窗。
        /// </summary>
        /// <param name="panelName">面板名称。</param>
        public void AddMutuallyExclusivePanel(string panelName)
        {
            if (!string.IsNullOrEmpty(panelName) && !_mutuallyExclusivePanelList.Contains(panelName))
            {
                _mutuallyExclusivePanelList.Add(panelName);
            }
        }

        /// <summary>
        /// 移除互斥弹窗。
        /// </summary>
        /// <param name="panelName">面板名称。</param>
        public void RemoveMutuallyExclusivePanel(string panelName)
        {
            _mutuallyExclusivePanelList.Remove(panelName);
        }

        /// <summary>
        /// 添加引导白名单面板。
        /// </summary>
        /// <param name="panelName">面板名称。</param>
        public void AddGuideWhiteListPanel(string panelName)
        {
            if (!string.IsNullOrEmpty(panelName) && !_guideWhiteList.Contains(panelName))
            {
                _guideWhiteList.Add(panelName);
            }
        }

        /// <summary>
        /// 移除引导白名单面板。
        /// </summary>
        /// <param name="panelName">面板名称。</param>
        public void RemoveGuideWhiteListPanel(string panelName)
        {
            _guideWhiteList.Remove(panelName);
        }

        /// <summary>
        /// 设置面板优先级。
        /// </summary>
        /// <param name="panelName">面板名称。</param>
        /// <param name="order">优先级（数值越小优先级越高）。</param>
        public void SetPanelSortOrder(string panelName, int order)
        {
            if (!string.IsNullOrEmpty(panelName))
            {
                _panelSortOrderDict[panelName] = order;
            }
        }

        #endregion

        #region 预置条件函数

        /// <summary>
        /// 主界面是否在最前面。
        /// </summary>
        /// <param name="_">参数（未使用）。</param>
        /// <returns>主界面是否在最前面。</returns>
        public bool HomeMainPanelInFront(object _)
        {
            // 需要根据实际项目实现
            // return PanelManager.Instance.IsPanelInTop(HomeMainPanel.Name);
            return true;
        }

        /// <summary>
        /// 主界面是否显示。
        /// </summary>
        /// <param name="_">参数（未使用）。</param>
        /// <returns>主界面是否显示。</returns>
        public bool HomeMainPanelIsShown(object _)
        {
            // 需要根据实际项目实现
            // return PanelManager.Instance.IsPanelShown<HomeMainPanel>();
            return true;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 启动检查协程。
        /// </summary>
        private void StartCheckCoroutine()
        {
            PeriodCheckAsync().Forget();
        }

        /// <summary>
        /// 周期检查（每0.1秒）。
        /// </summary>
        private async UniTaskVoid PeriodCheckAsync()
        {
            while (_isChecking && _waitingList.Count > 0)
            {
                await UniTask.Delay(100, cancellationToken: _cts.Token);
                Check();
            }
            _isChecking = false;
        }

        /// <summary>
        /// 检查并弹出窗口。
        /// </summary>
        private void Check()
        {
            // 引导期间过滤不在白名单中的弹窗
            if (IsGuiding())
            {
                for (int i = _waitingList.Count - 1; i >= 0; i--)
                {
                    if (!_guideWhiteList.Contains(_waitingList[i].PanelName))
                    {
                        _waitingList.RemoveAt(i);
                    }
                }

                if (_waitingList.Count == 0)
                {
                    return;
                }
            }

            // 检查是否可以弹出
            if (!CanPop())
            {
                return;
            }

            // 按优先级排序
            _waitingList.Sort(SortPanel);

            // 弹出第一个满足条件的面板
            int index = -1;
            for (int i = 0; i < _waitingList.Count; i++)
            {
                if (Pop(_waitingList[i]))
                {
                    index = i;
                    break;
                }
            }

            // 移除已弹出的面板
            if (index != -1)
            {
                _waitingList.RemoveAt(index);
            }
        }

        /// <summary>
        /// 检查是否可以弹出窗口。
        /// </summary>
        /// <returns>是否可以弹出。</returns>
        private bool CanPop()
        {
            return !IsPanelShowing();
        }

        /// <summary>
        /// 检查指定面板是否可以在引导期间弹出。
        /// </summary>
        /// <param name="panelName">面板名称。</param>
        /// <returns>是否可以弹出。</returns>
        private bool CanPopInNewbieGuiding(string panelName)
        {
            return !IsGuiding() || _guideWhiteList.Contains(panelName);
        }

        /// <summary>
        /// 检查是否正在引导。
        /// </summary>
        /// <returns>是否正在引导。</returns>
        private bool IsGuiding()
        {
            // 需要根据实际项目实现
            // return GuideModel.Instance.IsGuiding;
            return false;
        }

        /// <summary>
        /// 检查是否有互斥弹窗正在显示。
        /// </summary>
        /// <returns>是否有互斥弹窗显示。</returns>
        private bool IsPanelShowing()
        {
            for (int i = 0; i < _mutuallyExclusivePanelList.Count; i++)
            {
                if (FairyUIModule.Instance.HasWindow(_mutuallyExclusivePanelList[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 弹出窗口。
        /// </summary>
        /// <param name="info">等待信息。</param>
        /// <returns>是否成功弹出。</returns>
        private bool Pop(WaitingInfo info)
        {
            if (MeetPanelPreconditionFunc(info.PreconditionFunc, info.PreFuncParam))
            {
                // 使用 FairyUIModule 显示窗口
                FairyUIModule.Instance.ShowUI(info.PanelName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查前置条件是否满足。
        /// </summary>
        /// <param name="func">前置条件函数。</param>
        /// <param name="param">函数参数。</param>
        /// <returns>条件是否满足。</returns>
        private bool MeetPanelPreconditionFunc(Func<object, bool> func, object param)
        {
            return func == null || func(param);
        }

        /// <summary>
        /// 面板优先级排序。
        /// </summary>
        private int SortPanel(WaitingInfo x, WaitingInfo y)
        {
            _panelSortOrderDict.TryGetValue(x.PanelName, out int xOrder);
            _panelSortOrderDict.TryGetValue(y.PanelName, out int yOrder);

            if (xOrder > yOrder) return 1;
            if (xOrder < yOrder) return -1;
            return 0;
        }

        #endregion
    }
}
