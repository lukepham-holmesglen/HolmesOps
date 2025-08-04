using UnityEngine;

public class DamageZoneUI : MonoBehaviour
{
    [Tooltip("Child object to show/hide during damage (e.g., red overlay)")]
    public GameObject overlayObject;

    private void Start()
    {
        // Assign this UI to any DamageZone that hasn't got one
        DamageZone[] zones = FindObjectsOfType<DamageZone>();
        foreach (DamageZone zone in zones)
        {
            if (zone.damageUI == null)
                zone.SetDamageUI(this);
        }

        // Start hidden
        if (overlayObject != null)
            overlayObject.SetActive(false);
    }

    public void ShowOverlay(bool show)
    {
        if (overlayObject != null)
            overlayObject.SetActive(show);
    }
}
