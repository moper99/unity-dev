using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GameLogic
{
    public class DownloadManager: Singleton<DownloadManager>
    {
        public delegate void DownloadFinishDelegate(string url);
        
        private Dictionary<string, WebFileDownloader> _dictDownloader;

        private const long MaxFileSize = 200 * 1024 * 1024;//200m
        

        protected override void OnInit()
        {
            CheckStorage();
            _dictDownloader = new Dictionary<string, WebFileDownloader>();
            WebFileDownloader.FinishedDelegate += DownloadFinished;
        }

        protected override void OnRelease()
        {
            foreach (var downloader in _dictDownloader.Values)
            {
                downloader.StopDownload();
            }
            _dictDownloader = null;
        }
        
        public void DownloadFile(string url, Action<string> callback)
        {
            if(_dictDownloader.TryGetValue(url, out var downloader))
            {
                downloader.AddCallback(callback);
            }
            else
            {
                downloader = new WebFileDownloader();
                downloader.DownloadFile(url, callback);
            }
        }

        void DownloadFinished(string url)
        {
            if (_dictDownloader == null) return;
            if (_dictDownloader.ContainsKey(url))
            {
                _dictDownloader.Remove(url);
            }
        }
        
        //简单策略：每次启动应用的时候检查，如果文件大小超过200m，就把最久下载的文件删除，直到大小小为200m的30%
        private void CheckStorage()
        {
            if (!Directory.Exists(LocalFileManager.FilePath))
            {
                Directory.CreateDirectory(LocalFileManager.FilePath);
                return;
            }
            var dir = new DirectoryInfo(LocalFileManager.FilePath);
            var files = dir.GetFiles();
            if (files.Length > 0)
            {
                long totalSize = 0;
                foreach (var file in files)
                {
                    totalSize += file.Length;
                }

                if (totalSize > MaxFileSize)
                {
                    var remainSize = MaxFileSize * 0.3f;
                    var sortedFiles = files.OrderBy(f => f.LastWriteTime).ToList();
                    while (totalSize > remainSize)
                    {
                        var file = sortedFiles[0];
                        totalSize -= file.Length;
                        file.Delete();
                        sortedFiles.RemoveAt(0);
                    }
                }
            }
        }
        
    }

}