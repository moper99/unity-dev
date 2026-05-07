using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using FairyGUI;
using TEngine;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace GameLogic
{
    public class UITextureManager : Singleton<UITextureManager>
    {
        public struct TexturePoolItem
        {
            public string URL;
            public NTexture Texture;
            public AssetHandle Handle;
            public Texture RawTexture;
        }

        class LoadItem
        {
            public string URL;
            public Action<TexturePoolItem> OnSuccess;
            public Action<string> OnFail;
            public AssetHandle Handle;
        }

        //建立url到TexturePoolItem的映射
        private readonly Dictionary<string, TexturePoolItem> _urlToPoolItem = new();

        //建立url到LoadItem的映射
        private readonly Dictionary<string, LoadItem> _urlToLoadItem = new();
        private Coroutine _coroutineCheck;
        private const float FreeInterval = 30f;
        private const int FreeThreshold = 200;//达到200个时，开始回收
        private Dictionary<string, string> _textureIdDict;


        protected override void OnInit()
        {
            UIObjectFactory.SetLoaderExtension(typeof(UITextureGLoader));
            //interval check _urlToPoolItem if it need to be recycled
            InitTextureXml();
            _coroutineCheck = Utility.Unity.StartCoroutine(FreeIdleTextures());
        }
        
        protected override void OnRelease()
        {
            Utility.Unity.StopCoroutine(_coroutineCheck);
            foreach (var item in _urlToPoolItem.Values)
            {
                if (item.Handle != null)
                {
                    item.Handle.Release();
                }
                else
                {
                    Object.Destroy(item.RawTexture);
                }
            }

            _urlToPoolItem.Clear();
            _urlToLoadItem.Clear();
        }

        public void LoadTexture(string url, Action<TexturePoolItem> onSuccess, Action<string> onFail)
        {
            //check if it exist in pool
            if (_urlToPoolItem.TryGetValue(url, out var poolItem))
            {
                onSuccess?.Invoke(poolItem);
            }
            else
            {
                //check if it exist in loading
                if (_urlToLoadItem.TryGetValue(url, out var loadItem))
                {
                    loadItem.OnSuccess += onSuccess;
                    loadItem.OnFail += onFail;
                }
                else
                {
                    if (IsWebTexture(url))
                    {
                        //走web请求
                        loadItem = new LoadItem(); 
                        loadItem.OnSuccess += onSuccess;
                        loadItem.OnFail += onFail;
                        loadItem.URL = url;
                        _urlToLoadItem.Add(url, loadItem);
                        LocalFileManager.Instance.GetTexture(url, OnLoadWebTextureCompleted);
                    }
                    else
                    {
                        //走ab
                        var handle = YooAssets.LoadAssetSync<Texture>(url);
                        loadItem = new LoadItem(); 
                        loadItem.OnSuccess += onSuccess;
                        loadItem.OnFail += onFail;
                        loadItem.URL = url;
                        loadItem.Handle = handle;
                        _urlToLoadItem.Add(url, loadItem);
                        handle.Completed += OnLoadTextureCompleted;
                    }
                }
            }
        }

        void OnLoadTextureCompleted(AssetHandle op)
        {
            string url = string.Empty;
            //遍历_urlToLoadItem，找到对应的LoadItem
            foreach (var item in _urlToLoadItem.Values.Where(item => item.Handle == op))
            {
                url = item.URL;
                break;
            }
            if (!_urlToLoadItem.TryGetValue(url, out var loadItem)) return;
            _urlToLoadItem.Remove(url);
            if (op.Status == EOperationStatus.Succeed)
            {
                //add to pool
                var texture = op.GetAssetObject<Texture>();
                var poolItem = new TexturePoolItem
                {
                    URL = url,
                    Texture = new NTexture(texture),
                    Handle = op,
                };
                _urlToPoolItem.Add(url, poolItem);
                loadItem.OnSuccess?.Invoke(poolItem);
            }
            else
            {
                loadItem.OnFail?.Invoke(op.LastError);
                op.Release();
            }
            //remove from _urlToLoadItem
        }

        void OnLoadWebTextureCompleted(string url, Texture texture)
        {
            if (!_urlToLoadItem.TryGetValue(url, out var loadItem)) return;
            _urlToLoadItem.Remove(url);
            if (texture != null)
            {
                //add to pool
                var poolItem = new TexturePoolItem
                {
                    URL = url,
                    Texture = new NTexture(texture),
                    RawTexture = texture
                };
                _urlToPoolItem.Add(url, poolItem);
                loadItem.OnSuccess?.Invoke(poolItem);
            }
            else
            {
                string error = "Load WebTexture Failed:" + url;
                loadItem.OnFail?.Invoke(error);
            }
        }
        
        private IEnumerator FreeIdleTextures()
        {
            var wait = new WaitForSeconds(FreeInterval);
            var needFreeList = new List<string>();
            while (true)
            {
                yield return wait;
                //if(_urlToPoolItem.Count < FreeThreshold) continue;
                foreach (var kv in _urlToPoolItem)
                {
                    var poolItem = kv.Value;
                    if (poolItem.Texture.refCount <= 0)
                    {
                        needFreeList.Add(kv.Key);
                    }
                }

                if (needFreeList.Count > 0)
                {
                    foreach (string url in needFreeList)
                    {
                        var poolItem = _urlToPoolItem[url];
                        if (poolItem.Handle != null)
                        {
                            poolItem.Handle.Release();
                        }
                        else
                        {
                            Object.Destroy(poolItem.RawTexture);
                        }
                        _urlToPoolItem.Remove(url);
                    }
                    needFreeList.Clear();
                }
            }
        }
        
        public bool IsWebTexture(string url)
        {
            return url.StartsWith("http");
        }
        
        /// <summary>
        /// 初始化纹理xml文件，解析出纹理名称和路径的映射关系
        /// </summary>
        private void InitTextureXml()
        {
            _textureIdDict = new Dictionary<string, string>();
            //读取xml文件
            var assetOperationHandle = YooAssets.LoadAssetSync<TextAsset>("TexturePackage");
            //解析xml文件
            var textAsset = assetOperationHandle.GetAssetObject<TextAsset>();
            if(textAsset == null) return;
            var xml = textAsset.text;
            assetOperationHandle.Release();
            //解析xml文件，获取纹理名称和路径的映射关系
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            //获取纹理名称和路径的映射关系
            XmlNode rootNode = xmlDoc.DocumentElement;
            if (rootNode != null)
            {
                string packageId = rootNode.Attributes["id"]?.Value;
                // 获取所有image节点
                XmlNodeList imageNodes = rootNode.SelectNodes("//resources/image");
                if (imageNodes == null || imageNodes.Count == 0)
                {
                    return;
                }
            
                // 创建字典
                foreach (XmlNode imageNode in imageNodes)
                {
                    string imageId = imageNode.Attributes["id"]?.Value;
                    string fileName = imageNode.Attributes["name"]?.Value;
                    if (!string.IsNullOrEmpty(imageId) && !string.IsNullOrEmpty(fileName))
                    {
                        // 使用Path类去除文件后缀
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                        string combinedId = $"ui://{packageId}{imageId}";
                        _textureIdDict.TryAdd(combinedId, fileNameWithoutExtension);
                    }
                }
            }
        }
        
        /// <summary>
        /// 根据纹理url获取纹理名称
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetTextureNameByUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (_textureIdDict.TryGetValue(url, out var nameByUrl))
            {
                return nameByUrl;
            }
            return null;
        }
    }
}