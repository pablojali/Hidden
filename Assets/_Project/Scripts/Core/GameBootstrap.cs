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

        // Temporary diagnostic: IMGUI renders independently of URP/shaders,
        // so this proves whether the Player is alive and compositing frames
        // at all, isolating whether a device-side black screen is a 3D
        // rendering problem or something more fundamental. Remove once the
        // black-screen issue is resolved.
        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 48,
                normal = { textColor = Color.yellow }
            };
            GUI.Box(new Rect(20, 20, Screen.width - 40, 200), "HIDDEN DEBUG\nGameBootstrap is running.\nIf you see this, the app is alive.", style);
        }
    }
}
