using System;
using System.Collections;
using TEngine;
using UnityEngine;
using UnityEngine.Networking;

namespace GameLogic
{
    public class WebFileDownloader
    {
        public static DownloadManager.DownloadFinishDelegate FinishedDelegate;
        private UnityWebRequest _currentRequest;
        private Action<string> _pendingCallback;
        private string _url;
        private int _retryCount;//重试次数
        private Coroutine _coDownload;

        public void DownloadFile(string url, Action<string> callback)
        {
            _pendingCallback += callback;
            _url = url;
            _retryCount = 0;
            _coDownload = Utility.Unity.StartCoroutine(DownloadRoutine());
        }

        public void StopDownload()
        {
            Utility.Unity.StopCoroutine(_coDownload);
            if (_currentRequest != null)
            {
                _currentRequest.Abort();
                _pendingCallback = null;
                _currentRequest = null;
            }
            // FinishedDelegate.Invoke(_url);
        }

        public void AddCallback(Action<string> callback)
        {
            _pendingCallback += callback;
        }
        
        private IEnumerator DownloadRoutine()
        {
            while (_retryCount < 3)
            {
                using (_currentRequest = UnityWebRequest.Get(_url))
                {
                    yield return _currentRequest.SendWebRequest();
                    if (_currentRequest.result != UnityWebRequest.Result.Success)
                    {
                        _retryCount++;
                        continue;
                    }
                    var savePath = $"{LocalFileManager.FilePath}/{GetFileNameFromUrl(_url)}";
                    System.IO.File.WriteAllBytes(savePath, _currentRequest.downloadHandler.data);
                    OnSucceed(savePath);
                    yield break;
                }
            }
            OnFailed();
            _currentRequest = null;
        }

        private string GetFileNameFromUrl(string url)
        {
            var uri = new Uri(url);
            var fileName = System.IO.Path.GetFileName(uri.LocalPath);
            return fileName;
        }

        private void OnSucceed(string savePath)
        {
            FinishedDelegate.Invoke(_url);
            if (_pendingCallback != null)
            {
                var delayedCallback = _pendingCallback;
                _pendingCallback = null;
                delayedCallback.Invoke(savePath);
            }
        }

        private void OnFailed()
        {
            Debug.LogError($"Error downloading file: {_url}");
            FinishedDelegate.Invoke(_url);
            if (_pendingCallback != null)
            {
                var delayedCallback = _pendingCallback;
                _pendingCallback = null;
                delayedCallback.Invoke(string.Empty);
            }
        }
    }
}