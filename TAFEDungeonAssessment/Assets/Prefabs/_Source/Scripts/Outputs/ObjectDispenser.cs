using UnityEngine;
using UnityEngine.Events;

namespace PuzzleMechanics
{
    /// <summary>
    /// A stone chute in the ceiling that drops a new object (e.g. a box) when its inputs are on.
    /// It keeps track of the object it made: if that object is destroyed, falls out of
    /// the level or wanders too far away, a replacement is dropped automatically.
    ///
    /// You can also call Dispense() from a UnityEvent.
    /// </summary>
    public class ObjectDispenser : PuzzleReceiver
    {
        [Header("What to drop")]
        [Tooltip("The pickup to drop. Drag any prefab from 02_Pickups in here. " +
                 "If empty, a plain 0.8 m cube is made.")]
        public GameObject objectPrefab;

        [Tooltip("Tick to change how heavy the dropped object is.")]
        public bool overrideMass = false;

        [Tooltip("How heavy (kg) the dropped object is when 'Override Mass' is ticked.")]
        [Min(0.01f)] public float mass = 20f;

        [Header("Where")]
        [Tooltip("Where objects appear (inside the chute). If empty, the dispenser's own position is used.")]
        public Transform spawnPoint;

        [Header("When")]
        [Tooltip("Ticked: drops one as soon as the game starts.")]
        public bool dispenseOnStart = false;

        [Tooltip("Ticked: drops one each time the inputs turn on (e.g. each switch press).")]
        public bool dispenseWhenInputsSatisfied = true;

        [Tooltip("Ticked: the old object vanishes when a new one drops. " +
                 "Unticked: nothing drops until the old one is gone.")]
        public bool replaceExisting = true;

        [Header("Lost objects")]
        [Tooltip("Ticked: drops a new one by itself if the old one is destroyed or lost.")]
        public bool respawnWhenLost = true;

        [Tooltip("The object counts as lost if it falls below this height (metres).")]
        public float lostBelowY = -10f;

        [Tooltip("The object counts as lost if it is further than this (metres) from the chute. 0 = no limit.")]
        [Min(0f)] public float maxDistance = 100f;

        [Header("Trapdoor")]
        [Tooltip("Optional. Trapdoor flaps that swing open for a moment each time something drops. They turn around their red (X) axis.")]
        public Transform[] trapdoorFlaps = new Transform[0];

        [Tooltip("How far (degrees) the trapdoor flaps swing open.")]
        public float flapOpenAngle = 80f;

        [Header("Dispenser Events")]
        [Tooltip("Things to do each time an object drops, like playing a sound.")]
        public UnityEvent onDispensed = new UnityEvent();

        private GameObject current;
        private bool hasDispensed;
        private float flapTimer;
        private Quaternion[] flapRest;

        /// <summary>The object this dispenser most recently made (null if none).</summary>
        public GameObject CurrentObject => current;

        private void Awake()
        {
            flapRest = new Quaternion[trapdoorFlaps.Length];
            for (int i = 0; i < trapdoorFlaps.Length; i++)
            {
                if (trapdoorFlaps[i] != null)
                    flapRest[i] = trapdoorFlaps[i].localRotation;
            }
        }

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
            flapTimer = 1.2f;
            onDispensed.Invoke();
        }

        private void Update()
        {
            if (respawnWhenLost && hasDispensed && IsLost())
                Dispense();

            AnimateFlaps();
        }

        // The flaps snap open, stay open for a moment, then swing shut.
        private void AnimateFlaps()
        {
            if (flapTimer <= 0f || flapRest == null)
                return;

            flapTimer = Mathf.Max(0f, flapTimer - Time.deltaTime);
            float open = Mathf.Clamp01(Mathf.Min(flapTimer * 2f, (1.2f - flapTimer) * 8f));
            for (int i = 0; i < trapdoorFlaps.Length && i < flapRest.Length; i++)
            {
                if (trapdoorFlaps[i] != null)
                    trapdoorFlaps[i].localRotation = flapRest[i] * Quaternion.Euler(flapOpenAngle * open, 0f, 0f);
            }
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
