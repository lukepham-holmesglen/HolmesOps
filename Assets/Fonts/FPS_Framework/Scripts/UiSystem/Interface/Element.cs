using UnityEngine;

/// <summary>
/// Interface Element.
/// </summary>
public abstract class Element : MonoBehaviour
{
    #region FIELDS
    
    /// <summary>
    /// Player Character.
    /// </summary>
    protected CharacterBehaviour playerCharacter;
    /// <summary>
    /// Player Character Inventory.
    /// </summary>
    protected InventoryBehaviour playerCharacterInventory;

    /// <summary>
    /// Equipped Weapon.
    /// </summary>
    protected WeaponBehaviour equippedWeapon;
    
    #endregion

    #region UNITY

    /// <summary>
    /// Awake.
    /// </summary>
    protected virtual void Awake()
    {
        // Find the player character in the scene
        FindPlayerCharacter();
    }
    
    /// <summary>
    /// Update.
    /// </summary>
    private void Update()
    {
        // Try to find player character if we don't have one
        if (playerCharacter == null)
        {
            FindPlayerCharacter();
            return;
        }

        // Ignore if we don't have an Inventory.
        if (playerCharacterInventory == null)
            return;

        // Get Equipped Weapon.
        equippedWeapon = playerCharacterInventory.GetEquippedWeapon();
        
        // Tick.
        Tick();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Find and cache the player character references.
    /// </summary>
    private void FindPlayerCharacter()
    {
        // Try multiple methods to find the player character
        
        // Method 1: Use GameMan singleton if available
        if (GameMan.Instance != null && GameMan.Instance.playerInstance != null)
        {
            playerCharacter = GameMan.Instance.playerInstance.GetComponent<CharacterBehaviour>();
        }
        
        // Method 2: Find by tag if GameMan approach failed
        if (playerCharacter == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerCharacter = playerObject.GetComponent<CharacterBehaviour>();
            }
        }
        
        // Method 3: Find by component type as last resort
        if (playerCharacter == null)
        {
            playerCharacter = FindFirstObjectByType<CharacterBehaviour>();
        }
        
        // Get inventory if we found the player character
        if (playerCharacter != null)
        {
            // Try direct method first
            playerCharacterInventory = playerCharacter.GetComponent<InventoryBehaviour>();
            
            // If no direct component, try finding in children
            if (playerCharacterInventory == null)
            {
                playerCharacterInventory = playerCharacter.GetComponentInChildren<InventoryBehaviour>();
            }
        }
    }

    /// <summary>
    /// Tick.
    /// </summary>
    protected virtual void Tick() {}

    #endregion
}