using UnityEngine;

namespace Mirza.AERO
{
    // This script updates the volumetric fog material with information about
    // the number of additional lights in the scene and ambient lighting.

    // This is old. Use FullscreenCustomLightingFeature instead.

    [ExecuteAlways]
    public class VolumetricFogController : MonoBehaviour
    {
        public Material material;
        public int additionalLightCountBase;

        void Start()
        {

        }

        void Update()
        {
            int additionalLightCount = additionalLightCountBase;
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type != LightType.Directional)
                {
                    additionalLightCount++;
                }
            }

            // Need to loop framecount, else interleaved gradient noise becomes erratic.

            material.SetInteger("_FrameCount", Time.renderedFrameCount % 60);
            material.SetInteger("_AdditionalLightCount", additionalLightCount);

            Color ambientLighting = RenderSettings.ambientLight * RenderSettings.ambientIntensity;

            material.SetColor("_AmbientLighting", ambientLighting);
        }
    }
}
