using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class RagdollBone
{
    public Transform bone;
    public Rigidbody rigidBody;
    public Collider collider;
    
    [Header("Bone Settings")]
    public bool isMainBody = false;
    
    [HideInInspector] public float originalMass;
    [HideInInspector] public float originalLinearDrag;
    [HideInInspector] public float originalAngularDrag;
    [HideInInspector] public Vector3 storedVelocity;
    [HideInInspector] public Vector3 storedAngularVelocity;
}

public class RagdollController : MonoBehaviour
{
    [Header("🎬 Cinematic Death Forces")]
    [SerializeField] private float deathForceMultiplier = 8f;
    [SerializeField] private float upwardForceBoost = 3f;
    [SerializeField] private float spinTorqueMultiplier = 5f;
    [SerializeField] private float forceRandomization = 0.3f;
    [SerializeField] private bool enableLimbFlailing = true;
    [SerializeField] private float limbFlailIntensity = 4f;
    
    [Header("⏰ Localized Slow Motion")]
    [SerializeField] private bool useLocalSlowMotion = true;
    [SerializeField] private float slowMotionDuration = 2f;
    [SerializeField] private float freezeAtPeakDuration = 0.4f;
    [SerializeField] private float slowMotionDragMultiplier = 15f;
    [SerializeField] private AnimationCurve slowMotionCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    
    [Header("💥 Impact Response")]
    [SerializeField] private float minimumDeathForce = 12f;
    [SerializeField] private float maximumDeathForce = 25f;
    [SerializeField] private bool enableGroundBounce = true;
    [SerializeField] private float bounceForceMultiplier = 0.6f;
    [SerializeField] private int maxBounces = 2;
    
    [Header("🎭 Dramatic Effects")]
    [SerializeField] private bool enableDramaticFreeze = true;
    [SerializeField] private float dramaticFreezeDelay = 0.1f;
    [SerializeField] private bool enableExaggeratedPhysics = true;
    [SerializeField] private float exaggerationMultiplier = 1.5f;
    
    [Header("🔧 Technical Settings")]
    [SerializeField] private RagdollBone[] ragdollBones;
    [SerializeField] private Animator animator;
    [SerializeField] private float cleanupDelay = 12f;
    [SerializeField] private bool enableFadeOut = true;
    [SerializeField] private float fadeOutDuration = 3f;
    
    // Private variables
    private bool isRagdollActive = false;
    private bool isInSlowMotion = false;
    private Vector3 lastHitDirection;
    private float lastHitForce;
    private Vector3 lastHitPoint;
    private Dictionary<Rigidbody, int> bounceCounter = new Dictionary<Rigidbody, int>();
    
    // Original states for restoration
    private bool[] initialKinematicStates;
    private bool[] initialColliderStates;
    private bool initialAnimatorState;
    
    private void Awake()
    {
        SetupRagdollBones();
        StoreInitialStates();
        DeactivateRagdoll();
    }
    
    private void StoreInitialStates()
    {
        initialKinematicStates = new bool[ragdollBones.Length];
        initialColliderStates = new bool[ragdollBones.Length];
        
        for (int i = 0; i < ragdollBones.Length; i++)
        {
            var bone = ragdollBones[i];
            if (bone.rigidBody != null)
            {
                initialKinematicStates[i] = bone.rigidBody.isKinematic;
                bone.originalMass = bone.rigidBody.mass;
                bone.originalLinearDrag = bone.rigidBody.linearDamping;
                bone.originalAngularDrag = bone.rigidBody.angularDamping;
            }
            if (bone.collider != null)
            {
                initialColliderStates[i] = bone.collider.enabled;
            }
        }
        
        initialAnimatorState = animator != null ? animator.enabled : false;
    }
    
    private void SetupRagdollBones()
    {
        foreach (var bone in ragdollBones)
        {
            if (bone.bone == null) continue;
            
            // Auto-detect components if not assigned
            if (bone.rigidBody == null)
                bone.rigidBody = bone.bone.GetComponent<Rigidbody>();
            if (bone.collider == null)
                bone.collider = bone.bone.GetComponent<Collider>();
            
            // Auto-detect main body bone (usually hips)
            if (!bone.isMainBody && bone.bone.name.ToLower().Contains("hips"))
            {
                bone.isMainBody = true;
            }
            
            // Setup physics for exaggerated effects
            if (bone.rigidBody != null && enableExaggeratedPhysics)
            {
                bone.rigidBody.mass *= exaggerationMultiplier;
            }
        }
    }
    
    public void ActivateRagdoll(Vector3 hitDirection = default, float hitForce = 0f, Vector3 hitPoint = default)
    {
        if (isRagdollActive) return;
        
        isRagdollActive = true;
        lastHitDirection = hitDirection.normalized;
        lastHitForce = Mathf.Clamp(hitForce, minimumDeathForce, maximumDeathForce);
        lastHitPoint = hitPoint;
        
        // Disable character systems immediately
        DisableCharacterSystems();
        
        // Enable ragdoll physics
        EnableRagdollPhysics();
        
        // Apply dramatic death forces
        StartCoroutine(ApplyDramaticDeathSequence());
        
        // Start localized slow motion
        if (useLocalSlowMotion)
        {
            StartCoroutine(LocalizedSlowMotionEffect());
        }
        
        // Start cleanup timer
        StartCoroutine(CleanupRagdoll());
        
        //Debug.Log($"🎬 DRAMATIC DEATH ACTIVATED! Force: {lastHitForce}, Direction: {lastHitDirection}");
    }
    
    private void DisableCharacterSystems()
    {
        // Disable animator
        if (animator != null)
            animator.enabled = false;
        
        // Disable AI
        var aiState = GetComponent<AIStateMachine>();
        if (aiState != null)
            aiState.SetState(AIStateMachine.AIState.Dying);
        
        // Disable NavMesh agent
        var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.enabled = false;
        }
        
        // Disable character controller
        var charController = GetComponent<CharacterController>();
        if (charController != null)
            charController.enabled = false;
    }
    
    private void EnableRagdollPhysics()
    {
        foreach (var bone in ragdollBones)
        {
            if (bone.rigidBody != null)
            {
                bone.rigidBody.isKinematic = false;
                bone.rigidBody.useGravity = true;
                
                // Set low initial drag for dramatic movement
                bone.rigidBody.linearDamping = 0.1f;
                bone.rigidBody.angularDamping = 0.2f;
            }
            
            if (bone.collider != null)
            {
                bone.collider.enabled = true;
            }
        }
    }
    
    private IEnumerator ApplyDramaticDeathSequence()
    {
        // Wait a brief moment for ragdoll to fully initialize
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        
        // Optional dramatic freeze before the explosion
        if (enableDramaticFreeze)
        {
            // Freeze all bones briefly for dramatic effect
            foreach (var bone in ragdollBones)
            {
                if (bone.rigidBody != null)
                {
                    bone.storedVelocity = bone.rigidBody.linearVelocity;
                    bone.storedAngularVelocity = bone.rigidBody.angularVelocity;
                    bone.rigidBody.linearVelocity = Vector3.zero;
                    bone.rigidBody.angularVelocity = Vector3.zero;
                    bone.rigidBody.isKinematic = true;
                }
            }
            
            yield return new WaitForSeconds(dramaticFreezeDelay);
            
            // Unfreeze
            foreach (var bone in ragdollBones)
            {
                if (bone.rigidBody != null)
                {
                    bone.rigidBody.isKinematic = false;
                }
            }
            
            // Wait another frame after unfreezing
            yield return new WaitForFixedUpdate();
        }
        
        // Apply the main dramatic force IMMEDIATELY
        ApplyMainDeathForce();
        
        // Apply secondary limb forces
        yield return new WaitForFixedUpdate();
        ApplyLimbFlailingForces();
        
        //Debug.Log("🎬 DRAMATIC DEATH SEQUENCE COMPLETE!");
    }
    
    private void ApplyMainDeathForce()
    {
        var mainBody = FindMainBodyRigidbody();
        if (mainBody == null) 
        {
            //Debug.LogError("❌ NO MAIN BODY RIGIDBODY FOUND! Cannot apply force!");
            return;
        }
        
        // Calculate dramatic force direction
        Vector3 forceDirection = lastHitDirection;
        if (forceDirection == Vector3.zero)
        {
            // Default dramatic backwards and upward force
            forceDirection = (-transform.forward + Vector3.up * 0.5f).normalized;
            //Debug.Log("⚠️ No hit direction provided, using default backward force");
        }
        
        // Ensure significant upward component for dramatic effect
        forceDirection.y = Mathf.Max(forceDirection.y, 0.4f);
        forceDirection = forceDirection.normalized;
        
        // Calculate force with randomization
        float finalForce = lastHitForce * deathForceMultiplier;
        if (forceRandomization > 0)
        {
            finalForce *= Random.Range(1f - forceRandomization, 1f + forceRandomization);
        }
        
        // Ensure minimum force even if lastHitForce is low
        finalForce = Mathf.Max(finalForce, minimumDeathForce * deathForceMultiplier);
        
        // Apply main force (horizontal + upward)
        Vector3 horizontalForce = new Vector3(forceDirection.x, 0, forceDirection.z).normalized * finalForce;
        Vector3 upwardForce = Vector3.up * (finalForce * upwardForceBoost);
        Vector3 totalForce = horizontalForce + upwardForce;
        
        // CRITICAL: Apply force BEFORE slow motion drag is applied
        mainBody.linearDamping = 0.1f; // Ensure low drag for initial force
        mainBody.angularDamping = 0.1f;
        
        mainBody.AddForce(totalForce, ForceMode.Impulse);
        
        // Add dramatic spinning
        Vector3 spinTorque = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized * (spinTorqueMultiplier * finalForce * 0.1f);
        
        mainBody.AddTorque(spinTorque, ForceMode.Impulse);
        
        //Debug.Log($"💥 MAIN FORCE APPLIED TO {mainBody.name}: {finalForce} in direction {forceDirection}");
        //Debug.Log($"💥 Total Force Vector: {totalForce}, Magnitude: {totalForce.magnitude}");
        //Debug.Log($"💥 Main Body Mass: {mainBody.mass}, Drag: {mainBody.linearDamping}");
    }
    
    private void ApplyLimbFlailingForces()
    {
        if (!enableLimbFlailing) return;
        
        foreach (var bone in ragdollBones)
        {
            if (bone.rigidBody == null || bone.isMainBody) continue;
            
            // Apply random flailing forces to limbs
            Vector3 randomForce = Random.onUnitSphere * limbFlailIntensity;
            randomForce.y = Mathf.Abs(randomForce.y) * 0.7f; // Prefer upward forces
            
            bone.rigidBody.AddForce(randomForce, ForceMode.Impulse);
            
            // Add random torque for limb spinning
            Vector3 randomTorque = Random.onUnitSphere * limbFlailIntensity * 0.5f;
            bone.rigidBody.AddTorque(randomTorque, ForceMode.Impulse);
        }
        
        //Debug.Log("🌪️ LIMB FLAILING APPLIED!");
    }
    
    private IEnumerator LocalizedSlowMotionEffect()
    {
        isInSlowMotion = true;
        
        // Store original physics values for each bone
        var originalPhysicsData = new List<(Rigidbody rb, float drag, float angularDrag)>();
        
        foreach (var bone in ragdollBones)
        {
            if (bone.rigidBody != null)
            {
                originalPhysicsData.Add((bone.rigidBody, bone.originalLinearDrag, bone.originalAngularDrag));
                
                // Apply high drag for slow motion effect (only affects this ragdoll)
                float slowDrag = bone.originalLinearDrag * slowMotionDragMultiplier;
                float slowAngularDrag = bone.originalAngularDrag * slowMotionDragMultiplier;
                
                bone.rigidBody.linearDamping = slowDrag;
                bone.rigidBody.angularDamping = slowAngularDrag;
            }
        }
        
        //Debug.Log("⏰ SLOW MOTION ACTIVATED - LOCAL ONLY!");
        
        // Optional freeze at peak for maximum drama
        if (freezeAtPeakDuration > 0)
        {
            yield return new WaitForSeconds(0.3f); // Let them fly up first
            
            // Brief freeze at peak
            foreach (var bone in ragdollBones)
            {
                if (bone.rigidBody != null)
                {
                    bone.storedVelocity = bone.rigidBody.linearVelocity;
                    bone.storedAngularVelocity = bone.rigidBody.angularVelocity;
                    bone.rigidBody.linearVelocity = Vector3.zero;
                    bone.rigidBody.angularVelocity = Vector3.zero;
                    bone.rigidBody.isKinematic = true;
                }
            }
            
            yield return new WaitForSeconds(freezeAtPeakDuration);
            
            // Unfreeze with reduced velocity
            foreach (var bone in ragdollBones)
            {
                if (bone.rigidBody != null)
                {
                    bone.rigidBody.isKinematic = false;
                    bone.rigidBody.linearVelocity = bone.storedVelocity * 0.7f;
                    bone.rigidBody.angularVelocity = bone.storedAngularVelocity * 0.7f;
                }
            }
            
            //Debug.Log("🎭 DRAMATIC FREEZE COMPLETE!");
        }
        
        // Gradually restore normal physics over time
        float remainingTime = slowMotionDuration - (freezeAtPeakDuration > 0 ? 0.3f + freezeAtPeakDuration : 0f);
        float elapsedTime = 0f;
        
        while (elapsedTime < remainingTime)
        {
            float progress = elapsedTime / remainingTime;
            float curveValue = slowMotionCurve.Evaluate(progress);
            
            foreach (var (rb, originalDrag, originalAngularDrag) in originalPhysicsData)
            {
                if (rb != null)
                {
                    float currentSlowDrag = originalDrag * slowMotionDragMultiplier;
                    float currentSlowAngularDrag = originalAngularDrag * slowMotionDragMultiplier;
                    
                    rb.linearDamping = Mathf.Lerp(currentSlowDrag, originalDrag, curveValue);
                    rb.angularDamping = Mathf.Lerp(currentSlowAngularDrag, originalAngularDrag, curveValue);
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at original values
        foreach (var (rb, originalDrag, originalAngularDrag) in originalPhysicsData)
        {
            if (rb != null)
            {
                rb.linearDamping = originalDrag;
                rb.angularDamping = originalAngularDrag;
            }
        }
        
        isInSlowMotion = false;
        //Debug.Log("⏰ SLOW MOTION COMPLETE - NORMAL PHYSICS RESTORED!");
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!isRagdollActive || !enableGroundBounce) return;
        
        var rb = collision.rigidbody;
        if (rb == null) return;
        
        // Initialize bounce counter if needed
        if (!bounceCounter.ContainsKey(rb))
            bounceCounter[rb] = 0;
        
        // Apply bounce effect for ground impacts
        if (collision.gameObject.CompareTag("Ground") && bounceCounter[rb] < maxBounces)
        {
            Vector3 bounceDirection = Vector3.Reflect(rb.linearVelocity.normalized, collision.contacts[0].normal);
            float bounceForce = rb.linearVelocity.magnitude * bounceForceMultiplier;
            
            rb.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);
            bounceCounter[rb]++;
            
            //Debug.Log($"🏀 BOUNCE {bounceCounter[rb]}: {rb.name} bounced with force {bounceForce}");
        }
    }
    
    private Rigidbody FindMainBodyRigidbody()
    {
        // First, look for explicitly marked main body
        foreach (var bone in ragdollBones)
        {
            if (bone.isMainBody && bone.rigidBody != null)
                return bone.rigidBody;
        }
        
        // Fallback to finding by name
        string[] mainBodyNames = { "hips", "pelvis", "spine", "root", "body", "torso" };
        foreach (var bone in ragdollBones)
        {
            if (bone.bone == null || bone.rigidBody == null) continue;
            
            string boneName = bone.bone.name.ToLower();
            foreach (string mainName in mainBodyNames)
            {
                if (boneName.Contains(mainName))
                    return bone.rigidBody;
            }
        }
        
        // Last resort - return first available rigidbody
        foreach (var bone in ragdollBones)
        {
            if (bone.rigidBody != null)
                return bone.rigidBody;
        }
        
        return null;
    }
    
    public void DeactivateRagdoll()
    {
        if (!isRagdollActive) return;
        
        isRagdollActive = false;
        isInSlowMotion = false;
        
        // Stop all coroutines
        StopAllCoroutines();
        
        // Restore original states
        if (animator != null)
            animator.enabled = initialAnimatorState;
        
        // Restore ragdoll bones to original state
        for (int i = 0; i < ragdollBones.Length; i++)
        {
            var bone = ragdollBones[i];
            if (bone.rigidBody != null && i < initialKinematicStates.Length)
            {
                bone.rigidBody.isKinematic = initialKinematicStates[i];
                bone.rigidBody.mass = bone.originalMass;
                bone.rigidBody.linearDamping = bone.originalLinearDrag;
                bone.rigidBody.angularDamping = bone.originalAngularDrag;
            }
            
            if (bone.collider != null && i < initialColliderStates.Length)
            {
                bone.collider.enabled = initialColliderStates[i];
            }
        }
        
        bounceCounter.Clear();
    }
    
    private IEnumerator CleanupRagdoll()
    {
        yield return new WaitForSeconds(cleanupDelay - (enableFadeOut ? fadeOutDuration : 0f));
        
        if (enableFadeOut)
        {
            yield return StartCoroutine(FadeOutRagdoll());
        }
        
        // Destroy or return to pool
        var enemy = GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Die();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private IEnumerator FadeOutRagdoll()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        var originalMaterials = new Material[renderers.Length][];
        
        // Store original materials and create fade materials
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
            var fadeMaterials = new Material[originalMaterials[i].Length];
            
            for (int j = 0; j < originalMaterials[i].Length; j++)
            {
                fadeMaterials[j] = new Material(originalMaterials[i][j]);
                SetupTransparentMaterial(fadeMaterials[j]);
            }
            
            renderers[i].materials = fadeMaterials;
        }
        
        // Fade out over time
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            float alpha = 1f - (elapsedTime / fadeOutDuration);
            
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    SetMaterialAlpha(material, alpha);
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    
    private void SetupTransparentMaterial(Material material)
    {
        if (material.HasProperty("_Surface"))
        {
            // URP Material
            material.SetFloat("_Surface", 1); // Transparent
            material.SetFloat("_Blend", 0); // Alpha
        }
        else if (material.HasProperty("_Mode"))
        {
            // Built-in Material
            material.SetFloat("_Mode", 3); // Transparent
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
        }
    }
    
    private void SetMaterialAlpha(Material material, float alpha)
    {
        if (material.HasProperty("_Color"))
        {
            var color = material.color;
            color.a = alpha;
            material.color = color;
        }
        else if (material.HasProperty("_BaseColor"))
        {
            var color = material.GetColor("_BaseColor");
            color.a = alpha;
            material.SetColor("_BaseColor", color);
        }
    }
    
    // Public interface methods
    public bool IsRagdollActive => isRagdollActive;
    public bool IsInSlowMotion => isInSlowMotion;
    
    public void RegisterHit(Vector3 hitDirection, float hitForce, Vector3 hitPoint)
    {
        lastHitDirection = hitDirection;
        lastHitForce = hitForce;
        lastHitPoint = hitPoint;
    }
    
    public void OnEnemyShot(Vector3 shotDirection, float shotForce, Vector3 hitPoint)
    {
        RegisterHit(shotDirection, shotForce, hitPoint);
        ActivateRagdoll(shotDirection, shotForce, hitPoint);
    }
    
    public void OnEnemyDeath()
    {
        Vector3 direction = lastHitDirection != Vector3.zero ? lastHitDirection : Random.onUnitSphere;
        float force = lastHitForce > 0 ? lastHitForce : minimumDeathForce;
        ActivateRagdoll(direction, force, lastHitPoint);
    }
    
    // Editor helper methods
    [ContextMenu("Setup From Unity Ragdoll")]
    private void SetupFromUnityRagdoll()
    {
        #if UNITY_EDITOR
        var bones = new List<RagdollBone>();
        var rigidbodies = GetComponentsInChildren<Rigidbody>();
        
        foreach (var rb in rigidbodies)
        {
            if (rb.transform == transform) continue;
            
            var bone = new RagdollBone
            {
                bone = rb.transform,
                rigidBody = rb,
                collider = rb.GetComponent<Collider>(),
                isMainBody = rb.transform.name.ToLower().Contains("hips")
            };
            
            bones.Add(bone);
        }
        
        ragdollBones = bones.ToArray();
        
        //Debug.Log($"✅ Setup {bones.Count} ragdoll bones from Unity ragdoll wizard");
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    [ContextMenu("Test Dramatic Death")]
    private void TestDramaticDeath()
    {
        if (Application.isPlaying)
        {
            // Generate a strong test force
            Vector3 testDirection = new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f)).normalized;
            float testForce = 30f; // Strong test force
            Vector3 testHitPoint = transform.position + Vector3.up;
            
            //Debug.Log($"🧪 TESTING DRAMATIC DEATH: Direction={testDirection}, Force={testForce}");
            
            // Force specific values for testing
            lastHitDirection = testDirection;
            lastHitForce = testForce;
            lastHitPoint = testHitPoint;
            
            ActivateRagdoll(testDirection, testForce, testHitPoint);
        }
        else
        {
            //Debug.LogWarning("⚠️ Test Dramatic Death only works in Play Mode!");
        }
    }
    
    // Gizmos for visualization
    private void OnDrawGizmosSelected()
    {
        if (!isRagdollActive) return;
        
        // Draw main body
        foreach (var bone in ragdollBones)
        {
            if (bone.bone != null)
            {
                if (bone.isMainBody)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(bone.bone.position, 0.15f);
                }
                else
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(bone.bone.position, Vector3.one * 0.05f);
                }
            }
        }
        
        // Draw last hit direction
        if (lastHitDirection != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, lastHitDirection * 3f);
        }
    }
}