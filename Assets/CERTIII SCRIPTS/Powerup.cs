using System.Collections;
using System.Linq.Expressions;
using UnityEngine;

public class Powerup : MonoBehaviour
{
    public float speedMultiplier = 1.5f;
    [SerializeField]
    float powerupLength = 5;


    private void OnTriggerEnter(Collider other)
    {
        //Check if the object we collided with is the player
        if (other.gameObject.CompareTag("Player"))
        {
            //Get the PlayerMovement script attached to the player and have it run the powerup scipt below.
            //This is important since this object will be destroyed, and so the player must be running the coroutine or else the coroutine
            //would stop when this object is destroyed.
            PlayerMovement p = other.gameObject.GetComponent<PlayerMovement>();
            p.StartCoroutine(PowerupActive(p));
            Destroy(gameObject);
        }
    }
    /// <summary>
    /// Triggers the powerup and reverts the powerup after waiting a certain amount of time
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    IEnumerator PowerupActive(PlayerMovement player)
    {
        //Get the main camera (which is for some reason actually the child of the main camera)
        Camera cam = Camera.main.transform.GetChild(0).gameObject.GetComponent<Camera>();
        //Apply a multiplier to the player speed
        player.speedMultiplier = speedMultiplier;
        //Extra effects to make it clear what the change is and that the powerup has triggered succesfully
        cam.fieldOfView = 90;
        player.gameObject.GetComponent<AudioSource>().PlayOneShot(player.audioClipPowerup);
        //Wait for the set time
        yield return new WaitForSeconds(powerupLength);
        //Revert the changes
        player.speedMultiplier = 1;
        cam.fieldOfView = 60;
    }
}
