using System;
using System.Collections.Generic;
using FairyGUI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// UI组件对象池。
    /// 用于复用 FairyGUI 组件，减少频繁创建和销毁的开销。
    /// </summary>
    public class UIComponentPool
    {
        #region 私有字段

        /// <summary>
        /// 按组件名分组的空闲池。
        /// </summary>
        private readonly Dictionary<string, List<GComponent>> _pools = new Dictionary<string, List<GComponent>>();

        /// <summary>
        /// 池容量配置（组件名 -> 最大容量）。
        /// </summary>
        private readonly Dictionary<string, int> _capacityConfig = new Dictionary<string, int>();

        /// <summary>
        /// 默认池容量。
        /// </summary>
        private int _defaultCapacity = 20;

        #endregion

        #region 公开API

        /// <summary>
        /// 设置默认池容量。
        /// </summary>
        /// <param name="capacity">容量。</param>
        public void SetDefaultCapacity(int capacity)
        {
            _defaultCapacity = Math.Max(1, capacity);
        }

        /// <summary>
        /// 设置指定组件的池容量。
        /// </summary>
        /// <param name="componentName">组件名称。</param>
        /// <param name="capacity">容量。</param>
        public void SetCapacity(string componentName, int capacity)
        {
            if (!string.IsNullOrEmpty(componentName))
            {
                _capacityConfig[componentName] = Math.Max(1, capacity);
            }
        }

        /// <summary>
        /// 获取组件（优先从池中取，池空则创建新实例）。
        /// </summary>
        /// <param name="packageName">FairyGUI包名称。</param>
        /// <param name="componentName">组件名称。</param>
        /// <param name="parent">父容器。</param>
        /// <returns>组件实例。</returns>
        public GComponent Get(string packageName, string componentName, GComponent parent)
        {
            var pool = GetPool(componentName);

            GComponent comp;
            if (pool.Count > 0)
            {
                // 从池尾取（LIFO，提高缓存命中率）
                int lastIndex = pool.Count - 1;
                comp = pool[lastIndex];
                pool.RemoveAt(lastIndex);
            }
            else
            {
                // 池空则创建新实例
                comp = FairyUIPackageLoader.CreateObject(packageName, componentName) as GComponent;
                if (comp == null)
                {
                    Log.Error($"UIComponentPool.Get: Failed to create component: {packageName}/{componentName}");
                    return null;
                }

                // 添加到父容器
                if (parent != null)
                {
                    parent.AddChild(comp);
                }
            }

            comp.visible = true;
            return comp;
        }

        /// <summary>
        /// 回收组件到池中。
        /// </summary>
        /// <param name="comp">要回收的组件。</param>
        public void Recycle(GComponent comp)
        {
            if (comp == null) return;

            string componentName = comp.gameObjectName;
            if (string.IsNullOrEmpty(componentName))
            {
                componentName = comp.resourceURL ?? "unknown";
            }

            var pool = GetPool(componentName);
            int capacity = GetCapacity(componentName);

            // 检查池容量
            if (pool.Count >= capacity)
            {
                // 池已满，直接销毁
                comp.Dispose();
                return;
            }

            // 隐藏并加入池
            comp.visible = false;
            pool.Add(comp);
        }

        /// <summary>
        /// 预热池（预先创建指定数量的组件）。
        /// </summary>
        /// <param name="packageName">FairyGUI包名称。</param>
        /// <param name="componentName">组件名称。</param>
        /// <param name="parent">父容器。</param>
        /// <param name="count">预热数量。</param>
        public void PreWarm(string packageName, string componentName, GComponent parent, int count)
        {
            var pool = GetPool(componentName);
            int capacity = GetCapacity(componentName);
            int warmCount = Math.Min(count, capacity - pool.Count);

            for (int i = 0; i < warmCount; i++)
            {
                var comp = FairyUIPackageLoader.CreateObject(packageName, componentName) as GComponent;
                if (comp != null)
                {
                    if (parent != null)
                    {
                        parent.AddChild(comp);
                    }
                    comp.visible = false;
                    pool.Add(comp);
                }
            }

            Log.Debug($"UIComponentPool.PreWarm: {componentName}, warmed: {warmCount}");
        }

        /// <summary>
        /// 清空指定组件的池。
        /// </summary>
        /// <param name="componentName">组件名称。</param>
        public void Clear(string componentName)
        {
            if (_pools.TryGetValue(componentName, out var pool))
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    pool[i]?.Dispose();
                }
                pool.Clear();
            }
        }

        /// <summary>
        /// 清空所有池。
        /// </summary>
        public void ClearAll()
        {
            foreach (var kvp in _pools)
            {
                var pool = kvp.Value;
                for (int i = 0; i < pool.Count; i++)
                {
                    pool[i]?.Dispose();
                }
                pool.Clear();
            }
            _pools.Clear();
        }

        /// <summary>
        /// 获取池统计信息。
        /// </summary>
        /// <returns>统计信息字典（组件名 -> 当前池中数量）。</returns>
        public Dictionary<string, int> GetStatistics()
        {
            var stats = new Dictionary<string, int>();
            foreach (var kvp in _pools)
            {
                stats[kvp.Key] = kvp.Value.Count;
            }
            return stats;
        }

        /// <summary>
        /// 获取指定组件的池中数量。
        /// </summary>
        /// <param name="componentName">组件名称。</param>
        /// <returns>池中数量。</returns>
        public int GetPoolCount(string componentName)
        {
            if (_pools.TryGetValue(componentName, out var pool))
            {
                return pool.Count;
            }
            return 0;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取或创建指定组件的池。
        /// </summary>
        /// <param name="componentName">组件名称。</param>
        /// <returns>组件池。</returns>
        private List<GComponent> GetPool(string componentName)
        {
            if (!_pools.TryGetValue(componentName, out var pool))
            {
                pool = new List<GComponent>();
                _pools[componentName] = pool;
            }
            return pool;
        }

        /// <summary>
        /// 获取指定组件的池容量。
        /// </summary>
        /// <param name="componentName">组件名称。</param>
        /// <returns>池容量。</returns>
        private int GetCapacity(string componentName)
        {
            if (_capacityConfig.TryGetValue(componentName, out var capacity))
            {
                return capacity;
            }
            return _defaultCapacity;
        }

        #endregion
    }
}
