using UnityEngine;

namespace Hidden.Discovery
{
    // M0.9 polish: the whole-level completion beat, clearly bigger and
    // longer than FireworkEffect's per-discovery burst. Same architecture
    // as FireworkEffect -- a persistent, never-instantiated/destroyed rig
    // of pre-placed spark Transforms animated by one shared sine curve in
    // Update(), no per-frame allocation -- but this rig is parented to the
    // Main Camera (in camera-local space) instead of being repositioned to
    // a world point, so the burst always reads as covering the screen
    // regardless of the camera's current pan/zoom, with no per-frame
    // camera-tracking code needed.
    //
    // Subscribes to DiscoveryManager.OnCompleted the same one-directional
    // way FireworkEffect subscribes to OnDiscovery -- DiscoveryManager has
    // no reference back. DiscoveryManager already guarantees OnCompleted
    // fires at most once per manager (M0.5); HasPlayed is a second,
    // local guarantee that Play() itself is a one-shot regardless of how
    // many times it's called.
    public class CompletionCelebration : MonoBehaviour
    {
        [SerializeField] private DiscoveryManager manager;
        [SerializeField] private Transform[] sparks;
        [SerializeField] private float burstDuration = 1.1f;
        [SerializeField] private float burstRadius = 3.5f;
        [SerializeField] private float sparkSize = 0.5f;
        [SerializeField] private float horizontalSpread = 0.6f;

        public bool IsPlaying => timer > 0f;
        public bool HasPlayed { get; private set; }

        private DiscoveryManager boundManager;
        private Vector3[] sparkDirections;
        private float timer;

        public void Bind(DiscoveryManager newManager)
        {
            Unbind();

            manager = newManager;
            boundManager = newManager;

            if (manager != null)
            {
                manager.OnCompleted += HandleCompleted;
            }
        }

        // One-shot: later calls (including a duplicate OnCompleted, which
        // DiscoveryManager itself never fires, but this stays safe either
        // way) do nothing once the celebration has already played.
        public void Play()
        {
            if (HasPlayed)
            {
                return;
            }

            HasPlayed = true;
            timer = burstDuration;
        }

        private void Awake()
        {
            sparkDirections = new Vector3[sparks.Length];
            for (var i = 0; i < sparks.Length; i++)
            {
                var angle = i * Mathf.PI * 2f / Mathf.Max(sparks.Length, 1);
                sparkDirections[i] = new Vector3(Mathf.Cos(angle) * horizontalSpread, Mathf.Sin(angle), 0f);

                if (sparks[i] != null)
                {
                    sparks[i].localScale = Vector3.zero;
                    sparks[i].localPosition = Vector3.zero;
                }
            }
        }

        private void OnEnable()
        {
            Bind(manager);
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (boundManager != null)
            {
                boundManager.OnCompleted -= HandleCompleted;
            }

            boundManager = null;
        }

        private void Update()
        {
            if (timer <= 0f)
            {
                return;
            }

            timer -= Time.deltaTime;
            var remaining01 = Mathf.Clamp01(Mathf.Max(timer, 0f) / burstDuration);
            var t = 1f - remaining01;

            // Sparks shoot outward over the first third of the burst, then
            // hold/fade for the rest -- one clear pop, not a lingering
            // effect, same shape as FireworkEffect's curve just stretched
            // to this celebration's longer duration.
            var expand = Mathf.Sin(Mathf.Clamp01(t / 0.35f) * Mathf.PI * 0.5f);
            var fade = 1f - Mathf.Clamp01((t - 0.3f) / 0.7f);
            var scale = expand * fade;

            for (var i = 0; i < sparks.Length; i++)
            {
                if (sparks[i] == null)
                {
                    continue;
                }

                sparks[i].localPosition = sparkDirections[i] * (burstRadius * expand);
                sparks[i].localScale = Vector3.one * (sparkSize * scale);
            }

            if (timer <= 0f)
            {
                foreach (var spark in sparks)
                {
                    if (spark != null)
                    {
                        spark.localScale = Vector3.zero;
                    }
                }
            }
        }

        private void HandleCompleted()
        {
            Play();
        }
    }
}
