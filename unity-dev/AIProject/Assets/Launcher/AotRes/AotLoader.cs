using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Launcher
{
    public static class AotLoader
    {
        private static readonly Dictionary<string, AotBundleHandler> loadedMap = new Dictionary<string, AotBundleHandler>();

        // 不要在 AotResHandler 以外的地方调用这个方法
        internal static AssetBundle LoadBundle(string bundleName)
        {
            if (loadedMap.ContainsKey(bundleName))
            {
                loadedMap[bundleName].refCount += 1;
                return loadedMap[bundleName].bundle;
            }
            // 在安卓现在能直接这样加载了吗？
            var filePath = $"{AotResHandler.AOT_AB_PATH}/{bundleName}.ab";
            AssetBundle bundle = AssetBundle.LoadFromFile(filePath);
            if (bundle == null)
            {
                throw new Exception("Failed to load AssetBundle!");
            }
            var bundleHandler = new AotBundleHandler(bundle);
            bundleHandler.refCount += 1;
            loadedMap[bundleName] = bundleHandler;
            return bundle;
        }

        // 不要在 AotResHandler 以外的地方调用这个方法
        internal static void UnloadBundle(string bundleName)
        {
            if (loadedMap.ContainsKey(bundleName))
            {
                loadedMap[bundleName].refCount -= 1;
                if (loadedMap[bundleName].refCount <= 0)
                {
                    loadedMap[bundleName].bundle.Unload(true);
                    loadedMap.Remove(bundleName);
                }
            }
        }
    }
}