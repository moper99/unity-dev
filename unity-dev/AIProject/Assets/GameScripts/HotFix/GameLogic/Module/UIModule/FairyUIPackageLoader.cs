using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using YooAsset;

namespace GameLogic
{
    /// <summary>
    /// FairyGUI资源加载器 - 与YooAsset集成。
    /// </summary>
    public static class FairyUIPackageLoader
    {
        private static readonly HashSet<string> _loadedPackages = new HashSet<string>();
        private static readonly Dictionary<string, AssetHandle> _assetHandles = new Dictionary<string, AssetHandle>();
        private static string _assetBasePath = "Assets/AssetRaw/UIRaw/Atlas";

        /// <summary>
        /// 同步加载UIPackage。
        /// </summary>
        /// <param name="packageName">包名称。</param>
        /// <param name="onProgress">加载进度回调。</param>
        public static void AddPackage(string packageName, Action<float> onProgress = null)
        {
            if (_loadedPackages.Contains(packageName))
            {
                return;
            }

            string assetPath = GetAssetPath(packageName);
            FairyGUI.UIPackage.AddPackage(assetPath, LoadResource);
            _loadedPackages.Add(packageName);
            onProgress?.Invoke(1f);
        }

        /// <summary>
        /// 异步加载UIPackage。
        /// </summary>
        /// <param name="packageName">包名称。</param>
        /// <param name="ct">取消令牌。</param>
        public static async UniTask AddPackageAsync(string packageName, CancellationToken ct = default)
        {
            if (_loadedPackages.Contains(packageName))
            {
                return;
            }

            ct.ThrowIfCancellationRequested();

            string descPath = $"{GetAssetPath(packageName)}_fui.bytes";
            var descHandle = YooAssets.LoadAssetAsync<TextAsset>(descPath);

            await descHandle.ToUniTask(cancellationToken: ct);

            if (descHandle.AssetObject == null)
            {
                throw new InvalidOperationException($"Failed to load FairyGUI package description: {descPath}");
            }

            var textAsset = descHandle.AssetObject as TextAsset;
            string assetNamePrefix = GetAssetPath(packageName);
            FairyGUI.UIPackage.AddPackage(textAsset.bytes, assetNamePrefix, LoadResourceAsync);

            _assetHandles[packageName] = descHandle;
            _loadedPackages.Add(packageName);
        }

        /// <summary>
        /// 卸载所有UIPackage。
        /// </summary>
        public static void RemoveAllPackages()
        {
            foreach (var packageName in _loadedPackages)
            {
                FairyGUI.UIPackage.RemovePackage(packageName);
            }

            foreach (var handle in _assetHandles.Values)
            {
                handle.Dispose();
            }

            _loadedPackages.Clear();
            _assetHandles.Clear();
        }

        /// <summary>
        /// 检查包是否已加载。
        /// </summary>
        /// <param name="packageName">包名称。</param>
        /// <returns>是否已加载。</returns>
        public static bool IsPackageLoaded(string packageName)
        {
            return _loadedPackages.Contains(packageName);
        }

        /// <summary>
        /// 创建FairyGUI对象。
        /// </summary>
        /// <param name="packageName">包名称。</param>
        /// <param name="componentName">组件名称。</param>
        /// <returns>FairyGUI对象。</returns>
        public static FairyGUI.GObject CreateObject(string packageName, string componentName)
        {
            if (!_loadedPackages.Contains(packageName))
            {
                Log.Error($"FairyGUI package not loaded: {packageName}");
                return null;
            }

            return FairyGUI.UIPackage.CreateObject(packageName, componentName);
        }

        private static string GetAssetPath(string packageName)
        {
            return $"{_assetBasePath}/{packageName}";
        }

        private static object LoadResource(string name, string extension, Type type, out FairyGUI.DestroyMethod destroyMethod)
        {
            destroyMethod = FairyGUI.DestroyMethod.None;
            Log.Debug($"LoadResource: {name}, type: {type.FullName}");
            var assetHandle = YooAssets.LoadAssetSync(name, type);
            
            if (assetHandle != null && assetHandle.IsValid)
            {
                var asset = assetHandle.AssetObject;
                Log.Debug($"LoadResource result: {asset?.GetType().FullName ?? "null"}");
                return asset;
            }

            Log.Warning($"Failed to load FairyGUI resource: {name}");
            return null;
        }

        private static void LoadResourceAsync(string name, string extension, Type type, FairyGUI.PackageItem item)
        {
            var assetHandle = YooAssets.LoadAssetAsync(name, type);
            
            assetHandle.Completed += (handle) =>
            {
                if (handle != null && handle.IsValid && handle.AssetObject != null)
                {
                    item.owner.SetItemAsset(item, handle.AssetObject, FairyGUI.DestroyMethod.None);
                }
                else
                {
                    Log.Error($"Failed to load FairyGUI resource: {name}");
                }
            };
        }
    }
}
