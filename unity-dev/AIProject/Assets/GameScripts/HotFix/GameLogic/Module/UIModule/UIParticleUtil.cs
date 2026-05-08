using System;
using System.Collections;
using FairyGUI;
using TEngine;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace GameLogic
{
    /// <summary>
    /// UI粒子特效工具类。
    /// 提供粒子加载、播放、重播、缩放适配等功能。
    /// </summary>
    public static class UIParticleUtil
    {
        /// <summary>
        /// 异步加载粒子特效到GGraph。
        /// </summary>
        /// <param name="path">资源路径（YooAsset地址）。</param>
        /// <param name="graph">目标GGraph组件。</param>
        /// <param name="timeEndCallBack">粒子播放完成回调。</param>
        /// <param name="loadCallBack">资源加载完成回调。</param>
        /// <returns>资源操作句柄。</returns>
        public static AssetHandle LoadParticle(string path, GGraph graph, Action timeEndCallBack = null, Action loadCallBack = null)
        {
            var handle = YooAssets.LoadAssetSync<GameObject>(path);
            GameObject goEffect = handle.InstantiateSync();
            goEffect.transform.localPosition = Vector3.zero;
            AdaptParticle(graph, goEffect.transform);

            GoWrapper wrapper = new GoWrapper { name = path };
            wrapper.SetWrapTarget(goEffect, true);

            if (timeEndCallBack != null)
            {
                ParticleTimeEndHandler timeEndHandler = goEffect.AddComponent<ParticleTimeEndHandler>();
                timeEndHandler.Callback = timeEndCallBack;
            }

            graph.SetNativeObject(wrapper);

            loadCallBack?.Invoke();
            return handle;
        }

        /// <summary>
        /// 重播已有粒子特效。
        /// </summary>
        /// <param name="graph">目标GGraph组件。</param>
        /// <param name="timeEndCallBack">粒子播放完成回调。</param>
        public static void ReplayParticle(GGraph graph, Action timeEndCallBack = null)
        {
            if (graph.displayObject is GoWrapper wrapperOld && wrapperOld.wrapTarget != null)
            {
                if (timeEndCallBack != null)
                {
                    ParticleTimeEndHandler handler = wrapperOld.wrapTarget.GetComponent<ParticleTimeEndHandler>();
                    if (handler != null) Object.Destroy(handler);
                    handler = wrapperOld.wrapTarget.AddComponent<ParticleTimeEndHandler>();
                    handler.Callback = timeEndCallBack;
                }

                var trans = wrapperOld.wrapTarget.transform;
                PlayParticle(trans);
                AdaptParticle(graph, trans);
            }
        }

        /// <summary>
        /// 获取粒子系统总时长。
        /// </summary>
        /// <param name="transform">粒子根节点。</param>
        /// <returns>粒子时长（秒）。</returns>
        public static float GetParticleDuration(Transform transform)
        {
            ParticleSystem[] particleSystems = transform.GetComponentsInChildren<ParticleSystem>();
            float maxDuration = 0;

            foreach (ParticleSystem ps in particleSystems)
            {
                if (ps.emission.enabled)
                {
                    ParticleSystem.MainModule main = ps.main;
                    if (ps.main.loop)
                    {
                        return main.duration;
                    }

                    float duration = 0f;
                    if (ps.emission.rateOverTime.constant <= 0f)
                    {
                        duration = main.startDelay.constant + main.startLifetime.constant;
                    }
                    else
                    {
                        duration = main.startDelay.constant + Mathf.Max(main.duration, main.startLifetime.constant);
                    }

                    if (duration > maxDuration)
                    {
                        maxDuration = duration;
                    }
                }
            }

            return maxDuration;
        }

        /// <summary>
        /// 适配粒子缩放（处理翻转情况）。
        /// </summary>
        /// <param name="graph">GGraph组件。</param>
        /// <param name="transform">粒子根节点。</param>
        public static void AdaptParticle(GGraph graph, Transform transform)
        {
            var scale = graph.scaleX; // 可能存在左右翻转的情况
            transform.localScale = new Vector3(scale, Mathf.Abs(scale), Mathf.Abs(scale));
        }

        /// <summary>
        /// 播放粒子特效。
        /// </summary>
        /// <param name="transform">粒子根节点。</param>
        /// <param name="time">起始时间（秒）。</param>
        public static void PlayParticle(Transform transform, float time = 0f)
        {
            ParticleSystem[] particleSystems = transform.GetComponentsInChildren<ParticleSystem>(true);
            if (particleSystems == null) return;

            foreach (ParticleSystem particle in particleSystems)
            {
                particle.Simulate(time);
                particle.Play();
            }
        }
    }

    /// <summary>
    /// 粒子播放结束处理器。
    /// 在粒子播放完毕后触发回调。
    /// </summary>
    public class ParticleTimeEndHandler : MonoBehaviour
    {
        /// <summary>
        /// 播放完成回调。
        /// </summary>
        public Action Callback;

        private float _duration;

        private void OnEnable()
        {
            _duration = UIParticleUtil.GetParticleDuration(transform);
            StartCoroutine(HandleParticleWhenEnd());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private IEnumerator HandleParticleWhenEnd()
        {
            yield return new WaitForSeconds(_duration);
            Callback?.Invoke();
        }
    }
}
