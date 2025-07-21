using System;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public sealed class Character : CharacterBehaviour
{
    #region Variables
    private bool running;
    private bool aiming;
	private bool reloading;
	private bool holstering;
	private bool holstered;
	private bool cursorLocked;
	private float lastShotTime;
	private Vector2 axisMovement;
    private Vector2 axisLook;
	[SerializeField] private float maxHealth;
	[SerializeField] private float currentHealth;

	//holding buttons
	private bool holdingButtonAim;
	private bool holdingButtonRun;
	private bool holdingButtonFire;

	private WeaponBehaviour equippedWeapon;
	private WeaponAttachmentManagerBehaviour weaponAttachmentManager;
	private MagazineBehaviour equippedWeaponMagazine;
	[Tooltip("Inventory.")]
	[SerializeField]
	private InventoryBehaviour inventory;

	// animation things
	[Tooltip("Character Animator.")]
	[SerializeField]
	private Animator characterAnimator;
	private int layerOverlay;
	private int layerHolster;
	private int layerActions;

	private static readonly int HashAimingAlpha = Animator.StringToHash("Aiming");
	private static readonly int HashMovement = Animator.StringToHash("Movement");
	[Tooltip("Determines how smooth the locomotion blendspace is.")]
	[SerializeField]
	private float dampTimeLocomotion = 0.15f;
	[Tooltip("How smoothly we play aiming transitions. Beware that this affects lots of things!")]
	[SerializeField]
	private float dampTimeAiming = 0.3f;
	
	// Jump variables
	private bool wantsToJump;
	public bool WantsToJump => wantsToJump;

	[Header("Player Audio")]
	[Tooltip("Hurt/Pain sound clips played when player takes damage")]
	[SerializeField] private AudioClip[] hurtSounds;
	[Tooltip("Death sound clip played when player dies")]
	[SerializeField] private AudioClip deathSound;
	[Tooltip("Volume for hurt and death sounds")]
	[SerializeField] private float painAudioVolume = 0.8f;
	[Tooltip("Minimum time between hurt sounds to prevent spam")]
	[SerializeField] private float hurtSoundCooldown = 0.5f;

	// Simple audio tracking
	private float lastHurtSoundTime = 0f;
	
	// Death state tracking
	private bool isDead = false;
	private bool deathSoundPlayed = false;
	#endregion

	#region Getters

	public override bool IsRunning() => running;
    public override bool IsAiming() => aiming;
    public override bool IsCursorLocked() => cursorLocked;
    public override Vector2 GetInputMovement() => axisMovement;
    public override Vector2 GetInputLook() => axisLook;
	
	/// Returns true if the crosshair should be visible.
	/// </summary>
	public override bool IsCrosshairVisible()
	{
		// Hide crosshair when aiming (zoomed in)
		if (aiming)
			return false;
		
		// Hide crosshair when cursor is not locked (in menu mode)
		if (!cursorLocked)
			return false;
		
		// Hide crosshair when reloading
		if (reloading)
			return false;
		
		// Hide crosshair when holstering/holstered
		if (holstering || holstered)
			return false;
		
		// Show crosshair in all other cases
		return true;
	}
    #endregion

	#region Just Unity Things

	protected override void Awake()
	{
		cursorLocked = true;

		UpdateCursorState();

		inventory.Init();
		RefreshWeaponSetup();
	}

    protected override void Start()
	{
		layerOverlay = characterAnimator.GetLayerIndex("Layer Overlay");
		layerHolster = characterAnimator.GetLayerIndex("Layer Holster");
		layerActions = characterAnimator.GetLayerIndex("Layer Actions");

		currentHealth = maxHealth;
	}

    protected override void Update()
    {
		aiming = holdingButtonAim && CanAim();
		running = holdingButtonRun && CanRun();

		if(holdingButtonFire)
        {
			if(CanPlayAnimationFire() && equippedWeapon.HasAmmunition() && equippedWeapon.IsAutomatic())
            {
				if(Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
                {
					Fire();
                }
            }
        }

		//testing health loss
		if(Input.GetKeyDown(KeyCode.M))
        {
			ChangeCurrentHealth(-10);
        }

		// Reset jump flag after PlayerMovement has processed it
		if (wantsToJump)
		{
			// This will be consumed by PlayerMovement in FixedUpdate
			// We don't reset it here - let PlayerMovement handle that
		}

		//update the animator
		UpdateAnimator();
	}

    #endregion

    #region CHECKS
    private bool CanAim()
    {
		//return false if reloading
		return true;
    }
    private bool CanRun()
    {
		//return false if reloading or aiming
		if (holdingButtonFire || reloading || aiming)
			return false;

		//return false if running backwards or fully sideways
		if (axisMovement.y <= 0 || Math.Abs(Mathf.Abs(axisMovement.x) - 1) < 0.01f)
			return false;

		return true;
    }
	private bool CanChangeWeapon()
    {
		if (holstering || reloading)
			return false;

		return true;
    }
	private bool CanPlayAnimationReload()
    {
		if (reloading)
			return false;

		return true;
    }
    private bool CanPlayAnimationFire()
    {
		if (reloading)
			return false;

		return true;
	}
    #endregion

    #region Audio System
    
    /// <summary>
    /// Play hurt sound using AudioSource.PlayClipAtPoint for simple overlapping
    /// </summary>
    private void PlayHurtSound()
    {
		// Don't play hurt sounds if dead
		if (isDead)
		{
			return;
		}

		// Check cooldown to prevent excessive spam
		if (Time.time - lastHurtSoundTime < hurtSoundCooldown)
		{
			return;
		}

		// Check if hurt sounds are available
		if (hurtSounds == null || hurtSounds.Length == 0)
		{
			Debug.LogWarning("No hurt sounds assigned!");
			return;
		}

		// Get random hurt sound
		AudioClip hurtClip = hurtSounds[UnityEngine.Random.Range(0, hurtSounds.Length)];
		if (hurtClip == null)
		{
			Debug.LogWarning("Selected hurt sound clip is null!");
			return;
		}

		// Play sound at player position - this creates a temporary AudioSource automatically
		// The sound will play completely without being cut off
		AudioSource.PlayClipAtPoint(hurtClip, transform.position, painAudioVolume);
		
		lastHurtSoundTime = Time.time;
		Debug.Log($"Playing hurt sound: {hurtClip.name}");
    }

    /// <summary>
    /// Play death sound using AudioSource.PlayClipAtPoint for consistency and reliability
    /// </summary>
    private void PlayDeathSound()
    {
		// Ensure death sound only plays once
		if (deathSoundPlayed)
		{
			Debug.Log("Death sound already played, skipping");
			return;
		}

		if (deathSound == null)
		{
			Debug.LogWarning("No death sound assigned!");
			return;
		}

		// Mark as played before playing to prevent race conditions
		deathSoundPlayed = true;

		// Use PlayClipAtPoint for death sound too - more reliable
		// This ensures the sound plays even if the player GameObject gets disabled/destroyed
		AudioSource.PlayClipAtPoint(deathSound, transform.position, painAudioVolume);

		Debug.Log($"Playing death sound: {deathSound.name} (first and only time)");
    }

    /// <summary>
    /// Alternative method using GameObject pooling for even better control
    /// Uncomment this and replace PlayHurtSound() if you want more control over hurt sounds
    /// </summary>
    /*
    private void PlayHurtSoundPooled()
    {
        if (Time.time - lastHurtSoundTime < hurtSoundCooldown) return;
        if (hurtSounds == null || hurtSounds.Length == 0) return;

        AudioClip hurtClip = hurtSounds[UnityEngine.Random.Range(0, hurtSounds.Length)];
        if (hurtClip == null) return;

        // Create temporary GameObject for this sound
        GameObject soundObject = new GameObject($"HurtSound_{Time.time}");
        soundObject.transform.position = transform.position;
        
        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = hurtClip;
        source.volume = painAudioVolume;
        source.spatialBlend = 0.0f;
        source.Play();
        
        // Destroy the GameObject after the clip finishes
        Destroy(soundObject, hurtClip.length + 0.1f);
        
        lastHurtSoundTime = Time.time;
        Debug.Log($"Playing hurt sound (pooled): {hurtClip.name}");
    }
    */

    #endregion

    #region Functions
    private void UpdateCursorState()
	{
		//Update cursor visibility.
		Cursor.visible = !cursorLocked;
		//Update cursor lock state.
		Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
	}

	private IEnumerator Equip(int index = 0)
    {
		if(!holstered)
        {
			SetHolstered(holstering = true);
			//wait
			yield return new WaitUntil(() => holstering == false);
        }
		//unholster
		SetHolstered(false);
		characterAnimator.Play("Unholster", layerHolster, 0);

		inventory.Equip(index);
		RefreshWeaponSetup();
    }

	private void RefreshWeaponSetup()
    {
		if ((equippedWeapon = inventory.GetEquippedWeapon()) == null)
			return;

		equippedWeapon.SetOwner(this);

		characterAnimator.runtimeAnimatorController = equippedWeapon.GetAnimatorController();

		weaponAttachmentManager = equippedWeapon.GetAttachmentManager();
		if (weaponAttachmentManager == null)
			return;

		equippedWeaponMagazine = weaponAttachmentManager.GetEquippedMagazine();

		//update the UI
		StartCoroutine(ChangeWeaponUI());
    }

	private IEnumerator ChangeWeaponUI()
    {
		yield return new WaitForEndOfFrame();
		GameMan.Instance.gameUIInstance.ChangeWeapon(equippedWeapon.GetWeaponSprite(), equippedWeapon.GetAmmunitionCurrent(), equippedWeapon.GetAmmunitionTotal());
	}

	private void SetHolstered(bool value = true)
    {
		holstered = value;

		characterAnimator.SetBool("Holstered", holstered);
    }

	private void Fire()
    {
        lastShotTime = Time.time;
        
        // Always call weapon.Fire() - let the weapon handle empty cases
        equippedWeapon.Fire();
        
        // Play fire animation
        characterAnimator.CrossFade("Fire", 0.05f, layerOverlay, 0);
    }

    private void FireEmpty()
    {
        lastShotTime = Time.time;
        
        // Play empty fire sound through weapon
        Weapon weapon = equippedWeapon as Weapon;
        if (weapon != null)
        {
            weapon.PlayEmptyFireSound();
        }
        
        // Play empty fire animation
        characterAnimator.CrossFade("Fire Empty", 0.05f, layerOverlay, 0);
    }

    public override void ChangeCurrentHealth(int amount)
    {
		// Don't process health changes if already dead
		if (isDead)
		{
			Debug.Log("Player is already dead, ignoring health change");
			return;
		}

		float previousHealth = currentHealth;
		currentHealth += amount;
		
		if (currentHealth > maxHealth)
			currentHealth = maxHealth;
		
		// Play hurt sound if taking damage (amount is negative) and still alive
		if (amount < 0 && currentHealth > 0)
		{
			PlayHurtSound();
			Debug.Log($"Player took {-amount} damage. Health: {currentHealth}/{maxHealth}");
		}
		
		if (currentHealth <= 0)
		{
			Die();
		}
		else
		{
			// Update health bar for non-fatal damage
			GameMan.Instance.gameUIInstance.UpdateHealthBar(currentHealth / maxHealth);
		}
    }

	private void Die()
    {
		// Prevent multiple death calls
		if (isDead)
		{
			Debug.Log("Die() called but player is already dead");
			return;
		}

		// Mark as dead immediately
		isDead = true;
		
		Debug.Log("Player died!");
		
		// Play death sound FIRST (only plays once due to deathSoundPlayed flag)
		PlayDeathSound();
		
		// Update health bar to zero
		GameMan.Instance.gameUIInstance.UpdateHealthBar(0f);
		
		// Longer delay to let death sound play
		StartCoroutine(DelayedGameOver());
    }

	private IEnumerator DelayedGameOver()
	{
		// Wait longer to ensure death sound plays
		// If deathSound is assigned, wait for its length + buffer
		float waitTime = 0.5f; // Default wait time
		
		if (deathSound != null)
		{
			waitTime = Mathf.Max(deathSound.length + 0.2f, 0.5f);
			Debug.Log($"Waiting {waitTime} seconds for death sound to play (clip length: {deathSound.length})");
		}
		
		yield return new WaitForSeconds(waitTime);
		
		Debug.Log("Triggering Game Over after death sound delay");
		GameMan.Instance.GameOver();
	}

	/// <summary>
	/// Reset player state (useful for respawning or restarting)
	/// </summary>
	public void ResetPlayerState()
	{
		isDead = false;
		deathSoundPlayed = false;
		currentHealth = maxHealth;
		lastHurtSoundTime = 0f;
		
		Debug.Log("Player state reset - ready for new game");
	}

    #endregion

    #region Input Functions

    public void OnTryAiming(InputAction.CallbackContext context)
    {
		//ignore this if the cursor is not locked 
		if (!cursorLocked)
			return;

        switch (context.phase)
        {
            case InputActionPhase.Started:
				holdingButtonAim = true;
                break;
            case InputActionPhase.Canceled:
				holdingButtonAim = false;
                break;
        }
    }

	public void OnTryRun(InputAction.CallbackContext context)
	{
		//ignore this if the cursor is not locked 
		if (!cursorLocked)
			return;

		//Switch.
		switch (context.phase)
		{
			case InputActionPhase.Started:
				holdingButtonRun = true;
				break;
			case InputActionPhase.Canceled:
				holdingButtonRun = false;
				break;
		}
	}

	public void OnTryPlayReload(InputAction.CallbackContext context)
	{
		//ignore this if the cursor is not locked 
		if (!cursorLocked)
			return;
		if (!CanPlayAnimationReload())
			return;

		switch(context)
        {
			case { phase: InputActionPhase.Performed }:
				PlayReloadAnimation();
				break;
        }
	}

	public void OnTryFire(InputAction.CallbackContext context)
	{
		//ignore this if the cursor is not locked 
		if (!cursorLocked)
			return;

        switch (context.phase)
        {
            case InputActionPhase.Started:
				holdingButtonFire = true;
                break;
            case InputActionPhase.Performed:
				if (!CanPlayAnimationFire())
					break;

				if (equippedWeapon.HasAmmunition())
				{
					if (equippedWeapon.IsAutomatic())
						break;

					if (Time.time - lastShotTime > 60.0 / equippedWeapon.GetRateOfFire())
						Fire();
				}
				else
					FireEmpty();
                break;
            case InputActionPhase.Canceled:
				holdingButtonFire = false;
                break;
            default:
                break;
        }
    }

	/// <summary>
	/// Next Inventory Weapon - handles both button press and scroll wheel
	/// </summary>
	public void OnTryInventoryNext(InputAction.CallbackContext context)
	{
		//Block while the cursor is unlocked.
		if (!cursorLocked)
			return;
		
		//Null Check.
		if (inventory == null)
			return;
		
		//Switch.
		switch (context)
		{
			//Performed.
			case {phase: InputActionPhase.Performed}:
				//Get the index increment direction for our inventory using the scroll wheel direction. If we're not
				//actually using one, then just increment by one.
				float scrollValue = context.valueType.IsEquivalentTo(typeof(Vector2)) ? Mathf.Sign(context.ReadValue<Vector2>().y) : 1.0f;
				
				//Get the next index to switch to.
				int indexNext = scrollValue > 0 ? inventory.GetNextIndex() : inventory.GetPrevIndex();
				//Get the current weapon's index.
				int indexCurrent = inventory.GetEquippedIndex();
				
				//Make sure we're allowed to change, and also that we're not using the same index, otherwise weird things happen!
				if (CanChangeWeapon() && (indexCurrent != indexNext))
					StartCoroutine(nameof(Equip), indexNext);
				break;
		}
	}

	/// <summary>
	/// For scroll wheel weapon switching
	/// </summary>
	public void OnTryInventoryNextWheel(InputAction.CallbackContext context)
	{
		//Block while the cursor is unlocked.
		if (!cursorLocked)
			return;
		
		//Null Check.
		if (inventory == null)
			return;
		
		//Switch.
		switch (context)
		{
			//Performed.
			case {phase: InputActionPhase.Performed}:
				//Read scroll wheel value
				Vector2 scrollVector = context.ReadValue<Vector2>();
				float scrollValue = scrollVector.y;
				
				// Only process if there's actual scroll input
				if (Mathf.Abs(scrollValue) < 0.1f)
					return;
				
				//Get the next index to switch to based on scroll direction
				int indexNext = scrollValue > 0 ? inventory.GetNextIndex() : inventory.GetPrevIndex();
				//Get the current weapon's index.
				int indexCurrent = inventory.GetEquippedIndex();
				
				//Make sure we're allowed to change, and also that we're not using the same index
				if (CanChangeWeapon() && (indexCurrent != indexNext))
					StartCoroutine(nameof(Equip), indexNext);
				break;
		}
	}

    public void OnLockCursor(InputAction.CallbackContext context)
	{
		//Switch.
		switch (context)
		{
			//Performed.
			case { phase: InputActionPhase.Performed }:
				//Toggle the cursor locked value.
				cursorLocked = !cursorLocked;
				//Update the cursor's state.
				UpdateCursorState();
				break;
		}
	}

	public void OnMove(InputAction.CallbackContext context)
	{
		//Read.
		axisMovement = cursorLocked ? context.ReadValue<Vector2>() : default;
	}
	/// <summary>
	/// Look.
	/// </summary>
	public void OnLook(InputAction.CallbackContext context)
	{
		//Read.
		axisLook = cursorLocked ? context.ReadValue<Vector2>() : default;
	}

	/// <summary>
	/// Jump input handler
	/// </summary>
	public void OnJump(InputAction.CallbackContext context)
	{
		//Block while the cursor is unlocked.
		if (!cursorLocked)
			return;
		
		//Switch.
		switch (context.phase)
		{
			//Performed.
			case InputActionPhase.Performed:
				//Set jump flag
				wantsToJump = true;
				Debug.Log("Jump input received");
				break;
		}
	}

	// Public method to reset jump flag (called by PlayerMovement)
	public void ResetJump()
	{
		wantsToJump = false;
	}

	#endregion

	#region Animations

	private void PlayReloadAnimation()
    {
		string stateName = equippedWeapon.HasAmmunition() ? "Reload" : "Reload Empty";
		characterAnimator.Play(stateName, layerActions, 0.0f);

		reloading = true;

		equippedWeapon.Reload();
    }

	#endregion

	#region Animation Functions

	private void UpdateAnimator()
    {
		//move based on actual value not per-axis
		characterAnimator.SetFloat(HashMovement, Mathf.Clamp01(Mathf.Abs(axisMovement.x) + Mathf.Abs(axisMovement.y)), dampTimeLocomotion, Time.deltaTime);

		//update the aiming value to transition properly
		characterAnimator.SetFloat(HashAimingAlpha, Convert.ToSingle(aiming), 0.25f / 1.0f * dampTimeAiming, Time.deltaTime);

		characterAnimator.SetBool("Aim", aiming);
		characterAnimator.SetBool("Running", running);
		
		// If your animator has a jump trigger or bool, you can add it here
		// characterAnimator.SetBool("InAir", !isGrounded);
    }

	public override void AnimationEndedHolster()
	{
		//Stop Holstering.
		holstering = false;
	}
	public override void FillAmmunition(int amount)
	{
		//Notify the weapon to fill the ammunition by the amount.
		if (equippedWeapon != null)
			equippedWeapon.FillAmmunition(amount);
	}
	public override void AnimationEndedReload()
	{
		//Stop reloading!
		reloading = false;
	}

	#endregion

	#region Testing/Debug Methods

	[ContextMenu("Test Hurt Sound")]
	public void TestHurtSound()
	{
		PlayHurtSound();
	}

	[ContextMenu("Test Death Sound")]
	public void TestDeathSound()
	{
		Debug.Log("Testing death sound manually");
		PlayDeathSound();
	}

	[ContextMenu("Test Full Death")]
	public void TestFullDeath()
	{
		Debug.Log("Testing full death sequence");
		currentHealth = 1; // Set health to 1 so the next damage kills
		ChangeCurrentHealth(-1); // This should trigger death
	}

	[ContextMenu("Reset Player State")]
	public void TestResetPlayerState()
	{
		ResetPlayerState();
	}

	[ContextMenu("Check Player Status")]
	public void CheckPlayerStatus()
	{
		Debug.Log($"Player Status - Health: {currentHealth}/{maxHealth}, Is Dead: {isDead}, Death Sound Played: {deathSoundPlayed}");
	}

	[ContextMenu("Test Take Damage")]
	public void TestTakeDamage()
	{
		ChangeCurrentHealth(-10);
	}

	[ContextMenu("Test Rapid Damage")]
	public void TestRapidDamage()
	{
		StartCoroutine(RapidDamageTest());
	}

	private IEnumerator RapidDamageTest()
	{
		for (int i = 0; i < 5; i++)
		{
			ChangeCurrentHealth(-5);
			yield return new WaitForSeconds(0.2f);
		}
	}

	#endregion
}