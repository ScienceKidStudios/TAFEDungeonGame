using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// Lights up when its inputs are on. Use glowing floor runes to draw a line from a lever
    /// or plate to the door it opens, or put a brazier beside a door.
    ///
    /// Tip: stretch the floor runes by changing the object's Z scale.
    /// </summary>
    public class IndicatorLight : PuzzleReceiver
    {
        [Header("Indicator")]
        [Tooltip("The glowing part that changes colour. Leave empty to find it automatically.")]
        public StateVisual visual;

        [Tooltip("Optional. A light that only shines while this is lit.")]
        public Light glowLight;

        [Tooltip("Optional. An object (like a magic flame) that only appears while this is lit.")]
        public GameObject showWhenLit;

        [Tooltip("Ticked: lights up when the inputs are OFF instead.")]
        public bool invert = false;

        /// <summary>True while the indicator is lit.</summary>
        public bool IsLit { get; private set; }

        private void Awake()
        {
            if (visual == null)
                visual = GetComponentInChildren<StateVisual>();
        }

        protected override void Start()
        {
            SetLit(invert);
            base.Start();
        }

        protected override void OnInputsChanged(bool satisfied)
        {
            SetLit(satisfied != invert);
        }

        /// <summary>Turns the light on or off directly (can be called from a UnityEvent).</summary>
        public void SetLit(bool lit)
        {
            IsLit = lit;
            if (visual != null)
                visual.SetState(lit);
            if (glowLight != null)
                glowLight.enabled = lit;
            if (showWhenLit != null)
                showWhenLit.SetActive(lit);
        }
    }
}
