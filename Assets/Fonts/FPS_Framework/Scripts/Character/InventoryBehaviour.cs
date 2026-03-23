using UnityEngine;

public abstract class InventoryBehaviour : MonoBehaviour
{
    #region GETTERS

    public abstract int GetPrevIndex();
    public abstract int GetNextIndex();
    
    // Add GetLastIndex for compatibility with Infima system
    public abstract int GetLastIndex();
    
    // Keep both methods for compatibility
    public abstract WeaponBehaviour GetEquippedWeapon();
    public abstract WeaponBehaviour GetEquipped();
    
    public abstract int GetEquippedIndex();

    #endregion

    public abstract void Init(int equippedAtStart = 0);
    public abstract WeaponBehaviour Equip(int index);
}