using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// A treasure chest the player opens with the use key (E by default).
    /// It is also an input, so opening the chest can open a door or light a brazier.
    /// </summary>
    public class TreasureChest : PuzzleActivator, IInteractable
    {
        [Header("Chest")]
        [Tooltip("The lid's hinge. It turns around its own red (X) axis.")]
        public Transform lidPivot;

        [Tooltip("How far (degrees) the lid opens.")]
        public float openAngle = -110f;

        [Tooltip("How fast the lid moves (degrees per second).")]
        public float lidSpeed = 160f;

        [Tooltip("Ticked: the player can close the chest again. Unticked: it stays open.")]
        public bool canClose = false;

        [Tooltip("Optional. Something that only appears once the chest is open (like a glow).")]
        public GameObject showWhenOpen;

        private float currentAngle;

        protected override void Start()
        {
            base.Start();
            if (showWhenOpen != null)
                showWhenOpen.SetActive(false);
        }

        public string GetPrompt()
        {
            if (!IsActive)
                return "Open chest";
            return canClose ? "Close chest" : "The chest is open";
        }

        public void Interact(GameObject interactor)
        {
            if (IsActive && !canClose)
                return;
            SetOpen(!IsActive);
        }

        public void EndInteract(GameObject interactor) { }

        /// <summary>Opens the chest (can be called from a UnityEvent).</summary>
        public void Open() => SetOpen(true);

        private void SetOpen(bool open)
        {
            if (showWhenOpen != null)
                showWhenOpen.SetActive(open);
            SetActiveState(open);
        }

        private void Update()
        {
            if (lidPivot == null)
                return;

            float target = IsActive ? openAngle : 0f;
            currentAngle = Mathf.MoveTowards(currentAngle, target, lidSpeed * Time.deltaTime);
            lidPivot.localRotation = Quaternion.Euler(currentAngle, 0f, 0f);
        }
    }
}
