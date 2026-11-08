using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// Lights up when its inputs are satisfied. Use a row of these as lines on the floor
    /// leading from a button or plate to the door it opens (like in Portal).
    ///
    /// Tip: stretch the strip by changing the object's Z scale.
    /// </summary>
    public class IndicatorLight : PuzzleReceiver
    {
        [Header("Indicator")]
        [Tooltip("The visual to switch on and off. If empty, one is searched for in the children.")]
        public StateVisual visual;

        [Tooltip("Light up when the inputs are NOT satisfied instead.")]
        public bool invert = false;

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
            if (visual != null)
                visual.SetState(lit);
        }
    }
}
