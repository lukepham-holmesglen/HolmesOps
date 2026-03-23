using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject Spawn(GameObject prefabToSpawn)
    {
        if (prefabToSpawn == null)
        {
            Debug.LogError($"Spawner {gameObject.name}: No prefab provided to spawn!");
            return null;
        }

        GameObject newEnemy = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
        return newEnemy;
    }
}