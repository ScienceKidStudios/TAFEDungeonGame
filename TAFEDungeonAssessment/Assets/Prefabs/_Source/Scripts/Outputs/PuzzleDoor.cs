using System;
using UnityEngine;
using UnityEngine.Events;

namespace PuzzleMechanics
{
    /// <summary>
    /// A door that opens when its inputs are on.
    ///
    /// The door can have any number of moving parts. Each part can slide (a portcullis that
    /// lifts up) and/or swing (a wooden door on hinges).
    ///
    /// Drag levers, plates, switches etc. into the Inputs list. Set 'Input Mode' to All
    /// (every input needed) or Any (one input is enough).
    /// You can also call Open(), Close() or Toggle() from a UnityEvent.
    /// </summary>
    public class PuzzleDoor : PuzzleReceiver
    {
        [Serializable]
        public class SlidingPanel
        {
            [Tooltip("The moving part (a door leaf or the portcullis). Give it a kinematic Rigidbody so it pushes things smoothly.")]
            public Transform panel;

            [Tooltip("How far this part slides when the door is fully open, in metres. (0, 2.5, 0) = lifts up 2.5 m.")]
            public Vector3 openOffset = Vector3.zero;

            [Tooltip("How far this part turns when the door is fully open, in degrees. (0, 90, 0) = swings open on its hinge.")]
            public Vector3 openRotation = Vector3.zero;

            [NonSerialized] public Vector3 closedPosition;
            [NonSerialized] public Quaternion closedRotation;
            [NonSerialized] public Rigidbody body;
        }

        [Header("Moving Parts")]
        [Tooltip("The parts that move when the door opens.")]
        public SlidingPanel[] panels = new SlidingPanel[0];

        [Tooltip("Optional glowing runes on the frame: blue when locked, green when open.")]
        public StateVisual statusLights;

        [Header("Movement")]
        [Tooltip("How many seconds the door takes to open or close.")]
        [Min(0.05f)] public float openDuration = 1.2f;

        [Header("Behaviour")]
        [Tooltip("Ticked: once the door opens it never closes again.")]
        public bool stayOpenOnceOpened = false;

        [Tooltip("Ticked: the door starts open. (If inputs are connected, it follows them as soon as the game starts.)")]
        public bool startOpen = false;

        [Header("Door Events")]
        [Tooltip("Things to do when the door starts opening, like playing a sound.")]
        public UnityEvent onOpened = new UnityEvent();

        [Tooltip("Things to do when the door starts closing.")]
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
                p.closedRotation = p.panel.localRotation;
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

            // SmoothStep eases in and out so the parts don't start or stop with a jolt.
            float eased = Mathf.SmoothStep(0f, 1f, progress);

            foreach (SlidingPanel p in panels)
            {
                if (p.panel == null)
                    continue;

                Vector3 localPosition = p.closedPosition + p.openOffset * eased;
                Quaternion localRotation = p.closedRotation * Quaternion.Euler(p.openRotation * eased);
                Transform parent = p.panel.parent;

                if (p.body != null && p.body.isKinematic && parent != null)
                {
                    p.body.MovePosition(parent.TransformPoint(localPosition));
                    p.body.MoveRotation(parent.rotation * localRotation);
                }
                else
                {
                    p.panel.localPosition = localPosition;
                    p.panel.localRotation = localRotation;
                }
            }
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            // Show where each sliding part ends up when open.
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
            foreach (SlidingPanel p in panels)
            {
                if (p == null || p.panel == null || p.panel.parent == null || p.openOffset == Vector3.zero)
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
