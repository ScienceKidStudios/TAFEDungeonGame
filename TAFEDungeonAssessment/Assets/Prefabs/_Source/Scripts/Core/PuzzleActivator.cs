using System;
using UnityEngine;
using UnityEngine.Events;

namespace PuzzleMechanics
{
    /// <summary>
    /// Base class for every puzzle INPUT (pressure plate, switch, gong, lever, runestone, chest...).
    ///
    /// An input is simply something that is either ON or OFF.
    /// Outputs such as doors, dispensers and braziers hold a list of inputs
    /// and react whenever one of them changes.
    ///
    /// To make your own input, inherit from this class and call SetActiveState(true/false).
    /// </summary>
    public abstract class PuzzleActivator : MonoBehaviour
    {
        [Header("Events (optional, no code needed)")]
        [Tooltip("Things to do once when this turns ON. Click + and drag in any object.")]
        public UnityEvent onActivated = new UnityEvent();

        [Tooltip("Things to do once when this turns OFF. Click + and drag in any object.")]
        public UnityEvent onDeactivated = new UnityEvent();

        [Header("Glow")]
        [Tooltip("The part that glows when this is on. Leave empty to find it automatically.")]
        [SerializeField] protected StateVisual stateVisual;

        /// <summary>True while this input is ON.</summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Raised every time IsActive changes. Outputs (doors, braziers, runestones...) listen to this.
        /// </summary>
        public event Action<PuzzleActivator> StateChanged;

        protected virtual void Awake()
        {
            if (stateVisual == null)
                stateVisual = GetComponentInChildren<StateVisual>();
        }

        protected virtual void Start()
        {
            if (stateVisual != null)
                stateVisual.SetState(IsActive);
        }

        /// <summary>
        /// Turns this input on or off. Does nothing if the state is already the same,
        /// so it is safe to call every frame.
        /// </summary>
        protected void SetActiveState(bool active)
        {
            if (active == IsActive)
                return;

            IsActive = active;

            if (stateVisual != null)
                stateVisual.SetState(active);

            StateChanged?.Invoke(this);

            if (active)
                onActivated.Invoke();
            else
                onDeactivated.Invoke();
        }
    }
}
