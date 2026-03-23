using UnityEngine;

public class Muzzle : MonoBehaviour
{
    [Header("Muzzle Settings")]
    [Tooltip("Socket at the tip of the muzzle (firing point).")]
    [SerializeField] private Transform socket;

    [Header("Muzzle Flash")]
    [Tooltip("Muzzle flash particle effect prefab.")]
    public GameObject muzzleFlashPrefab;

    [Tooltip("How long to keep the muzzle flash alive.")]
    [SerializeField] private float flashDuration = 2f;

    [Header("Audio")]
    [Tooltip("Fire sound effect.")]
    [SerializeField] private AudioClip fireSound;

    [Tooltip("Fire sound volume.")]
    [SerializeField] private float fireVolume = 0.7f;

    private AudioSource audioSource;

    private void Awake()
    {
        SetupAudio();
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = fireVolume;
    }

    public void PlayMuzzleFlash()
    {
        // Spawn muzzle flash effect
        if (muzzleFlashPrefab != null && socket != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, socket.position, socket.rotation);
            Destroy(flash, flashDuration);
        }

        // Play fire sound
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound, fireVolume);
        }
    }

    public Transform GetSocket() => socket;
    public Vector3 GetMuzzlePosition() => socket != null ? socket.position : transform.position;
    public Vector3 GetMuzzleDirection() => socket != null ? socket.forward : transform.forward;
}