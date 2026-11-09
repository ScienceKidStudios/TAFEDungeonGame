using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// A target (like a gong) that turns ON when something is thrown at it hard enough.
    ///
    /// Setup: needs a solid (not trigger) collider on the SAME GameObject as this script,
    /// because Unity only tells the object that owns the collider about hits.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ImpactTarget : PuzzleActivator
    {
        [Header("Hit")]
        [Tooltip("How fast something must be moving (metres per second) to count as a hit. A gentle bump won't count.")]
        [Min(0f)] public float minImpactSpeed = 5f;

        [Tooltip("How heavy (kg) the thrown object must be. 0 = anything counts.")]
        [Min(0f)] public float minImpactMass = 0f;

        [Header("Reset")]
        [Tooltip("Ticked: stays on forever once hit. Unticked: turns off again after 'Reset After' seconds.")]
        public bool stayActivated = true;

        [Tooltip("How many seconds before the target turns off again (only when 'Stay Activated' is unticked).")]
        [Min(0.1f)] public float resetAfter = 5f;

        [Header("Swing")]
        [Tooltip("The part that swings when hit (the gong disc). Leave empty for no swing.")]
        public Transform swingPart;

        [Tooltip("How far (degrees) the part swings when hit.")]
        public float swingAngle = 18f;

        [Header("Debug")]
        [Tooltip("Print the speed of every hit to the Console. Handy for choosing 'Min Impact Speed'.")]
        public bool logImpacts = false;

        private float timeLeft;
        private float swingTime = 10f;
        private Quaternion swingRest;

        protected override void Awake()
        {
            base.Awake();
            if (swingPart != null)
                swingRest = swingPart.localRotation;
        }

        private void OnCollisionEnter(Collision collision)
        {
            Rigidbody rb = collision.rigidbody;
            if (rb == null)
                return;

            float speed = collision.relativeVelocity.magnitude;

            if (logImpacts)
                Debug.Log($"{name} hit by {rb.name} at {speed:0.0} m/s ({rb.mass} kg)", this);

            if (speed < minImpactSpeed || rb.mass < minImpactMass)
                return;

            swingTime = 0f;
            timeLeft = resetAfter;
            SetActiveState(true);
        }

        private void Update()
        {
            if (swingPart != null && swingTime < 3f)
            {
                // A swing that slowly dies away.
                swingTime += Time.deltaTime;
                float angle = swingAngle * Mathf.Sin(swingTime * 9f) * Mathf.Exp(-swingTime * 1.6f);
                swingPart.localRotation = swingRest * Quaternion.Euler(angle, 0f, 0f);
            }

            if (stayActivated || !IsActive)
                return;

            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
                SetActiveState(false);
        }

        /// <summary>Turns the target off again (can be called from a UnityEvent).</summary>
        public void ResetTarget() => SetActiveState(false);
    }
}
