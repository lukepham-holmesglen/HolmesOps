using System;
using UnityEngine;

public abstract class CharacterBehaviour : MonoBehaviour
{
    #region Virtual Unity Functions
    protected virtual void Awake()
    {

    }

    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {

    }

    protected virtual void LateUpdate() 
    {
    
    }
    #endregion

    #region Getters

    /// <summary>
    /// Returns true if the character is running.
    /// </summary>
    public abstract bool IsRunning();
    /// <summary>
    /// Returns true if the character is aiming.
    /// </summary>
    public abstract bool IsAiming();
    /// <summary>
    /// Returns true if the game cursor is locked.
    /// </summary>
    public abstract bool IsCursorLocked();
    /// <summary>
    /// Returns the Movement Input.
    /// </summary>
    public abstract Vector2 GetInputMovement();
    /// <summary>
    /// Returns the Look Input.
    /// </summary>
    public abstract Vector2 GetInputLook();
/// <summary>
/// Returns true if the crosshair should be visible.
/// </summary>
public abstract bool IsCrosshairVisible();

    #endregion

    #region Animations

    public abstract void AnimationEndedHolster();
    public abstract void AnimationEndedReload();
    public abstract void FillAmmunition(int amount);

    #endregion

    #region Exposed Functions
    public abstract void ChangeCurrentHealth(int amount);
    #endregion
}