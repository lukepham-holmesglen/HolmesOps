using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlatformCollision : MonoBehaviour
{
    // Tag of the player
    [SerializeField] string playerTag = "GameMan";
    // The moving platform (named "platform" - aka cloud platform no. 1)
    [SerializeField] Transform platform;


    private void OnTriggerEnter(Collider other)
    {
        // if something touches the box collider on the platform...
        // check if the tag of it is equal to the playerTag
        if (other.gameObject.tag.Equals(playerTag))
        {
            // if the player jumps onto the platform, player becomes a...
            // child object of the platform and equal to the platform
            other.gameObject.transform.parent = platform;
            // child object of the platform will move with the platform
        }
    }



    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag.Equals(playerTag))
        {
            other.gameObject.transform.parent = null;
        }
    }
}


