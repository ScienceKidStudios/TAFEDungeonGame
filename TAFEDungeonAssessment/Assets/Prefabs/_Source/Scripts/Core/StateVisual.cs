using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// Changes the colour and glow of some renderers to show an on/off state.
    /// Puzzle pieces use this so players can see what is switched on: runes, plates and
    /// braziers glow a soft blue when off and a bright magical green when on.
    ///
    /// Works with HDRP, URP and the Built-in pipeline. It uses a MaterialPropertyBlock,
    /// so your material assets are never changed.
    /// </summary>
    public class StateVisual : MonoBehaviour
    {
        [Tooltip("The parts that change colour. Leave this empty to change every part on this object and its children.")]
        public Renderer[] renderers;

        [Tooltip("The colour when switched OFF.")]
        public Color offColor = new Color(0.2f, 0.3f, 0.55f);

        [Tooltip("The colour when switched ON.")]
        public Color onColor = new Color(0.25f, 1f, 0.7f);

        [Tooltip("How brightly it glows when ON. 0 = no glow.")]
        [Min(0f)] public float onGlow = 2.5f;

        [Tooltip("How brightly it glows when OFF. 0 = no glow.")]
        [Min(0f)] public float offGlow = 0.25f;

        // Shader property names used by the different render pipelines.
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");          // HDRP + URP
        private static readonly int ColorId = Shader.PropertyToID("_Color");                  // Built-in
        private static readonly int HdrpEmissionId = Shader.PropertyToID("_EmissiveColor");   // HDRP
        private static readonly int HdrpExposureWeightId = Shader.PropertyToID("_EmissiveExposureWeight");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");       // URP + Built-in

        private MaterialPropertyBlock block;

        public bool IsOn { get; private set; }

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();

            SetState(IsOn);
        }

        /// <summary>Shows the ON or OFF look.</summary>
        public void SetState(bool on)
        {
            IsOn = on;

            if (block == null)
                block = new MaterialPropertyBlock();

            Color color = on ? onColor : offColor;
            Color glow = color * (on ? onGlow : offGlow);

            foreach (Renderer r in renderers)
            {
                if (r == null)
                    continue;

                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                block.SetColor(ColorId, color);
                block.SetColor(HdrpEmissionId, glow);
                // In HDRP, 0 makes the glow ignore camera exposure so it is visible in dark and bright rooms alike.
                block.SetFloat(HdrpExposureWeightId, 0f);
                block.SetColor(EmissionId, glow);
                r.SetPropertyBlock(block);
            }
        }
    }
}
