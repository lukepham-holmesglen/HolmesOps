using UnityEngine;

using UnityEngine.Rendering.Universal;

namespace Mirza.VFXToolKit
{
    // This script copies *almost* all settings from the target camera.
    // Target texture and culling mask are preserved for specific rendering purposes.

    // It's typically used for that -> same camera view, different render and texture.

    [ExecuteAlways]
    public class MVFXTK_CameraCopyFrom : MonoBehaviour
    {
        Camera camera;
        public Camera target;

        public float priorityOffset = -1;

        [Space]

        public bool copyBackgroundType = true;

        void LateUpdate()
        {
            if (!camera)
            {
                camera = GetComponent<Camera>();
            }

            RenderTexture targetTexture = camera.targetTexture;
            int cullingMask = camera.cullingMask;

            CameraClearFlags backgroundType = camera.clearFlags;
            Color backgroundColour = camera.backgroundColor;

            // ...

            camera.CopyFrom(target);

            camera.depth += priorityOffset;

            camera.targetTexture = targetTexture;
            camera.cullingMask = cullingMask;

            if (!copyBackgroundType)
            {
                camera.clearFlags = backgroundType;
                camera.backgroundColor = backgroundColour;
            }
        }
    }
}