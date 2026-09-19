using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hidden.Discovery
{
    // Owns global discovery progress across a fixed set of Discoverable
    // targets. Subscribes to each target's own one-shot Discovered event
    // (from M0.4) rather than Discoverable reaching into this class, so
    // Discoverable stays fully independent and reusable on its own.
    // DiscoverySystem only detects/triggers individual targets -- it never
    // touches progress; that responsibility lives here alone.
    public class DiscoveryManager : MonoBehaviour
    {
        // M0.7: a minimal session state -- Playing until every target is
        // found, then Completed, forever (IsComplete/completedFired below
        // already guarantee this can't un-happen or double-fire). This is
        // a thin, read-only view over state DiscoveryManager already
        // tracked since M0.5, not a new framework.
        public enum SessionState
        {
            Playing,
            Completed
        }

        [SerializeField] private List<Discoverable> targets = new List<Discoverable>();

        public event Action<Discoverable> OnDiscovery;
        public event Action OnCompleted;

        private readonly HashSet<Discoverable> discoveredSet = new HashSet<Discoverable>();
        private readonly Dictionary<Discoverable, Action> handlers = new Dictionary<Discoverable, Action>();
        private bool completedFired;

        public int TotalTargets => targets.Count;
        public int DiscoveredCount => discoveredSet.Count;
        public bool IsComplete => TotalTargets > 0 && DiscoveredCount >= TotalTargets;
        public SessionState State => IsComplete ? SessionState.Completed : SessionState.Playing;

        // Exposed so both Unity's own Awake() and EditMode tests (which
        // can't rely on Unity's lifecycle timing) can wire registration
        // deterministically -- the same pattern CharacterMover established
        // in M0.3. Safe to call again later to re-register a new set.
        public void RegisterTargets(IEnumerable<Discoverable> newTargets)
        {
            var incoming = new List<Discoverable>(newTargets);

            UnregisterAll();
            targets.Clear();
            discoveredSet.Clear();
            completedFired = false;

            foreach (var target in incoming)
            {
                if (target == null || targets.Contains(target))
                {
                    continue;
                }

                targets.Add(target);

                if (target.IsDiscovered)
                {
                    discoveredSet.Add(target);
                }

                void Handler() => HandleDiscovered(target);
                handlers[target] = Handler;
                target.Discovered += Handler;
            }

            if (!completedFired && IsComplete)
            {
                completedFired = true;
                OnCompleted?.Invoke();
            }
        }

        private void Awake()
        {
            RegisterTargets(targets);
        }

        private void OnDestroy()
        {
            UnregisterAll();
        }

        private void UnregisterAll()
        {
            foreach (var pair in handlers)
            {
                if (pair.Key != null)
                {
                    pair.Key.Discovered -= pair.Value;
                }
            }

            handlers.Clear();
        }

        private void HandleDiscovered(Discoverable target)
        {
            if (!discoveredSet.Add(target))
            {
                return;
            }

            OnDiscovery?.Invoke(target);

            if (!completedFired && IsComplete)
            {
                completedFired = true;
                OnCompleted?.Invoke();
            }
        }
    }
}
