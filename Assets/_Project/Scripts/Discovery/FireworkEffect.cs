using UnityEngine;

namespace Hidden.Discovery
{
    // M0.7: the per-discovery feedback, replacing M0.6's confirmation
    // text -- a short burst of sparks at the discovered target's position.
    // Subscribes to DiscoveryManager.OnDiscovery the same way DiscoveryUI
    // does; DiscoveryManager has no reference back. A single, persistent
    // rig (never instantiated/destroyed) is repositioned and replayed for
    // each discovery, so there is no per-discovery GameObject allocation
    // and no per-frame allocation in Update().
    public class FireworkEffect : MonoBehaviour
    {
        [SerializeField] private DiscoveryManager manager;
        [SerializeField] private Transform[] sparks;
        [SerializeField] private float burstDuration = 0.6f;
        [SerializeField] private float burstRadius = 0.9f;
        [SerializeField] private float startHeight = 1.1f;
        [SerializeField] private float sparkSize = 0.16f;

        public bool IsPlaying => timer > 0f;

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
                manager.OnDiscovery += HandleDiscovery;
            }
        }

        // Exposed for tests and for re-triggering without waiting on a
        // real DiscoveryManager event.
        public void Play(Vector3 position)
        {
            transform.position = position + Vector3.up * startHeight;
            timer = burstDuration;
        }

        private void Awake()
        {
            sparkDirections = new Vector3[sparks.Length];
            for (var i = 0; i < sparks.Length; i++)
            {
                var angle = i * Mathf.PI * 2f / Mathf.Max(sparks.Length, 1);
                sparkDirections[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.6f + 0.5f, Mathf.Sin(angle)).normalized;

                if (sparks[i] != null)
                {
                    sparks[i].localScale = Vector3.zero;
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
                boundManager.OnDiscovery -= HandleDiscovery;
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

            // Sparks shoot outward over the first ~40% of the burst, then
            // hold/fade for the rest -- one quick, readable pop, not a
            // lingering effect.
            var expand = Mathf.Sin(Mathf.Clamp01(t / 0.4f) * Mathf.PI * 0.5f);
            var fade = 1f - Mathf.Clamp01((t - 0.35f) / 0.65f);
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

        private void HandleDiscovery(Discoverable target)
        {
            Play(target.Position);
        }
    }
}
