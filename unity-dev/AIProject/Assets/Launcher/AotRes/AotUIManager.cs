using System;
using System.Collections.Generic;
using FairyGUI;
using UnityEngine;

namespace Launcher
{
    public static class AotUIManager
    {
        public static readonly string packageName = "aot";
        public static readonly string descName = $"{packageName}_fui.bytes";
        public static UIPackage aotPackage;
        private static AotResHandler _descHandle;
        private static Dictionary<string, AotResHandler> _OtherAssetHandleMap;
        private static bool needReloadAtlas = false;

        public static void InitAotPackage()
        {
            _descHandle = new AotResHandler(descName);
            var asset = _descHandle.LoadAssetSync<TextAsset>(descName);
            var desc = asset.bytes;
            _OtherAssetHandleMap = new Dictionary<string, AotResHandler>();
            aotPackage = UIPackage.AddPackage(desc, string.Empty,
                (string name, string _, Type type, out DestroyMethod method) =>
                {
                    method = DestroyMethod.None;
                    if (name.Contains("!a")) return null;
                    var location = $"{packageName}_{name}.png";
                    var handle = new AotResHandler(location);
                    _OtherAssetHandleMap[name] = handle;
                    var atlasAsset = handle.LoadAssetSync(location, type);
                    return atlasAsset;
                });
            needReloadAtlas = false;
        }

        public static void CheckReloadAtlas()
        {
            if (needReloadAtlas)
            {
                ReloadAtlas();
            }
        }

        public static void ReloadAtlas()
        {
            aotPackage.ReloadAssets();
            needReloadAtlas = false;
        }

        public static void UnloadAtlas()
        {
            aotPackage.UnloadAssets();
            foreach (var kv in _OtherAssetHandleMap)
            {
                var handler = kv.Value;
                handler.Release();
            }
            _OtherAssetHandleMap.Clear();
            needReloadAtlas = true;
        }
    }
}