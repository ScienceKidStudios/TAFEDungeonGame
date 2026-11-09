using UnityEngine;

namespace PuzzleMechanics
{
    public enum SpikeMode
    {
        [Tooltip("The spikes go up and down on a timer.")]
        UpAndDown,
        [Tooltip("The spikes are always up.")]
        AlwaysUp,
        [Tooltip("The spikes stay hidden (just decoration).")]
        AlwaysDown
    }

    /// <summary>
    /// Floor spikes that pop up and down. They can send the player back to the start
    /// (or to a respawn point) if the player touches them while they are up.
    ///
    /// Setup: needs a collider set to "Is Trigger" on the SAME GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SpikeTrap : MonoBehaviour
    {
        [Header("Spikes")]
        [Tooltip("The group of spikes that moves up and down.")]
        public Transform spikes;

        [Tooltip("Up And Down = on a timer. Always Up = always dangerous. Always Down = decoration only.")]
        public SpikeMode mode = SpikeMode.UpAndDown;

        [Tooltip("How high (metres) the spikes rise.")]
        public float raiseHeight = 0.5f;

        [Tooltip("How many seconds the spikes stay up.")]
        [Min(0.1f)] public float upTime = 1.5f;

        [Tooltip("How many seconds the spikes stay down.")]
        [Min(0.1f)] public float downTime = 2.5f;

        [Tooltip("Seconds to wait before the first rise. Give traps in a row different delays for a wave effect.")]
        [Min(0f)] public float startDelay = 0f;

        [Tooltip("How fast the spikes move (metres per second).")]
        public float moveSpeed = 4f;

        [Header("Danger")]
        [Tooltip("Ticked: touching the raised spikes sends the player back to the respawn point.")]
        public bool sendPlayerBack = true;

        [Tooltip("Where the player is sent. Leave empty to send them back to where they started the level.")]
        public Transform respawnPoint;

        private Vector3 downPosition;
        private float timer;
        private bool wantUp;
        private Vector3 playerStart;
        private bool hasPlayerStart;

        /// <summary>True while the spikes are more than half way up.</summary>
        public bool SpikesUp => spikes != null && spikes.localPosition.y > downPosition.y + raiseHeight * 0.5f;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (spikes != null)
                downPosition = spikes.localPosition;
            timer = -startDelay;
        }

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerStart = player.transform.position;
                hasPlayerStart = true;
            }
        }

        private void Update()
        {
            switch (mode)
            {
                case SpikeMode.AlwaysUp: wantUp = true; break;
                case SpikeMode.AlwaysDown: wantUp = false; break;
                default:
                    timer += Time.deltaTime;
                    if (timer >= 0f)
                    {
                        float cycle = upTime + downTime;
                        wantUp = timer % cycle < upTime;
                    }
                    break;
            }

            if (spikes != null)
            {
                Vector3 target = wantUp ? downPosition + Vector3.up * raiseHeight : downPosition;
                spikes.localPosition = Vector3.MoveTowards(spikes.localPosition, target, moveSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!sendPlayerBack || !SpikesUp)
                return;

            Transform player = other.CompareTag("Player") ? other.transform :
                other.transform.root.CompareTag("Player") ? other.transform.root : null;
            if (player != null)
                SendBack(player);
        }

        private void SendBack(Transform player)
        {
            Vector3 target = respawnPoint != null ? respawnPoint.position : hasPlayerStart ? playerStart : player.position;

            // A CharacterController must be switched off while it is moved, or it snaps back.
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;

            if (player.TryGetComponent(out Rigidbody body))
            {
                body.position = target;
                body.linearVelocity = Vector3.zero;
            }
            player.position = target;

            if (controller != null)
                controller.enabled = true;
        }

        private void OnDrawGizmos()
        {
            if (respawnPoint == null)
                return;
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
            Gizmos.DrawLine(transform.position, respawnPoint.position);
            Gizmos.DrawWireSphere(respawnPoint.position, 0.3f);
        }
    }
}
