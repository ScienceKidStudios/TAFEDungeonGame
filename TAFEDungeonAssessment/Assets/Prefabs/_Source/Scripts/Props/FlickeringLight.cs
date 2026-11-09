using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// Makes a light flicker like a real flame. Put it on the same object as a Light.
    /// It can also make a flame model grow and shrink a little in time with the light.
    /// </summary>
    public class FlickeringLight : MonoBehaviour
    {
        [Tooltip("The light to flicker. Leave empty to use the Light on this object.")]
        public Light targetLight;

        [Tooltip("Optional. The flame model that grows and shrinks with the flicker.")]
        public Transform flame;

        [Tooltip("How much the brightness changes. 0 = steady, 0.5 = very flickery.")]
        [Range(0f, 1f)] public float flickerAmount = 0.25f;

        [Tooltip("How fast it flickers.")]
        [Min(0f)] public float flickerSpeed = 7f;

        private float baseIntensity;
        private Vector3 flameScale;
        private float seed;

        private void Awake()
        {
            if (targetLight == null)
                targetLight = GetComponent<Light>();
            if (targetLight != null)
                baseIntensity = targetLight.intensity;
            if (flame != null)
                flameScale = flame.localScale;

            // Every torch gets its own random pattern so they don't all flicker together.
            seed = Random.value * 100f;
        }

        private void Update()
        {
            float t = Time.time * flickerSpeed;
            float slow = Mathf.PerlinNoise(seed, t) - 0.5f;
            float fast = Mathf.PerlinNoise(seed + 17f, t * 2.7f) - 0.5f;
            float flicker = 1f + flickerAmount * (slow * 1.4f + fast * 0.6f);

            if (targetLight != null)
                targetLight.intensity = baseIntensity * flicker;
            if (flame != null)
                flame.localScale = new Vector3(flameScale.x, flameScale.y * flicker, flameScale.z);
        }
    }
}
