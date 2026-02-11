using UnityEngine;

public abstract class WeaponBehaviour : MonoBehaviour
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

    #region GETTERS

    public abstract int GetAmmunitionCurrent();
    public abstract int GetAmmunitionTotal();
    public abstract Sprite GetWeaponSprite();
    public abstract Animator GetAnimator();
    public abstract bool IsAutomatic();
    public abstract bool HasAmmunition();
    public abstract bool IsFull();
    public abstract float GetRateOfFire();
    public abstract RuntimeAnimatorController GetAnimatorController();
    public abstract WeaponAttachmentManagerBehaviour GetAttachmentManager();

    #endregion

    public abstract void Fire(float spreadMultiplier = 1.0f);
    public abstract void Reload();
    public abstract void FillAmmunition(int amount);
    public abstract void EjectCasing();
    public abstract void SetOwner(CharacterBehaviour newOwner);
}
