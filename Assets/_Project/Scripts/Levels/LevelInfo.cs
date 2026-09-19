using Hidden.Discovery;
using UnityEngine;

namespace Hidden.Levels
{
    // Sits alongside DiscoveryManager as the boundary between "which level
    // is this" and "how is discovery going" -- DiscoveryManager remains the
    // only owner of discovery progress; this only names the level and can
    // flag a target-count mismatch against LevelDefinition's own data.
    public class LevelInfo : MonoBehaviour
    {
        [SerializeField] private LevelDefinition level;
        [SerializeField] private DiscoveryManager manager;

        public LevelDefinition Level => level;

        public bool MatchesExpectedTargetCount =>
            level != null && manager != null && manager.TotalTargets == level.ExpectedTargetCount;

        // Exposed so both Unity's own Awake() and EditMode tests (which
        // can't rely on Unity's lifecycle timing) can wire this
        // deterministically -- the same pattern established since M0.3.
        public void Bind(LevelDefinition newLevel, DiscoveryManager newManager)
        {
            level = newLevel;
            manager = newManager;
        }

        private void Awake()
        {
            if (!MatchesExpectedTargetCount)
            {
                Debug.LogWarning(
                    $"LevelInfo: expected {level?.ExpectedTargetCount} targets but DiscoveryManager reports {manager?.TotalTargets}.");
            }
        }
    }
}
