using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// Anything the player can "use" with the interact key (switches, levers, chests...).
    ///
    /// HOOKING UP YOUR OWN PLAYER:
    /// Your player script only needs to:
    ///   1. Raycast forward from the camera.
    ///   2. Find an IInteractable on the hit object:  hit.collider.GetComponentInParent&lt;IInteractable&gt;()
    ///   3. Call Interact(gameObject) when the interact key is PRESSED
    ///      and EndInteract(gameObject) when it is RELEASED (needed for "hold" switches).
    /// See PlayerInteractor.cs for a complete example.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short text for the on-screen prompt, e.g. "Pull lever".</summary>
        string GetPrompt();

        /// <summary>Called when the interact key is pressed while looking at this object.</summary>
        void Interact(GameObject interactor);

        /// <summary>Called when the interact key is released (or the player looks away).</summary>
        void EndInteract(GameObject interactor);
    }
}
