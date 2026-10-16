using System;
using System.Linq;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace SwitchyardRaiders.Editor
{
    public static class LaunchSequenceValidation
    {
        [MenuItem("Switchyard Raiders/Opening/Validate opening")]
        public static void Validate()
        {
            var gate = new LaunchGate();
            Require(!gate.Complete(), "Completion must not fire before room readiness.");
            Require(!gate.AdvanceRoom(false, true, 20f), "Missing passthrough must not start the title.");
            Require(!gate.AdvanceRoom(true, false, 20f), "System UI must suspend startup.");
            Require(!gate.AdvanceRoom(true, true, 0.2f), "Room-only interval must be visible.");
            Require(!gate.AdvanceRoom(false, true, 0.1f), "Lost room readiness must reset the interval.");
            Require(!gate.AdvanceRoom(true, true, 0.2f), "Room readiness must be sustained.");
            Require(gate.AdvanceRoom(true, true, 0.16f), "Title should start once the room is ready.");
            Require(!gate.AdvanceRoom(true, true, 2f), "Repeated ready events must not restart the title.");
            Require(gate.Complete() && !gate.Complete(), "Completion must fire once.");

            var launch = UnityEngine.Object.FindAnyObjectByType<LaunchSequence>();
            Require(launch != null, "Open SR_Launch before validating.");
            Require(launch.headset != null && launch.passthrough != null && launch.leftHand != null && launch.rightHand != null,
                "Meta runtime references must all be connected.");
            var timeline = launch.director.playableAsset as TimelineAsset;
            Require(timeline != null && Math.Abs(timeline.duration - 4.5) < 0.001, "Opening duration must be 4.5 seconds.");
            foreach (var track in timeline.GetOutputTracks().Where(t => t is AnimationTrack || t is AudioTrack))
            {
                Require(launch.director.GetGenericBinding(track) != null, "Unbound Timeline track: " + track.name);
                foreach (var clip in track.GetClips())
                    if (clip.asset is AudioPlayableAsset audio)
                        Require(audio.clip != null && !audio.loop && clip.end <= 4.5, "Invalid audio cue: " + clip.displayName);
            }
            var sources = launch.GetComponentsInChildren<AudioSource>(true);
            Require(sources.Length == 3 && sources.All(s => s.outputAudioMixerGroup != null && !s.playOnAwake),
                "Every opening audio source must have a mixer route and be driven by Timeline.");
            Require(!ShaderUtil.ShaderHasError(launch.presentation.titleRenderer.sharedMaterial.shader), "Title shader compilation failed.");
            Require(EditorBuildSettings.scenes.First(s => s.enabled).path == LaunchSequenceTools.ScenePath, "Launch must be the first enabled build scene.");
            Require(OVRProjectConfig.CachedProjectConfig.handTrackingSupport == OVRProjectConfig.HandTrackingSupport.HandsOnly,
                "App must remain hands only.");
            Require(OVRProjectConfig.CachedProjectConfig.insightPassthroughSupport == OVRProjectConfig.FeatureSupport.Required,
                "Passthrough must be required.");
            Require(!PlayerSettings.SplashScreen.show &&
                OVRProjectConfig.CachedProjectConfig.systemLoadingScreenBackground == OVRProjectConfig.SystemLoadingScreenBackground.ContextualPassthrough,
                "Use contextual system passthrough without the additional Unity splash.");
            Debug.Log("PASS — opening readiness/focus/once-only completion; Timeline duration and bindings; all 3 audio routes; shader; first build scene; hands-only/passthrough settings.");
        }

        public static void ReplaceCueLabels()
        {
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(LaunchSequenceTools.TimelinePath);
            foreach (var marker in timeline.markerTrack.GetMarkers().OfType<SignalEmitter>().ToArray())
            {
                var cue = timeline.markerTrack.CreateMarker<LaunchCueMarker>(marker.time);
                cue.label = cue.name = marker.name;
                timeline.markerTrack.DeleteMarker(marker);
            }
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
