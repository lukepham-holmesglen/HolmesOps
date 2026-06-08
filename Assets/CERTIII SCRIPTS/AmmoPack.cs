using UnityEngine;

public class AmmoPack : MonoBehaviour
{
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

    [Tooltip("The amount of ammo to pickup")]
    public int addAmmo = 100;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

     private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponent<Character>().equippedWeapon.AmmoPickup(addAmmo);
            Destroy(gameObject);
        }
    }


}
