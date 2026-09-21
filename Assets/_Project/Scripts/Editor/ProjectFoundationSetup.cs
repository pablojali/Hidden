#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
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

        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        public static void EnsureSetup()
        {
            EnsureAndroidGraphicsApi();
            EnsureRenderPipeline();
            EnsureGroundMaterial();
            EnsureCameraData();
        }

        // The Bootstrap scene's Main Camera was hand-authored before URP was
        // ever actually active, so it never got the
        // UniversalAdditionalCameraData component Unity normally attaches
        // to every camera as soon as URP is the active pipeline. The Editor
        // quietly falls back to defaults for a camera missing this
        // component, but a built Player apparently does not: on device the
        // camera rendered nothing at all (not even its clear color), while
        // everything else - including IMGUI, which doesn't go through the
        // camera - worked fine. Adds the component directly to the saved
        // scene so it's baked in regardless of when in the build pipeline
        // this runs.
        private static void EnsureCameraData()
        {
            var scene = EditorSceneManager.GetSceneByPath(BootstrapScenePath);
            var openedHere = false;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
                openedHere = true;
            }

            var changed = false;
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                var camera = rootObject.GetComponentInChildren<UnityEngine.Camera>(true);
                if (camera == null)
                {
                    continue;
                }

                if (camera.GetComponent<UniversalAdditionalCameraData>() == null)
                {
                    camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (openedHere)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // Every fix aimed at the URP asset/shader variants landed clean in
        // CI (no compile errors, no exceptions, real variant counts, URP
        // confirmed active) and the device still showed solid black every
        // time - none of it was the actual cause. The build compiles for
        // both Vulkan and OpenGLES3; if the device picks Vulkan and hits a
        // driver-level rendering problem there, that would produce exactly
        // this symptom (silent black screen, nothing wrong visible from the
        // build side) and has nothing to do with anything above. Forces
        // OpenGLES3 only, which is the more universally compatible option,
        // to rule this out.
        private static void EnsureAndroidGraphicsApi()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
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

            // Creating UniversalRendererData/UniversalRenderPipelineAsset via
            // script (as opposed to the Editor's Create > Rendering menu)
            // leaves their internal utility shader references (blit, copy
            // depth, etc.) null - the menu path populates them via this same
            // reloader as part of asset creation. On device this diagnosed
            // as RenderPipelineManager.currentPipeline staying null forever
            // despite the asset being correctly assigned everywhere: URP
            // silently failed to construct the actual pipeline instance,
            // which is consistent with a null shader reference blowing up
            // inside the renderer's setup. Explicitly reload them here.
            ResourceReloader.ReloadAllNullIn(rendererData, "Packages/com.unity.render-pipelines.universal");
            ResourceReloader.ReloadAllNullIn(pipelineAsset, "Packages/com.unity.render-pipelines.universal");

            pipelineAsset.msaaSampleCount = 2;
            pipelineAsset.renderScale = 1f;
            pipelineAsset.supportsCameraDepthTexture = false;
            pipelineAsset.supportsCameraOpaqueTexture = false;

            // Deliberately NOT touching shadow/additional-light/reflection-
            // probe feature flags here anymore. They were being poked via
            // SerializedObject on internal field names (m_MainLightShadowsSupported
            // etc.) as a shader-variant-count optimization, and every build
            // since that landed rendered a solid black screen on device with
            // no error anywhere in the build log - consistent with one of
            // those blind field writes corrupting something Unity's default
            // URP asset otherwise gets right. The IPreprocessShaders stripper
            // below already collapses the shader variant count without
            // needing this, so it's not worth the risk: leave the pipeline
            // asset's feature flags at Unity's own defaults.

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
