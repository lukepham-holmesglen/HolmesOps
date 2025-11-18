using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ApplyLightingProfile(LightingProfile profile)
    {
        if (profile == null) return;

        // Ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = profile.skyColor;
        RenderSettings.ambientEquatorColor = profile.equatorColor;
        RenderSettings.ambientGroundColor = profile.groundColor;
        RenderSettings.ambientIntensity = profile.ambientIntensity;

        // Skybox
        if (profile.skyboxMaterial != null)
            RenderSettings.skybox = profile.skyboxMaterial;

        // Fog
        RenderSettings.fog = profile.fogEnabled;
        RenderSettings.fogMode = profile.fogMode;
        RenderSettings.fogColor = profile.fogColor;
        RenderSettings.fogDensity = profile.fogDensity;
        RenderSettings.fogStartDistance = profile.fogStartDistance;
        RenderSettings.fogEndDistance = profile.fogEndDistance;

        // Reflection settings
        RenderSettings.reflectionIntensity = profile.reflectionIntensity;

        // Update reflections immediately
        DynamicGI.UpdateEnvironment();
    }
}
