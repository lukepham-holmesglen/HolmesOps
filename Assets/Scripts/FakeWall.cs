using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.AI;

public class FakeWall : MonoBehaviour
{
    [SerializeField, Min(0.01f)]
    private float fadeDuration = 0.75f;

    private readonly List<Material> materials = new();
    private readonly List<Color> startingColors = new();
    private readonly List<int> colorProperties = new();

    private Renderer[] wallRenderers;
    private Collider[] wallColliders;
    private NavMeshObstacle[] navMeshObstacles;
    private bool isFading;

    private static readonly int BaseColor =
        Shader.PropertyToID("_BaseColor");

    private static readonly int Color =
        Shader.PropertyToID("_Color");

    private void Awake()
    {
        wallRenderers = GetComponentsInChildren<Renderer>();
        wallColliders = GetComponentsInChildren<Collider>();
        navMeshObstacles = GetComponentsInChildren<NavMeshObstacle>();

        foreach (Renderer wallRenderer in wallRenderers)
        {
            foreach (Material material in wallRenderer.materials)
            {
                int colorProperty;

                if (material.HasProperty(BaseColor))
                    colorProperty = BaseColor;
                else if (material.HasProperty(Color))
                    colorProperty = Color;
                else
                    continue;

                materials.Add(material);
                startingColors.Add(material.GetColor(colorProperty));
                colorProperties.Add(colorProperty);
            }
        }
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (!isFading)
            StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        isFading = true;

        foreach (Material material in materials)
            MakeTransparent(material);

        foreach (Collider wallCollider in wallColliders)
            wallCollider.enabled = false;

        foreach (NavMeshObstacle obstacle in navMeshObstacles)
            obstacle.enabled = false;

        foreach (Renderer wallRenderer in wallRenderers)
            wallRenderer.shadowCastingMode = ShadowCastingMode.Off;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float fadeAmount = Mathf.Clamp01(elapsed / fadeDuration);

            for (int i = 0; i < materials.Count; i++)
            {
                Color color = startingColors[i];
                color.a = Mathf.Lerp(startingColors[i].a, 0f, fadeAmount);

                materials[i].SetColor(colorProperties[i], color);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void MakeTransparent(Material material)
    {
        if (!material.HasProperty("_Surface"))
            return;

        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat(
            "_DstBlend",
            (float)BlendMode.OneMinusSrcAlpha
        );
        material.SetFloat("_ZWrite", 0f);

        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        material.renderQueue = (int)RenderQueue.Transparent;
    }
}