using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HealthPickup : MonoBehaviour
{
    [Header("Health Pickup Settings")]
    [Tooltip("How much health to give to the player")]
    public int healthAmount = 25;

    [Tooltip("Should the pickup destroy itself after being used?")]
    public bool destroyOnPickup = true;

    [Tooltip("Optional pickup sound effect")]
    public AudioClip pickupSound;

    [Tooltip("Volume of the pickup sound")]
    [Range(0f, 1f)]
    public float pickupVolume = 0.7f;

    [Header("Rotation Settings")]
    [Tooltip("Enable or disable rotation")]
    public bool enableRotation = true;

    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 45f;

    private void Reset()
    {
        // Automatically set collider to trigger mode in editor
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Update()
    {
        // Rotate around Y-axis if enabled
        if (enableRotation)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered the trigger has a Character script
        Character character = other.GetComponent<Character>();
        if (character == null)
            return;

        // Don't apply health if already full or dead
        if (IsAtFullHealth(character))
            return;

        // Apply health
        character.ChangeCurrentHealth(healthAmount);

        // Play sound if set
        if (pickupSound)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);

        // Destroy pickup if required
        if (destroyOnPickup)
            Destroy(gameObject);
    }

    private bool IsAtFullHealth(Character character)
    {
        // Use reflection to access currentHealth and maxHealth if private (optional)
        var currentHealthField = typeof(Character).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var maxHealthField = typeof(Character).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (currentHealthField != null && maxHealthField != null)
        {
            float currentHealth = (float)currentHealthField.GetValue(character);
            float maxHealth = (float)maxHealthField.GetValue(character);

            return currentHealth >= maxHealth;
        }

        // If reflection fails, allow pickup anyway
        return false;
    }
}
