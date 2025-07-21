using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeController : MonoBehaviour
{
    public static FadeController Instance;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional if fading across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null) return;
        var color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    public void FadeToWhiteThen(GameObject levelPrefab, System.Action onMidFade = null)
    {
        StartCoroutine(FadeRoutine(levelPrefab, onMidFade));
    }

    private IEnumerator FadeRoutine(GameObject levelPrefab, System.Action onMidFade)
    {
        // Fade to white
        yield return Fade(0f, 1f);

        // Wait briefly (optional)
        yield return new WaitForSeconds(0.1f);

        // Run mid-fade action (e.g. load level)
        onMidFade?.Invoke();

        // Wait a frame so UI switches/rendering can happen
        yield return null;

        // Fade from white
        yield return Fade(1f, 0f);
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            SetAlpha(Mathf.Lerp(from, to, t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetAlpha(to);
    }
}
