using System;
using UnityEngine;
using UnityEngine.Events;

namespace PuzzleMechanics
{
    /// <summary>
    /// A sliding door that opens when its inputs are satisfied.
    ///
    /// The door can have any number of panels. The prefab has two halves that slide apart,
    /// but one panel sliding sideways (or up) works too: just change the Panels list.
    ///
    /// Set 'Input Mode' to All (every input needed) or Any (one input is enough).
    /// You can also call Open(), Close() or Toggle() from a UnityEvent.
    /// </summary>
    public class PuzzleDoor : PuzzleReceiver
    {
        [Serializable]
        public class SlidingPanel
        {
            [Tooltip("The moving part. Give it a kinematic Rigidbody so it pushes objects smoothly.")]
            public Transform panel;

            [Tooltip("How far this panel moves when the door is fully open (in the door's local space).")]
            public Vector3 openOffset = new Vector3(1f, 0f, 0f);

            [NonSerialized] public Vector3 closedPosition;
            [NonSerialized] public Rigidbody body;
        }

        [Header("Door Parts")]
        [Tooltip("The panels that slide when the door opens.")]
        public SlidingPanel[] panels = new SlidingPanel[0];

        [Tooltip("Optional lights on the frame: inactive colour when locked, active colour when unlocked.")]
        public StateVisual statusLights;

        [Header("Movement")]
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

        private float progress;      // 0 = closed, 1 = open
        private bool lockedOpen;

        /// <summary>True when the door has been told to open (it may still be moving).</summary>
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            foreach (SlidingPanel p in panels)
            {
                if (p.panel == null)
                    continue;
                p.closedPosition = p.panel.localPosition;
                p.body = p.panel.GetComponent<Rigidbody>();
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
            progress = Mathf.MoveTowards(progress, IsOpen ? 1f : 0f, Time.fixedDeltaTime / openDuration);

            // SmoothStep eases in and out so the panels don't start or stop with a jolt.
            float eased = Mathf.SmoothStep(0f, 1f, progress);

            foreach (SlidingPanel p in panels)
            {
                if (p.panel == null)
                    continue;

                Vector3 localTarget = p.closedPosition + p.openOffset * eased;
                if (p.body != null && p.body.isKinematic && p.panel.parent != null)
                    p.body.MovePosition(p.panel.parent.TransformPoint(localTarget));
                else
                    p.panel.localPosition = localTarget;
            }
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            // Show where each panel ends up when open.
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
            foreach (SlidingPanel p in panels)
            {
                if (p == null || p.panel == null || p.panel.parent == null)
                    continue;

                Vector3 closed = Application.isPlaying ? p.closedPosition : p.panel.localPosition;
                BoxCollider box = p.panel.GetComponent<BoxCollider>();
                Vector3 size = box != null ? Vector3.Scale(box.size, p.panel.localScale) : p.panel.localScale;
                Vector3 centre = box != null ? Vector3.Scale(box.center, p.panel.localScale) : Vector3.zero;
                Gizmos.matrix = p.panel.parent.localToWorldMatrix;
                Gizmos.DrawWireCube(closed + centre + p.openOffset, size);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
