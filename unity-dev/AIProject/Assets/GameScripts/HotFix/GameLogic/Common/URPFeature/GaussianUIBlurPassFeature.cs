using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Profiling;

namespace GameLogic
{
    public class GaussianUIBlurPassFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class HLSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

            // public Material mat;
            public float alpha = 0.5f;
            public float blurSize = 3.0f;

            public Color multiplyColor = Color.white;
            //目标RenderTexture 
            // public RenderTexture GrabTexTexture = null;
            // public RenderTexture BlurTexture = null;
        }

        public HLSettings settings = new HLSettings();


        GaussianUIBlurRenderPass m_ScriptablePass;

        public override void Create()
        {
            // settings.alpha= Mathf.Clamp(settings.alpha, 0, 1.0f);
            m_ScriptablePass = new GaussianUIBlurRenderPass("GaussianUIBlurRender", settings);
        }


        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            if (camera.cameraType!=CameraType.Game)
            {
   
                return;
            }

            /*if (camera.CompareTag("MainCamera"))
            {
                if (HasFullScreenPanel())
                {
                    return;
                }
            }*/

            m_ScriptablePass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(m_ScriptablePass);
        }

        class GaussianUIBlurRenderPass : ScriptableRenderPass
        {
            private Material mMat;
            private RenderTargetIdentifier source { get; set; }

            RenderTargetHandle m_BlurColorTexture1;

            RenderTargetHandle m_BlurColorTexture2;

            private HLSettings settings;
            private ProfilingSampler m_ProflingSampler;
            string m_ProfilerTag;

            int _grabTexID, _grabBlurTexID,blurRT1,blurRT2;
            private float _rtRot = 0.5f;

            public GaussianUIBlurRenderPass(string passname, GaussianUIBlurPassFeature.HLSettings _settings)
            {
                settings = _settings;
                m_ProfilerTag = passname;
                m_ProflingSampler = new ProfilingSampler(passname);
                this.renderPassEvent = settings.renderPassEvent;
                Shader shader = ShaderManager.GetShader("Custom/GaussianUIBlur");
                //Debug.Log("------SetBgBlurMaterial--GaussianUIBlurRenderPass-:"+shader);
                mMat = new Material(shader);
                // mMat.SetFloat("_BlurPower",0.05f);
                mMat.SetFloat("_BlurSize", settings.blurSize);
                mMat.SetFloat("_Alpha", settings.alpha);
                mMat.SetColor("_MultiplyColor", settings.multiplyColor);

                _grabTexID = Shader.PropertyToID("_GrabTex");
                _grabBlurTexID = Shader.PropertyToID("_GrabBlurTex");

                blurRT1 = Shader.PropertyToID("_BlurRT1");
                blurRT2 = Shader.PropertyToID("_BlurRT2");
                // Debug.Log("GaussianUIBlurRenderPass---------:"+_rtRot+"--index:"+index);
            }

            public void Setup(RenderTargetIdentifier src)
            {
                int index = QualitySettings.GetQualityLevel();
                if (index == 0)
                {
                    _rtRot =0.5f;
                }else if (index == 1)
                {
                    _rtRot =0.8f;
                }else if (index == 2)
                {
                    _rtRot =1.0f;
                }
                
                this.source = src;
                
               // Debug.Log("GaussianUIBlurRenderPass---------:"+_rtRot+"--index:"+index);
            }


            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
                using (new ProfilingScope(cmd, m_ProflingSampler))
                {
                    var camera_target_handle = renderingData.cameraData.renderer.cameraColorTarget;
                    RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                    // opaqueDesc.depthBufferBits = 0;
                    

                    int width = (int)(opaqueDesc.width * _rtRot);
                    int height = (int)(opaqueDesc.height * _rtRot);

                    cmd.GetTemporaryRT(blurRT1, width, height, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(blurRT2, width, height, 0, FilterMode.Bilinear);

                    Blit(cmd, source, blurRT1, mMat, 0);

                    Blit(cmd, blurRT1, blurRT2, mMat, 1);

                    // Blit(cmd,blurRT2, settings.BlurTexture);
                    cmd.SetGlobalTexture(_grabBlurTexID, blurRT2);

                    CoreUtils.SetRenderTarget(cmd, camera_target_handle);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(blurRT1);
                cmd.ReleaseTemporaryRT(blurRT2);
            }
        }
    }
}