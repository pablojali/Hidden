#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hidden.EditorTools
{
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

        private static void EnsureSetup()
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
}
#endif
