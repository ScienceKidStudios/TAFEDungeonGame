using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// Changes the colour and glow of some renderers to show an on/off state.
    /// Puzzle inputs and indicator lights use this so players can see what is active.
    ///
    /// Works with HDRP, URP and the Built-in pipeline. It uses a MaterialPropertyBlock,
    /// so your material assets are never modified.
    /// </summary>
    public class StateVisual : MonoBehaviour
    {
        [Tooltip("Renderers to recolour. If empty, every renderer on this object and its children is used.")]
        public Renderer[] renderers;

        [Tooltip("Colour when OFF.")]
        public Color offColor = new Color(0.8f, 0.15f, 0.1f);

        [Tooltip("Colour when ON.")]
        public Color onColor = new Color(0.2f, 0.9f, 0.3f);

        [Tooltip("How strongly the object glows when ON. 0 = no glow.")]
        [Min(0f)] public float onGlow = 2f;

        [Tooltip("How strongly the object glows when OFF. 0 = no glow.")]
        [Min(0f)] public float offGlow = 0.3f;

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
                // In HDRP, 0 makes the glow ignore camera exposure so it is visible in bright scenes.
                block.SetFloat(HdrpExposureWeightId, 0f);
                block.SetColor(EmissionId, glow);
                r.SetPropertyBlock(block);
            }
        }
    }
}
