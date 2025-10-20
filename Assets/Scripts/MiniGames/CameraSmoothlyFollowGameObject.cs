using UnityEngine;

public class CameraSmoothlyFollowGameObject : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("The GameObject the camera will follow")]
    public Transform target;
    
    [Tooltip("Offset from the target's position")]
    public Vector3 offset = new Vector3(0f, 5f, -10f);
    
    [Header("Smoothing Settings")]
    [Tooltip("How smoothly the camera follows (higher = smoother but slower)")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.125f;
    
    [Tooltip("Whether to follow on the X axis")]
    public bool followX = true;
    
    [Tooltip("Whether to follow on the Y axis")]
    public bool followY = true;
    
    [Tooltip("Whether to follow on the Z axis")]
    public bool followZ = true;
    
    [Header("Rotation Settings")]
    [Tooltip("Whether the camera should rotate to look at the target")]
    public bool lookAtTarget = false;
    
    [Tooltip("How smoothly the camera rotates to look at target")]
    [Range(0.01f, 1f)]
    public float rotationSmoothSpeed = 0.1f;
    
    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraSmoothlyFollowGameObject: No target assigned!");
            return;
        }
        
        // Calculate the desired position
        Vector3 desiredPosition = target.position + offset;
        
        // Apply axis constraints
        if (!followX) desiredPosition.x = transform.position.x;
        if (!followY) desiredPosition.y = transform.position.y;
        if (!followZ) desiredPosition.z = transform.position.z;
        
        // Smoothly move the camera towards the desired position
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
        transform.position = smoothedPosition;
        
        // Optionally rotate to look at the target
        if (lookAtTarget)
        {
            Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed);
        }
    }
    
    /// <summary>
    /// Sets a new target for the camera to follow
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    /// <summary>
    /// Sets a new offset from the target
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    /// <summary>
    /// Instantly moves the camera to the target position (no smoothing)
    /// </summary>
    public void SnapToTarget()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            velocity = Vector3.zero;
        }
    }
}
