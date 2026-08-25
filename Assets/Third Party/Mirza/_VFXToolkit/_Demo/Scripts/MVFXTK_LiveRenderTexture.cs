using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Rendering;

namespace Mirza.VFXToolKit
{
    [ExecuteInEditMode]
    public class MVFXTK_LiveRenderTexture : MonoBehaviour
    {
        [System.Serializable]
        public struct SendToMaterialData
        {
            public Material material;
            public string textureName;
        }

        // Do not assign anything to these in the editor,
        // they're managed entirely by the script.

        [Header("READ ONLY")]
        [Space]

        public RenderTexture renderTexture;
        public CustomRenderTexture customRenderTexture;

        // Aspect resolution is used by the custom editor to preview the render texture (prevent extreme aspect ratios).
        // The actual resolution for the texture may be different if an override is set.

        [Space]

        public Vector2Int aspectResolution;

        [Header("Read/Write")]
        [Space]

        public bool isCustomRenderTexture;

        [Space]

        [Range(1, 32)]
        public int downsampleLevel = 1;

        [Space]

        [Range(0.0f, 2.0f)]
        public float widthScale = 1.0f;

        [Range(0.0f, 2.0f)]
        public float heightScale = 1.0f;

        [Space]

        public FilterMode filterMode = FilterMode.Point;
        public GraphicsFormat renderTextureFormat = GraphicsFormat.R16G16B16A16_SFloat;

        [Space]

        public bool useMipMap;

        [Space]

        public Camera camera;

        [Space]

        public Material updateMaterial;

        // State change requiring refresh.

        Component previousThisComponent;

        void Awake()
        {
            // If new object (which may be a copy and carry over textures),
            // don't destroy textures, but ensure references are null so new ones will be created for THIS component,
            // while not destroying the old ones which would otherwise force the other component to recreate them, too.

            renderTexture = null;
            customRenderTexture = null;

            //print($"{name} - Nulling textures.");
        }
        void Start()
        {

        }

        void DestroyTextures()
        {
            // Not sure if release is necessary before destroying.
            // CustomRenderTexture is first (just in case) since a CRT is always a RT, but an RT isn't always a CRT.

            // Remove before releasing/destroying to prevent an annoying error message.

            if (camera)
            {
                camera.targetTexture = null;
            }

            // OBLIDERATE.

            if (customRenderTexture)
            {
                customRenderTexture.Release();
                DestroyImmediate(customRenderTexture);
            }
            if (renderTexture)
            {
                renderTexture.Release();
                DestroyImmediate(renderTexture);
            }

            //print($"{name} - Destroying textures ({textures} / 2).");
        }

        void OnDestroy()
        {
            DestroyTextures();
        }

        public Vector2Int GetDownsampledResolution()
        {
            return new Vector2Int(Screen.width / downsampleLevel, Screen.height / downsampleLevel);
        }

        void Update()
        {
            // Update resolution.

            aspectResolution = GetDownsampledResolution();
            Vector2Int resolution = aspectResolution;

            resolution.x = Mathf.FloorToInt(resolution.x * widthScale);
            resolution.y = Mathf.FloorToInt(resolution.y * heightScale);

            resolution.x = Mathf.Max(1, resolution.x);
            resolution.y = Mathf.Max(1, resolution.y);

            // Create/refresh render texture if needed.

            bool destroyTextures = !renderTexture;

            destroyTextures = destroyTextures || previousThisComponent != this;
            destroyTextures = destroyTextures || renderTexture.width != resolution.x || renderTexture.height != resolution.y;

            destroyTextures = destroyTextures || renderTexture.graphicsFormat != renderTextureFormat;

            destroyTextures = destroyTextures || renderTexture.useMipMap != useMipMap;

            // Need new render texture if custom render texture is toggled and current render texture is not a custom render texture.

            if (isCustomRenderTexture)
            {
                destroyTextures = destroyTextures || renderTexture != customRenderTexture;
            }
            else
            {
                destroyTextures = destroyTextures || customRenderTexture != null;
            }

            if (destroyTextures)
            {
                DestroyTextures();

                // Force editor to update so editor slots reflect internal state.

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.SceneView.RepaintAll();
#endif
            }

            //if (renderTexture)
            //{
            //    if (renderTexture.width != resolution.x || renderTexture.height != resolution.y)
            //    {
            //        renderTexture.Release();

            //        renderTexture.width = resolution.x;
            //        renderTexture.height = resolution.y;

            //        renderTexture.Create();
            //    }
            //}

            // Create if null.

            if (!renderTexture)
            {
                //print($"{name} - Creating texture.");

                if (!isCustomRenderTexture)
                {
                    renderTexture = new RenderTexture(resolution.x, resolution.y, 16, renderTextureFormat, 0)
                    {
                        useMipMap = useMipMap,
                        autoGenerateMips = useMipMap
                    };
                }
                else
                {
                    customRenderTexture = new CustomRenderTexture(resolution.x, resolution.y, renderTextureFormat);

                    renderTexture = customRenderTexture;
                }

                //print($"{name} - Textures created.");
            }

            // Update render texture settings.

            renderTexture.name = name;
            renderTexture.filterMode = filterMode;

            if (customRenderTexture)
            {
                customRenderTexture.material = updateMaterial;
                customRenderTexture.updateMode = CustomRenderTextureUpdateMode.Realtime;
            }

            // Set as target for camera.

            if (camera)
            {
                camera.targetTexture = renderTexture;
            }

            // State change requiring refresh.

            previousThisComponent = this;
        }
    }
}