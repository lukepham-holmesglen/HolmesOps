using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class EnemyType
{
    public GameObject prefab;
}

public class SpawnManager : MonoBehaviour
{
    [SerializeField] GameScreen gameUI;
    public List<GameObject> spawnerList;
    
    [Header("Enemy Management")]
    [Tooltip("List of spawned enemies currently in the scene")]
    public List<GameObject> enemies = new List<GameObject>();
    
    [Header("Enemy Types to Spawn")]
    [Tooltip("Different enemy types that can be spawned")]
    [SerializeField] private EnemyType[] enemyTypes;
    
    [Header("Spawn Settings")]
    [SerializeField] private float timer;
    [SerializeField] private int enemyCap;
    public bool UseWaves;
    
    private int roundNumber = 0;
    private bool roundSpawned;
    private int enemiesSpawned;
    public bool stopSpawning = false;

    private GameObject GetRandomEnemyPrefab()
    {
        if (enemyTypes == null || enemyTypes.Length == 0)
        {
            //Debug.LogError("SpawnManager: No enemy types configured!");
            return null;
        }

        // Random selection with equal probability
        return enemyTypes[Random.Range(0, enemyTypes.Length)].prefab;
    }

    public void StartGame()
    {
        //Debug.Log("=== GAME STARTED ===");
        GameObject[] spawners = GameObject.FindGameObjectsWithTag("Spawner");
        spawnerList.AddRange(spawners);
        //Debug.Log($"Found {spawnerList.Count} spawners");

        if (UseWaves)
        {
            //Debug.Log("Starting WAVE mode");
            StartCoroutine(SpawnWave());
        }
        else
        {
            //Debug.Log("Starting ENDLESS mode");
            StartCoroutine(EndlessAttemptSpawn());
        }
    }

    public void GameOver()
    {
        //Debug.Log("=== GAME OVER ===");
        StopAllCoroutines();

        foreach(GameObject enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }
        enemies.Clear();
        spawnerList.Clear();
    }

    public IEnumerator SpawnWave()
    {
        roundNumber++;
        gameUI.IncreaseRound(1);
        roundSpawned = false;
        enemiesSpawned = 0;
        
        // Calculate enemies for this wave - ensure at least 1 enemy spawns
        int enemiesToSpawn = Mathf.Max(1, Mathf.RoundToInt(enemyCap * (0.5f * roundNumber)));
        
        //Debug.Log($"=== STARTING WAVE {roundNumber} ===");
        //Debug.Log($"Enemy cap: {enemyCap}, Enemies to spawn: {enemiesToSpawn}");
        
        while(enemiesSpawned < enemiesToSpawn)
        {
            yield return new WaitForSeconds(timer);

            GameObject randomEnemyPrefab = GetRandomEnemyPrefab();
            GameObject newEnemy = spawnerList[Random.Range(0, spawnerList.Count)].GetComponent<Spawner>().Spawn(randomEnemyPrefab);
            if (newEnemy != null)
            {
                enemies.Add(newEnemy);
                enemiesSpawned++;
                //Debug.Log($"Wave {roundNumber}: Spawned enemy {enemiesSpawned}/{enemiesToSpawn} (Total alive: {enemies.Count})");
            }
        }
        
        roundSpawned = true;
        //Debug.Log($"=== WAVE {roundNumber} SPAWNING COMPLETE ===");
        //Debug.Log($"Total enemies spawned: {enemiesToSpawn}, Total alive: {enemies.Count}");
    }

    public IEnumerator EndlessAttemptSpawn()
    {
        while (!stopSpawning)
        {
            yield return new WaitForSeconds(timer);

            if (enemies.Count < enemyCap)
            {
                GameObject randomEnemyPrefab = GetRandomEnemyPrefab();
                GameObject newEnemy = spawnerList[Random.Range(0, spawnerList.Count)].GetComponent<Spawner>().Spawn(randomEnemyPrefab);
                if (newEnemy != null)
                {
                    enemies.Add(newEnemy);
                    //Debug.Log($"Endless mode: Spawned enemy (Total alive: {enemies.Count}/{enemyCap})");
                }
            }
        }
    }

    private void NoEnemies()
    {
        //Debug.Log("=== NO ENEMIES LEFT ===");
        
        if (UseWaves)
        {
            //Debug.Log($"Wave mode - Round spawned: {roundSpawned}");
            if (!roundSpawned) //havent finished spawning yet
            {
                //Debug.Log("Wave still spawning, waiting...");
                return;
            }
            else
            {
                //Debug.Log("Wave complete! Starting next wave...");
                //round complete
                StartCoroutine(SpawnWave());
            }
        }
        else
        {
            //Debug.Log("Endless mode - Spawning emergency enemy");
            //if there are no enemies, spawn an enemy, the endlessattemptspawn will continue to spawn also
            GameObject randomEnemyPrefab = GetRandomEnemyPrefab();
            GameObject newEnemy = spawnerList[Random.Range(0, spawnerList.Count)].GetComponent<Spawner>().Spawn(randomEnemyPrefab);
            if (newEnemy != null)
            {
                enemies.Add(newEnemy);
                //Debug.Log($"Emergency spawn complete (Total alive: {enemies.Count})");
            }
        }
    }

    public void DestroyEnemy(GameObject enemy)
    {
        //Debug.Log($"Enemy destroyed! Enemies before removal: {enemies.Count}");
        
        gameUI.IncreaseScore(100);
        enemies.Remove(enemy);
        
        // DON'T destroy the GameObject here - let the enemy handle its own cleanup
        // This allows ragdoll physics to play out before destruction
        //Debug.Log($"Enemy removed from list but GameObject preserved for ragdoll. Enemies after removal: {enemies.Count}");
        
        //Debug.Log($"Enemies after removal: {enemies.Count}");

        if (enemies.Count == 0)
        {
            //Debug.Log("All enemies dead - calling NoEnemies()");
            NoEnemies();
        }
    }
}