using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// A lever that flips on and off each time the player uses it (E by default).
    /// </summary>
    public class Lever : PuzzleActivator, IInteractable
    {
        [Header("Lever")]
        [Tooltip("Tick this if the lever should start in the ON position.")]
        public bool startOn = false;

        [Header("Moving Part")]
        [Tooltip("The handle's pivot. It turns around its own red (X) axis.")]
        public Transform handlePivot;

        [Tooltip("Handle angle (degrees) when OFF.")]
        public float offAngle = -35f;

        [Tooltip("Handle angle (degrees) when ON.")]
        public float onAngle = 35f;

        [Tooltip("How fast the handle moves (degrees per second).")]
        public float swingSpeed = 300f;

        private float currentAngle;

        protected override void Start()
        {
            base.Start();
            currentAngle = startOn ? onAngle : offAngle;
            SetActiveState(startOn);
        }

        public string GetPrompt() => IsActive ? "Pull lever (turn off)" : "Pull lever (turn on)";

        public void Interact(GameObject interactor) => SetActiveState(!IsActive);

        public void EndInteract(GameObject interactor) { }

        /// <summary>Lets UnityEvents or other scripts flip the lever.</summary>
        public void Flip() => SetActiveState(!IsActive);

        private void Update()
        {
            if (handlePivot == null)
                return;

            float target = IsActive ? onAngle : offAngle;
            currentAngle = Mathf.MoveTowards(currentAngle, target, swingSpeed * Time.deltaTime);
            handlePivot.localRotation = Quaternion.Euler(currentAngle, 0f, 0f);
        }
    }
}
