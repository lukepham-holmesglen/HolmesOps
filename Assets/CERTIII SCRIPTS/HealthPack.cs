using UnityEngine;
using UnityEngine.TextCore.Text;


public class HealthPack : MonoBehaviour
{
    //Adding game object health pack - J. Ryan
    //referencing from the 'HealthPickup' script
    public GameObject healthpack_prefab;
    //defining how much hp to restore on pickup:
    public int healthAmount = 25;
    //enabling an option to play a 'rooster crowing' sound effect on pickup
    public AudioClip pickupSound;
    // setting the volume for the sound effect
    // *BUG FOUND* I wanted to make this volume lower at 0.6f' and VS kept giving me the 'lightbulb'
    // For whatever reason, it disappeared after swapping to other scripts?!?!?!?
    private float pickupVolume = 0.6f;
   








    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// Variable for the amount of health to add
    /// set up an OnTriggerEnter function
    /// check if the collider has the "Player" tag
    /// if it does access:
    /// collider.gameObject.GetComponent<Character>().ChangeCurrentHealth(yourAmount);
    /// make sure to delete the pack on pickup.
    /// 
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    //J.Ryan -
    private void OnTriggerEnter(Collider collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            
                //When saving, the "collider.gameObject.GetComponent<Character>().ChangeCurrentHealth(yourAmount);" 
                // was updated to this
            collision.gameObject.GetComponent<Character>().ChangeCurrentHealth(healthAmount);
            AudioClipLoadType(pickupSound).Play();
            Destroy(gameObject);
        }

    }


    // Update is called once per frame
    void Update()
    {
        
    }


}
