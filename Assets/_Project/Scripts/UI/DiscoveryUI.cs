using System.Collections.Generic;
using Hidden.Discovery;
using UnityEngine;
using UnityEngine.UI;

namespace Hidden.UI
{
    // Subscribes to DiscoveryManager's events -- DiscoveryManager has no
    // reference to this or any other UI object, and this class never calls
    // a mutating method on it, only reads TotalTargets/DiscoveredCount.
    // Purely presentational, same one-directional-dependency shape as
    // CharacterVisual -> Discoverable and DiscoveryPulseFeedback ->
    // Discoverable.
    public class DiscoveryUI : MonoBehaviour
    {
        [SerializeField] private DiscoveryManager manager;
        [SerializeField] private Text progressText;
        [SerializeField] private Text confirmationText;
        [SerializeField] private CanvasGroup confirmationGroup;

        [Header("Microcopy (configurable here, never hardcoded in gameplay code)")]
        [SerializeField] private List<string> discoveryMessages = new List<string> { "There you are!" };
        [SerializeField] private string completionMessage = "All found!";

        [Header("Animation")]
        [SerializeField] private float messageDuration = 1.3f;
        [SerializeField] private float completionMessageDuration = 1.8f;
        [SerializeField] private float popScale = 1.15f;
        [SerializeField] private float completionPopScale = 1.3f;

        // Read-only state, updated synchronously (no Update() tick needed
        // to observe them), so EditMode tests can assert on the UI's
        // decisions without a live Canvas/Text or Play Mode.
        public string CurrentProgressText { get; private set; } = string.Empty;
        public string CurrentMessage { get; private set; } = string.Empty;
        public bool IsShowingMessage => messageTimer > 0f;
        public int DiscoveryMessageCount { get; private set; }
        public int CompletionMessageCount { get; private set; }

        private DiscoveryManager boundManager;
        private float messageTimer;
        private float messageTotalDuration;
        private float targetPopScale;
        private int nextMessageIndex;

        // Exposed so both Unity's own OnEnable() and EditMode tests (which
        // can't rely on Unity calling OnEnable usefully outside Play Mode)
        // can wire the subscription deterministically -- the same pattern
        // CharacterMover.Initialize()/DiscoverySystem.SetTargets()/
        // DiscoveryManager.RegisterTargets() already established.
        public void Bind(DiscoveryManager newManager)
        {
            Unbind();

            manager = newManager;
            boundManager = newManager;

            if (manager != null)
            {
                manager.OnDiscovery += HandleDiscovery;
                manager.OnCompleted += HandleCompleted;
            }

            RefreshProgress();
        }

        // Lets the confirmation/completion copy be swapped without editing
        // this component's code -- and later, without touching
        // DiscoveryManager/DiscoverySystem at all, a localization system
        // can supply this from a translated source instead.
        public void Configure(IEnumerable<string> newDiscoveryMessages, string newCompletionMessage)
        {
            discoveryMessages = new List<string>(newDiscoveryMessages);
            completionMessage = newCompletionMessage;
            nextMessageIndex = 0;
        }

        private void Awake()
        {
            if (confirmationGroup != null)
            {
                confirmationGroup.alpha = 0f;
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
                boundManager.OnCompleted -= HandleCompleted;
            }

            boundManager = null;
        }

        private void Update()
        {
            if (messageTimer <= 0f)
            {
                return;
            }

            messageTimer -= Time.deltaTime;
            var t = Mathf.Clamp01(1f - Mathf.Max(messageTimer, 0f) / messageTotalDuration);

            // One sine hump over the full duration: fades/scales in, holds
            // near the peak, fades/scales back out. Simple on purpose --
            // "avoid distracting motion".
            var alpha = Mathf.Max(Mathf.Sin(t * Mathf.PI), 0f);
            var scaleT = Mathf.Sin(Mathf.Clamp01(t / 0.5f) * Mathf.PI * 0.5f);
            var scale = Mathf.Lerp(1f, targetPopScale, scaleT);

            if (confirmationGroup != null)
            {
                confirmationGroup.alpha = alpha;
            }

            if (confirmationText != null)
            {
                confirmationText.transform.localScale = Vector3.one * scale;
            }

            if (messageTimer <= 0f && confirmationGroup != null)
            {
                confirmationGroup.alpha = 0f;
            }
        }

        private void HandleDiscovery(Discoverable _)
        {
            RefreshProgress();
            DiscoveryMessageCount++;
            ShowMessage(NextDiscoveryMessage(), messageDuration, popScale);
        }

        private void HandleCompleted()
        {
            CompletionMessageCount++;
            ShowMessage(completionMessage, completionMessageDuration, completionPopScale);
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

        private void ShowMessage(string message, float duration, float pop)
        {
            CurrentMessage = message;

            if (confirmationText != null)
            {
                confirmationText.text = message;
            }

            messageTimer = duration;
            messageTotalDuration = duration;
            targetPopScale = pop;
        }

        private string NextDiscoveryMessage()
        {
            if (discoveryMessages == null || discoveryMessages.Count == 0)
            {
                return string.Empty;
            }

            var message = discoveryMessages[nextMessageIndex % discoveryMessages.Count];
            nextMessageIndex++;
            return message;
        }
    }
}
