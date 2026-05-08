using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Launcher
{
    // 通过它加载资源，以AB为单位，在要释放资源之前，要持有这个对象
    public class AotResHandler: IDisposable
    {
        public static string AOT_RES_PATH = "Assets/GameRes/AOT/";
        public static string AOT_AB_PATH = Path.Combine(Application.streamingAssetsPath,
            "AOT", "GetPlatform");
        public string bundleName;
        public bool isAbLoaded = false;
        private AssetBundle _bundle;

        public AotResHandler(string name)
        {
#if !UNITY_EDITOR
            name = Path.GetFileNameWithoutExtension(name);
            bundleName = name;
            _bundle = null;
#endif
        }

        ~AotResHandler()
        {
            Release();
        }

        public T LoadAssetSync<T>(string assertName) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            var filePath = $"{AOT_RES_PATH}{assertName}";
            return AssetDatabase.LoadAssetAtPath<T>(filePath);
#else
            if (!isAbLoaded)
            {
                _bundle = AotLoader.LoadBundle(bundleName);
                isAbLoaded = true;
            }
            assertName = Path.GetFileNameWithoutExtension(assertName);
            return _bundle.LoadAsset<T>(assertName);
#endif
        }

        public UnityEngine.Object LoadAssetSync(string assertName, System.Type type)
        {
#if UNITY_EDITOR
             var filePath = $"{AOT_RES_PATH}{assertName}";
             return AssetDatabase.LoadAssetAtPath(filePath, type);
#else
            if (!isAbLoaded)
            {
                _bundle = AotLoader.LoadBundle(bundleName);
                isAbLoaded = true;
            }
            assertName = Path.GetFileNameWithoutExtension(assertName);
            return _bundle.LoadAsset(assertName, type);
#endif
        }

        public void Release()
        {
#if !UNITY_EDITOR
            if (isAbLoaded)
            {
                AotLoader.UnloadBundle(bundleName);
                _bundle = null;
                isAbLoaded = false;
            }
#endif
        }

        public void Dispose()
        {
            Release();
        }
    }
}