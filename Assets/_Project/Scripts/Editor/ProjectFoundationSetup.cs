#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
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

    // This foundation scene needs none of: shadows, extra lights beyond the
    // one directional light, lightmaps, reflection probes, screen-space
    // occlusion, or URP 17's Forward+ light clustering - but by default URP
    // compiles the full keyword combinatorics for all of that anyway, which
    // is what produced tens of thousands of shader variants and multi-hour
    // CI builds. Strips by keyword, shader by shader (including the ground
    // material's own URP/Lit shader), removing only variants that provably
    // cannot be reached given this project's fixed set of disabled features.
    internal sealed class ProjectFoundationShaderStripper : IPreprocessShaders
    {
        public int callbackOrder => 100;

        private static readonly string[] UnneededKeywords =
        {
            "_MAIN_LIGHT_SHADOWS",
            "_MAIN_LIGHT_SHADOWS_CASCADE",
            "_MAIN_LIGHT_SHADOWS_SCREEN",
            "_ADDITIONAL_LIGHTS_VERTEX",
            "_ADDITIONAL_LIGHTS",
            "_ADDITIONAL_LIGHT_SHADOWS",
            "_SHADOWS_SOFT",
            "_SHADOWS_SOFT_LOW",
            "_SHADOWS_SOFT_MEDIUM",
            "_SHADOWS_SOFT_HIGH",
            "_MIXED_LIGHTING_SUBTRACTIVE",
            "LIGHTMAP_ON",
            "LIGHTMAP_SHADOW_MIXING",
            "DIRLIGHTMAP_COMBINED",
            "DYNAMICLIGHTMAP_ON",
            "_REFLECTION_PROBE_BLENDING",
            "_REFLECTION_PROBE_BOX_PROJECTION",
            "_REFLECTION_PROBE_ATLAS",
            "_SCREEN_SPACE_OCCLUSION",
            "_LIGHT_LAYERS",
            "_LIGHT_COOKIES",
            // _FORWARD_PLUS / _CLUSTER_LIGHT_LOOP / _CLUSTERED_RENDERING are
            // deliberately NOT here: a freshly created UniversalRendererData
            // defaults to the Forward+ rendering path in this URP version,
            // so those keywords describe the mode actually in use, not an
            // optional feature to strip. Blacklisting them made scoring
            // prefer leftover classic-Forward variants that the renderer
            // never actually requests at runtime - shaders compiled fine
            // with no errors, but nothing on screen matched, which is what
            // produced a solid black screen with a perfectly clean build log.
        };

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // Score every variant by how many keywords this project doesn't
            // need are enabled in it (0 = exactly what's actually used).
            // Keep only the variants tied for the lowest score. For almost
            // every shader/pass that's the fully-clean (score 0) variant,
            // collapsing hundreds or thousands of variants down to a
            // handful - same effect as unconditionally dropping any variant
            // with an unneeded keyword. The difference matters for a pass
            // that turns out to have NO fully-clean variant at all (this is
            // what caused a solid black screen: URP/Lit's ForwardLit and
            // GBuffer passes have none in this URP version) - unconditional
            // removal stripped those to zero compiled variants, so nothing
            // rendered; scoring instead keeps whichever variants are
            // closest, so the pass still has something to compile and draw.
            var scores = new int[data.Count];
            var minScore = int.MaxValue;
            for (var i = 0; i < data.Count; i++)
            {
                scores[i] = CountUnneededKeywords(shader, data[i]);
                if (scores[i] < minScore)
                {
                    minScore = scores[i];
                }
            }

            for (var i = data.Count - 1; i >= 0; i--)
            {
                if (scores[i] > minScore)
                {
                    data.RemoveAt(i);
                }
            }
        }

        private static int CountUnneededKeywords(Shader shader, ShaderCompilerData variant)
        {
            var count = 0;
            foreach (var keywordName in UnneededKeywords)
            {
                ShaderKeyword keyword;
                try
                {
                    keyword = new ShaderKeyword(shader, keywordName);
                }
                catch
                {
                    continue;
                }

                if (variant.shaderKeywordSet.IsEnabled(keyword))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
#endif
