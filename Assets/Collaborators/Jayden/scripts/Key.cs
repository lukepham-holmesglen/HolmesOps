using UnityEngine;

public class Key : MonoBehaviour
{
    [Header("Assign the cube you want to remove")]
    public GameObject cubeToDelete;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object colliding is the player (make sure your player is tagged "Player")
        if (other.CompareTag("Player"))
        {
            // Destroy the cube
            if (cubeToDelete != null)
            {
                Destroy(cubeToDelete);
            }

            // Print message
            Debug.Log("Key collected!");

            // Destroy the key object itself
            Destroy(gameObject);
        }
    }
}
