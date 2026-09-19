using UnityEngine;

namespace Hidden.Levels
{
    // Minimal level metadata -- a name and how many Discoverable targets the
    // level is supposed to have. Deliberately does not own or duplicate any
    // discovery logic: DiscoveryManager stays the single owner of progress,
    // this is only data a level author fills in and LevelInfo can check
    // against it at runtime.
    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "Hidden/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string levelId = "level_01";
        [SerializeField] private string displayName = "Level";
        [SerializeField] private int expectedTargetCount = 1;

        public string LevelId => levelId;
        public string DisplayName => displayName;
        public int ExpectedTargetCount => expectedTargetCount;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(levelId) &&
            !string.IsNullOrWhiteSpace(displayName) &&
            expectedTargetCount > 0;
    }
}
