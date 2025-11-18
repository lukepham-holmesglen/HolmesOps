using UnityEngine;

[CreateAssetMenu(fileName = "LightingProfile", menuName = "Environment/Lighting Profile")]
public class LightingProfile : ScriptableObject
{
    [Header("Environment Lighting")]
    public Color skyColor = Color.gray;
    public Color equatorColor = Color.gray;
    public Color groundColor = Color.gray;
    public Material skyboxMaterial;
    public float ambientIntensity = 1f;

    [Header("Fog Settings")]
    public bool fogEnabled = true;
    public FogMode fogMode = FogMode.Linear;
    public Color fogColor = Color.gray;
    public float fogDensity = 0.01f;
    public float fogStartDistance = 0f;
    public float fogEndDistance = 300f;

    [Header("Reflection Settings")]
    [Range(0f, 1f)] public float reflectionIntensity = 1f;
}
