using UnityEngine;

public class PlatformToPlayer : MonoBehaviour { 

    public GameObject Player;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == Player)
        {
            // if player lands on platform, platform is parent object of player
            // Player will move with the platform
            Player.transform.parent = transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == Player)
        {
            // This removes the parent when exiting the box collider
            Player.transform.parent = null;
        }
    }






}
   
