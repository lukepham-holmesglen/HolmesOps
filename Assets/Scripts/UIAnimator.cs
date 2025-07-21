using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIAnimator : MonoBehaviour
{
    public enum AnimationType { None, PingPong, Loop }

    [Header("Position Animation")]
    public bool animatePosition = false;
    public Vector2 positionOffset = new Vector2(10f, 0f);
    public float positionSpeed = 1f;
    public AnimationType positionAnimationType = AnimationType.PingPong;

    [Header("Scale Animation")]
    public bool animateScale = false;
    public Vector3 scaleOffset = new Vector3(0.1f, 0.1f, 0);
    public float scaleSpeed = 1f;
    public AnimationType scaleAnimationType = AnimationType.PingPong;

    [Header("Auto Disable")]
    public bool disableAfterTime = false;        // Enable/disable auto-disable feature
    public float disableDelaySeconds = 3f;       // Delay before disabling the GameObject

    private RectTransform rectTransform;
    private Vector2 initialPos;
    private Vector3 initialScale;
    private float posTimer = 0f;
    private float scaleTimer = 0f;

    private float disableTimer = 0f;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        initialPos = rectTransform.anchoredPosition;
        initialScale = rectTransform.localScale;
        disableTimer = 0f;
    }

    void Update()
    {
        if (animatePosition)
            AnimatePosition();

        if (animateScale)
            AnimateScale();

        if (disableAfterTime)
        {
            disableTimer += Time.deltaTime;
            if (disableTimer >= disableDelaySeconds)
            {
                gameObject.SetActive(false);
            }
        }
    }

    void AnimatePosition()
    {
        posTimer += Time.deltaTime * positionSpeed;
        float factor = CalculateFactor(posTimer, positionAnimationType);
        rectTransform.anchoredPosition = initialPos + positionOffset * factor;
    }

    void AnimateScale()
    {
        scaleTimer += Time.deltaTime * scaleSpeed;
        float factor = CalculateFactor(scaleTimer, scaleAnimationType);
        rectTransform.localScale = initialScale + scaleOffset * factor;
    }

    float CalculateFactor(float t, AnimationType type)
    {
        switch (type)
        {
            case AnimationType.PingPong:
                return Mathf.Sin(t * Mathf.PI * 2f) * 0.5f + 0.5f;
            case AnimationType.Loop:
                return Mathf.Repeat(t, 1f);
            default:
                return 0f;
        }
    }
}
