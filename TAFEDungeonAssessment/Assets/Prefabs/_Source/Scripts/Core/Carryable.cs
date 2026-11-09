using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// Marks an object as something the player can pick up (a box, a barrel, a key...).
    ///
    /// The object's weight is its Rigidbody mass. Players can lift light and medium objects,
    /// can only drag heavy ones, and can't move very heavy ones at all (see PlayerCarrier).
    ///
    /// If you write your own carry script, set IsHeld = true while carrying the object
    /// so pressure plates ignore it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Carryable : MonoBehaviour
    {
        [Tooltip("The name shown on screen when the player looks at this object.")]
        public string displayName = "Box";

        private Rigidbody body;

        public Rigidbody Body
        {
            get
            {
                if (body == null)
                    body = GetComponent<Rigidbody>();
                return body;
            }
        }

        /// <summary>The object's weight in kg (its Rigidbody mass).</summary>
        public float Mass => Body.mass;

        /// <summary>True while a player is holding this object.</summary>
        public bool IsHeld { get; set; }

        /// <summary>Removes the object (used by magic barriers, dispensers and locks).</summary>
        public void Dissolve()
        {
            // Hide it straight away so it stops colliding this frame, then destroy it.
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
