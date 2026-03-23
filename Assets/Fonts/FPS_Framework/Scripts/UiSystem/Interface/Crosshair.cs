using UnityEngine;

/// <summary>
/// Crosshair.
/// </summary>
public class Crosshair : Element
{
    #region FIELDS SERIALIZED

    [Header("Settings")]
    
    [Tooltip("Visibility changing smoothness.")]
    [SerializeField]
    private float smoothing = 8.0f;

    [Tooltip("Minimum scale the Crosshair needs in order to be visible. Useful to avoid weird tiny images.")]
    [SerializeField]
    private float minimumScale = 0.15f;

    [Header("Fallback Settings")]
    [Tooltip("Show crosshair when player character reference is missing.")]
    [SerializeField]
    private bool showWhenNoPlayer = true;

    #endregion

    #region FIELDS
    
    /// <summary>
    /// Current.
    /// </summary>
    private float current = 1.0f;
    /// <summary>
    /// Target.
    /// </summary>
    private float target = 1.0f;

    /// <summary>
    /// Rect.
    /// </summary>
    private RectTransform rectTransform;

    #endregion
    
    #region UNITY
    
    protected override void Awake()
    {
        //Base.
        base.Awake();

        //Cache Rect Transform.
        rectTransform = GetComponent<RectTransform>();
    }

    #endregion
    
    #region METHODS
    
    protected override void Tick()
    {
        // Check if we have a valid player character
        bool visible = showWhenNoPlayer; // Default based on fallback setting
        
        if (playerCharacter != null)
        {
            // Use the player's crosshair visibility method
            visible = playerCharacter.IsCrosshairVisible();
        }
        
        // Update Target.
        target = visible ? 1.0f : 0.0f;

        // Interpolate Current.
        current = Mathf.Lerp(current, target, Time.deltaTime * smoothing);
        // Scale.
        rectTransform.localScale = Vector3.one * current;
        
        // Hide Crosshair Objects When Too Small.
        for (var i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(current > minimumScale);
    }
    
    #endregion
}