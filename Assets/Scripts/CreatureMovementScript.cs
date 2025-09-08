using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CreatureMovementBounded : MonoBehaviour
{
    public bool controlledByHuman = false;
    public float moveSpeed = 5f;
    [Range(0f, 1f)] public float backwardsSpeedMultiplier = 0.3f;
    public float turnSpeed = 120f;
    public float raycastDistance = 1f;

    // Define movement bounds
    public float minX = -10f;
    public float maxX = 10f;
    public float minZ = -10f;
    public float maxZ = 10f;

    private Rigidbody rb;
    private int landLayerMask;

    // Sheep body
    public Transform bodyTransform;
    public Vector3 bodyRotationOffset = Vector3.zero;

    // Movements
    public float horizontal;
    public float vertical;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        landLayerMask = 1 << 3; // Land layer
    }

    void Update()
    {
        if (controlledByHuman){
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
        }
    }

    public void SetHorizontal(float value)
    {
        if (!controlledByHuman) {
            horizontal = Mathf.Clamp(value, -1f, 1f);
        }
    }

    public void SetVertical(float value)
    {
        if (!controlledByHuman) {
            vertical = Mathf.Clamp(value, -1f, 1f);
        }
    }

    void FixedUpdate()
    {
        // Rotate creature
        transform.Rotate(Vector3.up, horizontal * turnSpeed * Time.fixedDeltaTime);

        // Calculate forward movement
        Vector3 move = transform.forward * vertical * moveSpeed * Time.fixedDeltaTime;
        if (vertical < 0) move *= backwardsSpeedMultiplier;

        // Raycast downward to detect land
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastDistance + 0.5f, landLayerMask))
        {
            // Align with surface normal
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            bodyTransform.rotation = Quaternion.Slerp(
                bodyTransform.rotation,
                targetRotation * Quaternion.Euler(bodyRotationOffset),
                10f * Time.fixedDeltaTime
            );

            // Move along land surface
            Vector3 projectedMove = Vector3.ProjectOnPlane(move, hit.normal);
            Vector3 newPosition = rb.position + projectedMove;

            bool teleported = false;

            // Wrap-around teleport logic with slight nudge
            float nudge = 0.1f; // inside the boundary

            if (newPosition.x > maxX) { newPosition.x = minX + nudge; teleported = true; }
            else if (newPosition.x < minX) { newPosition.x = maxX - nudge; teleported = true; }

            if (newPosition.z > maxZ) { newPosition.z = minZ + nudge; teleported = true; }
            else if (newPosition.z < minZ) { newPosition.z = maxZ - nudge; teleported = true; }

            // Only apply Y offset if creature teleported
            if (teleported)
            {
                newPosition.y += 3f;
            }

            rb.MovePosition(newPosition);
        }
    }

}
