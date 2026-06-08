using UnityEngine;

public class HealthPack : MonoBehaviour
{
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


    [Tooltip("The amount of health pickup gives")]
    public int addHealth = 100;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            GetComponent<Collider>().gameObject.GetComponent<Character>().ChangeCurrentHealth(addHealth);
            Destroy(gameObject);
        }
    }   



}
