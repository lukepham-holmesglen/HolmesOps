using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class ZombieAIStateMachine : MonoBehaviour
{
    [Header("AI Components")]
    [SerializeField] private NavMeshAgent myAgent;
    [SerializeField] private Animator animator;
    
    [Header("Movement Settings")]
    [SerializeField] public float roamingRange = 10f;
    [SerializeField] public float detectRange = 8f;
    [SerializeField] public float attackRange = 2f;
    [SerializeField] private float detectionDelay = 0.5f;
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 2.5f;
    
    [Header("Patrol Settings")]
    [SerializeField] private bool useRandomPatrolling = true;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float randomPatrolChance = 0.7f; // 70% chance to patrol instead of idle
    [SerializeField] private int maxPatrolAttempts = 10; // Max attempts to find valid patrol point
    
    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackAnimationTime = 1.2f;
    [SerializeField] private LayerMask playerMask = 1;
    
    [Header("Animation Blend Parameters")]
    [SerializeField] private string walkBlendParam = "WalkBlend";
    [SerializeField] private string idleBlendParam = "IdleBlend";
    [SerializeField] private string attackBlendParam = "AttackBlend";
    
    [Header("Animation Variant Counts")]
    [SerializeField] private int numberOfWalkAnimations = 13;
    [SerializeField] private int numberOfIdleAnimations = 17;
    [SerializeField] private int numberOfAttackAnimations = 6;
    
    [Header("Audio")]
    [SerializeField] private AudioClip[] growlSounds;
    [SerializeField] private AudioClip[] attackSounds;
    [SerializeField] private AudioClip[] idleSounds;
    [SerializeField] private float audioVolume = 0.7f;
    [SerializeField] private float randomSoundInterval = 5f;
    
    // State tracking
    public GameObject attackTarget;
    private Vector3 lastKnownPosition;
    private Vector3 currentPatrolTarget;
    private float attackTimer;
    private bool isAttacking = false;
    private AudioSource audioSource;
    private Coroutine randomSoundCoroutine;
    
    // Current animation tracking
    private bool isCurrentlyMoving = false;
    
    // Animation hashes for performance
    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashAttack = Animator.StringToHash("Attack");
    private static readonly int HashAttacking = Animator.StringToHash("Attacking");
    private static readonly int HashDeath = Animator.StringToHash("Death");

    public enum ZombieState
    {
        Idle,
        Patrolling,
        Roaming,
        Chasing,
        Attacking,
        Dying
    }

    public ZombieState currentState;
    private bool isDead = false;
    
    private void Start()
    {
        SetupZombie();
        RandomizeAnimations();
        ChooseInitialBehavior();
        StartCoroutine(SearchForPlayer());
        
        if (randomSoundCoroutine == null)
            randomSoundCoroutine = StartCoroutine(PlayRandomSounds());
    }
    
    private void SetupZombie()
    {
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = audioVolume;
        audioSource.spatialBlend = 1.0f; // 3D audio
        
        // Setup NavMesh agent
        if (myAgent != null)
        {
            myAgent.speed = walkSpeed;
            myAgent.acceleration = 4f;
            myAgent.angularSpeed = 120f;
        }
    }
    
    private void RandomizeAnimations()
    {
        // Log the available animation counts
        //Debug.Log($"Zombie {gameObject.name} initialized with {numberOfIdleAnimations} idles, {numberOfWalkAnimations} walks, {numberOfAttackAnimations} attacks");
        
        // Set initial random animations
        SetRandomAnimationBlend();
    }
    
    private void SetRandomAnimationBlend()
    {
        if (animator == null) return;

        switch (currentState)
        {
            case ZombieState.Idle:
                float idleBlend = Random.Range(0, numberOfIdleAnimations);
                animator.SetFloat(idleBlendParam, idleBlend);
                //Debug.Log($"Zombie {gameObject.name} set idle blend to: {idleBlend} (out of {numberOfIdleAnimations})");
                break;

            case ZombieState.Patrolling:
            case ZombieState.Roaming:
            case ZombieState.Chasing:
                float walkBlend = Random.Range(0, numberOfWalkAnimations);
                animator.SetFloat(walkBlendParam, walkBlend);
                //Debug.Log($"Zombie {gameObject.name} set walk blend to: {walkBlend} (out of {numberOfWalkAnimations})");
                break;

            case ZombieState.Attacking:
                float attackBlend = Random.Range(0, numberOfAttackAnimations);
                animator.SetFloat(attackBlendParam, attackBlend);
                //Debug.Log($"Zombie {gameObject.name} set attack blend to: {attackBlend} (out of {numberOfAttackAnimations})");
                break;
        }
    }
    
    private void ChooseInitialBehavior()
    {
        if (useRandomPatrolling && Random.value < randomPatrolChance)
        {
            SetState(ZombieState.Patrolling);
            MoveToRandomPatrolPoint();
        }
        else
        {
            SetState(ZombieState.Idle);
            StartCoroutine(IdleBehavior());
        }
    }
    
    public void SetState(ZombieState newState)
    {
        if (isDead && newState != ZombieState.Dying)
            return;
            
        ZombieState previousState = currentState;
        currentState = newState;
        
        // Set random animation blend for the new state
        SetRandomAnimationBlend();
        
        if (newState == ZombieState.Dying)
        {
            isDead = true;
            
            // STOP ALL AUDIO IMMEDIATELY
            StopAllAudioAndCoroutines();
            
            // Stop all movement
            if (myAgent != null)
            {
                myAgent.isStopped = true;
                myAgent.ResetPath();
            }
        }
        
        //Debug.Log($"Zombie {gameObject.name} transitioned from {previousState} to {newState}");
    }
    
    // NEW METHOD: Stop all audio and coroutines when dying
    private void StopAllAudioAndCoroutines()
    {
        // Stop all coroutines
        StopAllCoroutines();
        
        // Stop any currently playing audio
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        
        // Clear the random sound coroutine reference
        randomSoundCoroutine = null;
        
        //Debug.Log($"Stopped all audio and coroutines for dying zombie {gameObject.name}");
    }
    
    public void SetAttackTarget(GameObject newTarget)
    {
        if (isDead) return;
        
        attackTarget = newTarget;
        SetState(ZombieState.Chasing);
        myAgent.speed = chaseSpeed;
        StopCoroutine(nameof(IdleBehavior));
        StopCoroutine(nameof(PatrolBehavior));
        StartCoroutine(TrackPlayer());
    }
    
    private void Update()
    {
        if (isDead) return;
        
        switch (currentState)
        {
            case ZombieState.Idle:
                // Handled by coroutine
                break;
                
            case ZombieState.Patrolling:
                HandlePatrolling();
                break;
                
            case ZombieState.Roaming:
                HandleRoaming();
                break;
                
            case ZombieState.Chasing:
                HandleChasing();
                break;
                
            case ZombieState.Attacking:
                HandleAttacking();
                break;
                
            case ZombieState.Dying:
                if (myAgent != null)
                {
                    myAgent.isStopped = true;
                    myAgent.ResetPath();
                }
                break;
        }
        
        UpdateAnimator();
        
        // Attack cooldown
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }
    }
    
    private void HandlePatrolling()
    {
        if (myAgent.remainingDistance <= myAgent.stoppingDistance && !myAgent.pathPending)
        {
            StartCoroutine(PatrolBehavior());
        }
    }
    
    private void HandleRoaming()
    {
        if (myAgent.remainingDistance <= myAgent.stoppingDistance && !myAgent.pathPending)
        {
            Vector3 point;
            if (RandomPoint(transform.position, roamingRange, out point))
            {
                myAgent.SetDestination(point);
            }
            else
            {
                // If can't find random point, go to idle
                SetState(ZombieState.Idle);
                StartCoroutine(IdleBehavior());
            }
        }
    }
    
    private void HandleChasing()
    {
        if (attackTarget == null)
        {
            // Lost target, return to previous behavior
            ChooseInitialBehavior();
            return;
        }
        
        lastKnownPosition = attackTarget.transform.position;
        
        // Check if in attack range
        float distanceToTarget = Vector3.Distance(transform.position, attackTarget.transform.position);
        
        if (distanceToTarget <= attackRange && attackTimer <= 0)
        {
            SetState(ZombieState.Attacking);
            AttackTarget();
        }
        else if (distanceToTarget > detectRange)
        {
            // Target too far, lose interest
            attackTarget = null;
            ChooseInitialBehavior();
        }
        else
        {
            // Continue chasing
            myAgent.SetDestination(lastKnownPosition);
        }
    }
    
    private void HandleAttacking()
    {
        if (!isAttacking)
        {
            // Attack animation finished, return to chasing
            SetState(ZombieState.Chasing);
        }
    }
    
    private IEnumerator IdleBehavior()
    {
        float idleTime = Random.Range(3f, 8f);
        
        // Set random idle animation blend
        SetRandomAnimationBlend();
        
        // Occasionally play idle sounds
        if (Random.value < 0.3f && idleSounds.Length > 0)
        {
            PlayRandomSound(idleSounds);
        }
        
        yield return new WaitForSeconds(idleTime);
        
        if (currentState == ZombieState.Idle && !isDead)
        {
            // Choose next behavior
            if (useRandomPatrolling && Random.value < 0.5f)
            {
                SetState(ZombieState.Patrolling);
                MoveToRandomPatrolPoint();
            }
            else
            {
                SetState(ZombieState.Roaming);
                myAgent.speed = walkSpeed;
            }
        }
    }
    
    private IEnumerator PatrolBehavior()
    {
        // Wait at patrol point
        float waitTime = Random.Range(patrolWaitTime * 0.5f, patrolWaitTime * 1.5f);
        
        // Set to idle state for waiting and set random idle animation
        SetState(ZombieState.Idle);
        
        yield return new WaitForSeconds(waitTime);
        
        if (currentState == ZombieState.Idle && !isDead)
        {
            // Move to next random patrol point
            SetState(ZombieState.Patrolling);
            MoveToRandomPatrolPoint();
        }
    }
    
    private void MoveToRandomPatrolPoint()
    {
        Vector3 randomPoint;
        if (RandomPoint(transform.position, roamingRange, out randomPoint))
        {
            currentPatrolTarget = randomPoint;
            myAgent.speed = walkSpeed;
            myAgent.SetDestination(randomPoint);
            //Debug.Log($"Zombie {gameObject.name} moving to random patrol point: {randomPoint}");
        }
        else
        {
            // If can't find patrol point, switch to idle
            SetState(ZombieState.Idle);
            StartCoroutine(IdleBehavior());
        }
    }
    
    private void AttackTarget()
    {
        if (attackTarget == null || attackTimer > 0) return;
        
        isAttacking = true;
        attackTimer = attackCooldown;
        
        // Stop movement
        myAgent.isStopped = true;
        
        // Face target
        Vector3 directionToTarget = (attackTarget.transform.position - transform.position).normalized;
        directionToTarget.y = 0;
        if (directionToTarget != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionToTarget);
        }
        
        // Set random attack animation blend
        SetRandomAnimationBlend();
        
        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger(HashAttack);
            animator.SetBool(HashAttacking, true);
        }
        
        //Debug.Log($"Zombie {gameObject.name} attacking with blend: {animator.GetFloat(attackBlendParam)}");
        
        // Start attack sequence
        StartCoroutine(AttackSequence());
    }
    
    private IEnumerator AttackSequence()
    {
        // Wait for full animation duration as fallback
        // The animation events should handle damage and completion
        yield return new WaitForSeconds(attackAnimationTime);
        
        // Only reset if still alive and attacking
        if (isAttacking && !isDead)
        {
            //Debug.Log("Attack sequence fallback - animation events may not be set up");
            isAttacking = false;
            if (animator != null)
            {
                animator.SetBool(HashAttacking, false);
            }
            
            // Resume movement
            myAgent.isStopped = false;
        }
    }
    
    private IEnumerator TrackPlayer()
    {
        while (currentState == ZombieState.Chasing && !isDead)
        {
            yield return new WaitForSeconds(detectionDelay);
            
            if (isDead || attackTarget == null) break;
            
            // Check if can still see player
            Vector3[] points = GetBoundingPoints(attackTarget.GetComponent<Collider>().bounds);
            int hiddenPoints = 0;
            
            foreach (Vector3 point in points)
            {
                Vector3 targetDirection = point - transform.position;
                float targetDist = Vector3.Distance(transform.position, point);
                
                if (IsPointCovered(targetDirection, targetDist) || targetDist > detectRange)
                {
                    hiddenPoints++;
                }
            }
            
            if (hiddenPoints >= points.Length)
            {
                // Lost sight of player
                attackTarget = null;
                myAgent.speed = walkSpeed;
                ChooseInitialBehavior();
                StopCoroutine(TrackPlayer());
                StartCoroutine(SearchForPlayer());
            }
        }
    }
    
    private IEnumerator SearchForPlayer()
    {
        GameObject player = GameMan.Instance.playerInstance;
        
        while ((currentState == ZombieState.Idle || currentState == ZombieState.Patrolling || 
                currentState == ZombieState.Roaming) && !isDead)
        {
            yield return new WaitForSeconds(detectionDelay);
            
            if (isDead || player == null) break;
            
            Vector3[] points = GetBoundingPoints(player.GetComponent<Collider>().bounds);
            int hiddenPoints = 0;
            
            foreach (Vector3 point in points)
            {
                Vector3 targetDirection = point - transform.position;
                float targetDist = Vector3.Distance(transform.position, point);
                
                if (IsPointCovered(targetDirection, targetDist) || targetDist > detectRange)
                {
                    hiddenPoints++;
                }
            }
            
            if (hiddenPoints < points.Length)
            {
                // Found player!
                if (growlSounds.Length > 0)
                {
                    PlayRandomSound(growlSounds);
                }
                SetAttackTarget(player);
                StopCoroutine(SearchForPlayer());
            }
        }
    }
    
    private IEnumerator PlayRandomSounds()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(Random.Range(randomSoundInterval * 0.5f, randomSoundInterval * 2f));
            
            if (isDead) break; // Double-check death status
            
            // Play random growl/idle sound
            if (Random.value < 0.3f)
            {
                if (currentState == ZombieState.Chasing && growlSounds.Length > 0)
                {
                    PlayRandomSound(growlSounds);
                }
                else if (idleSounds.Length > 0)
                {
                    PlayRandomSound(idleSounds);
                }
            }
        }
        
        // Clear coroutine reference when it ends
        randomSoundCoroutine = null;
        //Debug.Log($"PlayRandomSounds coroutine ended for zombie {gameObject.name}");
    }
    
    private void PlayRandomSound(AudioClip[] clips)
    {
        // Don't play sounds if dead
        if (isDead || clips.Length == 0 || audioSource == null) return;
        
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        audioSource.PlayOneShot(clip, audioVolume);
    }
    
    private bool RandomPoint(Vector3 aroundPoint, float range, out Vector3 result)
    {
        for (int i = 0; i < maxPatrolAttempts; i++)
        {
            Vector3 randomPoint = aroundPoint + Random.insideUnitSphere * range;
            randomPoint.y = aroundPoint.y; // Keep on same height level
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, range * 0.5f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        
        result = Vector3.zero;
        return false;
    }
    
    private bool IsPointCovered(Vector3 targetDir, float targetDist)
    {
        Vector3 eyePosition = transform.position + Vector3.up * 0.5f;
        RaycastHit[] hits = Physics.RaycastAll(eyePosition, targetDir.normalized, detectRange, ~0);
        
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject == gameObject) continue;
            
            float coverDist = Vector3.Distance(eyePosition, hit.point);
            if (coverDist <= targetDist)
                return true;
        }
        
        return false;
    }
    
    private Vector3[] GetBoundingPoints(Bounds bounds)
    {
        return new Vector3[]
        {
            bounds.min,
            bounds.max,
            new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.min.z)
        };
    }
    
    private void UpdateAnimator()
    {
        if (isDead || animator == null) return;
        
        float movePercentage = myAgent.velocity.magnitude / myAgent.speed;
        animator.SetFloat(HashSpeed, movePercentage);
        
        bool isMoving = movePercentage > 0.1f;
        
        // Handle animation state changes
        if (isMoving && !isCurrentlyMoving)
        {
            // Just started moving - set random animation for current state
            isCurrentlyMoving = true;
            SetRandomAnimationBlend();
        }
        else if (!isMoving && isCurrentlyMoving)
        {
            // Just stopped moving - transition to idle
            isCurrentlyMoving = false;
            if (currentState != ZombieState.Attacking)
            {
                SetState(ZombieState.Idle);
            }
        }
    }
    
    // Public methods for external access
    public bool IsDead() => isDead;
    public ZombieState GetCurrentState() => currentState;
    
    // Method to force a new random patrol destination
    public void SetNewRandomPatrolTarget()
    {
        if (currentState == ZombieState.Patrolling || currentState == ZombieState.Roaming)
        {
            MoveToRandomPatrolPoint();
        }
    }
    
    // Method to get current patrol target (useful for debugging)
    public Vector3 GetCurrentPatrolTarget() => currentPatrolTarget;
    
    // Animation Event Method - Called by animation event at impact moment
    public void OnAttackImpact()
    {
        // Don't execute if dead
        if (isDead) return;
        
        // Called by animation event when attack should deal damage and play sound
        
        // Play attack sound
        if (attackSounds.Length > 0)
        {
            PlayRandomSound(attackSounds);
        }
        
        // Deal damage if target is in range
        if (attackTarget != null)
        {
            float distance = Vector3.Distance(transform.position, attackTarget.transform.position);
            if (distance <= attackRange)
            {
                Character player = attackTarget.GetComponent<Character>();
                if (player != null)
                {
                    player.ChangeCurrentHealth(-(int)attackDamage);
                    //Debug.Log($"Zombie dealt {attackDamage} damage to player via animation event!");
                }
            }
            else
            {
                //Debug.Log($"Attack missed - player too far ({distance:F2}m > {attackRange}m)");
            }
        }
    }
}