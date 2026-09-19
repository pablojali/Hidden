using Hidden.Discovery;
using UnityEngine;
using UnityEngine.UI;

namespace Hidden.UI
{
    // Subscribes to DiscoveryManager's OnDiscovery event -- DiscoveryManager
    // has no reference to this or any other UI object, and this class never
    // calls a mutating method on it, only reads TotalTargets/DiscoveredCount.
    //
    // M0.7: per-discovery/completion feedback is communicated visually
    // (FireworkEffect, CompletionFeedback -- both in Scripts/Discovery),
    // not through text. This component only shows the running progress
    // count; M0.6's pop-in confirmation/completion text was removed.
    public class DiscoveryUI : MonoBehaviour
    {
        [SerializeField] private DiscoveryManager manager;
        [SerializeField] private Text progressText;

        // Read-only, updated synchronously (no Update() tick needed to
        // observe it), so EditMode tests can assert on it without a live
        // Canvas/Text or Play Mode.
        public string CurrentProgressText { get; private set; } = string.Empty;

        private DiscoveryManager boundManager;

        // Exposed so both Unity's own OnEnable() and EditMode tests (which
        // can't rely on Unity calling OnEnable usefully outside Play Mode)
        // can wire the subscription deterministically -- the same pattern
        // CharacterMover.Initialize()/DiscoveryManager.RegisterTargets()
        // already established.
        public void Bind(DiscoveryManager newManager)
        {
            Unbind();

            manager = newManager;
            boundManager = newManager;

            if (manager != null)
            {
                manager.OnDiscovery += HandleDiscovery;
            }

            RefreshProgress();
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

        private void HandleDiscovery(Discoverable _)
        {
            RefreshProgress();
        }

        private void RefreshProgress()
        {
            if (manager == null)
            {
                return;
            }

            CurrentProgressText = $"{manager.DiscoveredCount} / {manager.TotalTargets}";

            if (progressText != null)
            {
                progressText.text = CurrentProgressText;
            }
        }
    }
}
