using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// A target that turns ON when something hits it hard enough (e.g. a thrown cube).
    ///
    /// Setup: needs a (non-trigger) collider on the SAME GameObject as this script,
    /// because Unity only sends collision messages to the object that owns the collider.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ImpactTarget : PuzzleActivator
    {
        [Header("Impact")]
        [Tooltip("Minimum impact speed (metres per second) needed to activate. A gentle bump won't count.")]
        [Min(0f)] public float minImpactSpeed = 5f;

        [Tooltip("Minimum mass (kg) of the object that hits it. 0 = anything with a Rigidbody counts.")]
        [Min(0f)] public float minImpactMass = 0f;

        [Header("Reset")]
        [Tooltip("If ticked, the target stays on forever once hit.")]
        public bool stayActivated = true;

        [Tooltip("If 'Stay Activated' is off: seconds before the target turns off again.")]
        [Min(0.1f)] public float resetAfter = 5f;

        [Header("Debug")]
        [Tooltip("Print the speed of every hit to the Console. Useful for tuning 'Min Impact Speed'.")]
        public bool logImpacts = false;

        private float timeLeft;

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

            timeLeft = resetAfter;
            SetActiveState(true);
        }

        private void Update()
        {
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
