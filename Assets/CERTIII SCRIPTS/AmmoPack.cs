using UnityEngine;

public class AmmoPack : MonoBehaviour
{
    [SerializeField] private int ammoAmount = 30;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to the Player
        if (other.CompareTag("Player"))
        {
            // Access the Character component and add ammo to the equipped weapon
            Character character = other.GetComponent<Character>();

            if (character != null && character.equippedWeapon != null)
            {
                character.equippedWeapon.AmmoPickup(ammoAmount);

                // Destroy the ammo pack object upon pickup
                Destroy(gameObject);
            }
        }
    }
}