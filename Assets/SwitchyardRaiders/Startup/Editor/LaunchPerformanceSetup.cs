using System;
using System.Linq;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SwitchyardRaiders.Editor
{
    public static class LaunchPerformanceSetup
    {
        public const string RendererPath = LaunchSequenceTools.Root + "/Art/SR_StartupRenderer.asset";

        [MenuItem("Switchyard Raiders/Opening/Use headset (Link or Simulator)")]
        public static void UseHeadset() => SetPreview(false);

        [MenuItem("Switchyard Raiders/Opening/Use desktop preview (no headset)")]
        public static void UseDesktop() => SetPreview(true);

        private static void SetPreview(bool preview)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before changing preview mode.");
            var launch = UnityEngine.Object.FindAnyObjectByType<LaunchSequence>();
            if (launch == null) throw new InvalidOperationException("Open SR_Launch first.");
            Undo.RecordObject(launch, "Set opening preview mode");
            launch.desktopPreviewInEditor = preview;
            // UI preview can skip package/editor domain initialization. Keep scene reload.
            // Link keeps a fresh domain until repeated physical passthrough is validated.
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = preview ? EnterPlayModeOptions.DisableDomainReload : EnterPlayModeOptions.None;
            // Editor Play uses Standalone XR settings even when Android is the build target.
            // Flat preview must not attempt to initialize a missing Link headset.
            var editorXR = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (editorXR != null)
            {
                editorXR.InitManagerOnStart = !preview;
                EditorUtility.SetDirty(editorXR);
                AssetDatabase.SaveAssets();
            }
            launch.enabled = true;
            launch.presentation.ResetForPlayback();
            launch.xrRigRoot.SetActive(!preview);
            launch.desktopPreviewRoot.SetActive(preview);
            EditorUtility.SetDirty(launch);
            EditorSceneManager.MarkSceneDirty(launch.gameObject.scene);
            EditorSceneManager.SaveScene(launch.gameObject.scene);
            Debug.Log((preview ? "Fast desktop preview: scene reload only, editor XR initialization disabled. Press Play." : "Headset mode selected with fresh scene/domain reload. Start Quest Link before pressing Play.") + " Reload options: " + EditorSettings.enterPlayModeOptions);
        }

        [MenuItem("Switchyard Raiders/Opening/Apply opening performance settings")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var launch = UnityEngine.Object.FindAnyObjectByType<LaunchSequence>();
            if (launch == null) throw new InvalidOperationException("Open SR_Launch first.");
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                // Copy an initialized URP renderer so all package resource references are retained.
                if (!AssetDatabase.CopyAsset("Assets/Settings/Mobile_Renderer.asset", RendererPath))
                    throw new InvalidOperationException("Could not copy the initialized mobile renderer.");
                renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            }
            renderer.name = "SR_StartupRenderer";
            renderer.rendererFeatures.Clear();
            renderer.renderingMode = RenderingMode.Forward;
            renderer.depthPrimingMode = DepthPrimingMode.Disabled;
            renderer.intermediateTextureMode = IntermediateTextureMode.Auto;
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);

            // The opening cameras use the same renderer slot in every included quality level.
            // Renderer zero remains available for the rest of the game.
            var pipelines = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets" })
                .Select(g => AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null).ToArray();
            foreach (var pipeline in pipelines)
            {
                // GPU Resident Drawer requires Forward+/Deferred+ across every renderer.
                // This title uses a single mesh with the mobile Forward path.
                pipeline.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
                EditorUtility.SetDirty(pipeline);
                var settings = new SerializedObject(pipeline);
                var list = settings.FindProperty("m_RendererDataList");
                if (list.arraySize > 1 && list.GetArrayElementAtIndex(1).objectReferenceValue != renderer)
                    throw new InvalidOperationException("Renderer slot 1 is already in use: " + pipeline.name);
                list.arraySize = Math.Max(2, list.arraySize);
                list.GetArrayElementAtIndex(1).objectReferenceValue = renderer;
                settings.ApplyModifiedProperties();
            }
            ConfigureRig(launch);
            var profile = UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile();
            if (profile != null)
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(profile)))
                {
                    if (asset == null || asset.GetType().Name != "BuildProfileQualitySettings") continue;
                    var quality = new SerializedObject(asset);
                    // The template profile referenced a quality level with a missing pipeline asset.
                    quality.FindProperty("m_DefaultQualityLevel").stringValue = "Mobile";
                    var levels = quality.FindProperty("m_QualityLevels");
                    levels.arraySize = 1;
                    levels.GetArrayElementAtIndex(0).stringValue = "Mobile";
                    quality.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(profile);
            }
            PrefabUtility.SaveAsPrefabAsset(launch.gameObject, LaunchSequenceTools.Root + "/Prefabs/SR_Launch.prefab");
            EditorSceneManager.MarkSceneDirty(launch.gameObject.scene);
            EditorSceneManager.SaveScene(launch.gameObject.scene);

            var texture = (TextureImporter)AssetImporter.GetAtPath(LaunchSequenceTools.Root + "/Art/SR_Title.png");
            var android = texture.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 2048;
            android.format = TextureImporterFormat.ASTC_4x4;
            android.compressionQuality = 100;
            texture.npotScale = TextureImporterNPOTScale.None;
            texture.isReadable = false;
            texture.SetPlatformTextureSettings(android);
            texture.SaveAndReimport();
            AssetDatabase.SaveAssets();
            Debug.Log("Opening optimized: forward renderer without features, no depth/color copies or post processing, ASTC title, headset mode, 90 Hz request when supported.");
        }

        private static void ConfigureRig(LaunchSequence launch)
        {
            foreach (var camera in launch.GetComponentsInChildren<Camera>(true))
            {
                var data = camera.GetUniversalAdditionalCameraData();
                data.SetRenderer(1);
                data.requiresDepthOption = CameraOverrideOption.Off;
                data.requiresColorOption = CameraOverrideOption.Off;
                data.renderPostProcessing = false;
                data.renderShadows = false;
                data.antialiasing = AntialiasingMode.None;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.useOcclusionCulling = false;
                camera.depthTextureMode = DepthTextureMode.None;
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
                PrefabUtility.RecordPrefabInstancePropertyModifications(data);
            }
            launch.desktopPreviewInEditor = false;
            launch.preferredRefreshRate = 90f;
            EditorUtility.SetDirty(launch);
        }
    }
}
