using System;
using System.Collections;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// UI面板辅助工具类。
    /// 提供协程管理、延迟调用、定时器等功能，跟随窗口生命周期自动清理。
    /// </summary>
    public class UIPanelHelper
    {
        #region 协程支持

        private readonly List<Coroutine> _coroutines = new List<Coroutine>();

        /// <summary>
        /// 启动协程（自动管理，窗口隐藏时自动停止）。
        /// </summary>
        /// <param name="routine">协程迭代器。</param>
        /// <returns>协程对象。</returns>
        public Coroutine StartCoroutine(IEnumerator routine)
        {
            var coroutine = GameModule.Base.StartCoroutine(routine);
            _coroutines.Add(coroutine);
            return coroutine;
        }

        /// <summary>
        /// 停止协程。
        /// </summary>
        /// <param name="routine">协程迭代器。</param>
        public void StopCoroutine(IEnumerator routine)
        {
            GameModule.Base.StopCoroutine(routine);
        }

        /// <summary>
        /// 停止协程。
        /// </summary>
        /// <param name="coroutine">协程对象。</param>
        public void StopCoroutine(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                GameModule.Base.StopCoroutine(coroutine);
                _coroutines.Remove(coroutine);
            }
        }

        /// <summary>
        /// 停止所有协程。
        /// </summary>
        public void StopAllCoroutines()
        {
            foreach (var coroutine in _coroutines)
            {
                if (coroutine != null)
                {
                    GameModule.Base.StopCoroutine(coroutine);
                }
            }
            _coroutines.Clear();
        }

        #endregion

        #region 延迟调用

        private readonly Dictionary<Action, Coroutine> _delayCalls = new Dictionary<Action, Coroutine>();

        /// <summary>
        /// 延迟调用。
        /// </summary>
        /// <param name="seconds">延迟秒数。</param>
        /// <param name="callback">回调函数。</param>
        public void DelayCall(float seconds, Action callback)
        {
            if (callback == null) return;

            // 移除已有的延迟调用
            RemoveDelayCall(callback);

            var coroutine = GameModule.Base.StartCoroutine(DelayCallCoroutine(seconds, callback));
            _delayCalls[callback] = coroutine;
        }

        /// <summary>
        /// 移除延迟调用。
        /// </summary>
        /// <param name="callback">回调函数。</param>
        public void RemoveDelayCall(Action callback)
        {
            if (callback != null && _delayCalls.TryGetValue(callback, out var coroutine))
            {
                if (coroutine != null)
                {
                    GameModule.Base.StopCoroutine(coroutine);
                }
                _delayCalls.Remove(callback);
            }
        }

        /// <summary>
        /// 检查是否有延迟调用。
        /// </summary>
        /// <param name="callback">回调函数。</param>
        /// <returns>是否存在。</returns>
        public bool HasDelayCall(Action callback)
        {
            return callback != null && _delayCalls.ContainsKey(callback);
        }

        private IEnumerator DelayCallCoroutine(float seconds, Action callback)
        {
            yield return new WaitForSeconds(seconds);
            _delayCalls.Remove(callback);
            callback?.Invoke();
        }

        #endregion

        #region 秒级定时器

        private readonly List<Action<bool>> _secondUpdates = new List<Action<bool>>();

        /// <summary>
        /// 添加秒级定时器（立即调用一次true，之后每秒调用false）。
        /// </summary>
        /// <param name="action">回调函数。</param>
        public void AddSecondUpdate(Action<bool> action)
        {
            if (action == null) return;
            RemoveSecondUpdate(action);
            _secondUpdates.Add(action);
            action.Invoke(true);
        }

        /// <summary>
        /// 移除秒级定时器。
        /// </summary>
        /// <param name="action">回调函数。</param>
        public void RemoveSecondUpdate(Action<bool> action)
        {
            if (action != null)
            {
                _secondUpdates.Remove(action);
            }
        }

        #endregion

        #region 帧更新

        private readonly List<Action> _updates = new List<Action>();
        private readonly List<Action> _lateUpdates = new List<Action>();

        /// <summary>
        /// 添加帧更新回调。
        /// </summary>
        /// <param name="action">回调函数。</param>
        public void AddUpdate(Action action)
        {
            if (action == null) return;
            RemoveUpdate(action);
            _updates.Add(action);
        }

        /// <summary>
        /// 移除帧更新回调。
        /// </summary>
        /// <param name="action">回调函数。</param>
        public void RemoveUpdate(Action action)
        {
            if (action != null)
            {
                _updates.Remove(action);
            }
        }

        /// <summary>
        /// 添加LateUpdate回调。
        /// </summary>
        /// <param name="action">回调函数。</param>
        public void AddLateUpdate(Action action)
        {
            if (action == null) return;
            RemoveLateUpdate(action);
            _lateUpdates.Add(action);
        }

        /// <summary>
        /// 移除LateUpdate回调。
        /// </summary>
        /// <param name="action">回调函数。</param>
        public void RemoveLateUpdate(Action action)
        {
            if (action != null)
            {
                _lateUpdates.Remove(action);
            }
        }

        /// <summary>
        /// 内部更新（由 FairyUIModule 驱动）。
        /// </summary>
        internal void InternalUpdate()
        {
            for (int i = 0; i < _updates.Count; i++)
            {
                _updates[i]?.Invoke();
            }
        }

        /// <summary>
        /// 内部LateUpdate（由 FairyUIModule 驱动）。
        /// </summary>
        internal void InternalLateUpdate()
        {
            for (int i = 0; i < _lateUpdates.Count; i++)
            {
                _lateUpdates[i]?.Invoke();
            }
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清理所有资源（窗口隐藏时调用）。
        /// </summary>
        public void Clear()
        {
            StopAllCoroutines();

            // 清理延迟调用
            foreach (var kvp in _delayCalls)
            {
                if (kvp.Value != null)
                {
                    GameModule.Base.StopCoroutine(kvp.Value);
                }
            }
            _delayCalls.Clear();

            // 清理定时器
            _secondUpdates.Clear();

            // 清理帧更新
            _updates.Clear();
            _lateUpdates.Clear();
        }

        #endregion
    }
}
