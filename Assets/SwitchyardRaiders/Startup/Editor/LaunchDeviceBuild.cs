using System;
using System.IO;
using System.Linq;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using UnityEngine.XR.OpenXR;

namespace SwitchyardRaiders.Editor
{
    public static class LaunchDeviceBuild
    {
        [Serializable]
        private sealed class BuildStatus
        {
            public string status;
            public string apk;
            public string error;
            public double seconds;
            public ulong bytes;
            public int errors;
            public int warnings;
        }

        [MenuItem("Switchyard Raiders/Opening/Validate headset readiness")]
        public static void Validate()
        {
            LaunchSequenceValidation.Validate();
            var launch = UnityEngine.Object.FindAnyObjectByType<LaunchSequence>();
            Require(!launch.desktopPreviewInEditor, "Select Use headset before testing the headset.");
            Require(launch.leftHand.isActiveAndEnabled && launch.rightHand.isActiveAndEnabled, "Both hands must be enabled.");
            Require(new SerializedObject(launch.leftHand).FindProperty("HandType").intValue == 0 &&
                new SerializedObject(launch.rightHand).FindProperty("HandType").intValue == 1, "Hand sides are incorrect.");
            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                var xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
                Require(xr != null && xr.InitManagerOnStart && xr.Manager.activeLoaders.Any(l => l.name == "OpenXRLoader"), "OpenXR loader is missing: " + group);
                var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
                Require(settings.renderMode == OpenXRSettings.RenderMode.SinglePassInstanced, "Single-pass stereo is required: " + group);
                Require(settings.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRFeature>().Any(f => f.enabled && f.GetType().Name == "MetaXRFeature"), "Meta XR hand/passthrough extensions are disabled: " + group);
            }
            foreach (var camera in launch.GetComponentsInChildren<Camera>(true))
            {
                var data = camera.GetUniversalAdditionalCameraData();
                Require(data.requiresDepthOption == CameraOverrideOption.Off && data.requiresColorOption == CameraOverrideOption.Off &&
                    !data.renderPostProcessing && !data.renderShadows && !camera.allowHDR, "Unexpected opening render cost: " + camera.name);
                Require(new SerializedObject(data).FindProperty("m_RendererIndex").intValue == 1, "Opening camera must use the lightweight renderer.");
            }
            Require(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP &&
                PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64, "Quest build must use IL2CPP ARM64.");
            var profile = BuildProfile.GetActiveBuildProfile();
            Require(profile != null && profile.GetScenesForBuild()[0].path == LaunchSequenceTools.ScenePath, "Quest entry scene is wrong.");
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(profile)))
                if (asset != null && asset.GetType().Name == "BuildProfileQualitySettings")
                    Require(new SerializedObject(asset).FindProperty("m_DefaultQualityLevel").stringValue == "Mobile", "Quest must use the valid Mobile quality level.");
            Debug.Log("PASS — headset mode, both hands, XR initialization/extensions, single-pass rendering, lightweight cameras, ARM64 IL2CPP, Mobile quality and launch entry.");
            Directory.CreateDirectory("Deliverables/Opening");
            File.WriteAllText("Deliverables/Opening/headset-configuration-check.txt",
                "PASS: headset configuration and opening performance settings.\nNo headset connected; device FPS and tracking behavior remain unmeasured.\n");
        }

        [MenuItem("Switchyard Raiders/Opening/Verify completed desktop playback")]
        public static void VerifyPlayback()
        {
            Require(EditorApplication.isPlaying, "Start the desktop preview and wait for the title to settle.");
            var launch = UnityEngine.Object.FindAnyObjectByType<LaunchSequence>();
            Require(launch != null && launch.IsDesktopPreview && launch.Phase == LaunchGate.Phase.Holding, "Desktop opening has not completed.");
            Require(!launch.enabled && !launch.presentation.enabled && !launch.presentation.GetComponent<Animator>().enabled,
                "Settled title must stop its controller, animation and presentation updates.");
            Require(!launch.director.playableGraph.IsValid(), "Timeline graph must be released on completion.");
            Require(Mathf.Approximately(launch.presentation.opacity, 1f), "Final title must remain visible.");
            Require(launch.GetComponentsInChildren<AudioSource>(true).All(s => !s.isPlaying), "Audio must finish with the opening.");
            Require(!ShaderUtil.ShaderHasError(launch.presentation.titleRenderer.sharedMaterial.shader), "Title shader failed.");
            Directory.CreateDirectory("Deliverables/Opening");
            File.WriteAllText("Deliverables/Opening/desktop-playback-check.txt",
                "PASS: title reached Holding, opacity 1; Timeline graph released; Animator/controller/presentation stopped; audio stopped; shader compiled.\nThis is a desktop functional check, not a Quest FPS measurement.\n");
            Debug.Log("PASS — optimized desktop playback completed and released its animation/audio work.");
        }

        [MenuItem("Switchyard Raiders/Opening/Build standalone Quest test APK")]
        public static void QueueBuild()
        {
            if (EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer) throw new InvalidOperationException("Exit Play Mode / wait for the active build.");
            Validate();
            Directory.CreateDirectory("Builds/Quest");
            File.WriteAllText("Builds/Quest/opening-build-status.json", JsonUtility.ToJson(new BuildStatus { status = "queued" }, true));
            EditorApplication.delayCall += Build;
        }

        private static void Build()
        {
            const string apk = "Builds/Quest/SwitchyardRaiders-Opening.apk";
            var status = new BuildStatus { status = "building", apk = Path.GetFullPath(apk) };
            WriteStatus(status);
            try
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { LaunchSequenceTools.ScenePath },
                    locationPathName = apk,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
                status.status = report.summary.result.ToString();
                status.seconds = report.summary.totalTime.TotalSeconds;
                status.bytes = report.summary.totalSize;
                status.errors = report.summary.totalErrors;
                status.warnings = report.summary.totalWarnings;
                var errors = report.steps.SelectMany(s => s.messages)
                    .Where(m => m.type == LogType.Error || m.type == LogType.Exception).Select(m => m.content).Take(12);
                status.error = string.Join("\n", errors);
            }
            catch (Exception exception) { status.status = "Failed"; status.error = exception.Message; }
            WriteStatus(status);
        }

        private static void WriteStatus(BuildStatus status) => File.WriteAllText("Builds/Quest/opening-build-status.json", JsonUtility.ToJson(status, true));
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
