using UnityEngine;
using System.Collections;

public class Projectile : MonoBehaviour
{
    private GameObject myOwner;
    private Rigidbody rigidBody;

    [Range(5, 100)]
    [Tooltip("After how long time should the bullet prefab be destroyed?")]
    public float lifeTime = 10f;
    public bool destroyOnImpact = true;
    
    [Header("Hit Detection")]
    [Tooltip("Use continuous collision detection for fast projectiles")]
    [SerializeField] private bool useContinuousCollision = true;
    [Tooltip("Alternative raycast-based hit detection for very fast projectiles")]
    [SerializeField] private bool useRaycastHitDetection = true;
    [Tooltip("Layers that the projectile can hit")]
    [SerializeField] private LayerMask hitLayers = -1;
    [Tooltip("Minimum speed required for raycast hit detection")]
    [SerializeField] private float minSpeedForRaycast = 10f;

    [Header("Impact particle effects")]
    public Transform[] concreteImpactPrefabs;
    public Transform[] bloodImpactPrefabs;
    
    [Header("Ragdoll Integration")]
    [SerializeField] private float impactForce = 100f;
    
    [Header("Debug")]
    [SerializeField] private bool debugHitDetection = false;
    
    // Raycast hit detection variables
    private Vector3 lastPosition;
    private bool hasHit = false;
    private bool isDestroying = false;
    
    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        
        // Setup collision detection mode for fast projectiles
        if (useContinuousCollision)
        {
            rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        
        StartCoroutine(DestroyAfter());
        lastPosition = transform.position;
    }
    
    void FixedUpdate()
    {
        // Raycast-based hit detection for very fast projectiles
        if (useRaycastHitDetection && !hasHit && !isDestroying)
        {
            float currentSpeed = rigidBody.linearVelocity.magnitude;
            if (currentSpeed >= minSpeedForRaycast)
            {
                RaycastHitDetection();
            }
        }
        
        lastPosition = transform.position;
    }
    
    private void RaycastHitDetection()
    {
        Vector3 direction = transform.position - lastPosition;
        float distance = direction.magnitude;
        
        if (distance > 0.01f) // Only check if we've moved enough
        {
            Ray ray = new Ray(lastPosition, direction.normalized);
            RaycastHit[] hits = Physics.RaycastAll(ray, distance, hitLayers);
            
            if (debugHitDetection)
            {
                Debug.DrawRay(lastPosition, direction, Color.yellow, 0.1f);
                Debug.Log($"Raycast hit detection: {hits.Length} hits found");
            }
            
            // Sort hits by distance to process closest first
            System.Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));
            
            foreach (RaycastHit hit in hits)
            {
                // Skip if hit our owner
                if (hit.collider.gameObject == myOwner)
                    continue;
                    
                // Skip if hit owner's children
                if (myOwner != null && hit.collider.transform.IsChildOf(myOwner.transform))
                    continue;
                    
                // Skip other projectiles
                if (hit.collider.GetComponent<Projectile>() != null)
                    continue;
                
                if (debugHitDetection)
                {
                    Debug.Log($"Raycast hit: {hit.collider.name} with tag {hit.collider.tag}");
                }
                
                // Process the hit
                ProcessHit(hit.collider, hit.point, hit.normal);
                return;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit || isDestroying) return; // Prevent multiple hits
        
        if (collision.gameObject.GetComponent<Projectile>() != null)
            return;
        if (collision.gameObject == myOwner)
            return;
        
        // Skip if hit owner's children
        if (myOwner != null && collision.transform.IsChildOf(myOwner.transform))
            return;

        if (debugHitDetection)
        {
            Debug.Log($"Collision hit: {collision.gameObject.name} with tag {collision.gameObject.tag}");
        }

        // IMMEDIATELY mark as hit and destroying to prevent bouncing
        hasHit = true;
        isDestroying = true;
        
        // Stop the projectile immediately
        if (rigidBody != null)
        {
            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            rigidBody.isKinematic = true;
        }
        
        // Disable the collider to prevent further collisions
        Collider projectileCollider = GetComponent<Collider>();
        if (projectileCollider != null)
        {
            projectileCollider.enabled = false;
        }

        // Process the hit
        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        Vector3 hitNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : -transform.forward;
        
        ProcessHit(collision.collider, hitPoint, hitNormal);
    }
    
    private void ProcessHit(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (debugHitDetection)
        {
            Debug.Log($"Processing hit on: {hitCollider.name} with tag: {hitCollider.tag}");
        }
        
        // Calculate impact information for ragdoll
        Vector3 impactDirection = rigidBody.linearVelocity.normalized;
        if (impactDirection == Vector3.zero)
        {
            impactDirection = transform.forward;
        }
        
        float calculatedForce = impactForce;

        // Handle different surface types
        if (hitCollider.CompareTag("Concrete"))
        {
            SpawnImpactEffect(hitPoint, hitNormal);
        }
        else if (hitCollider.CompareTag("Enemy"))
        {
            // Try to find Enemy component first (for existing enemies)
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy == null)
            {
                enemy = hitCollider.GetComponentInParent<Enemy>();
            }
            
            // If no Enemy component found, try Zombie component
            Zombie zombie = null;
            if (enemy == null)
            {
                zombie = hitCollider.GetComponent<Zombie>();
                if (zombie == null)
                {
                    zombie = hitCollider.GetComponentInParent<Zombie>();
                }
            }
            
            if (enemy != null)
            {
                if (debugHitDetection)
                {
                    Debug.Log($"Hit enemy: {enemy.name}, Is dead: {enemy.IsDead()}");
                }
                
                // Only process hit if enemy is not already dead
                if (!enemy.IsDead())
                {
                    // Find the closest point on the character mesh for impact effect
                    Vector3 meshHitPoint = GetMeshSurfacePoint(enemy.gameObject, hitPoint);
                    
                    enemy.RegisterProjectileHit(impactDirection, calculatedForce, hitPoint);
                    enemy.GetShot();
                    
                    // Spawn impact effect on the mesh surface
                    SpawnBloodEffect(meshHitPoint, -impactDirection);
                }
            }
            else if (zombie != null)
            {
                if (debugHitDetection)
                {
                    Debug.Log($"Hit zombie: {zombie.name}, Is dead: {zombie.IsDead()}");
                }
                
                // Only process hit if zombie is not already dead
                if (!zombie.IsDead())
                {
                    // Find the closest point on the character mesh for impact effect
                    Vector3 meshHitPoint = GetMeshSurfacePoint(zombie.gameObject, hitPoint);
                    
                    zombie.RegisterProjectileHit(impactDirection, calculatedForce, hitPoint);
                    zombie.GetShot();
                    
                    // Spawn impact effect on the mesh surface
                    SpawnBloodEffect(meshHitPoint, -impactDirection);
                }
            }
            else
            {
                Debug.LogWarning($"Hit Enemy-tagged object but no Enemy or Zombie component found: {hitCollider.name}");
                // Still spawn blood effect for visual feedback
                SpawnBloodEffect(hitPoint, -impactDirection);
            }
        }
        else if (hitCollider.CompareTag("Player"))
        {
            Character player = hitCollider.GetComponent<Character>();
            if (player == null)
            {
                player = hitCollider.GetComponentInParent<Character>();
            }
            
            if (player != null)
            {
                if (debugHitDetection)
                {
                    Debug.Log($"Hit player: {player.name}");
                }
                
                // Apply simple damage
                int damage = 15; // Fixed damage value
                player.ChangeCurrentHealth(-damage);
                
                // Spawn blood effect on player hit
                SpawnBloodEffect(hitPoint, -impactDirection);
            }
            else
            {
                Debug.LogWarning($"Hit Player-tagged object but no Character component found: {hitCollider.name}");
            }
        }
        else
        {
            // Generic surface hit - spawn generic impact effect
            SpawnImpactEffect(hitPoint, hitNormal);
        }

        // Destroy immediately
        if (gameObject != null)
        {
            Destroy(gameObject, 0.01f);
        }
    }
    
    private void SpawnBloodEffect(Vector3 position, Vector3 normal)
    {
        if (bloodImpactPrefabs != null && bloodImpactPrefabs.Length > 0)
        {
            Transform effectPrefab = bloodImpactPrefabs[Random.Range(0, bloodImpactPrefabs.Length)];
            if (effectPrefab != null)
            {
                GameObject effect = Instantiate(effectPrefab.gameObject, position, Quaternion.LookRotation(normal));
                
                // Auto-destroy impact effect after some time
                ParticleDeath particleDeath = effect.GetComponent<ParticleDeath>();
                if (particleDeath == null)
                {
                    effect.AddComponent<ParticleDeath>();
                }
                
                if (debugHitDetection)
                {
                    Debug.Log($"Spawned blood effect at {position}");
                }
            }
        }
        else
        {
            // Fallback to concrete effects if no blood effects assigned
            SpawnImpactEffect(position, normal);
            
            if (debugHitDetection)
            {
                Debug.LogWarning("No blood impact prefabs assigned, using concrete effects as fallback");
            }
        }
    }
    
    private Vector3 GetMeshSurfacePoint(GameObject targetObject, Vector3 hitPoint)
    {
        // Find the main character mesh renderer
        SkinnedMeshRenderer meshRenderer = targetObject.GetComponentInChildren<SkinnedMeshRenderer>();
        if (meshRenderer == null)
        {
            MeshRenderer staticMeshRenderer = targetObject.GetComponentInChildren<MeshRenderer>();
            if (staticMeshRenderer != null)
            {
                // For static mesh, use bounds center as approximation
                return staticMeshRenderer.bounds.ClosestPoint(hitPoint);
            }
        }
        else
        {
            // For skinned mesh, get the closest point on the bounds
            return meshRenderer.bounds.ClosestPoint(hitPoint);
        }
        
        // Fallback to original hit point if no mesh found
        return hitPoint;
    }

    public void SetData(GameObject owner, float projectileForce)
    {
        myOwner = owner;
        
        if (debugHitDetection)
        {
            Debug.Log($"Projectile owner set to: {owner.name}, Force: {projectileForce}");
        }
        
        // Disable colliding with my owner and all its children
        Collider[] ownerColliders = myOwner.GetComponentsInChildren<Collider>();
        Collider projectileCollider = GetComponent<Collider>();
        
        foreach (Collider col in ownerColliders)
        {
            if (col != null && projectileCollider != null)
            {
                Physics.IgnoreCollision(col, projectileCollider);
            }
        }

        // Set velocity
        rigidBody.linearVelocity = transform.forward * projectileForce;
        
        if (debugHitDetection)
        {
            Debug.Log($"Projectile velocity set to: {rigidBody.linearVelocity.magnitude:F2} m/s");
        }
    }

    private IEnumerator DestroyAfter()
    {
        yield return new WaitForSeconds(lifeTime);
        if (!isDestroying)
        {
            DestroyProjectile();
        }
    }
    
    private void DestroyProjectile()
    {
        if (isDestroying) return; // Prevent multiple destruction calls
        
        isDestroying = true;
        
        if (debugHitDetection)
        {
            Debug.Log($"Destroying projectile: {gameObject.name}");
        }
        
        // Stop all physics immediately
        if (rigidBody != null)
        {
            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            rigidBody.isKinematic = true;
        }
        
        // Disable collider to prevent further hits
        Collider projectileCollider = GetComponent<Collider>();
        if (projectileCollider != null)
        {
            projectileCollider.enabled = false;
        }
        
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
    
    private void SpawnImpactEffect(Vector3 position, Vector3 normal)
    {
        if (concreteImpactPrefabs != null && concreteImpactPrefabs.Length > 0)
        {
            Transform effectPrefab = concreteImpactPrefabs[Random.Range(0, concreteImpactPrefabs.Length)];
            if (effectPrefab != null)
            {
                GameObject effect = Instantiate(effectPrefab.gameObject, position, Quaternion.LookRotation(normal));
                
                // Auto-destroy impact effect after some time
                ParticleDeath particleDeath = effect.GetComponent<ParticleDeath>();
                if (particleDeath == null)
                {
                    effect.AddComponent<ParticleDeath>();
                }
            }
        }
    }
}