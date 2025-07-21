using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class AIStateMachine : MonoBehaviour
{
    [SerializeField] private NavMeshAgent myAgent;
    [SerializeField] private Animator animator;
    [SerializeField] private float roamingRange;
    [SerializeField] private float detectRange;
    [SerializeField] private float attackRange;
    [SerializeField] private float detectionDelay;
    public GameObject attackTarget;
    private Vector3 lastKnownPosition;
    private Vector3 lookPosition;

    private float attackTimer;
    [SerializeField] private Transform barrelPosition;
    [Range(0.1f, 1.0f)]
    [SerializeField] private float accuracy = 0.65f;
    [SerializeField] private float maxSpreadAngle = 30f;
    [SerializeField]
    private GameObject projectilePrefab;
    [Tooltip("How fast the projectiles are.")]
    [SerializeField]
    private float projectileImpulse = 400.0f;

    [Header("Muzzle Flash & Audio")]
    [Tooltip("Muzzle flash particle system attached to barrel position.")]
    [SerializeField] private ParticleSystem muzzleFlashParticles;
    [Tooltip("Alternative: Muzzle flash GameObject attached to barrel position.")]
    [SerializeField] private GameObject muzzleFlashObject;
    [Tooltip("How long to keep the muzzle flash visible.")]
    [SerializeField] private float flashDuration = 0.1f;
    [Tooltip("Fire sound effect.")]
    [SerializeField] private AudioClip fireSound;
    [Tooltip("Fire sound volume.")]
    [SerializeField] private float fireVolume = 0.7f;
    
    private AudioSource audioSource;
    private Coroutine muzzleFlashCoroutine;

    public enum AIState
    {
        Roaming,
        Seeking,
        Attacking,
        Dying
    };

    public AIState currentState;
    private bool isDead = false; // Add death flag
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentState = AIState.Roaming;
        SetupAudio();
        SetupMuzzleFlash();
        StartCoroutine(SearchForPlayer());
    }

    private void SetupMuzzleFlash()
    {
        // Auto-find muzzle flash components if not assigned
        if (muzzleFlashParticles == null && muzzleFlashObject == null && barrelPosition != null)
        {
            // Try to find ParticleSystem first
            muzzleFlashParticles = barrelPosition.GetComponentInChildren<ParticleSystem>();
            
            // If no ParticleSystem, look for any child GameObject with "flash" or "muzzle" in the name
            if (muzzleFlashParticles == null)
            {
                foreach (Transform child in barrelPosition)
                {
                    if (child.name.ToLower().Contains("flash") || child.name.ToLower().Contains("muzzle"))
                    {
                        muzzleFlashObject = child.gameObject;
                        break;
                    }
                }
            }
        }
        
        // Make sure muzzle flash starts disabled
        if (muzzleFlashParticles != null)
        {
            muzzleFlashParticles.Stop();
        }
        
        if (muzzleFlashObject != null)
        {
            muzzleFlashObject.SetActive(false);
        }
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
        audioSource.spatialBlend = 1.0f; // Make it 3D audio
    }

    public void SetState(AIState newState)
    {
        // Prevent state changes if dead
        if (isDead && newState != AIState.Dying)
            return;
            
        currentState = newState;
        
        // Set death flag when entering dying state
        if (newState == AIState.Dying)
        {
            isDead = true;
            // Stop all movement and coroutines when dying
            StopAllCoroutines();
            if (myAgent != null)
            {
                myAgent.isStopped = true;
                myAgent.ResetPath();
            }
            
            // Stop any active muzzle flash
            if (muzzleFlashParticles != null)
            {
                muzzleFlashParticles.Stop();
            }
            if (muzzleFlashObject != null)
            {
                muzzleFlashObject.SetActive(false);
            }
        }
    }
    
    public void SetAttackTarget(GameObject newTarget)
    {
        // Don't allow new targets if dead
        if (isDead) return;
        
        attackTarget = newTarget;
        SetState(AIState.Attacking);
        StartCoroutine(TrackPlayer());
    }

    // Update is called once per frame
    void Update()
    {
        // Don't update if dead
        if (isDead) return;
        
        switch (currentState)
        {
            case AIState.Roaming:
                if(myAgent.remainingDistance <= myAgent.stoppingDistance)
                {
                    Vector3 point;
                    if(RandomPoint(transform.position, roamingRange, out point))
                    {
                        myAgent.SetDestination(point);
                    }
                }
                break;
            case AIState.Seeking:
                if (myAgent.remainingDistance <= myAgent.stoppingDistance) // finished walking
                {
                    SetState(AIState.Roaming);
                }
                else
                {
                    myAgent.SetDestination(lastKnownPosition);
                }
                break;
            case AIState.Attacking:
                lastKnownPosition = attackTarget.transform.position;
                lookPosition = attackTarget.transform.position + Vector3.up * 1.14f;
                
                // FIXED: Only rotate on Y-axis, keep X and Z rotation locked
                Vector3 directionToTarget = lookPosition - transform.position;
                directionToTarget.y = 0; // Remove vertical component to prevent tilting
                
                if (directionToTarget != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                }

                if(CheckEnemyInRange())
                {
                    // FIXED: Stop moving when in range to attack
                    if (!myAgent.isStopped)
                    {
                        myAgent.isStopped = true;
                        myAgent.ResetPath();
                    }
                    
                    if (attackTimer >= 0.0f)
                        AttackCooldown();
                    else
                        AttackTarget();
                }
                else
                {
                    // FIXED: Resume movement when not in range
                    if (myAgent.isStopped)
                    {
                        myAgent.isStopped = false;
                    }
                    myAgent.SetDestination(lastKnownPosition);
                }
                break;
            case AIState.Dying:
                // Ensure everything is stopped when dying
                if (myAgent != null)
                {
                    myAgent.isStopped = true;
                    myAgent.ResetPath();
                }
                GetComponent<Collider>().enabled = false;
                break;
        }
        UpdateAnimator();
    }

    private bool CheckEnemyInRange()
    {
        // Don't check range if dead or no target
        if (isDead || attackTarget == null) return false;
        
        float distance = Vector3.Distance(transform.position, attackTarget.transform.position);
        return distance <= attackRange;
    }

    private void AttackCooldown()
    {
        attackTimer -= Time.deltaTime;
    }

    private void AttackTarget()
    {
        // Don't attack if dead
        if (isDead) return;
        
        // Ensure we're stopped for shooting
        myAgent.isStopped = true;
        myAgent.ResetPath();

        // PLAY MUZZLE FLASH EFFECT
        PlayMuzzleFlash();
        
        // PLAY FIRE SOUND
        PlayFireSound();

        // Calculate direction from barrel to target
        Vector3 shootDirection;
        if (attackTarget != null)
        {
            // Aim directly at the target from the barrel position
            shootDirection = (attackTarget.transform.position - barrelPosition.position).normalized;
        }
        else
        {
            // Fallback to forward direction
            shootDirection = barrelPosition.forward;
        }
        
        // Apply accuracy spread
        float spread = (1.0f - accuracy) * maxSpreadAngle;
        Vector3 spreadOffset = new Vector3(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            0.0f
        );
        shootDirection = Quaternion.Euler(spreadOffset) * shootDirection;
        
        // Calculate final rotation for the projectile
        Quaternion projectileRotation = Quaternion.LookRotation(shootDirection);
        
        // Spawn projectile at barrel position with calculated rotation
        GameObject projectile = Instantiate(projectilePrefab, barrelPosition.position, projectileRotation);
        projectile.GetComponent<Projectile>().SetData(gameObject, projectileImpulse);

        animator.Play("Fire");

        attackTimer = 0.3f;
    }

    private void PlayMuzzleFlash()
    {
        // Stop any existing muzzle flash coroutine
        if (muzzleFlashCoroutine != null)
        {
            StopCoroutine(muzzleFlashCoroutine);
        }
        
        // Start new muzzle flash
        muzzleFlashCoroutine = StartCoroutine(MuzzleFlashSequence());
    }
    
    private System.Collections.IEnumerator MuzzleFlashSequence()
    {
        // Method 1: Use ParticleSystem (preferred)
        if (muzzleFlashParticles != null)
        {
            // Play the particle effect
            muzzleFlashParticles.Play();
            
            // Wait for the flash duration
            yield return new WaitForSeconds(flashDuration);
            
            // Stop the particle effect
            muzzleFlashParticles.Stop();
        }
        // Method 2: Use GameObject activation
        else if (muzzleFlashObject != null)
        {
            // Enable the muzzle flash object
            muzzleFlashObject.SetActive(true);
            
            // Wait for the flash duration
            yield return new WaitForSeconds(flashDuration);
            
            // Disable the muzzle flash object
            muzzleFlashObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"No muzzle flash component assigned or found on {gameObject.name}! " +
                           "Please assign either a ParticleSystem or GameObject to the muzzle flash fields, " +
                           "or add a child object with 'flash' or 'muzzle' in its name to the barrel position.");
        }
        
        muzzleFlashCoroutine = null;
    }

    private void PlayFireSound()
    {
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound, fireVolume);
        }
        else
        {
            if (fireSound == null)
                Debug.LogWarning($"Fire sound is not assigned on {gameObject.name}!");
            if (audioSource == null)
                Debug.LogWarning($"AudioSource is missing on {gameObject.name}!");
        }
    }

    private IEnumerator TrackPlayer()
    {
        while(currentState == AIState.Attacking && !isDead) // FIXED: Stop tracking if dead
        {
            yield return new WaitForSeconds(detectionDelay);

            // Additional safety check
            if (isDead || attackTarget == null) break;

            Vector3[] points = GetBoundingPoints(attackTarget.gameObject.GetComponent<Collider>().bounds);

            int hiddenPoints = 0;

            foreach(Vector3 point in points)
            {
                Vector3 targetDirection = point - transform.position;
                float targetDist = Vector3.Distance(transform.position, point);
                float targetAngle = Vector3.Angle(targetDirection, transform.forward);

                if (IsPointCovered(targetDirection, targetDist) || targetDist > detectRange)
                {
                    hiddenPoints++;
                }
            }

            if(hiddenPoints >= points.Length)
            {
                //player is hidden
                attackTarget = null;
                SetState(AIState.Seeking);
                myAgent.SetDestination(lastKnownPosition);
                StopCoroutine(TrackPlayer());
                StartCoroutine(SearchForPlayer());
            }
            else
            {
                //player is still visible
            }
        }
    }

    private IEnumerator SearchForPlayer()
    {
        GameObject player = GameMan.Instance.playerInstance;
        while((currentState == AIState.Seeking || currentState == AIState.Roaming) && !isDead) // FIXED: Stop searching if dead
        {
            yield return new WaitForSeconds(detectionDelay);

            // Additional safety check
            if (isDead) break;

            Vector3[] points = GetBoundingPoints(player.gameObject.GetComponent<Collider>().bounds);

            int hiddenPoints = 0;

            foreach(Vector3 point in points)
            {
                Vector3 targetDirection = point - transform.position;
                float targetDist = Vector3.Distance(transform.position, point);
                float targetAngle = Vector3.Angle(targetDirection, transform.forward);

                if(IsPointCovered(targetDirection, targetDist) || targetDist > detectRange)
                {
                    hiddenPoints++;
                }
            }

            if(hiddenPoints >= points.Length)
            {
                //cannot see the player
            }
            else
            {
                //can see the player
                SetAttackTarget(player);
                StopCoroutine(SearchForPlayer());
            }
        }
    }

    private bool RandomPoint(Vector3 aroundPoint, float range, out Vector3 result)
    {
        Vector3 randomPoint = aroundPoint + Random.insideUnitSphere * range;
        NavMeshHit hit;
        if(NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    private bool IsPointCovered(Vector3 targetDir, float targetDist)
    {
        Vector3 eyePosition = transform.position;
        eyePosition.y += 0.5f;
        Debug.DrawRay(eyePosition, targetDir, Color.red, 0.2f);
        RaycastHit[] hits = Physics.RaycastAll(eyePosition, targetDir.normalized, detectRange, ~0);

        foreach(RaycastHit hit in hits)
        {
            float coverDist = Vector3.Distance(eyePosition, hit.point);

            if (coverDist <= targetDist)
                return true;
        }

        return false;
    }

    private Vector3[] GetBoundingPoints(Bounds bounds)
    {
        Vector3[] bounding_points =
        {
            bounds.min,
            bounds.max,
            new Vector3( bounds.min.x, bounds.min.y, bounds.max.z ),
            new Vector3( bounds.min.x, bounds.max.y, bounds.min.z ),
            new Vector3( bounds.max.x, bounds.min.y, bounds.min.z ),
            new Vector3( bounds.min.x, bounds.max.y, bounds.max.z ),
            new Vector3( bounds.max.x, bounds.min.y, bounds.max.z ),
            new Vector3( bounds.max.x, bounds.max.y, bounds.min.z )
        };

        return bounding_points;
    }

    private void UpdateAnimator()
    {
        // Don't update animator if dead
        if (isDead) return;
        
        float movePercentage = (myAgent.velocity.magnitude / myAgent.speed);
        animator.SetFloat("Speed", movePercentage);
    }
    
    // Public method to check if dead (useful for other scripts)
    public bool IsDead()
    {
        return isDead;
    }
}