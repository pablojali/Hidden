using UnityEngine;

namespace Hidden.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 60;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            Debug.Log("GameBootstrap: initialized.");
        }
    }
}
