using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// Marks an object as something the player can pick up (a cube, a crate...).
    ///
    /// The object's weight is its Rigidbody mass. Players can only lift light objects;
    /// heavier ones can only be dragged, and very heavy ones can't be moved at all
    /// (see PlayerCarrier for the limits).
    ///
    /// If you write your own carry script, set IsHeld = true while carrying the object
    /// so pressure plates ignore it (like in Portal, a held cube doesn't press a plate).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Carryable : MonoBehaviour
    {
        [Tooltip("Name shown on screen when the player looks at this object.")]
        public string displayName = "Cube";

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

        /// <summary>Removes the object (used by fizzlers and dispensers).</summary>
        public void Dissolve()
        {
            // Hide it straight away so it stops colliding this frame, then destroy it.
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
