
using UnityEngine;
using UnityEngine.InputSystem;

public class PhysicsGrabber : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("Grab Settings")]
    public float grabRange = 5f;
    public float holdDistance = 2.5f;
    public float minHoldDistance = 1f;
    public float maxHoldDistance = 5f;
    public float scrollSpeed = 1f;

    [Header("Weight Settings")]
    public float maxGrabbableMass = 150f;
    public float maxLiftableMass = 60f;
    public float springStrength = 180f;
    public float springDamping = 25f;
    public float maxGrabForce = 1500f;

    [Header("Throw Settings")]
    public float throwImpulse = 8f;

    [Header("Debug")]
    public bool showDebug = true;

    private Rigidbody grabbedBody;
    private Vector3 localGrabPoint;
    private float currentDistance;
    private int moveableLayer;

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        moveableLayer = LayerMask.NameToLayer("Moveable");

        if (moveableLayer == -1)
        {
            Debug.LogError(
                "PhysicsGrabber: Create a layer named Moveable!"
            );
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                "PhysicsGrabber: No camera assigned!"
            );
        }
    }

    void Update()
    {
        // New Unity Input System
        if (Mouse.current == null)
            return;

        // Left click to grab
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryGrab();
        }

        if (grabbedBody == null)
            return;

        // Release left click to drop
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ReleaseObject();
            return;
        }

        // Right click to throw
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            ThrowObject();
            return;
        }

        // Scroll to change holding distance
        float scroll = Mouse.current.scroll.ReadValue().y;

        currentDistance = Mathf.Clamp(
            currentDistance + scroll * scrollSpeed * 0.01f,
            minHoldDistance,
            maxHoldDistance
        );
    }

    void FixedUpdate()
    {
        if (grabbedBody != null)
        {
            MoveGrabbedObject();
        }
    }

    void TryGrab()
    {
        if (playerCamera == null || moveableLayer == -1)
            return;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (showDebug)
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction * grabRange,
                Color.green,
                1f
            );
        }

        // Raycast against all layers so walls block grabbing
        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            grabRange,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            if (showDebug)
                Debug.Log("Grab failed: Nothing was hit.");

            return;
        }

        // Only grab objects on the Moveable layer
        if (hit.collider.gameObject.layer != moveableLayer)
        {
            if (showDebug)
            {
                Debug.Log(
                    "Grab failed: " + hit.collider.name +
                    " is not on the Moveable layer."
                );
            }

            return;
        }

        Rigidbody rb = hit.rigidbody;

        if (rb == null)
        {
            if (showDebug)
                Debug.Log("Grab failed: No Rigidbody.");

            return;
        }

        if (rb.isKinematic)
        {
            if (showDebug)
                Debug.Log("Grab failed: Rigidbody is kinematic.");

            return;
        }

        if (rb.mass > maxGrabbableMass)
        {
            if (showDebug)
                Debug.Log("Grab failed: Object is too heavy.");

            return;
        }

        grabbedBody = rb;

        // Store the exact point clicked
        localGrabPoint =
            rb.transform.InverseTransformPoint(hit.point);

        currentDistance = Mathf.Clamp(
            holdDistance,
            minHoldDistance,
            maxHoldDistance
        );

        grabbedBody.WakeUp();

        if (showDebug)
        {
            Debug.Log(
                "Grabbed: " + rb.name +
                " | Mass: " + rb.mass
            );
        }
    }

    void MoveGrabbedObject()
    {
        Vector3 target =
            playerCamera.transform.position +
            playerCamera.transform.forward * currentDistance;

        Vector3 grabPoint =
            grabbedBody.transform.TransformPoint(localGrabPoint);

        Vector3 displacement = target - grabPoint;

        Vector3 pointVelocity =
            grabbedBody.GetPointVelocity(grabPoint);

        // Physics spring force
        Vector3 force =
            displacement * springStrength -
            pointVelocity * springDamping;

        // Limit force to simulate object weight
        force = Vector3.ClampMagnitude(
            force,
            maxGrabForce
        );

        // Heavy objects can only be dragged
        if (grabbedBody.mass > maxLiftableMass)
        {
            force.y = Mathf.Min(force.y, 0f);
        }

        grabbedBody.AddForceAtPosition(
            force,
            grabPoint,
            ForceMode.Force
        );
    }

    void ThrowObject()
    {
        if (grabbedBody == null)
            return;

        Rigidbody rb = grabbedBody;

        ReleaseObject();

        rb.AddForce(
            playerCamera.transform.forward * throwImpulse,
            ForceMode.Impulse
        );
    }

    void ReleaseObject()
    {
        if (grabbedBody == null)
            return;

        if (showDebug)
        {
            Debug.Log(
                "Released: " + grabbedBody.name
            );
        }

        grabbedBody = null;
    }

    void OnDisable()
    {
        ReleaseObject();
    }
}
