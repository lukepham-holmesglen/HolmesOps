using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaypointToggleSystem : MonoBehaviour
{
    public RectTransform waypointIcon;
    public TextMeshProUGUI toggleText;

    public float edgeBuffer = 30f;
    public float refreshInterval = 1f; // how often to check for new objects

    private Transform playerCamera;
    private Transform finalWaypoint;
    private Canvas canvas;
    private RectTransform canvasRect;
    private bool isWaypointActive = false;

    private float nextRefreshTime = 0f;

    void Start()
    {
        InitializeReferences();
        SetupUI();
    }

    void InitializeReferences()
    {
        // Find player camera
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Camera mainCam = player.GetComponentInChildren<Camera>();
            if (mainCam != null)
            {
                playerCamera = mainCam.transform;
            }
        }

        // Find final waypoint
        GameWinTrigger winTrigger = FindObjectOfType<GameWinTrigger>();
        if (winTrigger != null)
        {
            finalWaypoint = winTrigger.transform;
        }
    }

    void SetupUI()
    {
        canvas = waypointIcon.GetComponentInParent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();

        waypointIcon.gameObject.SetActive(false);
        if (toggleText != null)
            toggleText.text = "Press [E] to turn on the waypoint";
    }

    void Update()
    {
        // Toggle control
        if (Input.GetKeyDown(KeyCode.E))
        {
            isWaypointActive = !isWaypointActive;
            waypointIcon.gameObject.SetActive(isWaypointActive);

            if (toggleText != null)
                toggleText.text = isWaypointActive
                    ? "Press [E] to turn off the waypoint"
                    : "Press [E] to turn on the waypoint";
        }

        // Refresh references periodically (helps when prefabs are swapped)
        if (Time.time >= nextRefreshTime)
        {
            nextRefreshTime = Time.time + refreshInterval;
            RefreshIfMissing();
        }

        // Update waypoint marker
        if (isWaypointActive && playerCamera != null && finalWaypoint != null)
        {
            UpdateWaypointMarker();
        }
    }

    void RefreshIfMissing()
    {
        // If player camera or waypoint was destroyed or changed, find again
        if (playerCamera == null || !playerCamera.gameObject.activeInHierarchy)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Camera mainCam = player.GetComponentInChildren<Camera>();
                if (mainCam != null)
                    playerCamera = mainCam.transform;
            }
        }

        if (finalWaypoint == null || !finalWaypoint.gameObject.activeInHierarchy)
        {
            GameWinTrigger winTrigger = FindObjectOfType<GameWinTrigger>();
            if (winTrigger != null)
                finalWaypoint = winTrigger.transform;
        }
    }

    void UpdateWaypointMarker()
    {
        if (playerCamera == null || finalWaypoint == null)
            return;

        Vector3 screenPoint = Camera.main.WorldToScreenPoint(finalWaypoint.position);
        bool isBehind = screenPoint.z < 0;

        if (isBehind)
            screenPoint *= -1;

        Vector2 screenPosition = new Vector2(screenPoint.x, screenPoint.y);
        float canvasWidth = canvas.pixelRect.width;
        float canvasHeight = canvas.pixelRect.height;

        screenPosition.x = Mathf.Clamp(screenPosition.x, edgeBuffer, canvasWidth - edgeBuffer);
        screenPosition.y = Mathf.Clamp(screenPosition.y, edgeBuffer, canvasHeight - edgeBuffer);

        waypointIcon.position = screenPosition;

        Vector3 toTarget = finalWaypoint.position - playerCamera.position;
        Vector3 camForward = playerCamera.forward;
        float angle = Vector3.SignedAngle(camForward, toTarget, Vector3.up);
        waypointIcon.localRotation = Quaternion.Euler(0, 0, -angle);
    }
}
