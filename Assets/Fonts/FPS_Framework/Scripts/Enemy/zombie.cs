using UnityEngine;

public class Zombie : EnemyBehaviour
{
    [Header("Zombie Settings")]
    [SerializeField] private ZombieAIStateMachine zombieAI;
    [SerializeField] private Animator animator;
    [SerializeField] private RagdollController ragdollController;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int currentHealth;

    [Header("Collision Settings")]
    [SerializeField] private bool disableCollisionOnDeath = true;
    [SerializeField] private float collisionDisableDelay = 0.1f;

    [Header("Impact Animation Settings")]
    [SerializeField] private bool useImpactAnimations = true;
    [SerializeField] private string[] impactAnimations = { "Hit1", "Hit2", "Stagger" };
    [SerializeField] private float impactAnimationBlendTime = 0.1f;
    [SerializeField] private string impactLayerName = "Upper Body";

    [Header("Audio")]
    [SerializeField] private AudioClip[] hurtSounds;
    [SerializeField] private AudioClip[] deathSounds;
    [SerializeField] private float audioVolume = 0.8f;

    [Header("Death Settings")]
    [SerializeField] private float deathCleanupDelay = 3f; // Time before removing from spawn manager

    // Private variables
    private SpawnManager spawnMan;
    private bool isDead = false;
    private bool hasNotifiedSpawnManager = false; // Prevent double notification
    private Collider[] allColliders;
    private int impactLayerIndex = -1;
    private AudioSource audioSource;
    private string selectedImpactAnimation;

    // Animation parameter hashes
    private static readonly int HashImpactTrigger = Animator.StringToHash("Impact");
    private static readonly int HashDeath = Animator.StringToHash("Death");

    protected override void Awake()
    {
        // Auto-find components if not assigned
        if (zombieAI == null)
            zombieAI = GetComponent<ZombieAIStateMachine>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (ragdollController == null)
            ragdollController = GetComponent<RagdollController>();

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = audioVolume;
        audioSource.spatialBlend = 1.0f; // 3D audio

        // Cache colliders
        allColliders = GetComponentsInChildren<Collider>();

        // Initialize health
        currentHealth = maxHealth;

        // Randomize impact animation
        if (impactAnimations.Length > 0)
        {
            selectedImpactAnimation = impactAnimations[Random.Range(0, impactAnimations.Length)];
        }
        else
        {
            selectedImpactAnimation = "Hit";
        }
    }

    protected override void Start()
    {
        spawnMan = GameMan.Instance.spawnManInstance;

        // Find impact animation layer
        if (animator != null && useImpactAnimations)
        {
            impactLayerIndex = animator.GetLayerIndex(impactLayerName);
            if (impactLayerIndex == -1)
            {
                //Debug.LogWarning($"Impact layer '{impactLayerName}' not found on {gameObject.name}. Impact animations will not play.");
                useImpactAnimations = false;
            }
        }

        //Debug.Log($"Zombie {gameObject.name} spawned with {currentHealth}/{maxHealth} health");
    }

    public override void GetShot()
    {
        if (isDead) return;

        // Take damage
        TakeDamage(1);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // Play hurt sound (only if alive)
        if (hurtSounds.Length > 0 && Random.value < 0.7f && !isDead)
        {
            PlayRandomSound(hurtSounds);
        }

        // Play impact animation (only if alive)
        if (useImpactAnimations && animator != null && !isDead)
        {
            PlayImpactAnimation();
        }

        // Check for death
        if (currentHealth <= 0)
        {
            HandleDeath();
        }
        else
        {
            //Debug.Log($"Zombie {gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        }
    }

    private void PlayImpactAnimation()
    {
        if (impactLayerIndex == -1 || animator == null) return;

        // Don't interrupt attack animations
        if (zombieAI != null && zombieAI.GetCurrentState() == ZombieAIStateMachine.ZombieState.Attacking)
            return;

        // Check if animator has impact trigger
        bool hasImpactTrigger = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == "Impact" && param.type == AnimatorControllerParameterType.Trigger)
            {
                hasImpactTrigger = true;
                break;
            }
        }

        if (hasImpactTrigger)
        {
            animator.SetTrigger(HashImpactTrigger);
        }
        else
        {
            // Direct animation play as fallback
            animator.CrossFade(selectedImpactAnimation, impactAnimationBlendTime, impactLayerIndex);
        }

        //Debug.Log($"Playing impact animation: {selectedImpactAnimation} on zombie {gameObject.name}");
    }

    private void HandleDeath()
    {
        if (isDead) return;

        isDead = true;

        //Debug.Log($"=== ZOMBIE {gameObject.name} DIED ===");

        // CRITICAL: Immediately notify spawn manager for wave progression
        NotifySpawnManagerOfDeath();

        // Set AI state to dying FIRST (this stops all other sounds via ZombieAIStateMachine)
        if (zombieAI != null)
        {
            zombieAI.SetState(ZombieAIStateMachine.ZombieState.Dying);
        }

        // FORCE STOP any currently playing audio and play death sound immediately
        if (audioSource != null)
        {
            audioSource.Stop(); // Stop any current audio immediately

            // Play death sound with highest priority
            if (deathSounds.Length > 0)
            {
                AudioClip deathClip = deathSounds[Random.Range(0, deathSounds.Length)];
                audioSource.clip = deathClip; // Set as main clip for priority
                audioSource.Play(); // Use Play() instead of PlayOneShot() for higher priority
                //Debug.Log($"Playing death sound: {deathClip.name} for zombie {gameObject.name}");
            }
        }

        // Disable collision with player
        if (disableCollisionOnDeath)
        {
            StartCoroutine(DisablePlayerCollisionAfterDelay());
        }

        // Activate ragdoll if available
        if (ragdollController != null)
        {
            //Debug.Log($"Using ragdoll death for zombie {gameObject.name}");
            ragdollController.OnEnemyDeath();

            // For ragdoll death, start cleanup timer since animation won't call Die()
            StartCoroutine(CleanupAfterDelay());
        }
        else
        {
            //Debug.Log($"Using animation death for zombie {gameObject.name}");
            // Fallback to animation-based death - Die() will be called by animation
            if (animator != null)
            {
                animator.SetBool(HashDeath, true);
                animator.SetTrigger("Die");
            }
        }
    }

    private void NotifySpawnManagerOfDeath()
    {
        if (hasNotifiedSpawnManager) return;

        hasNotifiedSpawnManager = true;

        if (spawnMan != null)
        {
            //Debug.Log($"Notifying SpawnManager of zombie death: {gameObject.name}");
            spawnMan.DestroyEnemy(gameObject);
        }
        else
        {
            //Debug.LogError($"SpawnManager is null for zombie {gameObject.name}!");
        }
    }

    private System.Collections.IEnumerator CleanupAfterDelay()
    {
        //Debug.Log($"Starting cleanup timer for zombie {gameObject.name} ({deathCleanupDelay} seconds)");
        yield return new WaitForSeconds(deathCleanupDelay);

        //Debug.Log($"Cleanup timer finished for zombie {gameObject.name} - destroying GameObject");
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator DisablePlayerCollisionAfterDelay()
    {
        yield return new WaitForSeconds(collisionDisableDelay);

        GameObject player = GameMan.Instance?.playerInstance;
        if (player == null) yield break;

        Collider[] playerColliders = player.GetComponentsInChildren<Collider>();

        foreach (Collider enemyCollider in allColliders)
        {
            if (enemyCollider == null) continue;

            foreach (Collider playerCollider in playerColliders)
            {
                if (playerCollider == null) continue;

                Physics.IgnoreCollision(enemyCollider, playerCollider, true);
            }
        }

        //Debug.Log($"Disabled collision between dead zombie {gameObject.name} and player");
    }

    private void PlayRandomSound(AudioClip[] clips)
    {
        // Don't play random sounds if dead (death sounds are handled separately)
        if (isDead || clips.Length == 0 || audioSource == null) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        audioSource.PlayOneShot(clip, audioVolume);
    }

    // Method called by animation events or ragdoll system
    public void Die()
    {
        //Debug.Log($"Die() called by animation for zombie {gameObject.name}");

        // This method is for final cleanup - just destroy the GameObject
        // SpawnManager was already notified in HandleDeath()
        //Debug.Log($"Final cleanup: destroying zombie GameObject: {gameObject.name}");
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    // Method for projectiles to register hit information
    public void RegisterProjectileHit(Vector3 hitDirection, float hitForce, Vector3 hitPoint)
    {
        if (ragdollController != null && !isDead)
        {
            ragdollController.RegisterHit(hitDirection, hitForce, hitPoint);
        }
    }

    // Public methods for external access
    public bool IsDead() => isDead;
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;

    // Method to heal the zombie (useful for special abilities or testing)
    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        //Debug.Log($"Zombie {gameObject.name} healed for {amount}. Health: {currentHealth}/{maxHealth}");
    }

    // Method to set custom health values
    public void SetHealth(int newMaxHealth, int newCurrentHealth = -1)
    {
        maxHealth = newMaxHealth;
        currentHealth = newCurrentHealth == -1 ? newMaxHealth : Mathf.Min(newCurrentHealth, newMaxHealth);
    }

    // Method to manually disable all collisions
    public void DisableAllCollisions()
    {
        foreach (Collider col in allColliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    // Method to re-enable collisions (useful for object pooling)
    public void EnableAllCollisions()
    {
        foreach (Collider col in allColliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }
    }

    // Method to reset zombie state (useful for object pooling)
    public void ResetZombie()
    {
        isDead = false;
        hasNotifiedSpawnManager = false;
        currentHealth = maxHealth;
        EnableAllCollisions();

        if (zombieAI != null)
        {
            zombieAI.SetState(ZombieAIStateMachine.ZombieState.Idle);
        }

        if (animator != null)
        {
            animator.SetBool(HashDeath, false);
            animator.Rebind();
        }

        if (ragdollController != null)
        {
            ragdollController.DeactivateRagdoll();
        }

        // Reset audio
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
    }

    // Context menu methods for testing
    [ContextMenu("Test Take Damage")]
    public void TestTakeDamage()
    {
        TakeDamage(1);
    }

    [ContextMenu("Test Kill Zombie")]
    public void TestKillZombie()
    {
        TakeDamage(currentHealth);
    }

    [ContextMenu("Test Impact Animation")]
    public void TestImpactAnimation()
    {
        if (!isDead)
        {
            PlayImpactAnimation();
        }
    }

    [ContextMenu("Test Reset Zombie")]
    public void TestResetZombie()
    {
        ResetZombie();
    }

    [ContextMenu("Test Death Sound")]
    public void TestDeathSound()
    {
        if (deathSounds.Length > 0 && audioSource != null)
        {
            audioSource.Stop();
            AudioClip deathClip = deathSounds[Random.Range(0, deathSounds.Length)];
            audioSource.clip = deathClip;
            audioSource.Play();
            //Debug.Log($"Testing death sound: {deathClip.name}");
        }
    }

    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        if (zombieAI != null)
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, zombieAI.detectRange);

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, zombieAI.attackRange);

            // Draw roaming range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, zombieAI.roamingRange);
        }
    }
}
