using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameMan : MonoBehaviour
{
    public static GameMan Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private bool startGameAutomatically = false;
    [SerializeField] private GameObject autoStartLevelPrefab;

    [Header("Player References")]
    public GameObject playerPrefab;
    public GameObject playerInstance;
    public SpawnManager spawnManInstance;
    public GameScreen gameUIInstance;
    private GameObject levelInstance;

    [Header("Music Settings")]
    [SerializeField] private AudioSource musicSource;   // Main audio source for music
    [SerializeField] private AudioClip menuMusic;       // Menu music clip
    [SerializeField] private AudioClip gameMusic;       // Game music clip

    private float levelStartTime;
    private float levelEndTime;
    public float LevelCompletionTime => levelEndTime - levelStartTime;

    private bool gameRunning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (startGameAutomatically && autoStartLevelPrefab != null)
        {
            StartGame(autoStartLevelPrefab);
        }

        if (gameUIInstance == null)
        {
            gameUIInstance = FindObjectOfType<GameScreen>();
        }

        // Start menu music by default
        PlayMenuMusic();
    }

    private void Update()
    {
        if (gameRunning && gameUIInstance != null)
        {
            float currentTime = Time.time - levelStartTime;
            gameUIInstance.UpdateTimer(currentTime);
        }
    }

    public void StartGame(GameObject level)
    {
        if (levelInstance != null)
            Destroy(levelInstance);

        levelInstance = Instantiate(level, transform.position, Quaternion.identity);

        PlayerSpawnPoint spawnPoint = FindObjectOfType<PlayerSpawnPoint>();
        if (spawnPoint != null)
        {
            SpawnPlayer(spawnPoint.transform);
        }
        else
        {
            Debug.LogError("No PlayerSpawnPoint found in the scene. Player not spawned.");
        }

        if (spawnManInstance != null)
            spawnManInstance.StartGame();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (gameUIInstance == null)
            gameUIInstance = FindObjectOfType<GameScreen>();

        if (gameUIInstance != null)
        {
            gameUIInstance.gameObject.SetActive(true);
            gameUIInstance.Reset();
        }

        // Start timer
        levelStartTime = Time.time;
        gameRunning = true;

        // Switch to game music
        PlayGameMusic();
    }

    public void SpawnPlayer(Transform spawnPoint)
    {
        playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
    }

    public void GameOver()
    {
        gameRunning = false;

        UiSystem.Show<GameOverScreen>();
        UiSystem.GetView<GameOverScreen>().SetData(gameUIInstance.currentRound, gameUIInstance.currentScore);

        spawnManInstance.GameOver();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (playerInstance != null)
            Destroy(playerInstance.gameObject);
        if (levelInstance != null)
            Destroy(levelInstance.gameObject);

        // Switch back to menu music
        PlayMenuMusic();
    }

    public void GameWin()
    {
        gameRunning = false;
        levelEndTime = Time.time;

        UiSystem.Show<GameWinScreen>();
        var winScreen = UiSystem.GetView<GameWinScreen>();
        winScreen.SetData(gameUIInstance.currentRound, gameUIInstance.currentScore);
        winScreen.SetCompletionTime(LevelCompletionTime);
        UiSystem.GetView<GameWinScreen>().SetCompletionTime(LevelCompletionTime);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (playerInstance != null)
            Destroy(playerInstance.gameObject);
        if (levelInstance != null)
            Destroy(levelInstance.gameObject);

        // Switch back to menu music
        PlayMenuMusic();
    }

    private void PlayMenuMusic()
    {
        if (musicSource == null || menuMusic == null) return;
        if (musicSource.clip == menuMusic && musicSource.isPlaying) return;

        musicSource.clip = menuMusic;
        musicSource.loop = true;
        musicSource.Play();
    }

    private void PlayGameMusic()
    {
        if (musicSource == null || gameMusic == null) return;
        if (musicSource.clip == gameMusic && musicSource.isPlaying) return;

        musicSource.clip = gameMusic;
        musicSource.loop = true;
        musicSource.Play();
    }

    public bool IsAutoStarting => startGameAutomatically;
}
