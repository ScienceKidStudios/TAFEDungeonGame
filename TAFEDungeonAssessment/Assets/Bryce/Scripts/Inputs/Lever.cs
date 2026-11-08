using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// A floor lever that flips on/off each time the player interacts with it.
    /// </summary>
    public class Lever : PuzzleActivator, IInteractable
    {
        [Header("Lever")]
        [Tooltip("Should the lever start in the ON position?")]
        public bool startOn = false;

        [Header("Visuals")]
        [Tooltip("The pivot that rotates (around its local X axis) when the lever is pulled.")]
        public Transform handlePivot;

        [Tooltip("Handle angle (degrees) when OFF.")]
        public float offAngle = -35f;

        [Tooltip("Handle angle (degrees) when ON.")]
        public float onAngle = 35f;

        [Tooltip("How fast the handle swings (degrees per second).")]
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
