using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaypointToggleSystem : MonoBehaviour
{
    public Transform playerCamera;
    public Transform finalWaypoint;
    public RectTransform waypointIcon;
    public TextMeshProUGUI toggleText;

    public float edgeBuffer = 30f;

    private Canvas canvas;
    private RectTransform canvasRect;
    private bool isWaypointActive = false;

    void Start()
    {
        canvas = waypointIcon.GetComponentInParent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();

        waypointIcon.gameObject.SetActive(false);
        toggleText.text = "Press [E] to turn on the waypoint";
    }

    void Update()
    {
        // Check for E key press
        if (Input.GetKeyDown(KeyCode.E))
        {
            isWaypointActive = !isWaypointActive;
            waypointIcon.gameObject.SetActive(isWaypointActive);

            if (isWaypointActive)
            {
                toggleText.text = "Press [E] to turn off the waypoint";
            }
            else
            {
                toggleText.text = "Press [E] to turn on the waypoint";
            }
        }

        if (isWaypointActive)
        {
            UpdateWaypointMarker();
        }
    }

    void UpdateWaypointMarker()
    {
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(finalWaypoint.position);
        bool isBehind = screenPoint.z < 0;

        if (isBehind)
        {
            screenPoint *= -1;
        }

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
