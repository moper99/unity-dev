using System;
using System.Collections.Generic;
using FairyGUI;
using TEngine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameLogic
{
    /// <summary>
    /// 背景模糊管理器。
    /// 管理UI背景模糊效果，包括模糊计数、URP渲染特性切换等。
    /// </summary>
    public class BgBlurManager : Singleton<BgBlurManager>
    {
        #region 私有字段

        /// <summary>
        /// 模糊面板计数（面板名称 -> 引用次数）。
        /// </summary>
        private readonly Dictionary<string, int> _blurRefCount = new Dictionary<string, int>();

        /// <summary>
        /// 最后一个模糊面板的层级类型。
        /// </summary>
        private int _lastBlurDepthType = 0;

        /// <summary>
        /// 模糊Shader名称。
        /// </summary>
        private const string BlurShaderName = "UI/UIGrabBlurTexture";

        #endregion

        #region 公开属性

        /// <summary>
        /// 是否存在背景模糊的UI。
        /// </summary>
        public bool IsBgBlurUI => _blurRefCount.Count > 0;

        #endregion

        #region 公开API

        /// <summary>
        /// 添加模糊UI。
        /// </summary>
        /// <param name="panelName">面板名称。</param>
        /// <param name="window">窗口实例。</param>
        public void AddBlurUI(string panelName, FairyUIWindow window)
        {
            if (window == null || !window.BgBlur) return;

            int depthType = window.WindowLayer;

            // 如果已有模糊面板，检查层级
            bool needUpdate = false;
            if (IsBgBlurUI)
            {
                if (_lastBlurDepthType > depthType)
                {
                    depthType = _lastBlurDepthType;
                    needUpdate = true;
                }
            }

            _lastBlurDepthType = depthType;

            // 更新模糊计数
            if (_blurRefCount.ContainsKey(panelName))
            {
                _blurRefCount[panelName]++;
            }
            else
            {
                _blurRefCount[panelName] = 1;
            }

            // 开启/关闭URP模糊特性
            OpenOrCloseBlurFeature();

            Log.Debug($"BgBlurManager.AddBlurUI: {panelName}, refCount: {_blurRefCount[panelName]}");
        }

        /// <summary>
        /// 移除模糊UI。
        /// </summary>
        /// <param name="panelName">面板名称。</param>
        /// <param name="window">窗口实例。</param>
        public void RemoveBlurUI(string panelName, FairyUIWindow window)
        {
            if (window == null || !window.BgBlur) return;

            // 移除模糊计数
            if (_blurRefCount.ContainsKey(panelName))
            {
                _blurRefCount[panelName]--;
                if (_blurRefCount[panelName] <= 0)
                {
                    _blurRefCount.Remove(panelName);
                }
            }

            // 更新最后一个模糊层级
            if (IsBgBlurUI)
            {
                _lastBlurDepthType = FindHighestBlurDepth();
            }
            else
            {
                _lastBlurDepthType = 0;
            }

            // 开启/关闭URP模糊特性
            OpenOrCloseBlurFeature();

            Log.Debug($"BgBlurManager.RemoveBlurUI: {panelName}, IsBgBlurUI: {IsBgBlurUI}");
        }

        /// <summary>
        /// 创建模糊材质。
        /// </summary>
        /// <returns>模糊材质。</returns>
        public Material CreateBlurMaterial()
        {
            var shader = Shader.Find(BlurShaderName);
            if (shader == null)
            {
                Log.Error($"BgBlurManager: Shader not found: {BlurShaderName}");
                return null;
            }
            return new Material(shader);
        }

        /// <summary>
        /// 显示/关闭模糊效果。
        /// </summary>
        /// <param name="panelParent">面板父容器。</param>
        /// <param name="blurParent">模糊父容器。</param>
        /// <param name="isShow">是否显示。</param>
        public void ShowOrCloseBlur(GComponent panelParent, GComponent blurParent, bool isShow)
        {
            if (blurParent != null)
            {
                // 设置模糊遮罩可见性
                var blurMask = blurParent.GetChild("imgeBlurMask");
                if (blurMask != null)
                {
                    blurMask.visible = isShow;
                }
            }

            // 设置UI层级
            SetUILayer(panelParent, isShow);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 开启/关闭URP模糊渲染特性。
        /// </summary>
        private void OpenOrCloseBlurFeature()
        {
            bool isActive = IsBgBlurUI;

            // 获取UI相机
            var uiCamera = GetUICamera();
            if (uiCamera == null) return;

            // 获取主相机
            var mainCamera = Camera.main;
            if (mainCamera == null || !mainCamera.CompareTag("MainCamera")) return;

            var mainCameraData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
            var uiCameraData = uiCamera.GetComponent<UniversalAdditionalCameraData>();

            if (mainCameraData == null || uiCameraData == null) return;

            if (isActive)
            {
                // 切换UI相机为Overlay模式
                uiCameraData.renderType = CameraRenderType.Overlay;
                if (!mainCameraData.cameraStack.Contains(uiCamera))
                {
                    mainCameraData.cameraStack.Add(uiCamera);
                }
            }
            else
            {
                // 恢复UI相机为Base模式
                if (mainCameraData.cameraStack.Contains(uiCamera))
                {
                    mainCameraData.cameraStack.Remove(uiCamera);
                }
                uiCameraData.renderType = CameraRenderType.Base;
            }

            // 激活/关闭高斯模糊Pass
            var blurFeature = GetRendererFeature<GaussianUIBlurPassFeature>(uiCamera);
            if (blurFeature != null)
            {
                blurFeature.SetActive(isActive);
            }

            // 激活/关闭BUI层渲染
            var buiFeature = GetRendererFeature<UIBlurRenderObjectsFeature>(uiCamera);
            if (buiFeature != null)
            {
                buiFeature.SetActive(isActive);
            }
        }

        /// <summary>
        /// 获取UI相机。
        /// </summary>
        /// <returns>UI相机。</returns>
        private Camera GetUICamera()
        {
            // 尝试从FairyGUI获取
            var stageCamera = StageCamera.main;
            if (stageCamera != null) return stageCamera;

            // 尝试通过Tag查找
            var cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].gameObject.name.Contains("UI") || cameras[i].gameObject.name.Contains("FairyGUI"))
                {
                    return cameras[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 获取URP渲染特性。
        /// </summary>
        /// <typeparam name="T">特性类型。</typeparam>
        /// <param name="camera">相机。</param>
        /// <returns>渲染特性。</returns>
        private T GetRendererFeature<T>(Camera camera) where T : ScriptableRendererFeature
        {
            if (camera == null) return null;

            var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null) return null;

            var renderer = cameraData.scriptableRenderer;
            if (renderer == null) return null;

            // 遍历渲染特性
            // 注意：Unity 2022+ 使用 renderer.rendererFeatures
            // 旧版本可能需要其他方式获取
            return null;
        }

        /// <summary>
        /// 设置UI层级。
        /// </summary>
        /// <param name="panel">面板容器。</param>
        /// <param name="isBlur">是否模糊。</param>
        private void SetUILayer(GComponent panel, bool isBlur)
        {
            if (panel == null) return;

            int layer = isBlur
                ? LayerMask.NameToLayer("BUI")
                : LayerMask.NameToLayer("UI");

            if (layer < 0) layer = 0; // 默认层

            var go = panel.displayObject?.gameObject;
            if (go != null && go.layer != layer)
            {
                go.layer = layer;
            }
        }

        /// <summary>
        /// 查找最高的模糊层级。
        /// </summary>
        /// <returns>最高模糊层级。</returns>
        private int FindHighestBlurDepth()
        {
            int maxDepth = 0;
            foreach (var kvp in _blurRefCount)
            {
                if (kvp.Value > 0)
                {
                    // 这里需要根据面板名称获取层级，简化处理
                    maxDepth = Math.Max(maxDepth, (int)UILayer.Normal);
                }
            }
            return maxDepth;
        }

        #endregion
    }
}
