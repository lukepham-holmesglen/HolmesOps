using UnityEditor;
using UnityEngine;

namespace Mirza.VFXToolKit
{
    [CustomEditor(typeof(MVFXTK_LiveRenderTexture))]
    public class MVFXTK_LiveRenderTextureEditor : Editor
    {
        //public bool squareAspectRatioPreview;
        private MaterialEditor materialEditor;

        void OnEnable()
        {
            // Subscribe to Editor update for continuous refresh.

            EditorApplication.update += Repaint;
        }

        void OnDisable()
        {
            // Unsubscribe to prevent memory leaks.

            EditorApplication.update -= Repaint;
        }

        public override void OnInspectorGUI()
        {
            // Reference target component.

            MVFXTK_LiveRenderTexture manager = (MVFXTK_LiveRenderTexture)target;

            // Draw default inspector.

            DrawDefaultInspector();

            // Custom inspector.

            //EditorGUILayout.Space();
            //EditorGUILayout.LabelField("Custom Editor", EditorStyles.boldLabel);

            //squareAspectRatioPreview = EditorGUILayout.Toggle("Square Aspect Ratio Preview", squareAspectRatioPreview);

            // Preview if exists.

            if (manager.renderTexture != null)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField($"Render Texture Preview ({manager.renderTexture.width} x {manager.renderTexture.height})", EditorStyles.boldLabel);

                // Keep texture square.

                float renderTextureAspect;

                //if (squareAspectRatioPreview)
                //{
                //    renderTextureAspect = 1.0f;
                //}
                //else
                //{
                // Use the non-overridden resolution for preview.
                // Reason: with an override, the aspectio ratio may be too extreme.

                renderTextureAspect = manager.aspectResolution.x / (float)manager.aspectResolution.y;
                //}

                // Render.

                EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(renderTextureAspect), manager.renderTexture);

            }
            else
            {
                EditorGUILayout.HelpBox("No render texture to preview.", MessageType.Info);
            }

            // If material exists, allow editing and preview.

            if (manager.updateMaterial != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Material Settings", EditorStyles.boldLabel);

                // Check if MaterialEditor needs to be created or updated.

                if (materialEditor == null || materialEditor.target != manager.updateMaterial)
                {
                    materialEditor = (MaterialEditor)CreateEditor(manager.updateMaterial);
                }

                // Render MaterialEditor.

                materialEditor.DrawHeader();
                materialEditor.OnInspectorGUI();

                // Show preview of material.

                Rect materialPreviewRect = GUILayoutUtility.GetRect(100, 100);
                EditorGUI.DrawPreviewTexture(materialPreviewRect, Texture2D.whiteTexture, manager.updateMaterial);
            }
            else
            {
                EditorGUILayout.HelpBox("No update material assigned for editing.", MessageType.Info);
            }
        }
    }
}