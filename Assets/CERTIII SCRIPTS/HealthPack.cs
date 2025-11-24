using System.Data;
using UnityEngine;

public class HealthPack : MonoBehaviour
{
    public int healAmount = 50;
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

    // Update is called once per frame
    void Update()
    {

    }

    //Trigger command so that when you walk into healthpack prefab, it gets C O N S U M E D.
    //The script was taken from Google and changed to work with the other scripts.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.gameObject.GetComponent<Character>().ChangeCurrentHealth(healAmount);
            {
                Destroy(gameObject);
            }
        }
    }
}
