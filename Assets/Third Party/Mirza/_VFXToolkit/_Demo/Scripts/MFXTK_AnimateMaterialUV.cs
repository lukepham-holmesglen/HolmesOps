using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirza.VFXToolKit
{
    public class MFXTK_AnimateMaterialUV : MonoBehaviour
    {
        Material material;

        public Vector2 animation = new(0.0f, 0.5f);
        public string propertyName = "_MainTex";

        [Space]

        public bool sharedMaterial;
        Vector2 startOffset;

        void Start()
        {
            if (!sharedMaterial)
            {
                material = GetComponent<Renderer>().material;
            }
            else
            {
                material = GetComponent<Renderer>().sharedMaterial;
            }

            startOffset = material.GetTextureOffset(propertyName);
        }

        void OnDisable()
        {
            material.SetTextureOffset(propertyName, startOffset);
        }

        void Update()
        {
            Vector2 offset = material.GetTextureOffset(propertyName);
            offset += animation * Time.deltaTime;

            material.SetTextureOffset(propertyName, offset);
        }
    }
}