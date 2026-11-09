using UnityEngine;

namespace PuzzleMechanics
{
    public enum ButtonMode
    {
        [Tooltip("Each press flips it on or off.")]
        Toggle,
        [Tooltip("On only while the player holds the use key down.")]
        Momentary,
        [Tooltip("Turns on when pressed, then turns off by itself after a few seconds.")]
        Timed
    }

    /// <summary>
    /// A stone switch or button the player presses with the use key (E by default).
    /// Choose Toggle, Momentary (hold) or Timed in the Inspector.
    ///
    /// The same script drives the wall-mounted button (cap pushes in) and the
    /// pedestal button (cap pushes down). Only 'Press Direction' differs.
    /// </summary>
    public class WallButton : PuzzleActivator, IInteractable
    {
        [Header("Button")]
        [Tooltip("Toggle = press for on, press again for off. Momentary = on while held down. Timed = on for a few seconds.")]
        public ButtonMode mode = ButtonMode.Toggle;

        [Tooltip("Timed only: how many seconds it stays on.")]
        [Min(0.1f)] public float timedDuration = 3f;

        [Tooltip("Timed only: pressing it again while it is on starts the countdown again.")]
        public bool restartTimerOnPress = true;

        [Header("Moving Part")]
        [Tooltip("The stone that pushes in when pressed.")]
        public Transform buttonCap;

        [Tooltip("How far (metres) it pushes in.")]
        public float pressDepth = 0.03f;

        [Tooltip("Which way it moves when pressed. (0, 0, -1) = into the wall. (0, -1, 0) = down into a pedestal.")]
        public Vector3 pressDirection = Vector3.back;

        private Vector3 capOutPosition;
        private float timeLeft;

        /// <summary>Seconds left before a Timed button turns off.</summary>
        public float TimeLeft => timeLeft;

        protected override void Awake()
        {
            base.Awake();
            if (buttonCap != null)
                capOutPosition = buttonCap.localPosition;
        }

        public string GetPrompt()
        {
            switch (mode)
            {
                case ButtonMode.Momentary: return "Hold to press";
                case ButtonMode.Timed: return IsActive ? $"Switch ({timeLeft:0.0}s left)" : "Press switch";
                default: return IsActive ? "Press switch (turn off)" : "Press switch (turn on)";
            }
        }

        public void Interact(GameObject interactor)
        {
            switch (mode)
            {
                case ButtonMode.Toggle:
                    SetActiveState(!IsActive);
                    break;

                case ButtonMode.Momentary:
                    SetActiveState(true);
                    break;

                case ButtonMode.Timed:
                    if (!IsActive || restartTimerOnPress)
                        timeLeft = timedDuration;
                    SetActiveState(true);
                    break;
            }
        }

        public void EndInteract(GameObject interactor)
        {
            if (mode == ButtonMode.Momentary)
                SetActiveState(false);
        }

        /// <summary>Lets UnityEvents or other scripts press the button.</summary>
        public void Press() => Interact(null);

        private void Update()
        {
            if (mode == ButtonMode.Timed && IsActive)
            {
                timeLeft -= Time.deltaTime;
                if (timeLeft <= 0f)
                {
                    timeLeft = 0f;
                    SetActiveState(false);
                }
            }

            if (buttonCap != null)
            {
                Vector3 target = IsActive ? capOutPosition + pressDirection.normalized * pressDepth : capOutPosition;
                buttonCap.localPosition = Vector3.MoveTowards(buttonCap.localPosition, target, 0.5f * Time.deltaTime);
            }
        }
    }
}
