
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace GameLogic
{
    public class ShaderManager : Singleton<ShaderManager>
    {
        private Dictionary<string, AssetHandle> _shaderVariantCollections;
        private Dictionary<string, AssetHandle> _shaders;

        protected override void OnInit()
        {
            _shaderVariantCollections = new Dictionary<string, AssetHandle>();
            _shaders = new Dictionary<string, AssetHandle>();
        }

        protected override void OnRelease()
        {
            foreach (var shaderVariantCollection in _shaderVariantCollections)
            {
                shaderVariantCollection.Value.Dispose();
            }
            _shaderVariantCollections.Clear();
            
            foreach (var shader in _shaders)
            {
                shader.Value.Dispose();
            }
            _shaders.Clear();
        }
        
        // Shader 变体收集器的动态预热方法
        public void WarmUpShaderVariantCollection(string collectionName)
        {
            if (_shaderVariantCollections.ContainsKey(collectionName)) return;
            var handle = YooAssets.LoadAssetSync<ShaderVariantCollection>(collectionName);
            ShaderVariantCollection collection = handle.GetAssetObject<ShaderVariantCollection>();
            collection.WarmUp();
            handle.Dispose();
            _shaderVariantCollections.Add(collectionName, handle);
        }

        // 动态加载 shader 的方法
        private Shader InternalGetShader(string shaderPath)
        {
            var shader = Shader.Find(shaderPath);

            // 如果 shader 已经加载，则直接返回
            if (shader != null)
                return shader;
            
            string shaderName = shaderPath.Substring(shaderPath.LastIndexOf('/') + 1);
            if (_shaders.TryGetValue(shaderName, out AssetHandle handle))
            {
                return handle.GetAssetObject<Shader>();
            }
            
            handle = YooAssets.LoadAssetSync<Shader>(shaderName);
            shader = handle.GetAssetObject<Shader>();
            _shaders.Add(shaderName, handle);
            return shader;
        }

        public static Shader GetShader(string shaderPath)
        {
#if UNITY_EDITOR
            return Shader.Find(shaderPath);
#else
            return ShaderManager.Instance.InternalGetShader(shaderPath);
#endif
        }
    }
}