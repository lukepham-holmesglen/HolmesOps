using UnityEngine;

public class Inventory : InventoryBehaviour
{
    private WeaponBehaviour[] weapons;
    private WeaponBehaviour equipped;
    private int equippedIndex = -1;

    #region GETTERS
    // For Infima-style compatibility
    public override int GetLastIndex()
    {
        //Get the previous index, with wrap around
        int newIndex = equippedIndex - 1;
        if (newIndex < 0)
            newIndex = weapons.Length - 1;

        //Debug.Log($"GetLastIndex: Current index {equippedIndex}, Previous index {newIndex}");
        return newIndex;
    }
    
    public override int GetPrevIndex()
    {
        //Get the previous index, with wrap around
        int newIndex = equippedIndex - 1;
        if (newIndex < 0)
            newIndex = weapons.Length - 1;

        //Debug.Log($"GetPrevIndex: Current index {equippedIndex}, Previous index {newIndex}");
        return newIndex;
    }

    public override int GetNextIndex()
    {
        //Get the next index, with wrap around
        int newIndex = equippedIndex + 1;
        if (newIndex > weapons.Length - 1)
            newIndex = 0;

        //Debug.Log($"GetNextIndex: Current index {equippedIndex}, Next index {newIndex}");
        return newIndex;
    }

    // For Infima-style compatibility
    public override WeaponBehaviour GetEquipped() => equipped;
    
    public override WeaponBehaviour GetEquippedWeapon() => equipped;

    public override int GetEquippedIndex() => equippedIndex;

    // Add this helper method to check weapon count
    public int GetWeaponCount() => weapons != null ? weapons.Length : 0;

    #endregion

    public override void Init(int equippedAtStart = 0)
    {
        weapons = GetComponentsInChildren<WeaponBehaviour>(true);
        
        //Debug.Log($"Inventory Init: Found {weapons.Length} weapons");
        for (int i = 0; i < weapons.Length; i++)
        {
            //Debug.Log($"  Weapon {i}: {weapons[i].name}");
        }

        //disable all weapons
        foreach (WeaponBehaviour weapon in weapons)
            weapon.gameObject.SetActive(false);

        Equip(equippedAtStart);
    }

    public override WeaponBehaviour Equip(int index)
    {
        //Debug.Log($"Equip called with index {index}");
        
        //protections
        if (weapons == null)
        {
            //Debug.LogError("Weapons array is null!");
            return equipped;
        }
        
        if (weapons.Length == 0)
        {
            //Debug.LogError("No weapons found in inventory!");
            return equipped;
        }
        
        // Check if index is valid (also handle negative indices)
        if (index < 0 || index > weapons.Length - 1)
        {
            //Debug.LogWarning($"Invalid weapon index {index}. Weapon count: {weapons.Length}");
            return equipped;
        }
        
        if (equippedIndex == index)
        {
            //Debug.Log($"Weapon {index} is already equipped");
            return equipped;
        }

        // Deactivate current weapon
        if (equipped != null)
        {
            //Debug.Log($"Deactivating current weapon: {equipped.name}");
            equipped.gameObject.SetActive(false);
        }

        // Activate new weapon
        equippedIndex = index;
        equipped = weapons[equippedIndex];
        equipped.gameObject.SetActive(true);
        
        //Debug.Log($"Equipped weapon {index}: {equipped.name}");

        return equipped;
    }

}