using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirza.VFXToolKit
{
    // Sets live texture as a global shader texture.

    [ExecuteInEditMode]
    [RequireComponent(typeof(MVFXTK_LiveRenderTexture))]
    public class MVFXTK_LiveRenderTextureSetAsGlobalTexture : MonoBehaviour
    {
        MVFXTK_LiveRenderTexture liveRenderTexture;
        public string textureName = "_CameraGlobalTexture";

        void LateUpdate()
        {
            if (!liveRenderTexture)
            {
                liveRenderTexture = GetComponent<MVFXTK_LiveRenderTexture>();
            }

            Shader.SetGlobalTexture(textureName, liveRenderTexture.renderTexture);
        }
    }
}