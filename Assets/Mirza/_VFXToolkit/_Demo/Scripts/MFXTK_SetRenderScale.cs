using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering.Universal;

namespace Mirza.VFXToolKit
{
    [ExecuteAlways]
    public class MFXTK_SetRenderScale : MonoBehaviour
    {
        [Range(0.25f, 2.0f)]
        public float renderScale = 1.0f;

        public bool executeInEditMode;

        UniversalRenderPipelineAsset urpAsset;

        void Start()
        {
            urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        }

        void Update()
        {
            if (!executeInEditMode && !Application.isPlaying)
            {
                return;
            }

            urpAsset.renderScale = renderScale;
        }

        public void SetRenderScale(float value)
        {
            renderScale = value;
        }
    }
}