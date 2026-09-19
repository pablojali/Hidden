using System.Collections.Generic;
using UnityEngine;

namespace Hidden.Discovery
{
    // Evaluates the world from the camera's point of view: a Discoverable
    // becomes discovered once it's within range, inside the camera's view,
    // and (optionally) has line of sight. Knows nothing about
    // CharacterMover, CharacterVisual, or any other gameplay system -- it
    // only calls Discover() on whichever Discoverable targets it's given.
    public class DiscoverySystem : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera observerCamera;
        [SerializeField] private List<Discoverable> discoverables = new List<Discoverable>();
        [SerializeField] private float discoveryRange = 15f;
        [SerializeField] private bool requireLineOfSight;
        [SerializeField] private LayerMask lineOfSightMask = ~0;
        [SerializeField] private float viewportMargin = 0.05f;

        public void SetTargets(IReadOnlyList<Discoverable> targets)
        {
            discoverables.Clear();
            discoverables.AddRange(targets);
        }

        public void SetCamera(UnityEngine.Camera camera)
        {
            observerCamera = camera;
        }

        public bool CanDiscover(Discoverable target)
        {
            var cam = ResolveCamera();
            return cam != null && target != null && EvaluateConditions(cam, target);
        }

        private void Awake()
        {
            if (observerCamera == null)
            {
                observerCamera = GetComponent<UnityEngine.Camera>();
            }
        }

        private void Update()
        {
            var cam = ResolveCamera();
            if (cam == null)
            {
                return;
            }

            for (var i = 0; i < discoverables.Count; i++)
            {
                var target = discoverables[i];
                if (target == null || target.IsDiscovered)
                {
                    continue;
                }

                if (EvaluateConditions(cam, target))
                {
                    target.Discover();
                }
            }
        }

        private UnityEngine.Camera ResolveCamera()
        {
            return observerCamera != null ? observerCamera : UnityEngine.Camera.main;
        }

        private bool EvaluateConditions(UnityEngine.Camera cam, Discoverable target)
        {
            var position = target.Position;

            if (Vector3.Distance(cam.transform.position, position) > discoveryRange)
            {
                return false;
            }

            var viewportPoint = cam.WorldToViewportPoint(position);
            if (viewportPoint.z <= 0f)
            {
                return false;
            }

            var min = -viewportMargin;
            var max = 1f + viewportMargin;
            if (viewportPoint.x < min || viewportPoint.x > max || viewportPoint.y < min || viewportPoint.y > max)
            {
                return false;
            }

            if (requireLineOfSight && Physics.Linecast(cam.transform.position, position, lineOfSightMask))
            {
                return false;
            }

            return true;
        }
    }
}
