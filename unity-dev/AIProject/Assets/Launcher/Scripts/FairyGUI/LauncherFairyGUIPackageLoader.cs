using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Launcher
{
    /// <summary>
    /// Launcher FairyGUI 资源加载器 - 使用 Resources.Load 加载。
    /// </summary>
    /// <remarks>
    /// 与主框架的 FairyUIPackageLoader 不同，此类不依赖 YooAsset，
    /// 适用于热更新前的 Launcher 阶段。
    /// </remarks>
    public static class LauncherFairyGUIPackageLoader
    {
        private static readonly HashSet<string> _loadedPackages = new HashSet<string>();
        private static readonly Dictionary<string, UnityEngine.Object[]> _loadedAssets = new Dictionary<string, UnityEngine.Object[]>();

        /// <summary>
        /// FairyGUI 资源基础路径（相对于 Resources 目录）。
        /// </summary>
        private const string ResourceBasePath = "FairyGUI";

        /// <summary>
        /// 同步加载 FairyGUI UIPackage。
        /// </summary>
        /// <param name="packageName">包名称（如 "LauncherUI"）。</param>
        public static void AddPackage(string packageName)
        {
            if (_loadedPackages.Contains(packageName))
            {
                Debug.LogWarning($"[LauncherFairyGUIPackageLoader] Package already loaded: {packageName}");
                return;
            }

            try
            {
                // 加载 FairyGUI 包描述文件
                string descPath = $"{ResourceBasePath}/{packageName}_fui";
                var descAsset = Resources.Load<TextAsset>(descPath);

                if (descAsset == null)
                {
                    Debug.LogError($"[LauncherFairyGUIPackageLoader] Failed to load package description: {descPath}");
                    return;
                }

                // 使用自定义加载函数添加包
                FairyGUI.UIPackage.AddPackage(descAsset.bytes, $"{ResourceBasePath}/{packageName}", LoadResource);

                _loadedPackages.Add(packageName);
                Debug.Log($"[LauncherFairyGUIPackageLoader] Package loaded: {packageName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LauncherFairyGUIPackageLoader] Failed to load package: {packageName}, Error: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 异步加载 FairyGUI UIPackage。
        /// </summary>
        /// <param name="packageName">包名称。</param>
        /// <returns>异步任务。</returns>
        public static async UniTask AddPackageAsync(string packageName)
        {
            if (_loadedPackages.Contains(packageName))
            {
                Debug.LogWarning($"[LauncherFairyGUIPackageLoader] Package already loaded: {packageName}");
                return;
            }

            try
            {
                // 异步加载 FairyGUI 包描述文件
                string descPath = $"{ResourceBasePath}/{packageName}_fui";
                var descRequest = Resources.LoadAsync<TextAsset>(descPath);

                await descRequest.ToUniTask();

                var descAsset = descRequest.asset as TextAsset;
                if (descAsset == null)
                {
                    Debug.LogError($"[LauncherFairyGUIPackageLoader] Failed to load package description: {descPath}");
                    return;
                }

                // 使用异步加载函数添加包
                FairyGUI.UIPackage.AddPackage(descAsset.bytes, $"{ResourceBasePath}/{packageName}", LoadResourceAsync);

                _loadedPackages.Add(packageName);
                Debug.Log($"[LauncherFairyGUIPackageLoader] Package loaded async: {packageName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LauncherFairyGUIPackageLoader] Failed to load package async: {packageName}, Error: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 卸载 FairyGUI UIPackage。
        /// </summary>
        /// <param name="packageName">包名称。</param>
        public static void RemovePackage(string packageName)
        {
            if (!_loadedPackages.Contains(packageName))
            {
                return;
            }

            try
            {
                FairyGUI.UIPackage.RemovePackage(packageName);
                _loadedPackages.Remove(packageName);

                // 释放加载的资源
                if (_loadedAssets.TryGetValue(packageName, out var assets))
                {
                    foreach (var asset in assets)
                    {
                        if (asset != null)
                        {
                            Resources.UnloadAsset(asset);
                        }
                    }
                    _loadedAssets.Remove(packageName);
                }

                Debug.Log($"[LauncherFairyGUIPackageLoader] Package unloaded: {packageName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LauncherFairyGUIPackageLoader] Failed to unload package: {packageName}, Error: {e.Message}");
            }
        }

        /// <summary>
        /// 卸载所有 FairyGUI UIPackage。
        /// </summary>
        public static void RemoveAllPackages()
        {
            foreach (var packageName in _loadedPackages)
            {
                try
                {
                    FairyGUI.UIPackage.RemovePackage(packageName);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LauncherFairyGUIPackageLoader] Failed to unload package: {packageName}, Error: {e.Message}");
                }
            }

            // 释放所有加载的资源
            foreach (var assets in _loadedAssets.Values)
            {
                foreach (var asset in assets)
                {
                    if (asset != null)
                    {
                        Resources.UnloadAsset(asset);
                    }
                }
            }

            _loadedPackages.Clear();
            _loadedAssets.Clear();
            Debug.Log("[LauncherFairyGUIPackageLoader] All packages unloaded");
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
        /// 创建 FairyGUI 对象。
        /// </summary>
        /// <param name="packageName">包名称。</param>
        /// <param name="componentName">组件名称。</param>
        /// <returns>FairyGUI 对象，失败返回 null。</returns>
        public static FairyGUI.GObject CreateObject(string packageName, string componentName)
        {
            if (!_loadedPackages.Contains(packageName))
            {
                Debug.LogError($"[LauncherFairyGUIPackageLoader] Package not loaded: {packageName}");
                return null;
            }

            try
            {
                var obj = FairyGUI.UIPackage.CreateObject(packageName, componentName);
                if (obj == null)
                {
                    Debug.LogError($"[LauncherFairyGUIPackageLoader] Failed to create object: {packageName}/{componentName}");
                }
                return obj;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LauncherFairyGUIPackageLoader] Failed to create object: {packageName}/{componentName}, Error: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 同步加载资源函数（用于 FairyGUI LoadResource 委托）。
        /// </summary>
        /// <param name="name">资源名称。</param>
        /// <param name="extension">扩展名。</param>
        /// <param name="type">资源类型。</param>
        /// <param name="destroyMethod">销毁方法。</param>
        /// <returns>加载的资源对象。</returns>
        private static object LoadResource(string name, string extension, Type type, out FairyGUI.DestroyMethod destroyMethod)
        {
            destroyMethod = FairyGUI.DestroyMethod.None;

            string resourcePath = name;
            Debug.Log($"[LauncherFairyGUIPackageLoader] Loading resource: {resourcePath}, type: {type.Name}");

            var asset = Resources.Load(resourcePath, type);
            if (asset != null)
            {
                // 记录加载的资源以便后续释放
                string packageName = ExtractPackageName(resourcePath);
                if (!_loadedAssets.ContainsKey(packageName))
                {
                    _loadedAssets[packageName] = new UnityEngine.Object[0];
                }

                Debug.Log($"[LauncherFairyGUIPackageLoader] Resource loaded: {resourcePath}");
                return asset;
            }

            Debug.LogWarning($"[LauncherFairyGUIPackageLoader] Failed to load resource: {resourcePath}");
            return null;
        }

        /// <summary>
        /// 异步加载资源函数（用于 FairyGUI LoadResourceAsync 委托）。
        /// </summary>
        /// <param name="name">资源名称。</param>
        /// <param name="extension">扩展名。</param>
        /// <param name="type">资源类型。</param>
        /// <param name="item">包项目。</param>
        private static void LoadResourceAsync(string name, string extension, Type type, FairyGUI.PackageItem item)
        {
            string resourcePath = name;
            Debug.Log($"[LauncherFairyGUIPackageLoader] Loading resource async: {resourcePath}, type: {type.Name}");

            var request = Resources.LoadAsync(resourcePath, type);
            request.completed += (operation) =>
            {
                var asset = request.asset;
                if (asset != null)
                {
                    item.owner.SetItemAsset(item, asset, FairyGUI.DestroyMethod.None);
                    Debug.Log($"[LauncherFairyGUIPackageLoader] Resource loaded async: {resourcePath}");
                }
                else
                {
                    Debug.LogError($"[LauncherFairyGUIPackageLoader] Failed to load resource async: {resourcePath}");
                }
            };
        }

        /// <summary>
        /// 从资源路径中提取包名称。
        /// </summary>
        /// <param name="resourcePath">资源路径。</param>
        /// <returns>包名称。</returns>
        private static string ExtractPackageName(string resourcePath)
        {
            // 资源路径格式：FairyGUI/PackageName_xxx
            string fileName = System.IO.Path.GetFileNameWithoutExtension(resourcePath);
            int underscoreIndex = fileName.IndexOf('_');
            return underscoreIndex > 0 ? fileName.Substring(0, underscoreIndex) : fileName;
        }
    }
}
