using System;
using UnityEngine;

namespace Hidden.Discovery
{
    public interface IDiscoverable
    {
        bool IsDiscovered { get; }
        Vector3 Position { get; }
        void Discover();
    }

    // Completely independent from CharacterMover or any other gameplay
    // system -- it only tracks a one-shot discovered state and notifies
    // listeners once. DiscoverySystem is the only thing that calls
    // Discover(); anything else (visuals, later a counter/save system) can
    // subscribe to Discovered without this component knowing they exist.
    public class Discoverable : MonoBehaviour, IDiscoverable
    {
        [SerializeField] private bool isDiscovered;

        public event Action Discovered;

        public bool IsDiscovered => isDiscovered;
        public Vector3 Position => transform.position;

        public void Discover()
        {
            if (isDiscovered)
            {
                return;
            }

            isDiscovered = true;
            Discovered?.Invoke();
        }
    }
}
