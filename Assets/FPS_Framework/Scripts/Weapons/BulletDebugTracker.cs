using UnityEngine;

public class BulletDebugTracker : MonoBehaviour
{
    private Vector3 lastPosition;
    private bool hasHit = false;
    
    void Start()
    {
        lastPosition = transform.position;
    }
    
    void Update()
    {
        if (!hasHit)
        {
            // Draw the bullet's actual path as it moves
            Debug.DrawLine(lastPosition, transform.position, Color.cyan, 10f);
            lastPosition = transform.position;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;
        
        Vector3 hitPoint = collision.contacts[0].point;
        
        //Debug.Log($"=== BULLET HIT DEBUG ===");
        //Debug.Log($"Bullet hit: {collision.gameObject.name}");
        //Debug.Log($"Hit point: {hitPoint}");
        //Debug.Log($"Bullet position: {transform.position}");
        //Debug.Log($"Hit normal: {collision.contacts[0].normal}");
        
        // Draw big X at actual hit point
        Vector3 up = Vector3.up * 1f;
        Vector3 right = Vector3.right * 1f;
        Vector3 forward = Vector3.forward * 1f;
        
        // Red X marker at actual hit point
        Debug.DrawLine(hitPoint - up - right, hitPoint + up + right, Color.red, 15f);
        Debug.DrawLine(hitPoint - up + right, hitPoint + up - right, Color.red, 15f);
        Debug.DrawLine(hitPoint - forward, hitPoint + forward, Color.red, 15f);
        
        // Orange sphere around hit point (using custom orange color)
        Color orange = new Color(1f, 0.5f, 0f, 1f);
        DrawDebugSphere(hitPoint, 0.5f, orange, 15f);
    }
    
    void DrawDebugSphere(Vector3 center, float radius, Color color, float duration)
    {
        // Draw a simple sphere using lines
        int segments = 16;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            // XY plane circle
            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * radius;
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * radius;
            Debug.DrawLine(point1, point2, color, duration);
            
            // XZ plane circle
            point1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
            point2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;
            Debug.DrawLine(point1, point2, color, duration);
            
            // YZ plane circle
            point1 = center + new Vector3(0, Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
            point2 = center + new Vector3(0, Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;
            Debug.DrawLine(point1, point2, color, duration);
        }
    }
}