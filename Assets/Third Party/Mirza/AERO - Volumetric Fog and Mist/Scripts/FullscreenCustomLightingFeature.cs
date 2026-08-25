using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Mirza.AERO
{
    // Send lighting data/information to material.
    // Useful for custom fullscreen shaders that need lighting data, such as volumetric fog/mist.

    public class FullscreenCustomLightingFeature : ScriptableRendererFeature
    {
        // 'AfterRenderingTransparents' is the default.
        // After that, light count may not be accurate/correct.

        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material material;

        CustomPass countPass;

        public override void Create()
        {
            countPass = new CustomPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (material == null)
            {
                return;
            }

            countPass.renderPassEvent = renderPassEvent;
            countPass.material = material;

            renderer.EnqueuePass(countPass);
        }

        // ...

        class CustomPass : ScriptableRenderPass
        {
            static readonly int additionalLightCountId = Shader.PropertyToID("_AdditionalLightCount");
            static readonly int ambientLightingId = Shader.PropertyToID("_AmbientLighting");

            public Material material;

            // Material value is CPU-side, so it is written during recording, not render func.

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                // Ambient lighting is global render setting, so it needs no per-camera data.

                Color ambientLighting = RenderSettings.ambientLight * RenderSettings.ambientIntensity;

                material.SetColor(ambientLightingId, ambientLighting);

                // Additional light count is the visible set for this camera.

                if (!frameData.Contains<UniversalLightData>())
                {
                    return;
                }

                UniversalLightData lightData = frameData.Get<UniversalLightData>();
                material.SetInteger(additionalLightCountId, lightData.additionalLightsCount);
            }
        }
    }
}