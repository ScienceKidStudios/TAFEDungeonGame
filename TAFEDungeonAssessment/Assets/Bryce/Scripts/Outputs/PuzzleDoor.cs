using UnityEngine;
using UnityEngine.Events;

namespace PuzzleMechanics
{
    /// <summary>
    /// A sliding door that opens when its inputs are satisfied.
    ///
    /// Set 'Input Mode' to All (every input needed) or Any (one input is enough).
    /// You can also call Open(), Close() or Toggle() from a UnityEvent.
    /// </summary>
    public class PuzzleDoor : PuzzleReceiver
    {
        [Header("Door Parts")]
        [Tooltip("The moving panel. Give it a kinematic Rigidbody so it pushes objects smoothly.")]
        public Transform doorPanel;

        [Tooltip("Optional lights on the frame that show whether the door is open.")]
        public StateVisual statusLights;

        [Header("Movement")]
        [Tooltip("How far the panel moves when fully open, relative to the door (local space).")]
        public Vector3 openOffset = new Vector3(2.05f, 0f, 0f);

        [Tooltip("Seconds to fully open or close.")]
        [Min(0.05f)] public float openDuration = 1f;

        [Header("Behaviour")]
        [Tooltip("Once opened, the door never closes again.")]
        public bool stayOpenOnceOpened = false;

        [Tooltip("Start open. (If inputs are connected, the door will follow them as soon as the game starts.)")]
        public bool startOpen = false;

        [Header("Door Events")]
        public UnityEvent onOpened = new UnityEvent();
        public UnityEvent onClosed = new UnityEvent();

        private Vector3 closedLocalPosition;
        private Rigidbody panelBody;
        private float progress;      // 0 = closed, 1 = open
        private bool lockedOpen;

        /// <summary>True when the door has been told to open (it may still be moving).</summary>
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (doorPanel != null)
            {
                closedLocalPosition = doorPanel.localPosition;
                panelBody = doorPanel.GetComponent<Rigidbody>();
            }

            if (startOpen)
            {
                IsOpen = true;
                progress = 1f;
            }
        }

        protected override void Start()
        {
            UpdateStatusLights();
            base.Start();
        }

        protected override void OnInputsChanged(bool satisfied)
        {
            if (satisfied)
                Open();
            else
                Close();
        }

        public void Open()
        {
            if (stayOpenOnceOpened)
                lockedOpen = true;

            if (IsOpen)
                return;

            IsOpen = true;
            UpdateStatusLights();
            onOpened.Invoke();
        }

        public void Close()
        {
            if (lockedOpen || !IsOpen)
                return;

            IsOpen = false;
            UpdateStatusLights();
            onClosed.Invoke();
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        private void UpdateStatusLights()
        {
            if (statusLights != null)
                statusLights.SetState(IsOpen);
        }

        private void FixedUpdate()
        {
            if (doorPanel == null)
                return;

            progress = Mathf.MoveTowards(progress, IsOpen ? 1f : 0f, Time.fixedDeltaTime / openDuration);

            // SmoothStep eases in and out so the door doesn't start or stop with a jolt.
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 localTarget = closedLocalPosition + openOffset * eased;

            if (panelBody != null && panelBody.isKinematic)
                panelBody.MovePosition(doorPanel.parent.TransformPoint(localTarget));
            else
                doorPanel.localPosition = localTarget;
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (doorPanel == null)
                return;

            // Show where the panel ends up when open.
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
            Vector3 closed = Application.isPlaying ? closedLocalPosition : doorPanel.localPosition;
            Gizmos.matrix = doorPanel.parent != null ? doorPanel.parent.localToWorldMatrix : Matrix4x4.identity;
            Gizmos.DrawWireCube(closed + openOffset, doorPanel.localScale);
        }
    }
}
