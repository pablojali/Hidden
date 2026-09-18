using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        // so this proves whether the Player is alive at all, and now also
        // reports the actual on-device render pipeline/camera state instead
        // of guessing blind at the next fix. Remove once the black-screen
        // issue is resolved.
        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 28,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.yellow }
            };

            var report = new StringBuilder();
            report.AppendLine("HIDDEN DEBUG");
            report.AppendLine($"graphicsDeviceType: {SystemInfo.graphicsDeviceType}");
            report.AppendLine($"screen: {Screen.width}x{Screen.height}");
            report.AppendLine($"GraphicsSettings.currentRenderPipeline: {DescribeObject(GraphicsSettings.currentRenderPipeline)}");
            report.AppendLine($"QualitySettings.renderPipeline: {DescribeObject(QualitySettings.renderPipeline)}");
            report.AppendLine($"RenderPipelineManager.currentPipeline: {DescribeRuntimePipeline()}");
            report.AppendLine($"Camera.allCamerasCount: {Camera.allCamerasCount}");

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                report.AppendLine("Camera.main: NULL");
            }
            else
            {
                report.AppendLine($"Camera.main: '{mainCamera.name}' enabled={mainCamera.enabled} clearFlags={mainCamera.clearFlags}");
                var additionalData = mainCamera.GetUniversalAdditionalCameraData();
                report.AppendLine(additionalData == null
                    ? "UniversalAdditionalCameraData: MISSING"
                    : $"UniversalAdditionalCameraData: present, renderType={additionalData.renderType}");
            }

            GUI.Box(new Rect(20, 20, Screen.width - 40, 480), report.ToString(), style);
        }

        private static string DescribeObject(Object obj)
        {
            return obj == null ? "NULL" : $"{obj.GetType().Name} '{obj.name}'";
        }

        private static string DescribeRuntimePipeline()
        {
            var pipeline = RenderPipelineManager.currentPipeline;
            return pipeline == null ? "NULL" : pipeline.GetType().Name;
        }
    }
}
