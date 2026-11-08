using UnityEngine;

namespace PuzzleMechanics
{
    public enum ButtonMode
    {
        [Tooltip("Each press flips the button on or off.")]
        Toggle,
        [Tooltip("On only while the interact key is held down.")]
        Momentary,
        [Tooltip("Turns on when pressed, then turns off by itself after 'Timed Duration' seconds.")]
        Timed
    }

    /// <summary>
    /// A button the player presses with the interact key.
    /// Choose Toggle, Momentary (hold) or Timed in the Inspector.
    ///
    /// The same script drives the wall-mounted button (cap pushes in) and the
    /// pedestal button (cap pushes down). Only 'Press Direction' differs.
    /// </summary>
    public class WallButton : PuzzleActivator, IInteractable
    {
        [Header("Button")]
        [Tooltip("Toggle = press on/press off. Momentary = on while held. Timed = on for a few seconds.")]
        public ButtonMode mode = ButtonMode.Toggle;

        [Tooltip("Timed mode only: how many seconds the button stays on.")]
        [Min(0.1f)] public float timedDuration = 3f;

        [Tooltip("Timed mode only: pressing again while on restarts the countdown.")]
        public bool restartTimerOnPress = true;

        [Header("Visuals")]
        [Tooltip("The part that moves in when the button is pressed (optional).")]
        public Transform buttonCap;

        [Tooltip("How far (metres) the cap moves when pressed.")]
        public float pressDepth = 0.03f;

        [Tooltip("Which way the cap moves when pressed, in the button's local space. " +
                 "(0,0,-1) = into the wall, (0,-1,0) = down into a pedestal.")]
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
                case ButtonMode.Momentary: return "Hold to press button";
                case ButtonMode.Timed: return IsActive ? $"Button ({timeLeft:0.0}s left)" : "Press button";
                default: return IsActive ? "Turn button off" : "Turn button on";
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
