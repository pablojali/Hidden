using UnityEngine;

namespace Hidden.Discovery
{
    // Minimal, reusable one-shot discovery feedback for a Discoverable that
    // has no CharacterMover to drive walk/idle animation (e.g. a static
    // environmental target): a brief scale pulse, then back to normal.
    // Completely decoupled from movement -- CharacterVisual (M0.3/M0.4)
    // remains the feedback for the two moving targets; this covers the
    // static one without touching CharacterMover or CharacterVisual.
    [RequireComponent(typeof(Discoverable))]
    public class DiscoveryPulseFeedback : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private float pulseDuration = 0.7f;
        [SerializeField] private float pulseScale = 0.3f;

        private Discoverable discoverable;
        private float reactionTimer;

        private void Awake()
        {
            discoverable = GetComponent<Discoverable>();

            if (model == null)
            {
                model = transform;
            }
        }

        private void OnEnable()
        {
            discoverable.Discovered += HandleDiscovered;
        }

        private void OnDisable()
        {
            discoverable.Discovered -= HandleDiscovered;
        }

        private void Update()
        {
            if (reactionTimer <= 0f)
            {
                return;
            }

            reactionTimer -= Time.deltaTime;

            if (reactionTimer <= 0f)
            {
                model.localScale = Vector3.one;
                return;
            }

            var elapsed = Mathf.Clamp01(1f - reactionTimer / pulseDuration);
            var pulse = 1f + Mathf.Sin(elapsed * Mathf.PI) * pulseScale;
            model.localScale = Vector3.one * pulse;
        }

        private void HandleDiscovered()
        {
            reactionTimer = pulseDuration;
        }
    }
}
