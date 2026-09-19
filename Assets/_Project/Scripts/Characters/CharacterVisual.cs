using Hidden.Discovery;
using UnityEngine;

namespace Hidden.Characters
{
    // Purely cosmetic: reads CharacterMover's current state and animates a
    // child "model" transform -- never the root transform CharacterMover
    // uses for navigation. This is a procedural placeholder; a real rig/
    // animation controller can replace it later by driving the same model
    // transform without CharacterMover changing at all.
    //
    // Optionally reacts to a Discoverable on the same object (if any) with
    // a one-shot scale pulse -- this is the only place discovery feedback
    // lives; CharacterMover and Discoverable/DiscoverySystem never know
    // this reaction exists.
    [RequireComponent(typeof(CharacterMover))]
    public class CharacterVisual : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private float walkBobHeight = 0.06f;
        [SerializeField] private float walkBobSpeed = 6f;
        [SerializeField] private float idleSwaySpeed = 1.2f;
        [SerializeField] private float idleSwayAngle = 3f;
        [SerializeField] private float discoveryReactionDuration = 0.7f;
        [SerializeField] private float discoveryPulseScale = 0.25f;

        private CharacterMover mover;
        private Discoverable discoverable;
        private Vector3 modelBasePosition;
        private float animationTime;
        private float discoveryReactionTimer;

        private void Awake()
        {
            mover = GetComponent<CharacterMover>();
            discoverable = GetComponent<Discoverable>();

            if (model == null)
            {
                model = transform;
            }

            modelBasePosition = model.localPosition;
        }

        private void OnEnable()
        {
            if (discoverable != null)
            {
                discoverable.Discovered += HandleDiscovered;
            }
        }

        private void OnDisable()
        {
            if (discoverable != null)
            {
                discoverable.Discovered -= HandleDiscovered;
            }
        }

        private void Update()
        {
            animationTime += Time.deltaTime;

            if (mover.CurrentState == CharacterMover.State.Walking)
            {
                var bob = Mathf.Sin(animationTime * walkBobSpeed) * walkBobHeight;
                model.localPosition = modelBasePosition + Vector3.up * bob;
                model.localRotation = Quaternion.identity;
            }
            else
            {
                var sway = Mathf.Sin(animationTime * idleSwaySpeed) * idleSwayAngle;
                model.localPosition = modelBasePosition;
                model.localRotation = Quaternion.Euler(0f, sway, 0f);
            }

            model.localScale = Vector3.one * ComputeDiscoveryPulse();
        }

        private void HandleDiscovered()
        {
            discoveryReactionTimer = discoveryReactionDuration;
        }

        private float ComputeDiscoveryPulse()
        {
            if (discoveryReactionTimer <= 0f)
            {
                return 1f;
            }

            discoveryReactionTimer -= Time.deltaTime;
            var elapsed = Mathf.Clamp01(1f - discoveryReactionTimer / discoveryReactionDuration);
            return 1f + Mathf.Sin(elapsed * Mathf.PI) * discoveryPulseScale;
        }
    }
}
