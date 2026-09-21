using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hidden.EditorTools
{
    // Temporary diagnostic tool: opens a scene in the Editor (not Play
    // Mode) and renders the Main Camera's actual view to a PNG, so real
    // rendered output can be inspected/compared instead of inferring
    // appearance from scene YAML. Not part of the shipped project.
    public static class DiagnosticSceneCapture
    {
        public static void CaptureAll()
        {
            Capture("Assets/_Project/Scenes/Worlds/Level_01_ForestDiorama.unity",
                "level01_wide.png", 1080, 1920);
            Capture("Assets/_Project/Scenes/Bootstrap.unity",
                "bootstrap.png", 1080, 1920);
            Debug.Log("DIAGNOSTIC_CAPTURE_DONE");
        }

        private static void Capture(string scenePath, string fileName, int width, int height)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"DIAGNOSTIC_OPENED_SCENE: {scenePath} isLoaded={scene.isLoaded} rootCount={scene.rootCount}");

            DynamicGI.UpdateEnvironment();

            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                foreach (var c in Object.FindObjectsByType<UnityEngine.Camera>(FindObjectsSortMode.None))
                {
                    cam = c;
                    break;
                }
            }

            if (cam == null)
            {
                Debug.LogError($"DIAGNOSTIC_NO_CAMERA in {scenePath}");
                return;
            }

            var rt = new RenderTexture(width, height, 24);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(rt);

            var outDir = @"C:\Users\Pablo\AppData\Local\Temp\claude\C--Users-Pablo-Documents-GitHub-Hidden\01bb3b21-1bc5-4630-9c37-9bc7f55380d2\scratchpad";
            Directory.CreateDirectory(outDir);
            var outPath = Path.Combine(outDir, fileName);
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            Debug.Log($"DIAGNOSTIC_SAVED: {outPath}");
        }
    }
}
