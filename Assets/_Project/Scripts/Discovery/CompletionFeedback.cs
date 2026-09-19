using UnityEngine;

namespace Hidden.Discovery
{
    // Minimal one-shot completion feedback: briefly boosts the scene's key
    // light intensity once every target has been discovered, then returns
    // to normal. No UI, no audio, no particles -- purely a visual cue that
    // the loop is complete.
    public class CompletionFeedback : MonoBehaviour
    {
        [SerializeField] private DiscoveryManager manager;
        [SerializeField] private Light keyLight;
        [SerializeField] private float pulseDuration = 1f;
        [SerializeField] private float intensityBoost = 0.6f;

        private float baseIntensity;
        private float reactionTimer;

        private void Awake()
        {
            if (keyLight != null)
            {
                baseIntensity = keyLight.intensity;
            }
        }

        private void OnEnable()
        {
            if (manager != null)
            {
                manager.OnCompleted += HandleCompleted;
            }
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.OnCompleted -= HandleCompleted;
            }
        }

        private void Update()
        {
            if (reactionTimer <= 0f || keyLight == null)
            {
                return;
            }

            reactionTimer -= Time.deltaTime;

            if (reactionTimer <= 0f)
            {
                keyLight.intensity = baseIntensity;
                return;
            }

            var elapsed = Mathf.Clamp01(1f - reactionTimer / pulseDuration);
            keyLight.intensity = baseIntensity + Mathf.Sin(elapsed * Mathf.PI) * intensityBoost;
        }

        private void HandleCompleted()
        {
            reactionTimer = pulseDuration;
        }
    }
}
