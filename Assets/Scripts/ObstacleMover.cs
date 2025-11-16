using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    [Header("Movement Settings")]
    public bool enableMovement = true;
    public Vector3 moveDirection = Vector3.right;
    public float moveDistance = 2f;
    public float moveSpeed = 2f;
    public bool loopMovement = true;
    public AnimationCurve movementEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Rotation Settings")]
    public bool enableRotation = true;
    public Vector3 rotationAxis = Vector3.up;
    public float rotationExtent = 90f;
    public float rotationSpeed = 2f;

    public enum RotationMode { BackAndForth, Continuous }
    public RotationMode rotationMode = RotationMode.BackAndForth;

    public bool loopRotation = true; // For BackAndForth mode only
    public AnimationCurve rotationEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 startPos;
    private Quaternion startRot;

    // --- NEW: Random offsets to desync identical objects ---
    private float movementOffset;
    private float rotationOffset;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;

        // Randomize the phase offset so that identical objects move differently
        movementOffset = Random.Range(0f, 100f);
        rotationOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (enableMovement) HandleMovement();
        if (enableRotation) HandleRotation();
    }

    void HandleMovement()
    {
        if (loopMovement)
        {
            float pingPong = Mathf.PingPong((Time.time + movementOffset) * moveSpeed, 1f); // Added offset
            float easedT = movementEase.Evaluate(pingPong);
            float offset = Mathf.Lerp(-moveDistance, moveDistance, easedT);
            transform.position = startPos + moveDirection.normalized * offset;
        }
        else
        {
            float t = Mathf.Clamp01((Time.time + movementOffset) * moveSpeed / moveDistance);
            float easedT = movementEase.Evaluate(t);
            transform.position = Vector3.Lerp(startPos, startPos + moveDirection.normalized * moveDistance, easedT);
        }
    }

    void HandleRotation()
    {
        if (rotationMode == RotationMode.Continuous)
        {
            // Continuous spin in one direction (no offset needed for continuous motion)
            transform.Rotate(rotationAxis.normalized * rotationSpeed * Time.deltaTime, Space.Self);
        }
        else // BackAndForth
        {
            if (loopRotation)
            {
                float pingPong = Mathf.PingPong((Time.time + rotationOffset) * rotationSpeed, 1f); // Added offset
                float easedT = rotationEase.Evaluate(pingPong);
                float angle = Mathf.Lerp(-rotationExtent, rotationExtent, easedT);
                transform.rotation = startRot * Quaternion.AngleAxis(angle, rotationAxis.normalized);
            }
            else
            {
                float t = Mathf.Clamp01((Time.time + rotationOffset) * rotationSpeed / rotationExtent);
                float easedT = rotationEase.Evaluate(t);
                float angle = Mathf.Lerp(0f, rotationExtent, easedT);
                transform.rotation = startRot * Quaternion.AngleAxis(angle, rotationAxis.normalized);
            }
        }
    }
}
