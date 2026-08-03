using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirza.VFXToolKit
{
    // Reads from live render texture to send to current object's renderer material.

    // Example use: I have a camera rendering backfaces,
    // and I read that texture into the current renderer material to use in that shader,
    // such as for multi-transparency refraction (or something).

    [ExecuteInEditMode]
    public class MVFXTK_LiveRenderTextureReceiveToRendererMaterial : MonoBehaviour
    {
        public MVFXTK_LiveRenderTexture liveRenderTexture;

        Renderer renderer;
        public string textureName = "_MainTex";

        void Update()
        {
            if (!renderer)
            {
                renderer = GetComponent<Renderer>();
            }

            renderer.sharedMaterial.SetTexture(textureName, liveRenderTexture.renderTexture);
        }
    }
}