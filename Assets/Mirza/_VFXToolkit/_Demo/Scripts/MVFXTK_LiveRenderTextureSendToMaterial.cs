using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirza.VFXToolKit
{
    // Sends (assigns) the dynamic/live texture on the current game object to a material.
    // Example use: a live render texture from a camera is used to send to a blur custom render texture material.

    [ExecuteInEditMode]
    [RequireComponent(typeof(MVFXTK_LiveRenderTexture))]
    public class MVFXTK_LiveRenderTextureSendToMaterial : MonoBehaviour
    {
        MVFXTK_LiveRenderTexture liveRenderTexture;

        public Material material;
        public string textureName = "_MainTex";

        void SendToMaterial(Material material, string textureName)
        {
            if (material.HasProperty(textureName))
            {
                material.SetTexture(textureName, liveRenderTexture.renderTexture);
            }
            else
            {
                Debug.LogWarning($"Material does not have texture property named {textureName}.");
            }
        }

        void LateUpdate()
        {
            if (!liveRenderTexture)
            {
                liveRenderTexture = GetComponent<MVFXTK_LiveRenderTexture>();
            }

            SendToMaterial(material, textureName);
        }
    }
}