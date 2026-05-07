using System;
using System.IO;
using System.Threading.Tasks;
using GameLogic;
using UnityEngine;

namespace GameLogic
{
    public class LocalFileManager: Singleton<LocalFileManager>
    {
        public static string FilePath => $"{Application.persistentDataPath}/WebFiles/";
        
        /// <summary>
        /// 获取图片
        /// </summary>
        public void GetTexture(string url, Action<string, Texture> callback)
        {
            var savePath = $"{FilePath}/{GetFileNameFromUrl(url)}";
            if (File.Exists(savePath))
            {
                InternalGetTexture(url, savePath, callback);
            }
            else
            {
                DownloadManager.Instance.DownloadFile(url, (path) =>
                {
                    InternalGetTexture(url, path, callback);
                });
            }
        }

        void InternalGetTexture(string url, string path, Action<string, Texture> callback)
        {
            if (!string.IsNullOrEmpty(path))
            {
                byte[] imageData = File.ReadAllBytes(path);
                // 创建新的空纹理
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                // 加载PNG图片的字节数据到纹理中
                bool success = texture.LoadImage(imageData);
                if (success)
                {
                    callback(url, texture);
                    return;
                }
            }
            callback(url, null);
            Debug.LogError("can't get texture: " + url);
        }
        
        private string GetFileNameFromUrl(string url)
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);
            return fileName;
        }
        
        public bool IsWebFileExistInLocal(string url)
        {
            var savePath = $"{FilePath}/{GetFileNameFromUrl(url)}";
            return File.Exists(savePath);
        }

        public  async Task<string> SaveFileTolocal(string filename,byte[] bytes)
        {
             string savePath = $"{FilePath}{filename}";
             await File.WriteAllBytesAsync(savePath, bytes);
             return savePath;
        }
    }
}