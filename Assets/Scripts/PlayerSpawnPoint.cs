using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    [Tooltip("Optional marker object to hide when the game starts.")]
    public GameObject markerObject;

    void Start()
    {
        if (markerObject != null)
        {
            markerObject.SetActive(false);
        }
    }
}
