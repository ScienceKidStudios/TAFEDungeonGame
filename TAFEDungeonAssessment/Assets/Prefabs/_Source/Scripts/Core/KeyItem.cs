using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// Makes a pickup work as a key. Carry it to a locked door with a matching Key Name to open it.
    /// </summary>
    [RequireComponent(typeof(Carryable))]
    public class KeyItem : MonoBehaviour
    {
        [Tooltip("Which locks this key opens. A lock with the same Key Name (or a blank one) accepts it.")]
        public string keyName = "Iron Key";
    }
}
