using System;
using System.Collections.Generic;
using FairyGUI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 通用列表组件封装。
    /// 简化 FairyGUI GList 的使用，支持数据绑定、虚拟列表、分页加载等。
    /// </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    public class CompList<T>
    {
        #region 公开字段

        /// <summary>
        /// 数据列表。
        /// </summary>
        public List<T> uiDataLs = new List<T>();

        /// <summary>
        /// FairyGUI GList 组件。
        /// </summary>
        public GList uiList;

        #endregion

        #region 私有字段

        private Action<int> _bottomCallback = null;
        private int _max = 5;
        private Action<GObject, T> _itemCallback = null;

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建基础列表。
        /// </summary>
        /// <param name="list">FairyGUI GList 组件。</param>
        /// <param name="callback">Item渲染回调（可选）。</param>
        public CompList(GList list, Action<GObject, T> callback = null)
        {
            uiList = list;
            _itemCallback = callback;
            uiList.itemRenderer = OnItemRenderer;
        }

        /// <summary>
        /// 创建虚拟列表（带滑到底部回调）。
        /// </summary>
        /// <param name="list">FairyGUI GList 组件。</param>
        /// <param name="bottomCallback">滑到底部回调。</param>
        /// <param name="max">列表显示的长度（一定要大于请求数据的长度）。</param>
        public CompList(GList list, Action<int> bottomCallback, int max)
        {
            uiList = list;
            _bottomCallback = bottomCallback;
            _max = max;
            SetIsVirtual();
            uiList.itemRenderer = OnItemRendererVirtual;
        }

        #endregion

        #region 公开API

        /// <summary>
        /// 设置为虚拟列表。
        /// </summary>
        public void SetIsVirtual()
        {
            uiList.SetVirtual();
        }

        /// <summary>
        /// 设置数据列表（替换所有数据）。
        /// </summary>
        /// <param name="dataLs">数据列表。</param>
        public void SetDataList(List<T> dataLs)
        {
            uiDataLs ??= new List<T>();
            uiDataLs.Clear();

            if (dataLs != null)
            {
                for (int i = 0; i < dataLs.Count; i++)
                {
                    uiDataLs.Add(dataLs[i]);
                }
            }

            uiList.numItems = uiDataLs.Count;

            if (uiList.isVirtual)
            {
                uiList.RefreshVirtualList();
            }
        }

        /// <summary>
        /// 追加数据列表（用于动态加载）。
        /// </summary>
        /// <param name="dataLs">追加的数据列表。</param>
        public void AddDataList(List<T> dataLs)
        {
            if (dataLs == null) return;

            uiDataLs ??= new List<T>();
            for (int i = 0; i < dataLs.Count; i++)
            {
                uiDataLs.Add(dataLs[i]);
            }

            uiList.numItems = uiDataLs.Count;

            if (uiList.isVirtual)
            {
                uiList.RefreshVirtualList();
            }
        }

        /// <summary>
        /// 列表自适应大小。
        /// </summary>
        public void ResizeToFit()
        {
            uiList.ResizeToFit();
        }

        /// <summary>
        /// 设置Item Provider（虚拟列表不同类型Item）。
        /// </summary>
        /// <param name="itemProvider">返回Item资源URL的函数。</param>
        public void SetItemProvider(Func<T, string> itemProvider)
        {
            uiList.itemProvider = (index) =>
            {
                if (index >= 0 && index < uiDataLs.Count)
                {
                    var data = uiDataLs[index];
                    return itemProvider(data);
                }
                return null;
            };
        }

        /// <summary>
        /// 设置点击回调（FairyGUI EventCallback1）。
        /// </summary>
        /// <param name="callback1">点击回调。</param>
        public void SetItemClickCallback(EventCallback1 callback1)
        {
            uiList.onClickItem.Add(callback1);
        }

        /// <summary>
        /// 设置点击回调（Action）。
        /// </summary>
        /// <param name="callback">点击回调（GList, 点击数据）。</param>
        public void SetItemClickCallAction(Action<GList, object> callback)
        {
            uiList.onClickItem.Set((EventContext context) =>
            {
                callback?.Invoke(uiList, context.data);
            });
        }

        /// <summary>
        /// 刷新单个Item。
        /// </summary>
        /// <param name="data">要刷新的数据。</param>
        public void RefreshItem(T data)
        {
            int index = uiDataLs.IndexOf(data);
            if (index < 0 || index >= uiDataLs.Count) return;

            uiDataLs[index] = data;

            if (!uiList.isVirtual)
            {
                var item = uiList.GetChildAt(index);
                if (item != null)
                {
                    OnItemRender(item, data);
                }
            }
            else
            {
                int childIndex = uiList.ItemIndexToChildIndex(index);
                if (childIndex >= 0 && childIndex < uiList.numChildren)
                {
                    var item = uiList.GetChildAt(childIndex);
                    if (item != null)
                    {
                        OnItemRender(item, data);
                    }
                }
            }
        }

        /// <summary>
        /// 滚动到指定位置。
        /// </summary>
        /// <param name="index">目标索引。</param>
        /// <param name="ani">是否使用动画。</param>
        public void ScrollToView(int index, bool ani = false)
        {
            if (index >= 0 && index < uiDataLs.Count)
            {
                uiList.ScrollToView(index, ani);
            }
        }

        /// <summary>
        /// 从 EventContext 获取点击数据。
        /// </summary>
        /// <param name="context">事件上下文。</param>
        /// <returns>点击的数据。</returns>
        public T GetClickData(EventContext context)
        {
            var item = context.data as GObject;
            if (item != null)
            {
                int childIndex = uiList.GetChildIndex(item);
                int itemIndex = uiList.ChildIndexToItemIndex(childIndex);
                if (itemIndex >= 0 && itemIndex < uiDataLs.Count)
                {
                    return uiDataLs[itemIndex];
                }
            }
            return default;
        }

        /// <summary>
        /// 获取数据列表。
        /// </summary>
        /// <returns>数据列表。</returns>
        public List<T> GetDataList()
        {
            return uiDataLs;
        }

        /// <summary>
        /// 获取数据数量。
        /// </summary>
        /// <returns>数据数量。</returns>
        public int GetCount()
        {
            return uiDataLs?.Count ?? 0;
        }

        /// <summary>
        /// 清空数据。
        /// </summary>
        public void Clear()
        {
            uiDataLs?.Clear();
            uiList.numItems = 0;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// Item渲染回调（基础列表）。
        /// </summary>
        private void OnItemRenderer(int index, GObject item)
        {
            if (index < 0 || index >= uiDataLs.Count) return;

            var data = uiDataLs[index];
            try
            {
                OnItemRender(item, data);
                _itemCallback?.Invoke(item, data);
            }
            catch (Exception e)
            {
                Log.Error($"CompList OnItemRenderer error: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Item渲染回调（虚拟列表）。
        /// </summary>
        private void OnItemRendererVirtual(int index, GObject item)
        {
            if (index < 0 || index >= uiDataLs.Count) return;

            var data = uiDataLs[index];
            try
            {
                OnItemRender(item, data);
                _itemCallback?.Invoke(item, data);

                // 虚拟列表滑到底部回调
                if (uiList.isVirtual && _bottomCallback != null)
                {
                    int childIndex = uiList.ItemIndexToChildIndex(index);
                    int itemIndex = uiList.ChildIndexToItemIndex(childIndex);

                    if (itemIndex >= _max && itemIndex >= uiDataLs.Count - 1)
                    {
                        _bottomCallback(itemIndex);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"CompList OnItemRendererVirtual error: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 渲染Item（调用 OnGUI 方法）。
        /// </summary>
        private void OnItemRender(GObject item, T data)
        {
            // 尝试调用 item 的 OnGUI 方法（如果实现了 IListItem 接口）
            if (item is IListItem<T> listItem)
            {
                listItem.OnGUI(data);
            }
        }

        #endregion
    }

    /// <summary>
    /// 列表Item接口（实现此接口可自动绑定数据）。
    /// </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    public interface IListItem<T>
    {
        /// <summary>
        /// 渲染Item。
        /// </summary>
        /// <param name="data">数据。</param>
        void OnGUI(T data);
    }
}
