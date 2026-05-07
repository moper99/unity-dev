using System;
using UnityEngine;
using FairyGUI;

namespace GameLogic
{
    public class UITextureGLoader : GLoader
    {

        public Action LoadSuccessCallback;
        
        private string _url;
        private string _tempUrl;
        //重载icon属性，当设置url时，判断其资源是否需要下载，如果需要下载，那么先下载，然后再设置icon
        public override string icon
        {
            get => _url;
            set
            {
                if (_url == value)
                    return;
                _tempUrl = value;
                if (UITextureManager.Instance.IsWebTexture(value) &&
                    !LocalFileManager.Instance.IsWebFileExistInLocal(value))
                {
                    UITextureManager.Instance.LoadTexture(value, poolItem =>
                    {
                        if (isDisposed || string.IsNullOrEmpty(_tempUrl) || poolItem.URL != _tempUrl)
                            return;
                        url = _tempUrl;
                    }, null);
                }
                else
                {
                    url = value;
                }
            }
        }
        
        protected override void LoadExternal()
        {
            UITextureManager.Instance.LoadTexture(url, OnLoadSuccess, OnLoadFail);
        }

        protected override void FreeExternal(NTexture tex)
        {
            // tex.refCount--; TODO @yangjianwei 这里不需要减少引用计数，因为在UITextureManager中会自动释放。（先修复bug，待测试验证）
        }

        void OnLoadSuccess(UITextureManager.TexturePoolItem poolItem)
        {
            //由于是异步加载，在加载完成后，可能已经不需要这个图片了
            if (isDisposed || string.IsNullOrEmpty(url) || poolItem.URL != url)
                return;
            onExternalLoadSuccess(poolItem.Texture);
            LoadSuccessCallback?.Invoke();
        }

        void OnLoadFail(string error)
        {
            Debug.Log($"load texture {url} failed:{error}");
            onExternalLoadFailed();
        }

        protected override void LoadContent()
        {
            ClearContent();
        
            if (string.IsNullOrEmpty(url))
                return;
        
            var textureName = UITextureManager.Instance.GetTextureNameByUrl(url);
            if (textureName != null)
                url = textureName;
            else if (url.StartsWith(UIPackage.URL_PREFIX))
                LoadFromPackage(url);
            else
                LoadExternal();
        }
    }
}