using UnityEngine;

public class Enemy : EnemyBehaviour
{
    private SpawnManager spawnMan;
    [SerializeField] private AIStateMachine aiState;
    [SerializeField] private Animator animator;
    [SerializeField] private RagdollController ragdollController;
    
    [Header("Collision Settings")]
    [SerializeField] private bool disableCollisionOnDeath = true;
    [SerializeField] private float collisionDisableDelay = 0.1f;
    
    [Header("Impact Animation Settings")]
    [SerializeField] private bool useImpactAnimations = true;
    [SerializeField] private string impactAnimationName = "Impact";
    [SerializeField] private float impactAnimationBlendTime = 0.1f;
    [SerializeField] private string impactLayerName = "Upper Body";
    
    [Header("Death Settings")]
    [SerializeField] private float deathCleanupDelay = 3f; // Time before destroying GameObject
    
    private int health = 3;
    private bool isDead = false;
    private bool hasNotifiedSpawnManager = false; // Prevent double notification
    private Collider[] allColliders;
    private int impactLayerIndex = -1;
    
    // Animation parameter hash for performance
    private static readonly int HashImpactTrigger = Animator.StringToHash("Impact");

    protected override void Start()
    {
        spawnMan = GameMan.Instance.spawnManInstance;
        
        // Auto-find ragdoll controller if not assigned
        if (ragdollController == null)
        {
            ragdollController = GetComponent<RagdollController>();
        }
        
        // Cache all colliders for efficient access
        allColliders = GetComponentsInChildren<Collider>();
        
        // Find the impact animation layer index
        if (animator != null && useImpactAnimations)
        {
            impactLayerIndex = animator.GetLayerIndex(impactLayerName);
            if (impactLayerIndex == -1)
            {
                Debug.LogWarning($"Impact layer '{impactLayerName}' not found on {gameObject.name}. Impact animations will not play.");
                useImpactAnimations = false;
            }
        }
        
        Debug.Log($"Enemy {gameObject.name} initialized with {health} health");
    }

    public override void GetShot()
    {
        // Don't process shots if already dead
        if (isDead) return;
        
        Debug.Log($"Enemy {gameObject.name} got shot! Health: {health-1}");
        
        // Play impact animation before applying damage
        if (useImpactAnimations && animator != null && !isDead)
        {
            PlayImpactAnimation();
        }
        
        health--;
        if (health <= 0)
        {
            HandleDeath();
        }
    }
    
    private void PlayImpactAnimation()
    {
        if (impactLayerIndex == -1) return;
        
        // Method 1: Using animator trigger parameter (requires setup in Animator Controller)
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
            // Method 2: Direct animation play (fallback)
            animator.CrossFade(impactAnimationName, impactAnimationBlendTime, impactLayerIndex);
        }
        
        if (Application.isEditor && Debug.isDebugBuild)
        {
            Debug.Log($"Playing impact animation: {impactAnimationName} on layer {impactLayerIndex}");
        }
    }
    
    private void HandleDeath()
    {
        // Mark as dead immediately to prevent multiple death calls
        isDead = true;
        
        Debug.Log($"=== ENEMY {gameObject.name} DIED ===");
        
        // CRITICAL: Immediately notify spawn manager for wave progression
        NotifySpawnManagerOfDeath();
        
        // Set AI state to dying first
        if (aiState != null)
        {
            aiState.SetState(AIStateMachine.AIState.Dying);
        }
        
        // Disable collision with player if requested
        if (disableCollisionOnDeath)
        {
            StartCoroutine(DisablePlayerCollisionAfterDelay());
        }
        
        // Activate ragdoll if available, otherwise use animation
        if (ragdollController != null)
        {
            Debug.Log($"Using ragdoll death for enemy {gameObject.name}");
            ragdollController.OnEnemyDeath();
            
            // For ragdoll death, start cleanup timer since animation won't call Die()
            StartCoroutine(CleanupAfterDelay());
        }
        else
        {
            Debug.Log($"Using animation death for enemy {gameObject.name}");
            // Original animation-based death - Die() will be called by animation
            if (animator != null)
            {
                animator.SetBool("Death", true);
            }
        }
    }
    
    private void NotifySpawnManagerOfDeath()
    {
        if (hasNotifiedSpawnManager) return;
        
        hasNotifiedSpawnManager = true;
        
        if (spawnMan != null)
        {
            Debug.Log($"Notifying SpawnManager of enemy death: {gameObject.name}");
            spawnMan.DestroyEnemy(gameObject);
        }
        else
        {
            Debug.LogError($"SpawnManager is null for enemy {gameObject.name}!");
        }
    }
    
    private System.Collections.IEnumerator CleanupAfterDelay()
    {
        Debug.Log($"Starting cleanup timer for enemy {gameObject.name} ({deathCleanupDelay} seconds)");
        yield return new WaitForSeconds(deathCleanupDelay);
        
        Debug.Log($"Cleanup timer finished for enemy {gameObject.name} - destroying GameObject");
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
    
    private System.Collections.IEnumerator DisablePlayerCollisionAfterDelay()
    {
        // Wait a small delay to let ragdoll initialize
        yield return new WaitForSeconds(collisionDisableDelay);
        
        // Find the player
        GameObject player = GameMan.Instance?.playerInstance;
        if (player == null) yield break;
        
        // Get all player colliders
        Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
        
        // Disable collision between all enemy colliders and all player colliders
        foreach (Collider enemyCollider in allColliders)
        {
            if (enemyCollider == null) continue;
            
            foreach (Collider playerCollider in playerColliders)
            {
                if (playerCollider == null) continue;
                
                Physics.IgnoreCollision(enemyCollider, playerCollider, true);
            }
        }
        
        Debug.Log($"Disabled collision between dead enemy {gameObject.name} and player");
    }

    // This is the original Die method that gets called by the animation system
    public void Die()
    {
        Debug.Log($"Die() called by animation for enemy {gameObject.name}");
        
        // This method is for final cleanup - just destroy the GameObject
        // SpawnManager was already notified in HandleDeath()
        Debug.Log($"Final cleanup: destroying enemy GameObject: {gameObject.name}");
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
    
    // Public method to check if dead
    public bool IsDead()
    {
        return isDead;
    }
    
    // Method to manually trigger impact animation (useful for testing)
    [ContextMenu("Test Impact Animation")]
    public void TestImpactAnimation()
    {
        if (!isDead)
        {
            PlayImpactAnimation();
        }
    }
    
    // Method to manually disable all collisions (useful for cleanup)
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
        isDead = false;
        hasNotifiedSpawnManager = false;
        health = 3; // Reset health
    }
}