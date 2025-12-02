using UnityEngine;

public class AmmoPack : MonoBehaviour
{
    [SerializeField] private int amountAmmo= 30;

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// Variable for the amount of ammo to add
    /// set up an OnTriggerEnter function
    /// check if the collider has the "Player" tag
    /// if it does access:
    /// collider.gameObject.GetComponent<Character>().equippedWeapon.AmmoPickup(yourAmount);
    /// make sure to delete the pack on pickup.
    /// 
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Player"))
        {
            col.GetComponent<Character>().equippedWeapon.AmmoPickup(amountAmmo);
            Destroy(gameObject);
            
        }
    }
    //in AmmoPack.cs add serialezedField amount of ammo
    // add OnTriggerEnter method for check when collide with Player via tag comparisson
    //if true - Get Character componentm after weapon class and fire the AmmoPickup method wtih "amountAmmo" argument
    //Destroy this object
}
