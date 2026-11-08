using System;
using UnityEngine;
using UnityEngine.Events;

namespace PuzzleMechanics
{
    /// <summary>
    /// Base class for every puzzle INPUT (pressure plate, button, target, lever, logic gate, timer...).
    ///
    /// An activator is simply something that is either ON (active) or OFF (inactive).
    /// Outputs such as doors, dispensers and indicator lights hold a list of activators
    /// and react whenever one of them changes state.
    ///
    /// To make your own input, inherit from this class and call SetActiveState(true/false).
    /// </summary>
    public abstract class PuzzleActivator : MonoBehaviour
    {
        [Header("Events (hook up anything here, no code needed)")]
        [Tooltip("Called once when this input turns ON.")]
        public UnityEvent onActivated = new UnityEvent();

        [Tooltip("Called once when this input turns OFF.")]
        public UnityEvent onDeactivated = new UnityEvent();

        [Header("Feedback")]
        [Tooltip("Optional. Changes colour when this input turns on/off. " +
                 "If left empty, the first StateVisual found on this object or its children is used.")]
        [SerializeField] protected StateVisual stateVisual;

        /// <summary>True while this input is ON.</summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Raised every time IsActive changes. Outputs (doors, lights, gates...) listen to this.
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
