using UnityEngine;

public class Weapon : WeaponBehaviour
{
    #region Variables

    [SerializeField]
    public RuntimeAnimatorController controller;
    [Tooltip("Is this weapon automatic? If yes, then holding down the firing button will continuously fire.")]
    [SerializeField]
    private bool automatic;
    [Tooltip("Amount of shots this weapon can shoot in a minute. It determines how fast the weapon shoots.")]
    [SerializeField]
    private int roundsPerMinute = 200;
    private Transform muzzlePos;
    [Tooltip("Maximum distance at which this weapon can fire accurately. Shots beyond this distance will not use linetracing for accuracy.")]
    [SerializeField]
    private float maximumDistance = 500.0f;
    [Tooltip("Mask of things recognized when firing.")]
    [SerializeField]
    private LayerMask mask;
    [Tooltip("Projectile Prefab. This is the prefab spawned when the weapon shoots.")]
    [SerializeField]
    private GameObject projectilePrefab;
    [Tooltip("How fast the projectiles are.")]
    [SerializeField]
    private float projectileImpulse = 400.0f;
    [SerializeField]
    private MagazineBehaviour magazineBehaviour;
    private WeaponAttachmentManagerBehaviour attachmentManager;
    private CharacterBehaviour characterOwner;

    [SerializeField]
    private Sprite weaponSprite;
    private Animator animator;
    private int ammunitionCurrent;
    [SerializeField]
    private Transform playerCamera;

    [Header("Muzzle Flash")]
    [Tooltip("Muzzle component for visual and audio effects.")]
    [SerializeField]
    private Muzzle muzzle;

    [Header("Casing Ejection")]
    [Tooltip("Transform that represents the weapon's ejection port.")]
    [SerializeField]
    private Transform socketEjection;
    [Tooltip("Casing Prefab.")]
    [SerializeField]
    private GameObject prefabCasing;

    [Header("Audio Clips Holster")]
    [Tooltip("Holster Audio Clip.")]
    [SerializeField]
    private AudioClip audioClipHolster;
    [Tooltip("Unholster Audio Clip.")]
    [SerializeField]
    private AudioClip audioClipUnholster;

    [Header("Audio Clips Reloads")]
    [Tooltip("Reload Audio Clip.")]
    [SerializeField]
    private AudioClip audioClipReload;
    [Tooltip("Reload Empty Audio Clip.")]
    [SerializeField]
    private AudioClip audioClipReloadEmpty;

    [Header("Audio Clips Other")]
    [Tooltip("AudioClip played when this weapon is fired without any ammunition.")]
    [SerializeField]
    private AudioClip audioClipFireEmpty;

    // Audio source for weapon sounds
    private AudioSource audioSource;

    #endregion

    #region GETTERS
    public override int GetAmmunitionCurrent() => ammunitionCurrent;
    public override int GetAmmunitionTotal() => magazineBehaviour.GetAmmunitionTotal();
    public override Sprite GetWeaponSprite() => weaponSprite;
    public override bool IsFull() => ammunitionCurrent == magazineBehaviour.GetAmmunitionTotal();
    public override Animator GetAnimator() => animator;
    public override RuntimeAnimatorController GetAnimatorController() => controller;
    public override float GetRateOfFire() => roundsPerMinute;
    public override bool HasAmmunition() => ammunitionCurrent > 0;
    public override bool IsAutomatic() => automatic;
    public override WeaponAttachmentManagerBehaviour GetAttachmentManager() => attachmentManager;

    // Audio getters
    public AudioClip GetAudioClipHolster() => audioClipHolster;
    public AudioClip GetAudioClipUnholster() => audioClipUnholster;
    public AudioClip GetAudioClipReload() => audioClipReload;
    public AudioClip GetAudioClipReloadEmpty() => audioClipReloadEmpty;
    public AudioClip GetAudioClipFireEmpty() => audioClipFireEmpty;
    #endregion

    #region Unity Stuff
    protected override void Awake()
    {
        animator = GetComponent<Animator>();
        attachmentManager = GetComponent<WeaponAttachmentManagerBehaviour>();
        
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;
        // Auto-find muzzle if not assigned
        if (muzzle == null)
        {
            muzzle = GetComponent<Muzzle>();
        }
    }

    protected override void Start()
    {
        magazineBehaviour = attachmentManager.GetEquippedMagazine();
        muzzlePos = attachmentManager.GetEquippedMuzzlePos();
        
        // Start with full ammunition
        ammunitionCurrent = magazineBehaviour.GetAmmunitionTotal();
        
        // Update UI to show correct ammo count
        if (GameMan.Instance != null && GameMan.Instance.gameUIInstance != null)
        {
            GameMan.Instance.gameUIInstance.UpdateAmmoCount(ammunitionCurrent);
        }
        
        //Debug.Log($"Weapon started with {ammunitionCurrent}/{magazineBehaviour.GetAmmunitionTotal()} ammunition");
    }
    #endregion

    #region Functions
    // Method to test casing ejection (for debugging)
    [ContextMenu("Test Casing Ejection")]
    public void TestCasingEjection()
    {
        //Debug.Log("Testing casing ejection manually");
        EjectCasing();
    }

    public override void EjectCasing()
    {
        // Spawn casing prefab at ejection port
        if (prefabCasing != null && socketEjection != null)
        {
         //   //Debug.Log("Ejecting casing");
            GameObject casing = Instantiate(prefabCasing, socketEjection.position, socketEjection.rotation);
            
            // Make sure the casing has the CasingScript component
            if (casing.GetComponent<CasingScript>() == null)
            {
          //      //Debug.LogWarning("Casing prefab doesn't have CasingScript component!");
            }
        }
        else
        {
            if (prefabCasing == null)
            Debug.LogWarning("Casing prefab is null!");
            if (socketEjection == null)
                Debug.LogWarning("Socket ejection is null!");
        }
    }

    public override void FillAmmunition(int amount)
    {
        ammunitionCurrent = amount != 0 ? Mathf.Clamp(ammunitionCurrent + amount, 0, GetAmmunitionTotal()) : magazineBehaviour.GetAmmunitionTotal();
        GameMan.Instance.gameUIInstance.UpdateAmmoCount(ammunitionCurrent);
    }

    public override void Fire(float spreadMultiplier = 1)
{
    if (playerCamera == null) return;
    if (!HasAmmunition()) 
    {
        // Play empty fire sound
        PlayAudioClip(audioClipFireEmpty);
        return;
    }

    // Cache muzzle position before any effects
    Vector3 cachedMuzzlePosition;
    if (muzzle != null && muzzle.GetSocket() != null)
    {
        cachedMuzzlePosition = muzzle.GetSocket().position;
    }
    else
    {
        cachedMuzzlePosition = muzzlePos.position;
    }

    // Calculate rotation (exact copy of old working system)
    Quaternion rotation = Quaternion.LookRotation(playerCamera.forward * 1000.0f - cachedMuzzlePosition);
    
    if (Physics.Raycast(new Ray(playerCamera.position, playerCamera.forward),
        out RaycastHit hit, maximumDistance, mask))
        rotation = Quaternion.LookRotation(hit.point - cachedMuzzlePosition);
        
    // Spawn projectile
    GameObject projectile = Instantiate(projectilePrefab, cachedMuzzlePosition, rotation);
    projectile.GetComponent<Rigidbody>().linearVelocity = projectile.transform.forward * projectileImpulse;
    
    // Ignore collision with owner
    Collider ownerCollider = characterOwner.GetComponent<Collider>();
    Collider projectileCollider = projectile.GetComponent<Collider>();
    if (ownerCollider != null && projectileCollider != null)
    {
        Physics.IgnoreCollision(ownerCollider, projectileCollider);
    }

    // Update ammunition and UI
    ammunitionCurrent = Mathf.Clamp(ammunitionCurrent - 1, 0, magazineBehaviour.GetAmmunitionTotal());
    GameMan.Instance.gameUIInstance.UpdateAmmoCount(ammunitionCurrent);

    // Play firing animation
    animator.Play("Fire", 0, 0.0f);

    // EJECT CASING - Add this line
    EjectCasing();

    // Play muzzle flash effect
    if (muzzle != null)
    {
        StartCoroutine(PlayMuzzleFlashDelayed());
    }
}
    
    private System.Collections.IEnumerator PlayMuzzleFlashDelayed()
    {
        yield return null; // Wait one frame
        muzzle.PlayMuzzleFlash();
    }

    public override void SetOwner(CharacterBehaviour newOwner)
    {
        characterOwner = newOwner;
    }

    public override void Reload()
    {
        // Play appropriate reload animation
        string animationName = HasAmmunition() ? "Reload" : "Reload Empty";
        animator.Play(animationName, 0, 0.0f);

        // Play appropriate reload sound
        AudioClip reloadSound = HasAmmunition() ? audioClipReload : audioClipReloadEmpty;
        PlayAudioClip(reloadSound);
    }

    // Method to play empty fire sound (called by Character script)
    public void PlayEmptyFireSound()
    {
        //Debug.Log("Playing empty fire sound");
        PlayAudioClip(audioClipFireEmpty);
    }

    // Method to play holster sound
    public void PlayHolsterSound()
    {
        PlayAudioClip(audioClipHolster);
    }

    // Method to play unholster sound
    public void PlayUnholsterSound()
    {
        PlayAudioClip(audioClipUnholster);
    }

    // Helper method to play audio clips
    private void PlayAudioClip(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            //Debug.Log($"Playing audio clip: {clip.name}");
            audioSource.PlayOneShot(clip);
        }
        else
        {
            if (clip == null)
                //Debug.LogWarning("Audio clip is null!");
            if (audioSource == null)
                Debug.LogWarning("AudioSource is null!");
        }
    }

    // Additional method to get muzzle reference (useful for other systems)
    public Muzzle GetMuzzle()
    {
        return muzzle;
    }

    // Method to manually set muzzle (useful for weapon customization)
    public void SetMuzzle(Muzzle newMuzzle)
    {
        muzzle = newMuzzle;
    }

    #endregion
}