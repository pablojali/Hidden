#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hidden.EditorTools
{
    // Runs on two triggers, because neither alone is reliable everywhere:
    // - [InitializeOnLoad] + delayCall covers opening the project interactively.
    // - IPreprocessBuildWithReport covers headless/CI builds (-batchmode), where
    //   delayCall is not guaranteed to fire before the build runs.
    [InitializeOnLoad]
    internal static class ProjectFoundationSetup
    {
        private const string RendererDataPath = "Assets/_Project/Settings/URP-Mobile-Renderer.asset";
        private const string PipelineAssetPath = "Assets/_Project/Settings/URP-Mobile.asset";
        private const string GroundMaterialPath = "Assets/_Project/Materials/M_Ground.mat";

        static ProjectFoundationSetup()
        {
            EditorApplication.delayCall += EnsureSetup;
        }

        public static void EnsureSetup()
        {
            EnsureRenderPipeline();
            EnsureGroundMaterial();
        }

        private static void EnsureRenderPipeline()
        {
            var settingsDirectory = Path.GetDirectoryName(PipelineAssetPath);
            if (!string.IsNullOrEmpty(settingsDirectory) && !AssetDatabase.IsValidFolder(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
                AssetDatabase.Refresh();
            }

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererDataPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererDataPath);
            }

            var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipelineAsset == null)
            {
                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetPath);
            }

            pipelineAsset.msaaSampleCount = 2;
            pipelineAsset.renderScale = 1f;
            pipelineAsset.supportsCameraDepthTexture = false;
            pipelineAsset.supportsCameraOpaqueTexture = false;

            // Keep the shader-variant space small: this foundation scene has a
            // single directional light and no shadows/mixed lighting, but the
            // default URP asset enables features (additional lights, shadows,
            // reflection probes) that multiply compiled shader variants into
            // the tens of thousands and make CI builds take hours. These are
            // read-only via the public API in this URP version, so they're set
            // through the serialized fields instead.
            pipelineAsset.shadowCascadeCount = 1;
            var serializedPipelineAsset = new SerializedObject(pipelineAsset);
            SetBoolIfPresent(serializedPipelineAsset, "m_MainLightShadowsSupported", false);
            SetIntIfPresent(serializedPipelineAsset, "m_AdditionalLightsRenderingMode", 0);
            SetBoolIfPresent(serializedPipelineAsset, "m_AdditionalLightShadowsSupported", false);
            SetBoolIfPresent(serializedPipelineAsset, "m_MixedLightingSupported", false);
            SetBoolIfPresent(serializedPipelineAsset, "m_ReflectionProbeBlending", false);
            SetBoolIfPresent(serializedPipelineAsset, "m_ReflectionProbeBoxProjection", false);
            serializedPipelineAsset.ApplyModifiedPropertiesWithoutUndo();

            if (GraphicsSettings.defaultRenderPipeline != pipelineAsset)
            {
                GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            }

            var activeQualityLevel = QualitySettings.GetQualityLevel();
            var qualityLevelCount = QualitySettings.names.Length;
            for (var i = 0; i < qualityLevelCount; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                if (QualitySettings.renderPipeline != pipelineAsset)
                {
                    QualitySettings.renderPipeline = pipelineAsset;
                }
            }
            QualitySettings.SetQualityLevel(activeQualityLevel, applyExpensiveChanges: false);

            EditorUtility.SetDirty(pipelineAsset);
            AssetDatabase.SaveAssets();
        }

        private static void SetBoolIfPresent(SerializedObject serializedObject, string propertyName, bool value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"ProjectFoundationSetup: URP asset has no property '{propertyName}'; skipping.");
                return;
            }
            property.boolValue = value;
        }

        private static void SetIntIfPresent(SerializedObject serializedObject, string propertyName, int value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"ProjectFoundationSetup: URP asset has no property '{propertyName}'; skipping.");
                return;
            }
            property.intValue = value;
        }

        private static void EnsureGroundMaterial()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (material == null)
            {
                return;
            }

            if (material.shader != urpLit)
            {
                material.shader = urpLit;
                material.color = new Color(0.42f, 0.55f, 0.36f);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();
            }
        }
    }

    internal sealed class ProjectFoundationBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            ProjectFoundationSetup.EnsureSetup();
        }
    }
}
#endif
