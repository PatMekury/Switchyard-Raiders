using System;
using System.IO;
using System.Linq;
using Fusion;
using Meta.XR.MRUtilityKit;
using Photon.Voice;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static class RaidersServicesSetup
    {
        private const string Root = "Assets/SwitchyardRaiders/Startup/";
        [MenuItem("Switchyard Raiders/Main Menu/Wire room and voice services")]
        public static void Apply()
        {
            Guard();
            var room = Object.FindAnyObjectByType<RoomChoiceMenu>(FindObjectsInactive.Include);
            if (!room || room.gameObject.scene.path != Root + "Scenes/SR_Launch.unity")
                throw new InvalidOperationException("Open SR_Launch in Switchyard Raiders first.");
            var runnerPrefab = MakeRunner();
            Wire(room, runnerPrefab);
            var project = OVRProjectConfig.CachedProjectConfig;
            Undo.RecordObject(project, "Enable spatial data support");
            project.sceneSupport = OVRProjectConfig.FeatureSupport.Supported; EditorUtility.SetDirty(project);
            EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
            if (!EditorSceneManager.SaveScene(room.gameObject.scene)) throw new IOException("Cannot save SR_Launch.");
            var path = Root + "Prefabs/SR_Launch.prefab";
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Wire(contents.GetComponentInChildren<RoomChoiceMenu>(true), runnerPrefab);
                PrefabUtility.SaveAsPrefabAsset(contents, path, out bool saved);
                if (!saved) throw new IOException("Cannot save launch prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            AssetDatabase.SaveAssets(); Validate();
        }
        private static void Wire(RoomChoiceMenu room, GameObject runnerPrefab)
        {
            var voice = Get<RaidersVoice>(room.gameObject);
            var understanding = Get<RaidersRoom>(room.gameObject);
            var mruk = room.launch.GetComponentInChildren<MRUK>(true);
            if (!mruk)
            {
                var go = new GameObject("Room understanding"); Undo.RegisterCreatedObjectUndo(go, "Add MRUK");
                go.transform.SetParent(room.launch.transform, false); mruk = Undo.AddComponent<MRUK>(go);
            }
            Undo.RecordObject(mruk, "Configure explicit room loading");
            mruk.SceneSettings = new MRUK.MRUKSettings { LoadSceneOnStartup = false, DataSource = MRUK.SceneDataSource.Device };
            Undo.RecordObject(understanding, "Wire MRUK"); understanding.mruk = mruk;
            Undo.RecordObject(room, "Wire room permission flow"); room.roomUnderstanding = understanding;
            var session = room.GetComponent<RaidersSession>(); Undo.RecordObject(session, "Wire session voice"); session.voice = voice;
            var table = room.GetComponent<RaidersTable>(); Undo.RecordObject(table, "Wire scanned room placement"); table.roomMenu = room; table.roomUnderstanding = understanding;
            Undo.RecordObject(voice, "Wire voice runner"); voice.runnerPrefab = runnerPrefab;
            var manager = room.launch.xrRigRoot.GetComponent<OVRManager>();
            var managerSettings = new SerializedObject(manager);
            managerSettings.FindProperty("requestScenePermissionOnStartup").boolValue = false;
            managerSettings.FindProperty("requestRecordAudioPermissionOnStartup").boolValue = false;
            managerSettings.ApplyModifiedProperties();
            foreach (var component in new Component[] { voice, understanding, room, session, table, mruk, manager })
            { EditorUtility.SetDirty(component); PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
        }
        private static GameObject MakeRunner()
        {
            var speaker = new GameObject("SR_VoiceSpeaker");
            GameObject speakerPrefab;
            try
            {
                var audio = speaker.AddComponent<AudioSource>();
                audio.playOnAwake = false; audio.spatialBlend = 0; audio.loop = false;
                speaker.AddComponent<Speaker>();
                speakerPrefab = PrefabUtility.SaveAsPrefabAsset(speaker, Root + "Prefabs/SR_VoiceSpeaker.prefab");
            }
            finally { Object.DestroyImmediate(speaker); }
            var runner = new GameObject("SR_NetworkRunner"); runner.SetActive(false);
            try
            {
                runner.AddComponent<NetworkRunner>();
                var voice = runner.AddComponent<FusionVoiceClient>();
                var rec = runner.AddComponent<Recorder>();
                rec.RecordingEnabled = false; rec.TransmitEnabled = false; rec.RecordWhenJoined = false;
                rec.MicrophoneType = Recorder.MicType.Unity;
                rec.UseMicrophoneTypeFallback = true; rec.StopRecordingWhenPaused = true;
                rec.VoiceDetection = true; rec.VoiceDetectionThreshold = .01f; rec.VoiceDetectionDelayMs = 500;
                rec.SamplingRate = POpusCodec.Enums.SamplingRate.Sampling48000;
                rec.FrameDuration = OpusCodec.FrameDuration.Frame20ms; rec.Bitrate = 30000; rec.DebugEchoMode = false;
                var dsp = runner.AddComponent<WebRtcAudioDsp>(); dsp.AEC = true; dsp.NoiseSuppression = true; dsp.HighPass = true;
                voice.PrimaryRecorder = rec; voice.SpeakerPrefab = speakerPrefab;
                voice.UseFusionAppSettings = true; voice.UseFusionAuthValues = false; voice.AutoConnectAndJoin = false;
                voice.ApplyDontDestroyOnLoad = false;
                var serialized = new SerializedObject(voice);
                serialized.FindProperty("usePrimaryRecorder").boolValue = true; serialized.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(runner, Root + "Prefabs/SR_NetworkRunner.prefab");
            }
            finally { Object.DestroyImmediate(runner); }
        }
        private static T Get<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? Undo.AddComponent<T>(go);
        private static void Guard()
        {
            if (!Application.dataPath.Replace('\\', '/').EndsWith("/Switchyard Raiders/Assets") || EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Switchyard Raiders in Edit Mode is required.");
        }
        [MenuItem("Switchyard Raiders/Main Menu/Validate room and voice services")]
        public static void Validate()
        {
            Guard();
            var room = Object.FindAnyObjectByType<RoomChoiceMenu>(FindObjectsInactive.Include);
            Check(room && room.roomUnderstanding && room.roomUnderstanding.mruk, "Room service and MRUK are assigned");
            Check(!room.roomUnderstanding.mruk.SceneSettings.LoadSceneOnStartup, "MRUK does not compete for permission dialogs");
            Check(Object.FindObjectsByType<MRUK>(FindObjectsInactive.Include).Length == 1, "Exactly one MRUK scene service");
            var session = room.GetComponent<RaidersSession>();
            Check(session && session.voice && session.voice.runnerPrefab, "Session owns a configured voice runner");
            var prefab = session.voice.runnerPrefab;
            var voice = prefab.GetComponent<FusionVoiceClient>();
            Check(!prefab.activeSelf && prefab.GetComponent<NetworkRunner>(), "Runner starts inactive for atomic setup");
            Check(voice && voice.UsePrimaryRecorder && voice.PrimaryRecorder && voice.SpeakerPrefab, "Primary recorder and remote speaker assigned");
            Check(voice.UseFusionAppSettings && !voice.UseFusionAuthValues && !voice.AutoConnectAndJoin, "Voice follows Fusion region with a separate, fresh Meta user proof");
            Check(!voice.PrimaryRecorder.RecordingEnabled && !voice.PrimaryRecorder.TransmitEnabled && !voice.PrimaryRecorder.RecordWhenJoined, "No microphone capture before runtime permission, including automatic room joins");
            Check(voice.SpeakerPrefab.GetComponent<Speaker>() && voice.SpeakerPrefab.GetComponent<AudioSource>(), "Remote speaker can play received audio");
            var settings = Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings;
            Check(Guid.TryParse(settings.AppIdFusion, out _) && Guid.TryParse(settings.AppIdVoice, out _), "Active Fusion settings contain valid Fusion and Voice app IDs");
            var table = room.GetComponent<RaidersTable>();
            Check(table.roomMenu == room && table.roomUnderstanding == room.roomUnderstanding, "Yard uses loaded room geometry for placement");
            var manager = new SerializedObject(room.launch.xrRigRoot.GetComponent<OVRManager>());
            Check(!manager.FindProperty("requestScenePermissionOnStartup").boolValue && !manager.FindProperty("requestRecordAudioPermissionOnStartup").boolValue, "OVRManager automatic permission dialogs disabled");
            var launchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/SR_Launch.prefab");
            var savedRoom = launchPrefab.GetComponentInChildren<RoomChoiceMenu>(true);
            Check(savedRoom && savedRoom.roomUnderstanding && savedRoom.GetComponent<RaidersSession>().voice, "Launch prefab persists service wiring");
            var report = "PASS: scene and prefab room/voice references, one MRUK, manual permission ownership, inactive runner, recorder permission gate, remote playback, active Fusion/Voice IDs and scanned-room yard placement.\nNo Play Mode, APK build, microphone capture or headset test was performed.\n";
            Directory.CreateDirectory("Deliverables/MainMenuFlow"); File.WriteAllText("Deliverables/MainMenuFlow/room-voice-validation.txt", report); Debug.Log(report);
        }
        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); Debug.Log("[SR Services] PASS: " + message); }
    }
}
