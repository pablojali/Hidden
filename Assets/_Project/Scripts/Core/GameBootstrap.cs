using UnityEngine;

namespace Hidden.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 60;

        private void Awake()
        {
            Initialize();
        }

        // Unity does not call Awake() for a plain (non-ExecuteAlways)
        // MonoBehaviour outside Play Mode, so EditMode tests can't rely on
        // AddComponent triggering it (same reason CharacterMover exposes
        // Initialize()). Exposed publicly so tests can drive it directly.
        public void Initialize()
        {
            Application.targetFrameRate = targetFrameRate;
            Debug.Log("GameBootstrap: initialized.");
        }
    }
}
