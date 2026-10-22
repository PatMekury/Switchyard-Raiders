using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SwitchyardRaiders.Editor
{
    public sealed class RaidersRenderPipelineCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android) Validate();
        }

        [MenuItem("Switchyard Raiders/Build/Check Quest render pipeline")]
        public static void Validate()
        {
            // The rejected startup build selected URP through Quality Settings but
            // serialized a null active global-settings pointer with no default RP.
            if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
                throw new BuildFailedException("Switchyard Raiders requires a default URP asset in Project Settings > Graphics. Assign Mobile_RPAsset before building for Quest.");

            var shaders = GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineRuntimeShaders>();
            var textures = GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineRuntimeTextures>();
            var volume = GraphicsSettings.GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>();
            if (shaders == null || shaders.coreBlitPS == null || shaders.coreBlitColorAndDepthPS == null
                || textures == null || volume == null)
                throw new BuildFailedException("URP global settings are missing required runtime resources. Repair the URP Global Settings asset before building for Quest.");

            Debug.Log("PASS: Quest default URP and required global runtime resources are assigned.");
        }
    }
}
