using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;
using SwitchyardRaiders.Startup;

namespace SwitchyardRaiders.Editor
{
    public static class LaunchSequenceTools
    {
        public const string Root = "Assets/SwitchyardRaiders/Startup";
        public const string ScenePath = Root + "/Scenes/SR_Launch.unity";
        public const string TimelinePath = Root + "/Timeline/SR_Opening.playable";

        [MenuItem("Switchyard Raiders/Opening/Open launch scene")]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Switchyard Raiders/Opening/Select opening Timeline")]
        public static void SelectTimeline()
        {
            var launch = UnityEngine.Object.FindFirstObjectByType<LaunchSequence>();
            Selection.activeGameObject = launch != null ? launch.director.gameObject : null;
            EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
        }

        [MenuItem("Switchyard Raiders/Opening/Reset returning-player skip")]
        public static void ResetSeen() => PlayerPrefs.DeleteKey("SwitchyardRaiders.OpeningSeen.v1");

        public static void Create()
        {
            if (!Application.dataPath.Replace('\\', '/').EndsWith("/Switchyard Raiders/Assets"))
                throw new InvalidOperationException("Wrong project; refused to create launch assets.");
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene and exit Play Mode first.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException("Launch scene already exists; use the Open menu instead.");
            foreach (var folder in new[] { "Scenes", "Timeline", "Prefabs" })
                if (!AssetDatabase.IsValidFolder(Root + "/" + folder)) AssetDatabase.CreateFolder(Root, folder);
            AssetDatabase.Refresh();
            ConfigureTexture();
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Root + "/Audio/SR_Startup.mixer");
            if (mixer == null || mixer.FindMatchingGroups("Music").Length != 1 || mixer.FindMatchingGroups("SFX").Length != 1)
                throw new InvalidOperationException("Startup mixer requires Music and SFX groups.");
            var music = mixer.FindMatchingGroups("Music")[0];
            var sfx = mixer.FindMatchingGroups("SFX")[0];
            var shader = Shader.Find("Switchyard Raiders/Startup/Railway Title");
            if (shader == null) throw new InvalidOperationException("Title shader has not imported.");
            var material = new Material(shader) { name = "SR_Title" };
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/SR_Title.png"));
            AssetDatabase.CreateAsset(material, Root + "/Art/SR_Title.mat");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            var root = new GameObject("Switchyard Raiders — Launch");
            var launch = root.AddComponent<LaunchSequence>();
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
            var rigObject = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            rigObject.name = "Meta XR — Passthrough Rig";
            rigObject.transform.SetParent(root.transform, false);
            var rig = rigObject.GetComponent<OVRCameraRig>();
            var manager = rigObject.GetComponent<OVRManager>();
            var managerData = new SerializedObject(manager);
            managerData.FindProperty("isInsightPassthroughEnabled").boolValue = true;
            managerData.FindProperty("_trackingOriginType").intValue = (int)OVRManager.TrackingOrigin.FloorLevel;
            managerData.ApplyModifiedPropertiesWithoutUndo();
            foreach (var camera in rigObject.GetComponentsInChildren<Camera>(true))
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 25f;
                camera.allowHDR = false;
                var urp = camera.GetUniversalAdditionalCameraData();
                urp.renderPostProcessing = false;
                urp.renderShadows = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            }
            rig.centerEyeAnchor.GetComponent<Camera>().tag = "MainCamera";
            var passthrough = rigObject.AddComponent<OVRPassthroughLayer>();
            // Meta XR 207 constructs the passthrough layer as an underlay internally.
            passthrough.textureOpacity = 1f;
            passthrough.edgeRenderingEnabled = false;
            launch.xrRigRoot = rigObject;
            launch.headset = rig.centerEyeAnchor;
            launch.passthrough = passthrough;
            launch.leftHand = AddHand(rig.leftHandAnchor, 0, "Left hand — skip input");
            launch.rightHand = AddHand(rig.rightHandAnchor, 1, "Right hand — skip input");

            var preview = new GameObject("Editor Preview — solid background, no passthrough");
            preview.transform.SetParent(root.transform, false);
            preview.transform.localPosition = new Vector3(0, 1.2f, 0);
            var previewCamera = preview.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.033f, 0.066f, 0.072f, 1f);
            previewCamera.fieldOfView = 55f;
            previewCamera.nearClipPlane = 0.05f;
            previewCamera.allowHDR = false;
            preview.AddComponent<AudioListener>();
            launch.desktopPreviewRoot = preview;
            launch.desktopPreviewHead = preview.transform;
            preview.SetActive(false);

            var title = new GameObject("Title — world anchored");
            title.transform.SetParent(root.transform, false);
            title.transform.localPosition = new Vector3(0, 0.9892f, 1.5f);
            title.transform.localRotation = Quaternion.Euler(8f, 0, 0);
            var animator = title.AddComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var presentation = title.AddComponent<LaunchPresentation>();
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Railway nameplate";
            quad.transform.SetParent(title.transform, false);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            presentation.titleRenderer = renderer;
            presentation.titleVisual = quad.transform;
            presentation.Apply();
            launch.presentation = presentation;
            launch.titleAnchor = title.transform;
            var director = root.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.Hold;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            launch.director = director;
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "SR_Opening";
            timeline.editorSettings.frameRate = 60;
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            timeline.fixedDuration = 4.5;
            AssetDatabase.CreateAsset(timeline, TimelinePath);
            director.playableAsset = timeline;
            var reveal = new AnimationClip { name = "SR_Title_Reveal", frameRate = 60 };
            Curve(reveal, "opacity", (0f, 0f), (0.18f, 0f), (0.82f, 1f), (4.5f, 1f));
            Curve(reveal, "settle", (0f, 0.965f), (0.28f, 0.965f), (0.9f, 1.006f), (1.18f, 1f), (4.5f, 1f));
            Curve(reveal, "verticalOffset", (0f, -0.025f), (0.28f, -0.025f), (1.05f, 0f), (4.5f, 0f));
            Curve(reveal, "sweep", (0f, -0.2f), (1.35f, -0.2f), (2.25f, 1.4f), (4.5f, 1.4f));
            AssetDatabase.CreateAsset(reveal, Root + "/Timeline/SR_Title_Reveal.anim");
            var track = timeline.CreateTrack<AnimationTrack>(null, "Title · reveal / settle / brass sweep");
            var revealClip = track.CreateClip(reveal);
            revealClip.start = 0;
            revealClip.duration = 4.5;
            revealClip.displayName = "Title reveal → light sweep → hold";
            director.SetGenericBinding(track, animator);
            AddAudio(timeline, director, title.transform, "Coupler", 0.9, 0.6f, 0.6f, sfx);
            AddAudio(timeline, director, title.transform, "Sweep", 1.35, 0.35f, 0.25f, sfx);
            AddAudio(timeline, director, title.transform, "Sting", 0.9, 0.45f, 0f, music);
            timeline.CreateMarkerTrack();
            Marker(timeline, 0, "Room visible first — title starts after 0.35 s");
            Marker(timeline, 0.9, "Coupler / musical signature");
            Marker(timeline, 1.35, "Soft brass light sweep");
            Marker(timeline, 4.5, "Title ready — next-stage event");

            var config = OVRProjectConfig.CachedProjectConfig;
            config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.HandsOnly;
            config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
            OVRProjectConfig.CommitProjectConfig(config);
            OVRManifestPreprocessor.GenerateOrUpdateAndroidManifest(true);
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/SR_Launch.prefab");
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            Debug.Log("Switchyard Raiders opening created. Desktop Play preview is enabled; disable it on LaunchSequence for Link/Simulator. Android always uses Meta passthrough.");
        }

        private static OVRHand AddHand(Transform parent, int side, string name)
        {
            // Tracking only: the user's real hands remain visible through passthrough.
            var handObject = new GameObject(name);
            handObject.transform.SetParent(parent, false);
            var hand = handObject.AddComponent<OVRHand>();
            var serialized = new SerializedObject(hand);
            serialized.FindProperty("HandType").intValue = side;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return hand;
        }

        private static void ConfigureTexture()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/Art/SR_Title.png");
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void Curve(AnimationClip clip, string property, params (float time, float value)[] points)
        {
            var curve = new AnimationCurve(points.Select(p => new Keyframe(p.time, p.value)).ToArray());
            for (int i = 0; i < points.Length; i++)
            {
                var mode = property == "sweep" ? AnimationUtility.TangentMode.Linear : AnimationUtility.TangentMode.ClampedAuto;
                AnimationUtility.SetKeyLeftTangentMode(curve, i, mode);
                AnimationUtility.SetKeyRightTangentMode(curve, i, mode);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(LaunchPresentation), property), curve);
        }

        private static void AddAudio(TimelineAsset timeline, PlayableDirector director, Transform parent,
            string cue, double start, float volume, float spatialBlend, AudioMixerGroup group)
        {
            string path = Root + "/Audio/SR_Startup_" + cue + ".wav";
            var audio = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (audio == null) throw new InvalidOperationException("Missing preview cue: " + path);
            var go = new GameObject(cue + " · " + group.name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.volume = volume;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0f;
            source.minDistance = 1f;
            source.maxDistance = 8f;
            source.outputAudioMixerGroup = group;
            var track = timeline.CreateTrack<AudioTrack>(null, cue + " · " + group.name);
            var clip = track.CreateClip(audio);
            clip.start = start;
            clip.duration = Math.Min(audio.length, 4.5 - start);
            clip.displayName = "TEMP " + cue + " — replace WAV with final recording";
            ((AudioPlayableAsset)clip.asset).loop = false;
            clip.easeInDuration = cue == "Coupler" ? 0 : 0.03;
            clip.easeOutDuration = 0.08;
            director.SetGenericBinding(track, source);
        }

        private static void Marker(TimelineAsset timeline, double time, string label)
        {
            var marker = timeline.markerTrack.CreateMarker<SignalEmitter>(time);
            marker.name = label;
            marker.emitOnce = true;
            // Cue labels only. The controller owns completion to prevent duplicate signals on resume.
        }
    }
}
