
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PhysicsInteraction : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;

    [Header("Pushing")]
    public float pushForce = 12f;
    public float maxPushableMass = 100f;
    public float maxPushSpeed = 10f;

    [Header("Standing on Objects")]
    public bool rideMovingObjects = true;
    public LayerMask platformLayers = ~0;
    public float groundCheckDistance = 0.35f;
    public float minGroundNormalY = 0.65f;

    [Header("Player Interaction")]
    public bool canPushPlayers = true;
    public LayerMask playerLayers;

    private Vector3 lastPosition;
    private Vector3 movementVelocity;

    private Rigidbody standingBody;
    private Vector3 lastPlatformPosition;

    void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        movementVelocity =
            (transform.position - lastPosition) /
            Mathf.Max(Time.deltaTime, 0.0001f);

        lastPosition = transform.position;
    }

    void LateUpdate()
    {
        UpdateStandingBody();

        if (rideMovingObjects && standingBody != null)
        {
            Vector3 movement =
                standingBody.position - lastPlatformPosition;

            if (movement.sqrMagnitude > 0.000001f)
            {
                controller.Move(movement);
                lastPosition = transform.position;
            }

            lastPlatformPosition = standingBody.position;
        }
    }

    void UpdateStandingBody()
    {
        // Check for a surface directly beneath the player.
        Vector3 origin = transform.TransformPoint(
            controller.center
        );

        float bottomOffset =
            controller.height * 0.5f - controller.radius;

        origin += Vector3.down * bottomOffset;

        float distance =
            controller.radius + groundCheckDistance;

        if (Physics.SphereCast(
            origin,
            controller.radius * 0.8f,
            Vector3.down,
            out RaycastHit hit,
            distance,
            platformLayers,
            QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y >= minGroundNormalY &&
                hit.rigidbody != null)
            {
                if (standingBody != hit.rigidbody)
                {
                    standingBody = hit.rigidbody;
                    lastPlatformPosition = standingBody.position;
                }

                return;
            }
        }

        standingBody = null;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb == null || rb.isKinematic)
            return;

        if (rb.mass > maxPushableMass)
            return;

        // Don't push the object we're standing on.
        if (hit.moveDirection.y < -0.3f)
            return;

        if (rb == standingBody)
            return;

        bool isPlayer =
            (playerLayers.value &
            (1 << hit.collider.gameObject.layer)) != 0;

        if (isPlayer && !canPushPlayers)
            return;

        Vector3 direction = movementVelocity;
        direction.y = 0f;

        float speed = Mathf.Min(
            direction.magnitude,
            maxPushSpeed
        );

        if (speed < 0.1f)
            return;

        direction.Normalize();

        rb.AddForceAtPosition(
            direction * pushForce * speed,
            hit.point,
            ForceMode.Force
        );
    }
}
