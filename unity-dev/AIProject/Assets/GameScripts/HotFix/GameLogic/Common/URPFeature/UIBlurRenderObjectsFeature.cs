using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;

namespace GameLogic
{
    public class UIBlurRenderObjectsFeature : ScriptableRendererFeature
    {

        private string passTag = "UIBlurRenderObjectsFeature";
        private RenderObjectsPass renderObjectsPass;

        public override void Create()
        {
            int layer =1<< UnityEngine.LayerMask.NameToLayer("BUI");
           // string[] shaderTags = new string[] { "SRPDefaultUnlit", "UniversalForward", "UniversalForwardOnly" };
            renderObjectsPass = new RenderObjectsPass(passTag, RenderPassEvent.AfterRenderingTransparents, new string[]{"SRPDefaultUnlit","UniversalForward","UniversalForwardOnly"},
                RenderQueueType.Transparent, layer, new RenderObjects.CustomCameraSettings());
            
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(renderObjectsPass);
        }
        
        
    }
}
