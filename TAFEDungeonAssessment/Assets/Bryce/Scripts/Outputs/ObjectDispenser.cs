using UnityEngine;
using UnityEngine.Events;

namespace PuzzleMechanics
{
    /// <summary>
    /// Drops a new object (e.g. a cube) when its inputs are satisfied.
    /// It keeps track of the object it made: if that object is destroyed, falls out of
    /// the level or wanders too far away, a replacement is dropped automatically.
    ///
    /// You can also call Dispense() from a UnityEvent.
    /// </summary>
    public class ObjectDispenser : PuzzleReceiver
    {
        [Header("What to spawn")]
        [Tooltip("The object to drop. Should have a Rigidbody (and ideally a Carryable). " +
                 "If empty, a plain 0.8 m cube is created.")]
        public GameObject objectPrefab;

        [Tooltip("Override the spawned object's Rigidbody mass?")]
        public bool overrideMass = false;

        [Tooltip("Mass (kg) to give the spawned object when 'Override Mass' is ticked.")]
        [Min(0.01f)] public float mass = 20f;

        [Header("Where")]
        [Tooltip("Where objects appear. If empty, the dispenser's own position is used.")]
        public Transform spawnPoint;

        [Header("When")]
        [Tooltip("Drop an object as soon as the game starts.")]
        public bool dispenseOnStart = false;

        [Tooltip("Drop an object each time the inputs become satisfied (e.g. each button press).")]
        public bool dispenseWhenInputsSatisfied = true;

        [Tooltip("If an object from this dispenser already exists, remove it and drop a fresh one. " +
                 "If off, nothing happens until the old one is gone.")]
        public bool replaceExisting = true;

        [Header("Lost objects")]
        [Tooltip("Automatically drop a replacement when the current object is destroyed or lost.")]
        public bool respawnWhenLost = true;

        [Tooltip("The object counts as lost if it falls below this height.")]
        public float lostBelowY = -10f;

        [Tooltip("The object counts as lost if it gets further than this (metres) from the dispenser. 0 = no limit.")]
        [Min(0f)] public float maxDistance = 100f;

        [Header("Dispenser Events")]
        public UnityEvent onDispensed = new UnityEvent();

        private GameObject current;
        private bool hasDispensed;

        /// <summary>The object this dispenser most recently made (null if none).</summary>
        public GameObject CurrentObject => current;

        protected override void Start()
        {
            base.Start();
            if (dispenseOnStart)
                Dispense();
        }

        protected override void OnInputsChanged(bool satisfied)
        {
            if (satisfied && dispenseWhenInputsSatisfied)
                Dispense();
        }

        /// <summary>Drops a new object (replacing the old one if 'Replace Existing' is on).</summary>
        public void Dispense()
        {
            if (current != null)
            {
                if (!replaceExisting)
                    return;
                RemoveCurrent();
            }

            Transform point = spawnPoint != null ? spawnPoint : transform;
            current = objectPrefab != null
                ? Instantiate(objectPrefab, point.position, point.rotation)
                : CreateDefaultCube(point);

            if (overrideMass && current.TryGetComponent(out Rigidbody rb))
                rb.mass = mass;

            hasDispensed = true;
            onDispensed.Invoke();
        }

        private void Update()
        {
            if (respawnWhenLost && hasDispensed && IsLost())
                Dispense();
        }

        private bool IsLost()
        {
            if (current == null)
                return true;

            Vector3 pos = current.transform.position;
            if (pos.y < lostBelowY)
                return true;

            return maxDistance > 0f && Vector3.Distance(pos, transform.position) > maxDistance;
        }

        private void RemoveCurrent()
        {
            if (current.TryGetComponent(out Carryable carryable))
            {
                carryable.Dissolve();
            }
            else
            {
                current.SetActive(false);
                Destroy(current);
            }
            current = null;
        }

        private static GameObject CreateDefaultCube(Transform point)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Dispensed Cube";
            cube.transform.SetPositionAndRotation(point.position, point.rotation);
            cube.transform.localScale = Vector3.one * 0.8f;
            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.mass = 20f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            cube.AddComponent<Carryable>();
            return cube;
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Transform point = spawnPoint != null ? spawnPoint : transform;
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(point.position, Vector3.one * 0.8f);
        }
    }
}
