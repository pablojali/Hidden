using UnityEngine;

namespace Hidden.Characters
{
    // Purely cosmetic: reads CharacterMover's current state and animates a
    // child "model" transform -- never the root transform CharacterMover
    // uses for navigation. This is a procedural placeholder; a real rig/
    // animation controller can replace it later by driving the same model
    // transform without CharacterMover changing at all.
    [RequireComponent(typeof(CharacterMover))]
    public class CharacterVisual : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private float walkBobHeight = 0.06f;
        [SerializeField] private float walkBobSpeed = 6f;
        [SerializeField] private float idleSwaySpeed = 1.2f;
        [SerializeField] private float idleSwayAngle = 3f;

        private CharacterMover mover;
        private Vector3 modelBasePosition;
        private float animationTime;

        private void Awake()
        {
            mover = GetComponent<CharacterMover>();

            if (model == null)
            {
                model = transform;
            }

            modelBasePosition = model.localPosition;
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
        }
    }
}
