using System;
using System.Linq;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region Audio

    [Header("Audio Clips")]

    [Tooltip("The audio clip that is played while walking.")]
    [SerializeField]
    private AudioClip audioClipWalking;

    [Tooltip("The audio clip that is played while running.")]
    [SerializeField]
    private AudioClip audioClipRunning;
    
    [Tooltip("The audio clip that is played when jumping.")]
    [SerializeField]
    private AudioClip audioClipJump;
    
    [Tooltip("The audio clip that is played when landing.")]
    [SerializeField]
    private AudioClip audioClipLand;

    #endregion

    #region Variables

    private Rigidbody rigidBody;
    private CapsuleCollider capsule;
    private AudioSource audioSource;
    private AudioSource audioSourceEffects; // For jump/land sounds
    [SerializeField]
    private CharacterBehaviour playerCharacter;
    
    // Cast to Character to access jump flag
    private Character character;

    [Header("Movement Settings")]
    [SerializeField]
    private float speedWalking = 5.0f;
    [SerializeField]
    private float speedRunning = 9.0f;
    
    [Header("Jump Settings")]
    [SerializeField]
    private float jumpForce = 8.0f;
    [SerializeField]
    private float gravityMultiplier = 1.5f; // Makes jumping feel more responsive
    [SerializeField]
    private float groundCheckDistance = 0.1f;
    
    /// <summary>
    /// If this is True then the character is currently grounded.
    /// </summary>
    private bool isGrounded;
    private bool wasGrounded; // To detect landing
    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    #endregion

    #region GetSet
    private Vector3 Velocity
    {
        get => rigidBody.linearVelocity;
        set => rigidBody.linearVelocity = value;
    }
    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
        capsule = GetComponent<CapsuleCollider>();
        
        // Cast to Character type to access jump flag
        character = playerCharacter as Character;

        //Audio Source Setup for footsteps.
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = audioClipWalking;
        audioSource.loop = true;
        
        // Create a second audio source for jump/land effects
        audioSourceEffects = gameObject.AddComponent<AudioSource>();
        audioSourceEffects.playOnAwake = false;
        audioSourceEffects.loop = false;
    }

    private void Update()
    {
        // Check for landing
        if (!wasGrounded && isGrounded)
        {
            OnLanded();
        }
        
        wasGrounded = isGrounded;
    }

    private void FixedUpdate()
    {
        // Check ground status
        CheckGrounded();
        
        // Handle jumping
        HandleJump();
        
        // Move character
        MoveCharacter();
        
        // Apply extra gravity for better jump feel
        if (!isGrounded && rigidBody.linearVelocity.y < 0)
        {
            rigidBody.linearVelocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void CheckGrounded()
    {
        //Bounds.
        Bounds bounds = capsule.bounds;
        //Extents.
        Vector3 extents = bounds.extents;
        //Radius.
        float radius = extents.x - 0.01f;

        //Cast. This checks whether there is indeed ground, or not.
        Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down,
            groundHits, extents.y - radius * 0.5f + groundCheckDistance, ~0, QueryTriggerInteraction.Ignore);

        //Check if we have valid ground hits
        isGrounded = groundHits.Any(hit => hit.collider != null && hit.collider != capsule);
        
        //Clear the array for next check
        if (isGrounded)
        {
            for (var i = 0; i < groundHits.Length; i++)
                groundHits[i] = new RaycastHit();
        }
    }

    private void MoveCharacter()
    {
        //calculate velocity
        Vector2 frameInput = playerCharacter.GetInputMovement();
        Vector3 movement = new Vector3(frameInput.x, 0.0f, frameInput.y);

        //calc speed
        if (playerCharacter.IsRunning())
            movement *= speedRunning;
        else
            movement *= speedWalking;

        //convert to world space to apply as velocity
        movement = transform.TransformDirection(movement);

        // Preserve Y velocity (for jumping/falling)
        Velocity = new Vector3(movement.x, rigidBody.linearVelocity.y, movement.z);
        
        // Play footstep sounds
        PlayFootstepSounds();
    }
    
    private void HandleJump()
    {
        // Check if character wants to jump and is grounded
        if (character != null && character.WantsToJump && isGrounded)
        {
            // Apply jump force
            rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, jumpForce, rigidBody.linearVelocity.z);
            
            // Play jump sound
            if (audioClipJump != null && audioSourceEffects != null)
            {
                audioSourceEffects.PlayOneShot(audioClipJump);
            }
            
            // Reset jump flag
            character.ResetJump();
            
            Debug.Log("Jump executed!");
        }
    }
    
    private void OnLanded()
    {
        // Play landing sound
        if (audioClipLand != null && audioSourceEffects != null)
        {
            audioSourceEffects.PlayOneShot(audioClipLand);
        }
        
        Debug.Log("Landed!");
    }
    
    private void PlayFootstepSounds()
    {
        //Check if we're moving on the ground. We don't need footsteps in the air.
        if (isGrounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f)
        {
            //Select the correct audio clip to play.
            audioSource.clip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;
            //Play it!
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        //Pause it if we're doing something like flying, or not moving!
        else if (audioSource.isPlaying)
            audioSource.Pause();
    }
}